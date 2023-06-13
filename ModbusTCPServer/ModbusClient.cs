using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net;
using ModbusTotalKit;

namespace ModbusTCPServer
{
    public class ModbusClient : INotifyPropertyChanged
    {
        public ModbusClient(TcpClient tcpClient, ModbusServer modbusServer)
        {
            ClientID = (currentID++).ToString();
            client = tcpClient;
            server = modbusServer;

            serverLogger = modbusServer.ServerLogger;

            clientHandleThread = new Thread(new ThreadStart(ClientHandle));
            clientHandleThread.IsBackground = true;

            modbusServer.AddClientToList(this);
        }

        static int currentID;

        string clientID;
        public string ClientID
        {
            get { return clientID; }
            private set { clientID = value; }
        }

        NetworkStream clientStream;
        public NetworkStream ClientStream
        {
            get { return clientStream; }
            private set { clientStream = value; }
        }

        Logger serverLogger;

        public TcpClient client;

        ModbusServer server;

        Thread clientHandleThread;
        bool stopClientHandle;

        public void ClientHandle()
        {
            serverLogger.AddMessage("Client handle thread started for " + ClientID);

            ClientStream = client.GetStream();
            byte[] incomingBytes;

            while (true)
            {
                if (stopClientHandle)
                    break;

                incomingBytes = GetMessage();

                if (incomingBytes == null)
                {
                    serverLogger.AddMessage("Lost connection with " + ClientID);
                    CloseConnection();
                    break;
                }

                serverLogger.AddMessage(String.Format("[{0} - {1}] Rx: {2} [ {3} bytes ]", 
                    ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(),
                    ClientID,
                    incomingBytes.ToHexString(),
                    incomingBytes.Count().ToString()
                    ));

                server.ResponceOnMessage(this, incomingBytes);
            }

            serverLogger.AddMessage("Client handle thread finished for " + ClientID);
        }

        string ByteArrayToString(byte[] byteArray)
        {
            return BitConverter.ToString(byteArray).Replace("-", " ");
        }

        // Получение запросов от клиента
        private byte[] GetMessage()
        {
            try
            {
                byte[] data = new byte[1024];
                int bytes = 0;

                do
                {
                    bytes = ClientStream.Read(data, 0, data.Length);
                }
                while (ClientStream.DataAvailable);

                if (bytes > 0)
                {
                    byte[] result = new byte[bytes];
                    Array.Copy(data, result, bytes);
                    return result;
                }
            }
            catch { }

            return null;
        }

        // Разрыв подключения
        public void CloseConnection()
        {
            if (ClientStream != null)
                ClientStream.Close();
            if (client != null)
                client.Close();

            server.RemoveClientFromList(this);
        }

        public void StartHandleClient()
        {
            serverLogger.AddMessage("Start handle client command for " + ClientID);

            stopClientHandle = false;
            clientHandleThread.Start();
        }

        public void StopClientHandle()
        {
            serverLogger.AddMessage("Stop handle client command for " + ClientID);

            stopClientHandle = true;
        }

        public string ConnectedClientString
        {
            get { return ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " [ID " + ClientID + "]"; }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
