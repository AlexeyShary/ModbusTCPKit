using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Input;
using ModbusTotalKit;

namespace ModbusTCPClient
{
    [DataContract(IsReference = true)]
    public class ModbusServer : INotifyPropertyChanged
    {
        static ObservableCollection<ModbusServer> serverList = new ObservableCollection<ModbusServer>();
        public static ObservableCollection<ModbusServer> ServerList
        {
            get { return serverList; }
        }

        public ModbusServer()
        {
            ServerName = "New server";
            ServerIP = "127.0.0.1";
            ServerPort = 502;

            Initialize();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context)
        {
            this.Initialize();
        }

        void Initialize()
        {
            ServerList.Add(this);

            ClientLogger = new Logger();

            ClientTCPState = TcpState.Unknown;
            ModbusClientState = ModbusClientStateEnum.ClientDown;

            clientThread = new BackgroundWorker();
            clientThread.WorkerSupportsCancellation = true;
            clientThread.DoWork += clientThread_DoWork;
            clientThread.RunWorkerCompleted += clientThread_RunWorkerCompleted;

            modbusHandleThread = new BackgroundWorker();
            modbusHandleThread.WorkerSupportsCancellation = true;
            modbusHandleThread.DoWork += modbusHandleThread_DoWork;
            modbusHandleThread.RunWorkerCompleted += modbusHandleThread_RunWorkerCompleted;
        }

        RequestBuilder selectedRequestBuilder;
        public RequestBuilder SelectedRequestBuilder
        {
            get { return selectedRequestBuilder; }
            set
            {
                if (selectedRequestBuilder != null)
                    OnModbusResponce -= SelectedRequestBuilder.OnModbusResponce;

                selectedRequestBuilder = value;
                OnPropertyChanged();

                OnModbusResponce += SelectedRequestBuilder.OnModbusResponce;
            }
        }

        Logger clientLogger;
        public Logger ClientLogger
        {
            get { return clientLogger; }
            set { clientLogger = value; OnPropertyChanged(); }
        }

        byte[] modbusResponce;
        public byte[] ModbusResponce
        {
            get { return modbusResponce; }
            private set
            {
                modbusResponce = value;
                OnPropertyChanged();

                if (OnModbusResponce != null)
                    OnModbusResponce(modbusResponce);
            }
        }

        public delegate void ResponseData(byte[] modbusResponce);
        public event ResponseData OnModbusResponce;

        byte[] modbusRequest;
        public byte[] ModbusRequest
        {
            get { return modbusRequest; }
            set
            {
                modbusRequest = value;
                OnPropertyChanged();

                ModbusRequestIsValid = ModbusRequestValidCheck();
            }
        }

        bool modbusRequestIsValid;
        public bool ModbusRequestIsValid
        {
            get { return modbusRequestIsValid; }
            set { modbusRequestIsValid = value; Console.WriteLine("valid set"); OnPropertyChanged(); }
        }

