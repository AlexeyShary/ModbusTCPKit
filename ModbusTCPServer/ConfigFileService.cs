using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;

namespace ModbusTCPServer
{
    public static class ConfigFileService
    {
        const string DEVICE_LIST_FILENAME = "DeviceList.data";

        public static void LoadDeviceList()
        {
            DataContractSerializer dataFormatter = new DataContractSerializer(typeof(List<Device>));

            using (FileStream fs = new FileStream(DEVICE_LIST_FILENAME, FileMode.OpenOrCreate))
            {
                try
                {
                    dataFormatter.ReadObject(fs);
                }
                catch { }
            }

            if (Device.DeviceList.Count == 0)
            {
                Device newDevice = new Device();
                newDevice.DeviceName = "Default device";
            }
        }

        public static void SaveDeviceList()
        {
            DataContractSerializer dataFormatter = new DataContractSerializer(typeof(List<Device>));

            using (FileStream fileStream = new FileStream(DEVICE_LIST_FILENAME, FileMode.Create))
            {
                dataFormatter.WriteObject(fileStream, Device.DeviceList);
            }
        }
    }
}