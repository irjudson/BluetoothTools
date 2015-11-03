using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using BluetoothLEPair.Input;

namespace BluetoothLEPair.ViewModel
{
    class BluetoothAdvertismentViewerViewModel
    {
        private readonly CoreDispatcher _dispatcher;
        private readonly ObservableCollection<BluetoothLEAdvertismentViewModel> _devices;
        private readonly ConcurrentDictionary<string, BluetoothLEAdvertismentViewModel> _table;
        private readonly BluetoothLEAdvertisementWatcher _watcher;

        public BluetoothAdvertismentViewerViewModel()
        {
            _table = new ConcurrentDictionary<string, BluetoothLEAdvertismentViewModel>();
            _devices = new ObservableCollection<BluetoothLEAdvertismentViewModel>();
            Advertisements = new CollectionViewSource();
            Advertisements.Source = _devices;
            _dispatcher = Advertisements.Dispatcher;

            _watcher = new BluetoothLEAdvertisementWatcher();

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            //var writer = new DataWriter();
            //writer.WriteUInt16(0x1234);
            //manufacturerData.Data = writer.DetachBuffer();
            //watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);
            _watcher.SignalStrengthFilter.InRangeThresholdInDBm = -70;
            _watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -75;
            _watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);
            _watcher.Received += OnAdvertisementReceived;
            _watcher.Stopped += OnAdvertisementWatcherStopped;

            RunCommand = new SimpleCommand(StartWatcher);
            StopCommand = new SimpleCommand(StopWatcher, false);
        }

        public CollectionViewSource Advertisements { get; set; }
        public SimpleCommand RunCommand { get; set; }
        public SimpleCommand StopCommand { get; set; }

        /// <summary>
        /// Invoked as an event handler when the Run button is pressed.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private void StartWatcher()
        {
            // Calling watcher start will start the scanning if not already initiated by another client
            _watcher.Start();
            RunCommand.IsEnabled = false;
            StopCommand.IsEnabled = true;
        }

        /// <summary>
        /// Invoked as an event handler when the Stop button is pressed.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private void StopWatcher()
        {
            // Stopping the watcher will stop scanning if this is the only client requesting scan
            _watcher.Stop();
            RunCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
        }


        /// <summary>
        /// Invoked as an event handler when an advertisement is received.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about the advertisement event.</param>
        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            // We can obtain various information about the advertisement we just received by accessing 
            // the properties of the EventArgs class

            var newDevice = new BluetoothLEAdvertismentViewModel(eventArgs);
            BluetoothLEAdvertismentViewModel oldDevice = null;

            _table.TryGetValue(newDevice.Address, out oldDevice);
            _table[newDevice.Address] = newDevice;

            // Notify the user that the watcher was stopped
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (oldDevice != null)
                {
                    _devices.Remove(oldDevice);
                }
                _devices.Add(newDevice);
            });
        }

        /// <summary>
        /// Invoked as an event handler when the watcher is stopped or aborted.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about why the watcher stopped or aborted.</param>
        private void OnAdvertisementWatcherStopped(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs)
        {
            // TODO: Notify the user that the watcher was stopped
        }
    }
}