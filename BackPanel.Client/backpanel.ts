module BackPanel
{
	let ws = new WebSocket("{{model.websocket_url}}");
	let root = document.getElementById("content");
	ws.onopen = onOpen;
	ws.onmessage = onMessage;

	// Requests

	interface Reset
	{
		Case: "Reset";
	}

	interface Command
	{
		Case: "Command";
		Fields: [string];
	}

	// Responses

	interface Update
	{
		Case: "Update";
		// version, HTML
		Fields: [number, string];
	}

	function onOpen(event: Event)
	{
		sendObject({ Case: "Reset" } as Reset);
	}

	function onMessage(event: MessageEvent)
	{
		const message: Update = event.data;
		switch (message.Case)
		{
			case "Update":
				root.innerHTML = message.Fields[1];
				break;
		}
	}

	export function sendCommand(command: string)
	{
		sendObject({ Case: "Command", Fields: [command] } as Command);
	}

	function sendObject(obj: any)
	{
		ws.send(JSON.stringify(obj));
	}
}