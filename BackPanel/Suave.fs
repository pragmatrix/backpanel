/// Copied some Suave code to avoid a memory leak in ConcurrentBag, which happens when the elements are not
/// being removed.
module BackPanel.Suave

open System
open System.Collections.Concurrent
open System.Threading
open System.Net
open System.Net.Sockets
open Suave
open Suave.Logging
open Suave.Logging.Message
open Suave.Sockets
open Suave.Utils
open Suave.Tcp

let private logger = Log.create "Suave.Tcp"

let private aFewTimes f =
  let s ms = System.Threading.Thread.Sleep (ms : int)
  let rec run = function
    | 0us | 1us -> f ()
    | n -> try f () with e -> s 10; run (n - 1us)
  run 3us

type DisposableConcurrentPool<'T>(objectGenerator: DisposableConcurrentPool<'T> -> 'T ) as self =

  let objects = ConcurrentBag<'T> ()

  member x.Pop() =
   match objects.TryTake()with
   | true, item ->
     item
   | _,_ -> objectGenerator self

  member x.Push(item) =
      objects.Add(item)

  interface IDisposable with
    member this.Dispose() =
      match objects.TryTake() with
      | true, _ -> (this :> IDisposable).Dispose()
      | _ -> ()

type TcpTransport(acceptArgs     : SocketAsyncEventArgs,
                  readArgs     : SocketAsyncEventArgs,
                  writeArgs     : SocketAsyncEventArgs,
                  transportPool : DisposableConcurrentPool<TcpTransport>,
                  listenSocket : Socket) =

  let shutdownSocket _ =
    try
      if acceptArgs.AcceptSocket <> null then
        try
          acceptArgs.AcceptSocket.Shutdown(SocketShutdown.Both)
        with _ ->
          ()

        acceptArgs.AcceptSocket.Dispose ()
    with _ -> ()

  let remoteBinding (socket : Socket) =
    let rep = socket.RemoteEndPoint :?> IPEndPoint
    { ip = rep.Address; port = uint16 rep.Port }

  member this.accept() =
      asyncDo listenSocket.AcceptAsync ignore (fun a -> remoteBinding a.AcceptSocket) acceptArgs

  interface ITransport with
    member this.read (buf : ByteSegment) =
      async{
       if acceptArgs.AcceptSocket = null then
         return Choice2Of2 (ConnectionError "read error: acceptArgs.AcceptSocket = null") 
       else
         return! asyncDo acceptArgs.AcceptSocket.ReceiveAsync (setBuffer buf) (fun a -> a.BytesTransferred) readArgs
       }

    member this.write (buf : ByteSegment) =
      async{
        if acceptArgs.AcceptSocket = null then
         return Choice2Of2 (ConnectionError "write error: acceptArgs.AcceptSocket = null") 
       else
         return! asyncDo acceptArgs.AcceptSocket.SendAsync (setBuffer buf) ignore writeArgs
      }

    member this.shutdown() = async {
      shutdownSocket ()
      acceptArgs.AcceptSocket <- null
      transportPool.Push(this)
      return ()
      }


let createTransport transportPool listenSocket =
  let readEventArg = new SocketAsyncEventArgs()
  let userToken = new AsyncUserToken()
  readEventArg.UserToken <- userToken
  readEventArg.add_Completed(fun a b -> userToken.Continuation b)

  let writeEventArg = new SocketAsyncEventArgs()
  let userToken = new AsyncUserToken()
  writeEventArg.UserToken <- userToken
  writeEventArg.add_Completed(fun a b -> userToken.Continuation b)

  let acceptArg = new SocketAsyncEventArgs()
  let userToken = new AsyncUserToken()
  acceptArg.UserToken <- userToken
  acceptArg.add_Completed(fun a b -> userToken.Continuation b)

  new TcpTransport(acceptArg,readEventArg,writeEventArg, transportPool,listenSocket)


let createPools listenSocket logger maxOps bufferSize autoGrow =

  let transportPool = new DisposableConcurrentPool<TcpTransport>(fun pool -> createTransport pool listenSocket)

  let bufferManager = new BufferManager(bufferSize * (maxOps + 1), bufferSize, autoGrow)
  bufferManager.Init()

  //Pre-allocate a set of reusable transportObjects
  for x = 0 to maxOps - 1 do
    let transport = createTransport transportPool listenSocket
    transportPool.Push transport

  (transportPool, bufferManager)

let runServer maxConcurrentOps bufferSize autoGrow (binding: SocketBinding) startData
              (acceptingConnections: AsyncResultCell<StartedData>) serveClient = async {
  try
    use listenSocket = new Socket(binding.endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
    listenSocket.NoDelay <- true

    let transportPool, bufferManager =
      createPools listenSocket logger maxConcurrentOps bufferSize autoGrow
    
    use _ = transportPool

    aFewTimes (fun () -> listenSocket.Bind binding.endpoint)
    listenSocket.Listen MaxBacklog

    use! disposable = Async.OnCancel(fun () ->
      stopTcp "runServer async cancelled" listenSocket)

    let startData =
      { startData with socketBoundUtc = Some (DateTimeOffset.UtcNow) }

    acceptingConnections.complete startData |> ignore
    
    logger.info (
      eventX "Smooth! Suave listener started in {startedListeningMilliseconds:#.###} with binding {ipAddress}:{port}"
      >> setFieldValue "startedListeningMilliseconds" (startData.GetStartedListeningElapsedMilliseconds())
      // .Address can throw exceptions, just log its string representation
      >> setFieldValue "ipAddress" (startData.binding.ip.ToString())
      >> setFieldValue "port" startData.binding.port
      >> setSingleName "Suave.Tcp.runServer")

    let! token = Async.CancellationToken

    while not (token.IsCancellationRequested) do
      try
        let transport = transportPool.Pop()
        let! r = transport.accept()
        match r with
        | Choice1Of2 remoteBinding ->
          // start a new async worker for each accepted TCP client
          Async.Start (job serveClient remoteBinding transport bufferManager, token)
        | Choice2Of2 e ->
          failwithf "Socket failed to accept client, error: %A" e

      with ex ->
        logger.error (eventX "Socket failed to accept a client" >> addExn ex)

  with ex ->
    logger.fatal (eventX "TCP server failed" >> addExn ex)
    return raise ex
}


type BackPanelTcpServerFactory() =
  interface TcpServerFactory with
    member this.create (maxOps, bufferSize, autoGrow, binding) =
        runServer maxOps bufferSize autoGrow binding