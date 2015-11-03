namespace BluetoothLEPair.Bluetooth
{
    /// <remarks>
    ///    https://www.bluetooth.org/en-us/specification/assigned-numbers/generic-access-profile
    /// </remarks>
    public enum BluetoothDataType
    {
        Flags = 0x01,
        CompleteListof16bitServiceClassUUIDs = 0x03,
        ShortLocalName = 0x08,
        CompleteLocalName = 0x09,
        TxPowerLevel = 0x0A,
        ClassofDevice = 0x0D,
        DeviceID = 0x10,
        Appearance = 0x19,
        AdvertisingInterval = 0x1A,
        LEBluetoothAddress = 0x1B,
        LERole = 0x1C,
        URI = 0x24
    }
}