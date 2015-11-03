using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Bluetooth.Advertisement;
using BluetoothLEPair.Bluetooth;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BluetoothLEPair.ViewModel
{
    public class BluetoothLEAdvertismentViewModel
    {
        private readonly BluetoothLEAdvertisement ad;
        private readonly BluetoothLEAdvertisementReceivedEventArgs ble;

        public BluetoothLEAdvertismentViewModel(BluetoothLEAdvertisementReceivedEventArgs device)
        {
            ble = device;
            ad = ble.Advertisement;
            this.Address = ble.BluetoothAddress.ToString();
        }

        public string Description
        {
            get
            {
                string name = string.Empty;
                string manufacturer = String.Empty;

                StringBuilder sb = new StringBuilder();
                sb.Append($"{ble.AdvertisementType} ");
                sb.Append($"{ble.RawSignalStrengthInDBm}db ");

                foreach (var data in ad.DataSections)
                {

                    if (data.Data.Length > 0 && ((BluetoothDataType)data.DataType) == BluetoothDataType.CompleteLocalName)
                    {
                        name = Encoding.UTF8.GetString(data.Data.ToArray());
                    }
                    else
                    {
                        sb.Append($"{(BluetoothDataType)data.DataType}({data.Data.Length}) ");
                    }
                }

                foreach (var mfr in ad.ManufacturerData)
                {
                    manufacturer = BluetoothLookup.GetManufacturer(mfr.CompanyId);
                    manufacturer += " - ";
                }

                return $"{manufacturer}{name} ({ble.BluetoothAddress}). {sb}";
            }
        }

        public string Address { get; }

    }
}
