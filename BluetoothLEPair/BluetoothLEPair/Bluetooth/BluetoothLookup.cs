using System.Collections.Generic;

namespace BluetoothLEPair.Bluetooth
{
    public class BluetoothLookup
    {
        private readonly static IDictionary<int, string> manufacturers;
        private readonly static IDictionary<int, string> datatypes;

        static BluetoothLookup()
        {
            // https://www.bluetooth.org/en-us/specification/assigned-numbers/company-identifiers
            manufacturers = new Dictionary<int, string>()
            {
                {6, "Microsoft" },
                {76, "Apple" },
                {222, "Muzik LLC" }
            };
        }

        public static string GetManufacturer(int id)
        {
            string result = null;
            if (!manufacturers.TryGetValue(id, out result))
            {
                result = id.ToString();
            }
            return result;
        }
    }
}
