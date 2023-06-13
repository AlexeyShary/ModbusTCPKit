using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModbusTotalKit;

namespace ModbusTCPClient
{
    public class FC16RequestBuilder : RequestBuilder
    {
        public FC16RequestBuilder(ModbusServer modbusServer) : base(modbusServer)
        {
            RequestBuilderName = "FC16 - Write registers";

            FormatSelectionEnabled = true;
            AddressCountEnabled = true;
            DataRowsReadOnly = false;

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

                BuildModbusRequest();
                OnPropertyChanged();
            }
        }

        protected override void BuildModbusRequest()
        {
            if (SelectedDataFormat == MultipleRegistersFormatEnum.Float)
                if (AddressCount % 2 != 0)
                {
                    ModbusRequest = null;
                    return;
                }

            int valueBytesCount = AddressCount * 2;

            byte[] request = new byte[13 + valueBytesCount];

            int messageSize = 6 + valueBytesCount;

            byte[] startRegister = BitConverter.GetBytes(StartAddress).SwapBytes();

            request[0] = 0;     // Transaction identifier
            request[1] = 0;     // Transaction identifier
            request[2] = 0;     // Protocol identifier = 0
            request[3] = 0;     // Protocol identifier = 0
            request[4] = 0;     // Message size in bytes after [5] 
            request[5] = (byte)(messageSize);     // Message size in bytes after [5] 
            request[6] = SlaveID;     // Slave address
            request[7] = 16;     // Function code
            request[8] = startRegister[0];     // Start register
            request[9] = startRegister[1];     // Start register

            byte[] registerCount = new byte[2];
            registerCount = BitConverter.GetBytes(AddressCount).SwapBytes();

            request[10] = registerCount[0];    // Bit count
            request[11] = registerCount[1];    // Bit count

            request[12] = (byte)(valueBytesCount);    // Byte count

            if (SelectedDataFormat != MultipleRegistersFormatEnum.Float)
                for (int i = 0; i < AddressCount; i++)
                {
                    byte[] valBytes = new byte[2];

                    switch (SelectedDataFormat)
                    {
                        case MultipleRegistersFormatEnum.Binary:
                            try
                            {
                                short registerVal = Convert.ToInt16(DataRows[i].DataValue, 2);
                                valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                            }
                            catch { ModbusRequest = null; return; }
                            break;
                        case MultipleRegistersFormatEnum.Hex:
                            try
                            {
                                short registerVal = Convert.ToInt16(DataRows[i].DataValue, 16);
                                valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                            }
                            catch { ModbusRequest = null; return; }
                            break;
                        case MultipleRegistersFormatEnum.Short:
                            try
                            {
                                short registerVal = Convert.ToInt16(DataRows[i].DataValue);
                                valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                            }
                            catch { ModbusRequest = null; return; }
                            break;
                        case MultipleRegistersFormatEnum.UShort:
                            try
                            {
                                ushort registerVal = Convert.ToUInt16(DataRows[i].DataValue);
                                valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                            }
                            catch { ModbusRequest = null; return; }
                            break;
                    }

                    request[13 + i * 2] = valBytes[0];
                    request[13 + 1 + i * 2] = valBytes[1];
                }
            else
            {
                for (int i = 0; i < AddressCount / 2; i++)
                {
                    byte[] valBytes = new byte[4];

                    try
                    {
                        Single registerVal = Convert.ToSingle(DataRows[i].DataValue);
                        valBytes = BitConverter.GetBytes(registerVal).SwapBytes();
                    }
                    catch { ModbusRequest = null; return; }

                    switch (SelectedFloatByteOrder)
                    {
                        case FloatByteOrderEnum.NoSwap:

                            request[13 + 0 + i * 4] = valBytes[0];
                            request[13 + 1 + i * 4] = valBytes[1];
                            request[13 + 2 + i * 4] = valBytes[2];
                            request[13 + 3 + i * 4] = valBytes[3];

                            break;

                        case FloatByteOrderEnum.ByteSwap:

                            request[13 + 0 + i * 4] = valBytes[1];
                            request[13 + 1 + i * 4] = valBytes[0];
                            request[13 + 2 + i * 4] = valBytes[3];
                            request[13 + 3 + i * 4] = valBytes[2];

                            break;

                        case FloatByteOrderEnum.WoradSwap:

                            request[13 + 0 + i * 4] = valBytes[2];
                            request[13 + 1 + i * 4] = valBytes[3];
                            request[13 + 2 + i * 4] = valBytes[0];
                            request[13 + 3 + i * 4] = valBytes[1];

                            break;

                        case FloatByteOrderEnum.ByteAndWordSwap:

                            request[13 + 0 + i * 4] = valBytes[3];
                            request[13 + 1 + i * 4] = valBytes[2];
                            request[13 + 2 + i * 4] = valBytes[0];
                            request[13 + 3 + i * 4] = valBytes[1];

                            break;
                    }
                }
            }

            ModbusRequest = request;
        }
    }
}