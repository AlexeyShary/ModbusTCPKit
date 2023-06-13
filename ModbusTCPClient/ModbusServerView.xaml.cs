using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ModbusTotalKit;

namespace ModbusTCPClient
{
    public partial class ModbusServerView : UserControl, INotifyPropertyChanged
    {
        ModbusServer associatedServer;
        public ModbusServer AssociatedServer
        {
            get { return associatedServer; }
            private set { associatedServer = value; OnPropertyChanged(); }
        }

        public ModbusServerView()
        {
            InitializeComponent();
        }

        public ModbusServerView(ModbusServer server)
        {
            InitializeComponent();

            AssociatedServer = server;
            AssociatedServer.PropertyChanged += ModbusServer_PropertyChanged;

            DataContext = this;

            SelectedRequestType = RequestTypeEnum.ReadHoldingRegisters;
        }

        RequestTypeEnum selectedRequestType;
        public RequestTypeEnum SelectedRequestType
        {
            get { return selectedRequestType; }
            set
            {
                selectedRequestType = value;
                
                if (SelectedRequestBuilderView != null)
                    SelectedRequestBuilderView.SelectedRequestBuilder.PropertyChanged -= AssociatedServer.RequestBuilder_PropertyChanged;

                switch (selectedRequestType)
                {
                    case RequestTypeEnum.ReadCoils:
                        SelectedRequestBuilderView = new RequestBuilderView(new FC01RequestBuilder(AssociatedServer));
                        break;
                    case RequestTypeEnum.ReadDiscreteInputs:
                        SelectedRequestBuilderView = new RequestBuilderView(new FC02RequestBuilder(AssociatedServer));
                        break;
                    case RequestTypeEnum.ReadHoldingRegisters:
                        SelectedRequestBuilderView = new RequestBuilderView(new FC03RequestBuilder(AssociatedServer));
                        break;
                    case RequestTypeEnum.ReadInputRegisters:
                        SelectedRequestBuilderView = new RequestBuilderView(new FC04RequestBuilder(AssociatedServer));
                        break;
                    case RequestTypeEnum.WriteCoil:
                        SelectedRequestBuilderView = new RequestBuilderView(new FC05RequestBuilder(AssociatedServer));
                        break;
                    case RequestTypeEnum.WriteRegister:
                        SelectedRequestBuilderView = new RequestBuilderView(new FC06RequestBuilder(AssociatedServer));
                        break;
                    case RequestTypeEnum.WriteCoils:
                        SelectedRequestBuilderView = new RequestBuilderView(new FC15RequestBuilder(AssociatedServer));
                        break;
                    case RequestTypeEnum.WriteRegisters:
                        SelectedRequestBuilderView = new RequestBuilderView(new FC16RequestBuilder(AssociatedServer));
                        break;
                    case RequestTypeEnum.ManualRequestBuild:
                        SelectedRequestBuilderView = new RequestBuilderView(new ManualRequestBuilder(AssociatedServer));
                        break;
                }

                if (selectedRequestType != RequestTypeEnum.ManualRequestBuild)
                {
                    ModbusRequestHexString = BitConverter.ToString(SelectedRequestBuilderView.SelectedRequestBuilder.ModbusRequest).Replace("-", " ");

                    SelectedRequestBuilderView.SelectedRequestBuilder.PropertyChanged += AssociatedServer.RequestBuilder_PropertyChanged;
                    AssociatedServer.ModbusRequest = SelectedRequestBuilderView.SelectedRequestBuilder.ModbusRequest;
                }

                AssociatedServer.SelectedRequestBuilder = SelectedRequestBuilderView.SelectedRequestBuilder;

                OnPropertyChanged();
            }
        }

        public IEnumerable<RequestTypeEnum> RequestTypes
        {
            get { return Enum.GetValues(typeof(RequestTypeEnum)).Cast<RequestTypeEnum>(); }
        }

