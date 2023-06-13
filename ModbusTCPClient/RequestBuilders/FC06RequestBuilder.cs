using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModbusTotalKit;

namespace ModbusTCPClient
{
    public class FC06RequestBuilder : RequestBuilder
    {
        public FC06RequestBuilder(ModbusServer modbusServer) : base(modbusServer)
        {
            RequestBuilderName = "FC06 - Write single register";

            FormatSelectionEnabled = true;
            AddressCountEnabled = false;
            DataRowsReadOnly = false;

            RebuildDataRows();
            FillValues();

            BuildModbusRequest();
        }

        void FillValues()
        {
            foreach (DataRow dataRow in DataRows)
                dataRow.DataValue = "0";
        }

        public IEnumerable<SingleRegisterFormatEnum> DataFormats
        {
            get { return Enum.GetValues(typeof(SingleRegisterFormatEnum)).Cast<SingleRegisterFormatEnum>(); }
        }

        SingleRegisterFormatEnum selectedDataFormat = SingleRegisterFormatEnum.UShort;
        public SingleRegisterFormatEnum SelectedDataFormat
        {
            get { return selectedDataFormat; }
            set { selectedDataFormat = value; RebuildDataRows(); BuildModbusRequest(); OnPropertyChanged(); }
        }

        protected override void BuildModbusRequest()
        {
            byte[] request = new byte[12];

            byte[] startRegister = BitConverter.GetBytes(StartAddress).SwapBytes();

            request[0] = 0;     // Transaction identifier
            request[1] = 0;     // Transaction identifier
            request[2] = 0;     // Protocol identifier = 0
            request[3] = 0;     // Protocol identifier = 0
            request[4] = 0;     // Message size in bytes after [5] 
            request[5] = 6;     // Message size in bytes after [5] 
            request[6] = SlaveID;     // Slave address
            request[7] = 6;     // Function code
            request[8] = startRegister[0];     // Start register
            request[9] = startRegister[1];     // Start register

            byte[] valBytes = new byte[2];

            switch (SelectedDataFormat)
            {
                case SingleRegisterFormatEnum.Binary:
                    try
                    {
                        short registerVal = Convert.ToInt16(DataRows[0].DataValue, 2);
                        valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                    }
                    catch { ModbusRequest = null; return; }
                    break;
                case SingleRegisterFormatEnum.Hex:
                    try
                    {
                        short registerVal = Convert.ToInt16(DataRows[0].DataValue, 16);
                        valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                    }
                    catch { ModbusRequest = null; return; }
                    break;
                case SingleRegisterFormatEnum.Short:
                    try
                    {
                        short registerVal = Convert.ToInt16(DataRows[0].DataValue);
                        valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                    }
                    catch { ModbusRequest = null; return; }
                    break;
                case SingleRegisterFormatEnum.UShort:
                    try
                    {
                        ushort registerVal = Convert.ToUInt16(DataRows[0].DataValue);
                        valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                    }
                    catch { ModbusRequest = null; return; }
                    break;
            }

            request[10] = valBytes[0];
            request[11] = valBytes[1];

            ModbusRequest = request;
        }
    }
}
