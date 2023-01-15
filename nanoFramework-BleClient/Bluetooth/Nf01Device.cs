using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
using System;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework_BleClient.Bluetooth
{
    internal class Nf01Device
    {
        private Guid _serviceUuid = new Guid("A7EEDF2C-DA87-4CB5-A9C5-5151C78B0057");
        private Guid _characteristicUuid = new Guid("A7EEDF2C-DA89-4CB5-A9C5-5151C78B0057");
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private readonly ulong _bleAddr;
        private readonly BluetoothAddressType _bluetoothAddressType;

        public Nf01Device(ulong bleAddr, BluetoothAddressType bluetoothAddressType)
        {
            _bleAddr = bleAddr;
            _bluetoothAddressType = bluetoothAddressType;
        }

        public void StartConnect()
        {
            try
            {
                using BluetoothLEDevice ble = BluetoothLEDevice.FromBluetoothAddress(_bleAddr, _bluetoothAddressType);
                ble.ConnectionStatusChanged += Ble_ConnectionStatusChanged;
                ble.GattServicesChanged += Ble_GattServicesChanged;

                if (ble.ConnectionStatus != BluetoothConnectionStatus.Connected)
                {
                    GattDeviceServicesResult sr = ble.GetGattServicesForUuid(_serviceUuid);
                    Debug.WriteLine("Init Gatt Services");

                    if (sr.Status == GattCommunicationStatus.Success)
                    {
                        if (sr.Services.Length == 0)
                        {
                            Debug.WriteLine("No Services found");
                            goto end;
                        }
                       
                        GattCharacteristicsResult cr = sr.Services[0].GetCharacteristicsForUuid(_characteristicUuid);

                        if (cr.Characteristics.Length == 0)
                        {
                            Debug.WriteLine("No Characteristics found");
                            goto end;
                        }

                        if (cr.Status == GattCommunicationStatus.Success)
                        {
                            Debug.WriteLine("Characteristic connected");

                            var cr0 = cr.Characteristics[0];
                            
                            cr0.ValueChanged += Characteristic_ValueChanged;
                            cr0.WriteClientCharacteristicConfigurationDescriptorWithResult(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                            if (cr0.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                            {
                                Debug.WriteLine("First Characteristic Write WWOR");
                                cr0.WriteValueWithResult(new Buffer(new byte[] { 0x21, 0x25, 0x00, 0x00, 0x00, 0x00, 0xA3, 0x19 }), GattWriteOption.WriteWithoutResponse);

                                Debug.WriteLine("Second Characteristic Write WWOR");
                                // Power off the device
                                cr0.WriteValueWithResult(new Buffer(new byte[] { 0x21, 0x50, 0x00, 0x00, 0x00, 0x00, 0xA3, 0x19 }), GattWriteOption.WriteWithoutResponse);
                            }

                            if (cr0.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                            {
                                Debug.WriteLine("First Characteristic Write");
                                cr0.WriteValueWithResult(new Buffer(new byte[] { 0x21, 0x25, 0x00, 0x00, 0x00, 0x00, 0xA3, 0x19 }), GattWriteOption.WriteWithResponse);

                                Debug.WriteLine("Second Characteristic Write");
                                // Power off the device
                                cr0.WriteValueWithResult(new Buffer(new byte[] { 0x21, 0x50, 0x00, 0x00, 0x00, 0x00, 0xA3, 0x19 }), GattWriteOption.WriteWithResponse);
                            }

                            _autoResetEvent.WaitOne(10_000, true);
                            cr0.ValueChanged-= Characteristic_ValueChanged;

                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Device not Connected");
                }

            end:
                Debug.WriteLine("End of Nf01Device");

            }
            catch (Exception ex)
            {
                Debug.WriteLine("BLE Connect Error: " + ex.Message);
            }
        }


        private void Ble_GattServicesChanged(object sender, EventArgs e)
        {
            BluetoothLEDevice dev = (BluetoothLEDevice)sender;
            Debug.WriteLine("Service Changed");

        }

        private void Ble_ConnectionStatusChanged(object sender, EventArgs e)
        {
            BluetoothLEDevice dev = (BluetoothLEDevice)sender;
            if (dev.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                _autoResetEvent.Set();
                Debug.WriteLine($"Device {dev.BluetoothAddress:X} disconnected");
                Debug.WriteLine("AutoEvent Set");
            }
            else
            {
                Debug.WriteLine($"Device {dev.BluetoothAddress:X} Connected");
            }

        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs valueChangedEventArgs)
        {
            DataReader rdr = DataReader.FromBuffer(valueChangedEventArgs.CharacteristicValue);
            byte[] readbyte = new byte[rdr.UnconsumedBufferLength];

            rdr.ReadBytes(readbyte);

            if (readbyte.Length > 1)
            {
                switch (readbyte[1])
                {
                    case 0x25:
                        Debug.WriteLine("Data: " + BitConverter.ToString(readbyte));
                        break;
                    case 0x50:
                        Debug.WriteLine("Power off: " + BitConverter.ToString(readbyte));
                        _autoResetEvent.Set();
                        break;
                }
            }

        }
    }
}