        public void RequestBuilder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ModbusRequest":
                    ModbusRequest = SelectedRequestBuilder.ModbusRequest;
                    break;
            }
        }

        bool ModbusRequestValidCheck()
        {
            if (ModbusRequest == null)
                return false;

            return true;
        }

        #region ModbusHandleThread

        BackgroundWorker modbusHandleThread;

        void modbusHandleThread_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (modbusHandleThread.CancellationPending)
                    return;

                try
                {
                    GetMessage();
                    ClientStream.Flush();
                }
                catch (Exception ex)
                {
                    if (!modbusHandleThread.CancellationPending)
                        ClientLogger.AddMessage(ex.Message);
                    
                    return;
                }
            }
        }

        void modbusHandleThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        void GetMessage()
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

                ClientLogger.AddMessage("Rx: " + result.ToHexString());

                ModbusResponce = result;
            }
        }

        #endregion

        #region TcpClientThread

        TcpClient modbusTCPClient = new TcpClient();
        TcpClient ModbusTCPClient
        {
            get { return modbusTCPClient; }
            set { modbusTCPClient = value; }
        }

        NetworkStream clientStream;
        NetworkStream ClientStream
        {
            get { return clientStream; }
            set { clientStream = value; }
        }

        TcpState clientTCPState;
        public TcpState ClientTCPState
        {
            get { return clientTCPState; }
            private set { clientTCPState = value; OnPropertyChanged(); }
        }

        ModbusClientStateEnum modbusClientState;
        public ModbusClientStateEnum ModbusClientState
        {
            get { return modbusClientState; }
            private set { modbusClientState = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        BackgroundWorker clientThread;

        void clientThread_DoWork(object sender, DoWorkEventArgs e)
        {
            ModbusClientState = ModbusClientStateEnum.ClientUp;

            ModbusTCPClient = new TcpClient();

            while (true)
            {
                if (clientThread.CancellationPending)
                {
                    DisconnectFromServer();

                    modbusHandleThread.CancelAsync();
                    while (modbusHandleThread.IsBusy)
                        Thread.Sleep(100);

                    CheckConnection();
                    return;
                }

                if (!ModbusTCPClient.Connected)
                    ConnectToServer();

                if (!modbusHandleThread.IsBusy && ModbusTCPClient.Connected)
                    modbusHandleThread.RunWorkerAsync();

                CheckConnection();

                Thread.Sleep(500);
            }
        }

        void clientThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ModbusClientState = ModbusClientStateEnum.ClientDown;
        }

        void ConnectToServer()
        {
            try
            {
                ClientLogger.AddMessage(String.Format("Attempt to connect to server {0}:{1}", ServerIP, ServerPort));

                ModbusTCPClient = new TcpClient();

                ModbusTCPClient.Connect(ServerIP, ServerPort);
                ClientStream = ModbusTCPClient.GetStream();

                ClientLogger.AddMessage(String.Format("Connected to server {0}:{1}", ServerIP, ServerPort));

                modbusHandleThread.RunWorkerAsync();
            }
            catch (Exception e) { ClientLogger.AddMessage("Connect to server failed. " + e.Message); }
        }

        void CheckConnection()
        {
            TcpConnectionInformation[] tcpConnections = null;

            try
            {
                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                tcpConnections = ipProperties.GetActiveTcpConnections().Where(x =>
                x.LocalEndPoint.Equals(ModbusTCPClient.Client.LocalEndPoint) &&
                x.RemoteEndPoint.Equals(ModbusTCPClient.Client.RemoteEndPoint)).ToArray();
            }
            catch { }

            if (tcpConnections != null && tcpConnections.Length > 0)
            {
                TcpState stateOfConnection = tcpConnections.First().State;
                ClientTCPState = stateOfConnection;
            }
            else
                ClientTCPState = TcpState.Unknown;
        }

        void DisconnectFromServer()
        {
            if (ClientStream != null)
                ClientStream.Close();
            if (ModbusTCPClient != null)
                ModbusTCPClient.Close();

            ClientLogger.AddMessage("Disconnected from server");
        }

        public void StartClientCommand()
        {
            ModbusClientState = ModbusClientStateEnum.ClientStarting;
            clientThread.RunWorkerAsync();
        }

        public void StopClientCommand()
        {
            ModbusClientState = ModbusClientStateEnum.ClientShuttingdown;
            clientThread.CancelAsync();
        }

        public void SendRequest()
        {
            ClientLogger.AddMessage("Tx: " + ModbusRequest.ToHexString());
            ClientStream.Write(ModbusRequest, 0, ModbusRequest.Length);
        }

        public enum ModbusClientStateEnum
        {
            ClientUp,
            ClientDown,
            ClientStarting,
            ClientShuttingdown
        }

        #endregion

        #region Properties

        string serverName;
        [DataMember]
        public string ServerName
        {
            get { return serverName; }
            set { serverName = value; OnPropertyChanged(); }
        }

        string serverIP;
        [DataMember]
        public string ServerIP
        {
            get { return serverIP; }
            set { serverIP = value; OnPropertyChanged(); }
        }

        int serverPort;
        [DataMember]
        public int ServerPort
        {
            get { return serverPort; }
            set { serverPort = value; OnPropertyChanged(); }
        }

        #endregion

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
