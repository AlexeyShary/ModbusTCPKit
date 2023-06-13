using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace ModbusTCPClient
{
    public abstract class RequestBuilder : INotifyPropertyChanged
    {
        ModbusServer server;
        protected ModbusServer Server
        {
            get { return server; }
            set { server = value; OnPropertyChanged(); }
        }

        public RequestBuilder(ModbusServer modbusServer)
        {
            Server = modbusServer;
        }

        string requestBuilderName;
        public string RequestBuilderName
        {
            get { return requestBuilderName; }
            protected set { requestBuilderName = value; OnPropertyChanged(); }
        }

        bool formatSelectionEnabled;
        public bool FormatSelectionEnabled
        {
            get { return formatSelectionEnabled; }
            protected set { formatSelectionEnabled = value; OnPropertyChanged(); }
        }

        byte slaveID = 1;
        public byte SlaveID
        {
            get { return slaveID; }
            set { slaveID = value; BuildModbusRequest(); OnPropertyChanged(); }
        }

        ushort startAddress;
        public ushort StartAddress
        {
            get { return startAddress; }
            set { startAddress = value; RebuildDataRows(); BuildModbusRequest(); OnPropertyChanged(); }
        }

        byte addressCount = 1;
        public byte AddressCount
        {
            get { return addressCount; }
            set { addressCount = value; RebuildDataRows(); BuildModbusRequest(); OnPropertyChanged(); }
        }


        bool addressCountEnabled;
        public bool AddressCountEnabled
        {
            get { return addressCountEnabled; }
            protected set { addressCountEnabled = value; OnPropertyChanged(); }
        }

        bool isFloatFormat;
        public bool IsFloatFormat
        {
            get { return isFloatFormat; }
            set { isFloatFormat = value; OnPropertyChanged(); }
        }

        bool isManualRequestBuilder;
        public bool IsManualRequestBuilder
        {
            get { return isManualRequestBuilder; }
            set { isManualRequestBuilder = value; IsReadOnly = !value; OnPropertyChanged(); }
        }

        bool isReadOnly = true;
        public bool IsReadOnly
        {
            get { return isReadOnly; }
            set { isReadOnly = value; OnPropertyChanged(); }
        }

        byte[] modbusRequest;
        public byte[] ModbusRequest
        {
            get { return modbusRequest; }
            protected set { modbusRequest = value; OnPropertyChanged(); }
        }

        byte[] modbusResponce;
        protected byte[] ModbusResponce
        {
            get { return modbusResponce; }
            set { modbusResponce = value; OnPropertyChanged(); }
        }

        public void OnModbusResponce(byte[] modbusResponce)
        {
            ModbusResponce = modbusResponce;
            HandleModbusResponce();
        }

        protected virtual bool HandleModbusResponce()
        {
            if (ModbusResponce.Length < 9)
            {
                Server.ClientLogger.AddMessage(String.Format("Illegal responce length"
                                ));

                return false;
            }

            if (ModbusResponce[7] == ModbusRequest[7] + 128)
            {
                switch (ModbusResponce[8])
                {
                    case 1:
                        Server.ClientLogger.AddMessage(String.Format("Error code 01 - Illegal function code"
                                ));
                        break;
                    case 2:
                        Server.ClientLogger.AddMessage(String.Format("Error code 02 - Illegal data addres"
                                ));
                        break;
                    default:
                        Server.ClientLogger.AddMessage(String.Format("Error code {0}",
                            ModbusResponce[8]
                                ));
                        break;
                }

                return false;
            }

            return true;
        }

        ObservableCollection<DataRow> dataRows = new ObservableCollection<DataRow>();
        public ObservableCollection<DataRow> DataRows
        {
            get { return dataRows; }
            protected set { dataRows = value; OnPropertyChanged(); }
        }

        protected void RebuildDataRows()
        {
            foreach (DataRow datarow in DataRows)
                datarow.PropertyChanged -= DataRow_PropertyChanged;

            if (!IsFloatFormat)
                try
                {
                    DataRows = new ObservableCollection<DataRow>();
                    for (int i = 0; i < AddressCount; i++)
                    {
                        DataRow newDataRow = new DataRow((ushort)(StartAddress + i));

                        if (!DataRowsReadOnly)
                            newDataRow.DataValue = "0";

                        newDataRow.PropertyChanged += DataRow_PropertyChanged;
                        DataRows.Add(newDataRow);
                    }
                }
                catch { }
            else
                try
                {
                    DataRows = new ObservableCollection<DataRow>();

                    if (AddressCount % 2 != 0)
                    {
                        ModbusRequest = null;
                        return;
                    }

                    for (int i = 0; i < AddressCount; i += 2)
                    {
                        DataRow newDataRow = new DataRow((ushort)(StartAddress + i));

                        if (!DataRowsReadOnly)
                            newDataRow.DataValue = "0";

                        newDataRow.PropertyChanged += DataRow_PropertyChanged;
                        DataRows.Add(newDataRow);
                    }
                }
                catch { }
        }

        bool dataRowsReadOnly;
        public bool DataRowsReadOnly
        {
            get { return dataRowsReadOnly; }
            protected set { dataRowsReadOnly = value; OnPropertyChanged(); }
        }

        protected abstract void BuildModbusRequest();

        private void DataRow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DataValue":
                    BuildModbusRequest();
                    break;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public class DataRow : INotifyPropertyChanged
        {
            public DataRow(ushort address)
            {
                DataAddress = address;
            }

            ushort dataAddress;
            public ushort DataAddress
            {
                get { return dataAddress; }
                set { dataAddress = value; OnPropertyChanged(); }
            }

            string dataValue;
            public string DataValue
            {
                get { return dataValue; }
                set { dataValue = value; OnPropertyChanged(); }
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
}
