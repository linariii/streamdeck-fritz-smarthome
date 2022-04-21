document.addEventListener('websocketCreate', function () {
    console.log("Websocket created!");
    showHideSettings(actionInfo.payload.settings);

    websocket.addEventListener('message', function (event) {
        console.log("Got message event!");

        // Received message from Stream Deck
        var jsonObj = JSON.parse(event.data);

        if (jsonObj.event === 'sendToPropertyInspector') {
            var payload = jsonObj.payload;
            showHideSettings(payload);
        }
        else if (/*jsonObj.event === 'didReceiveSettings' || */jsonObj.event === 'didReceiveGlobalSettings') {
            var payload = jsonObj.payload;
            showHideSettings(payload.settings);
        }
    });
});

function showHideSettings(payload) {
    console.log("Show Hide Settings Called");
    setAllSettings("none");
    if (payload['baseUrl'] && payload['baseUrl'].length > 0 && payload['userName'] && payload['userName'].length > 0 && payload['password'] && payload['password'].length > 0) {
        setAllSettings("");
    }
}

function setAllSettings(displayValue) {
    var dvAllSettings = document.getElementById('dvAllSettings');
    dvAllSettings.style.display = displayValue;
}