function reloadDevices() {
    var payload = {};
    payload.property_inspector = 'reloadDevices';
    sendPayloadToPlugin(payload);
}