using nanoFramework.Hardware.Esp32;
using nanoFramework_BleClient.Bluetooth;
using System;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework_BleClient
{
    public class Program
    {
        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");
            PrintMemory("Start");
            Thread.Sleep(2_000);

            var ble = BleClient.Create();
            ble.StartScan();

            PrintMemory("BLE Started");

            Thread.Sleep(Timeout.Infinite);

           
        }


        public static void PrintMemory(string msg)
        {
            NativeMemory.GetMemoryInfo(NativeMemory.MemoryType.Internal, out uint totalSize, out uint totalFree, out uint largestFree);
            Debug.WriteLine($"{msg} -> Internal Mem:  Total Internal: {totalSize} Free: {totalFree} Largest: {largestFree}");
            Debug.WriteLine($"nF Mem:  {nanoFramework.Runtime.Native.GC.Run(false)}");
        }
    }
}