        RequestBuilderView selectedRequestBuilderView;
        public RequestBuilderView SelectedRequestBuilderView
        {
            get { return selectedRequestBuilderView; }
            private set { selectedRequestBuilderView = value; OnPropertyChanged(); }
        }

        private void ModbusServer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ModbusRequest":
                    if (SelectedRequestBuilderView.SelectedRequestBuilder.ModbusRequest != null)
                        if (!(SelectedRequestBuilderView.SelectedRequestBuilder is ManualRequestBuilder))
                        ModbusRequestHexString = AssociatedServer.ModbusRequest.ToHexString();
                    else
                        ModbusRequestHexString = "Invalid request parameters";
                    break;
            }
        }

        string modbusRequestHexString;
        public string ModbusRequestHexString
        {
            get { return modbusRequestHexString; }
            set
            {
                modbusRequestHexString = value;
                OnPropertyChanged();

                if (SelectedRequestBuilderView.SelectedRequestBuilder is ManualRequestBuilder)
                {
                    string stringToParse = modbusRequestHexString;
                    stringToParse = stringToParse.Replace(" ", "");

                    if (stringToParse.Length % 2 != 0)
                    {
                        AssociatedServer.ModbusRequest = null;
                        AssociatedServer.ModbusRequestIsValid = false;
                        return;
                    }

                    int arrayLength = stringToParse.Length / 2;

                    if (arrayLength == 0)
                    {
                        AssociatedServer.ModbusRequest = null;
                        AssociatedServer.ModbusRequestIsValid = false;
                        return;
                    }

                    byte[] modbusRequest = new byte[arrayLength];

                    try
                    {
                        for (int i = 0; i < stringToParse.Length; i += 2)
                            modbusRequest[i / 2] = Convert.ToByte(stringToParse.Substring(i, 2), 16);
                    }
                    catch
                    {
                        AssociatedServer.ModbusRequest = null;
                        AssociatedServer.ModbusRequestIsValid = false;
                        return;
                    }

                    AssociatedServer.ModbusRequest = modbusRequest;
                    AssociatedServer.ModbusRequestIsValid = true;
                }
            }
        }

        private void CtrlCCopyCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ListBox listbox = (ListBox)(sender);
            List<Logger.LogEntry> selectedLogEntries = new List<Logger.LogEntry>();

            foreach (object selectedItem in listbox.SelectedItems)
                selectedLogEntries.Add((Logger.LogEntry)selectedItem);

            string text = "";
            foreach (Logger.LogEntry logEntry in selectedLogEntries)
                text += logEntry.FormatedMessage + Environment.NewLine;

            Clipboard.SetText(text);
        }

        #region Commands

        BaseCommand startClientCommand;
        public BaseCommand StartClientCommand
        {
            get
            {
                return startClientCommand ??
                    (startClientCommand = new BaseCommand(obj =>
                    {
                        AssociatedServer.StartClientCommand();
                    },
                    (obj) => AssociatedServer.ModbusClientState == ModbusServer.ModbusClientStateEnum.ClientDown));
            }
        }

        BaseCommand stopClientCommand;
        public BaseCommand StopClientCommand
        {
            get
            {
                return stopClientCommand ??
                    (stopClientCommand = new BaseCommand(obj =>
                    {
                        AssociatedServer.StopClientCommand();
                    },
                    (obj) => AssociatedServer.ModbusClientState == ModbusServer.ModbusClientStateEnum.ClientUp));
            }
        }

        BaseCommand sendMessageCommand;
        public BaseCommand SendMessageCommand
        {
            get
            {
                return sendMessageCommand ??
                    (sendMessageCommand = new BaseCommand(obj =>
                    {
                        AssociatedServer.SendRequest();
                    },
                    (obj) => (AssociatedServer.ClientTCPState == System.Net.NetworkInformation.TcpState.Established) && (AssociatedServer.ModbusRequestIsValid)));
            }
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
