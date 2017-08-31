declare var $: any;
declare var picodom: any;

module BackPanel
{
	let ws = new WebSocket("{{websocket_url}}");
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

	var currentElement: HTMLElement;
	var currentDOM: any;

	function onMessage(event: MessageEvent)
	{
		const message: Update = JSON.parse(event.data);
		switch (message.Case)
		{
			case "Update":
				var newDOM = JSON.parse(message.Fields[1]);
				currentElement = picodom.patch(currentDOM, newDOM, currentElement, root);
				if (!currentDOM)
				{
					$('[remove-on-init="remove"]').remove();
					$('[data-toggle="checkbox"]').radiocheck();
				}
				currentDOM = newDOM;
				break;
		}
	}

	export function sendEvent(payload: any)
	{
		sendObject({ Case: "Event", Fields: [JSON.stringify(payload)] } as Event_);
	}

	function sendObject(obj: any)
	{
		ws.send(JSON.stringify(obj));
	}
}