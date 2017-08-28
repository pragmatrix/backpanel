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

	interface Command
	{
		kind: "command";
		command: string;
	}

	interface UpdateMessage
	{
		kind: "update";
		version: number;
		html: string;
	}

	function onOpen(event: Event)
	{
		ws.send({ kind: "reset" } as Reset);
	}

	function onMessage(event: MessageEvent)
	{
		const message: UpdateMessage = event.data;
		switch (message.kind)
		{
			case "update":
				root.innerHTML = message.html;
				break;
		}
	}

	export function sendCommand(command: string)
	{
		ws.send({ kind: "command", command: command } as Command);
	}
}