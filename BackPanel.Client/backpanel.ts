declare var $: any;
declare var picodom: any;

module BackPanel
{
	const root = document.getElementById("content");
	var ws : WebSocket;

	function initiateReset()
	{
		ws = new WebSocket("{{websocket_url}}");
		ws.onopen = onOpen;
		ws.onmessage = onMessage;
		ws.onclose = onClose;
		return ws;
	}
	
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

	// State

	var currentElement: HTMLElement;
	var currentDOM: any;

	// Responses

	interface Update
	{
		Case: "Update";
		// version, HTML
		Fields: [number, string];
	}

	function onOpen(event: Event)
	{
		$('#bp-connection-lost').modal("hide");

		currentElement = null;
		currentDOM = null;
		root.innerHTML = "";
		sendObject({ Case: "Reset" } as Reset);
	}

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

	function onClose(close: CloseEvent)
	{
		$('#bp-connection-lost').modal("show");

		setTimeout(() => initiateReset(), 1000);
	}

	initiateReset();
}