using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModbusTotalKit;

namespace ModbusTCPClient
{
    public class FC15RequestBuilder : RequestBuilder
    {
        public FC15RequestBuilder(ModbusServer modbusServer) : base(modbusServer)
        {
            RequestBuilderName = "FC15 - Write coils";

            FormatSelectionEnabled = false;
            AddressCountEnabled = true;
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

        protected override void BuildModbusRequest()
        {
            int valueBytesCount = 1 + (AddressCount / 8);
            if (AddressCount % 8 == 0)
                valueBytesCount--;

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
            request[7] = 15;     // Function code
            request[8] = startRegister[0];     // Start register
            request[9] = startRegister[1];     // Start register

            byte[] bitCount = new byte[2];
            bitCount = BitConverter.GetBytes(AddressCount).SwapBytes();

            request[10] = bitCount[0];    // Bit count
            request[11] = bitCount[1];    // Bit count

            request[12] = (byte)(valueBytesCount);    // Byte count (packed bits)

            for (int i = 0; i < valueBytesCount; i++)
            {
                int bitArrayLength = 0;

                if (i < valueBytesCount - 1)
                    bitArrayLength = 8;
                else
                    bitArrayLength = AddressCount - i * 8;

                bool[] values = new bool[8];

                for (int j = 0; j < bitArrayLength; j++)
                {
                    try
                    {
                        if ( Convert.ToInt16(DataRows[j + i * 8].DataValue) != 0 && Convert.ToInt16(DataRows[j + i * 8].DataValue) != 1 )
                        {
                            ModbusRequest = null;
                            return;
                        }

                        values[j] = Convert.ToBoolean(Convert.ToInt16(DataRows[j + i * 8].DataValue));
                    }
                    catch
                    {
                        try
                        {
                            values[j] = Convert.ToBoolean(DataRows[j + i * 8].DataValue);
                        }
                        catch { ModbusRequest = null; return; }
                    }
                }

                request[13 + i] = values.Reverce().ToByte();
            }

            ModbusRequest = request;
        }
    }
}
