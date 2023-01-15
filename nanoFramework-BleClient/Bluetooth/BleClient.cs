
using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.Advertisement;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework_BleClient.Bluetooth
{
    public class BleDevice
    {
        public ulong BluetoothAddress { get; set; }
        public BluetoothAddressType BluetoothAddressType { get; set; }
        
    }

    internal class BleClient
    {
        private BluetoothLEAdvertisementWatcher _watcher;
        private Thread _thread;
        private AutoResetEvent _autoResetEvent;
        private BleDevice _bleDevice;

        public static BleClient Create()
        {
            return new BleClient();
        }

        public void StartScan()
        {
            _watcher = new();
            // Debug.WriteLine("Start BLE Watcher After New");
            _watcher.ScanningMode = BluetoothLEScanningMode.Active;
            _watcher.Received += Watcher_Received;

            _thread = new Thread(BleThread);
            _thread.Start();
        }

        public void StopScan()
        {
            if (_watcher != null && _watcher.Status != BluetoothLEAdvertisementWatcherStatus.Stopped)
            {
                _watcher.Stop();
            }
        }

        private void BleThread()
        {
            _autoResetEvent = new AutoResetEvent(false);
            while (true)
            {
                if (_watcher.Status != BluetoothLEAdvertisementWatcherStatus.Started)
                {
                    _watcher.Start();
                    Debug.WriteLine("Start BLE Scan");

                    _autoResetEvent.WaitOne();
                    Debug.WriteLine("After AutoResetEvent WaitOne");

                    if (_watcher.Status != BluetoothLEAdvertisementWatcherStatus.Stopped)
                    {
                        Thread.Sleep(600);
                        Debug.WriteLine("Stop Watcher");
                        _watcher.Stop();
                        
                    }

                    if (_bleDevice != null)
                    {

                        var dev = new Nf01Device(_bleDevice.BluetoothAddress, _bleDevice.BluetoothAddressType);
                        dev.StartConnect();

                        _bleDevice = null;
                    }


                }
                else
                {
                    Debug.WriteLine($"BLE Status {_watcher.Status}");
                }

                Debug.WriteLine("Before Sleep");
                Thread.Sleep(200);
            }
        }


        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            string localName = args.Advertisement.LocalName;
            Debug.WriteLine($"Name: {localName} Mac: {args.BluetoothAddress:X}");
            if (localName.Contains("nF-01"))
            {
                _bleDevice = new BleDevice
                {
                    BluetoothAddress = args.BluetoothAddress,
                    BluetoothAddressType = args.BluetoothAddressType,
                };

                //sender.Stop();
                //Thread.Sleep(200);
                _autoResetEvent.Set();

            }
        }
    }
}
