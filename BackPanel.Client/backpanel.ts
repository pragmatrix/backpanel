module BackPanel
{
	let ws = new WebSocket("{{model.websocket_url}}");
	let root = document.getElementById("content");
	ws.onopen = onOpen;
	ws.onmessage = onMessage;

	interface Reset
	{
		kind: "reset";
	}

	interface Response
	{
		kind: "update";
		sequence: number;
	}

	function onOpen(event: Event)
	{
		ws.send(<Reset>{ kind: "reset" });
	}

	function onMessage(event: MessageEvent)
	{
		root.innerHTML = event.data;
	}
}