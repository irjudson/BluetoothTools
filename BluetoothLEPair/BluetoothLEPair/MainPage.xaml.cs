using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BluetoothLEPair
{
    public enum NotifyType
    {
        StatusMessage,
        ErrorMessage
    };

    public class AdvertisedBLEDevice
    {
        public AdvertisedBLEDevice(BluetoothLEAdvertisementReceivedEventArgs inAd) {
            ad = inAd;
        }
        public BluetoothLEAdvertisementReceivedEventArgs ad { get; set; }
        public string manufacturer {
            get {
                // Check if there are any manufacturer-specific sections.
                // If there is, print the raw data of the first manufacturer section (if there are multiple).
                string manufacturerDataString = "";
                var manufacturerSections = ad.Advertisement.ManufacturerData;
                if (manufacturerSections.Count > 0) {
                    // Only print the first one of the list
                    var manufacturerData = manufacturerSections[0];
                    var data = new byte[manufacturerData.Data.Length];
                    using (var reader = DataReader.FromBuffer(manufacturerData.Data)) {
                        reader.ReadBytes(data);
                    }
                    // Print the company ID + the raw data in hex format
                    manufacturerDataString = string.Format("0x{0}: {1}",
                        manufacturerData.CompanyId.ToString("X"),
                        BitConverter.ToString(data));
                }
                return manufacturerDataString;
            }
        }
        public string address {
            get {
                return Guid.NewGuid().ToString();
            }
        }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BluetoothLEAdvertisementWatcher watcher;
        Dictionary<System.UInt64, AdvertisedBLEDevice> lastAdvertisements;

        public ObservableCollection<AdvertisedBLEDevice> advertisements;

        public MainPage()
        {
            InitializeComponent();

            watcher = new BluetoothLEAdvertisementWatcher();
            lastAdvertisements = new Dictionary<System.UInt64, AdvertisedBLEDevice>();

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            //var writer = new DataWriter();
            //writer.WriteUInt16(0x1234);
            //manufacturerData.Data = writer.DetachBuffer();
            //watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);
            watcher.SignalStrengthFilter.InRangeThresholdInDBm = -70;
            watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -75;
            watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        ///
        /// We will enable/disable parts of the UI if the device doesn't support it.
        /// </summary>
        /// <param name="eventArgs">Event data that describes how this page was reached. The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            // Attach a handler to process the received advertisement. 
            // The watcher cannot be started without a Received handler attached
            watcher.Received += OnAdvertisementReceived;

            // Attach a handler to process watcher stopping due to various conditions,
            // such as the Bluetooth radio turning off or the Stop method was called
            watcher.Stopped += OnAdvertisementWatcherStopped;

            // Attach handlers for suspension to stop the watcher when the App is suspended.
            Application.Current.Suspending += App_Suspending;
            Application.Current.Resuming += App_Resuming;

           NotifyUser("Press Run to start watcher.", NotifyType.StatusMessage);
        }

        /// <summary>
        /// Invoked immediately before the Page is unloaded and is no longer the current source of a parent Frame.
        /// </summary>
        /// <param name="e">
        /// Event data that can be examined by overriding code. The event data is representative
        /// of the navigation that will unload the current Page unless canceled. The
        /// navigation can potentially be canceled by setting Cancel.
        /// </param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            // Remove local suspension handlers from the App since this page is no longer active.
            Application.Current.Suspending -= App_Suspending;
            Application.Current.Resuming -= App_Resuming;

            // Make sure to stop the watcher when leaving the context. Even if the watcher is not stopped,
            // scanning will be stopped automatically if the watcher is destroyed.
            watcher.Stop();

            // Always unregister the handlers to release the resources to prevent leaks.
            watcher.Received -= OnAdvertisementReceived;
            watcher.Stopped -= OnAdvertisementWatcherStopped;

            NotifyUser("Navigating away. Watcher stopped.", NotifyType.StatusMessage);
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e) {
            // Make sure to stop the watcher on suspend.
            watcher.Stop();
            // Always unregister the handlers to release the resources to prevent leaks.
            watcher.Received -= OnAdvertisementReceived;
            watcher.Stopped -= OnAdvertisementWatcherStopped;

            NotifyUser("App suspending. Watcher stopped.", NotifyType.StatusMessage);
        }

        /// <summary>
        /// Invoked when application execution is being resumed.
        /// </summary>
        /// <param name="sender">The source of the resume request.</param>
        /// <param name="e"></param>
        private void App_Resuming(object sender, object e) {
            watcher.Received += OnAdvertisementReceived;
            watcher.Stopped += OnAdvertisementWatcherStopped;
        }

        /// <summary>
        /// Invoked as an event handler when the Run button is pressed.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private void RunButton_Click(object sender, RoutedEventArgs e) {
            // Calling watcher start will start the scanning if not already initiated by another client
            watcher.Start();
            RunButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            NotifyUser("Watcher started.", NotifyType.StatusMessage);
        }

        /// <summary>
        /// Invoked as an event handler when the Stop button is pressed.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private void StopButton_Click(object sender, RoutedEventArgs e) {
            // Stopping the watcher will stop scanning if this is the only client requesting scan
            watcher.Stop();
            StopButton.IsEnabled = false;
            RunButton.IsEnabled = true;
            NotifyUser("Watcher stopped.", NotifyType.StatusMessage);
        }


        /// <summary>
        /// Invoked as an event handler when an advertisement is received.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about the advertisement event.</param>
        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs) {
            // We can obtain various information about the advertisement we just received by accessing 
            // the properties of the EventArgs class

            AdvertisedBLEDevice newDevice = new AdvertisedBLEDevice(eventArgs);
            if (lastAdvertisements.ContainsKey(newDevice.ad.BluetoothAddress)) {
                lastAdvertisements.Remove(newDevice.ad.BluetoothAddress);
            }
            lastAdvertisements[newDevice.ad.BluetoothAddress] = newDevice;

            // Notify the user that the watcher was stopped
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Store the last advertisement for each address
                ReceivedAdvertisementListBox.Items.Add(newDevice.address);
            });
        }

        /// <summary>
        /// Invoked as an event handler when the watcher is stopped or aborted.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about why the watcher stopped or aborted.</param>
        private async void OnAdvertisementWatcherStopped(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs) {
            // Notify the user that the watcher was stopped
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                NotifyUser(string.Format("Watcher stopped or aborted: {0}", eventArgs.Error.ToString()), NotifyType.StatusMessage);
            });
        }

        /// <summary>
        /// Used to display messages to the user
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type) {
            switch (type) {
                case NotifyType.StatusMessage:
                    ErrorBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    ErrorBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            ErrorBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty) {
                ErrorBorder.Visibility = Visibility.Visible;
                StatusBlock.Visibility = Visibility.Visible;
            } else {
                ErrorBorder.Visibility = Visibility.Collapsed;
                StatusBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void PairButton_Click(object sender, RoutedEventArgs e) {

        }

        private void ReceivedAdvertisementListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }
    }
}
