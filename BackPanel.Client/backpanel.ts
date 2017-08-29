declare var $: any;

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

	interface Event_
	{
		Case: "Event";
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
		const message: Update = JSON.parse(event.data);
		switch (message.Case)
		{
			case "Update":
				root.innerHTML = message.Fields[1];
				$('[data-toggle="checkbox"]').radiocheck();
				break;
		}
	}

	export function sendEventBase64(event: string)
	{
		sendEvent(atob(event));
	}

	function sendEvent(event: string)
	{
		sendObject({ Case: "Event", Fields: [event] } as Event_);
	}

	function sendObject(obj: any)
	{
		ws.send(JSON.stringify(obj));
	}
}