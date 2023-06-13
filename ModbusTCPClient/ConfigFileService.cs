using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;

namespace ModbusTCPClient
{
    public static class ConfigFileService
    {
        const string SERVER_LIST_FILENAME = "ServerList.data";

        public static void LoadServerList()
        {
            DataContractSerializer dataFormatter = new DataContractSerializer(typeof(List<ModbusServer>));

            using (FileStream fs = new FileStream(SERVER_LIST_FILENAME, FileMode.OpenOrCreate))
            {
                try
                {
                    dataFormatter.ReadObject(fs);
                }
                catch { }
            }
        }

        public static void SaveServerList()
        {
            DataContractSerializer dataFormatter = new DataContractSerializer(typeof(List<ModbusServer>));

            using (FileStream fileStream = new FileStream(SERVER_LIST_FILENAME, FileMode.Create))
            {
                dataFormatter.WriteObject(fileStream, ModbusServer.ServerList);
            }
        }
    }
}
