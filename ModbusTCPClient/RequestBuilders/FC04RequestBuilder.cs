using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModbusTotalKit;

namespace ModbusTCPClient
{
    public class FC04RequestBuilder : RequestBuilder
    {
        public FC04RequestBuilder(ModbusServer modbusServer) : base(modbusServer)
        {
            RequestBuilderName = "FC04 - Read input registers";

            FormatSelectionEnabled = true;
            AddressCountEnabled = true;
            DataRowsReadOnly = true;

            RebuildDataRows();

            BuildModbusRequest();
        }

        public IEnumerable<MultipleRegistersFormatEnum> DataFormats
        {
            get { return Enum.GetValues(typeof(MultipleRegistersFormatEnum)).Cast<MultipleRegistersFormatEnum>(); }
        }

        MultipleRegistersFormatEnum selectedDataFormat = MultipleRegistersFormatEnum.Short;
        public MultipleRegistersFormatEnum SelectedDataFormat
        {
            get { return selectedDataFormat; }
            set
            {
                selectedDataFormat = value;

                if (selectedDataFormat == MultipleRegistersFormatEnum.Float)
                    IsFloatFormat = true;
                else
                    IsFloatFormat = false;

                RebuildDataRows();

                if (ModbusResponce != null)
                    HandleModbusResponce();

                BuildModbusRequest();
                OnPropertyChanged();
            }
        }

        public IEnumerable<FloatByteOrderEnum> FloatByteOrder
        {
            get { return Enum.GetValues(typeof(FloatByteOrderEnum)).Cast<FloatByteOrderEnum>(); }
        }

        FloatByteOrderEnum selectedFloatByteOrder;
        public FloatByteOrderEnum SelectedFloatByteOrder
        {
            get { return selectedFloatByteOrder; }
            set
            {
                selectedFloatByteOrder = value;

                if (ModbusResponce != null)
                    HandleModbusResponce();

                OnPropertyChanged();
            }
        }

        protected override void BuildModbusRequest()
        {
            byte[] request = new byte[12];

            byte[] startRegister = BitConverter.GetBytes(StartAddress).SwapBytes();
            byte[] registerCount = BitConverter.GetBytes(AddressCount).SwapBytes();

            request[0] = 0;     // Transaction identifier
            request[1] = 0;     // Transaction identifier
            request[2] = 0;     // Protocol identifier = 0
            request[3] = 0;     // Protocol identifier = 0
            request[4] = 0;     // Message size in bytes after [5] 
            request[5] = 6;     // Message size in bytes after [5] 
            request[6] = SlaveID;     // Slave address
            request[7] = 4;     // Function code
            request[8] = startRegister[0];     // Start register
            request[9] = startRegister[1];     // Start register
            request[10] = registerCount[0];    // Register count
            request[11] = registerCount[1];    // Register count

            ModbusRequest = request;
        }

        override protected bool HandleModbusResponce()
        {
            if (!base.HandleModbusResponce())
                return false;

            for (int i = 0; i < DataRows.Count; i++)
            {
                int register = StartAddress + i;
                string formattedValue = "";

                byte[] valueBytes;

                switch (SelectedDataFormat)
                {
                    case MultipleRegistersFormatEnum.Binary:

                        valueBytes = new byte[2];
                        valueBytes[0] = ModbusResponce[9 + i * 2];
                        valueBytes[1] = ModbusResponce[9 + i * 2 + 1];

                        formattedValue = Convert.ToString(valueBytes[0], 2).PadLeft(8, '0') + "_" + Convert.ToString(valueBytes[1], 2).PadLeft(8, '0');

                        break;
                    case MultipleRegistersFormatEnum.UShort:

                        valueBytes = new byte[2];
                        valueBytes[0] = ModbusResponce[9 + i * 2];
                        valueBytes[1] = ModbusResponce[9 + i * 2 + 1];

                        formattedValue = BitConverter.ToUInt16(valueBytes.SwapBytes(), 0).ToString();

                        break;
                    case MultipleRegistersFormatEnum.Short:

                        valueBytes = new byte[2];
                        valueBytes[0] = ModbusResponce[9 + i * 2];
                        valueBytes[1] = ModbusResponce[9 + i * 2 + 1];

                        formattedValue = BitConverter.ToInt16(valueBytes.SwapBytes(), 0).ToString();

                        break;
                    case MultipleRegistersFormatEnum.Hex:

                        valueBytes = new byte[2];
                        valueBytes[0] = ModbusResponce[9 + i * 2];
                        valueBytes[1] = ModbusResponce[9 + i * 2 + 1];

                        formattedValue = valueBytes.ToHexString();

                        break;
                    case MultipleRegistersFormatEnum.Float:

                        if ((9 + i * 2 + 3) > ModbusResponce.Length - 1)
                            return false;

                        valueBytes = new byte[4];

                        switch (SelectedFloatByteOrder)
                        {
                            case FloatByteOrderEnum.NoSwap:

                                valueBytes[0] = ModbusResponce[9 + i * 4 + 0];
                                valueBytes[1] = ModbusResponce[9 + i * 4 + 1];
                                valueBytes[2] = ModbusResponce[9 + i * 4 + 2];
                                valueBytes[3] = ModbusResponce[9 + i * 4 + 3];

                                break;

                            case FloatByteOrderEnum.ByteSwap:

                                valueBytes[0] = ModbusResponce[9 + i * 4 + 1];
                                valueBytes[1] = ModbusResponce[9 + i * 4 + 0];
                                valueBytes[2] = ModbusResponce[9 + i * 4 + 3];
                                valueBytes[3] = ModbusResponce[9 + i * 4 + 2];

                                break;

                            case FloatByteOrderEnum.WoradSwap:

                                valueBytes[0] = ModbusResponce[9 + i * 4 + 2];
                                valueBytes[1] = ModbusResponce[9 + i * 4 + 3];
                                valueBytes[2] = ModbusResponce[9 + i * 4 + 0];
                                valueBytes[3] = ModbusResponce[9 + i * 4 + 1];

                                break;

                            case FloatByteOrderEnum.ByteAndWordSwap:

                                valueBytes[0] = ModbusResponce[9 + i * 4 + 3];
                                valueBytes[1] = ModbusResponce[9 + i * 4 + 2];
                                valueBytes[2] = ModbusResponce[9 + i * 4 + 1];
                                valueBytes[3] = ModbusResponce[9 + i * 4 + 0];

                                break;
                        }

                        formattedValue = BitConverter.ToSingle(valueBytes.SwapBytes(), 0).ToString();

                        break;
                }

                DataRows[i].DataValue = formattedValue;
            }

            return true;
        }
    }
}
