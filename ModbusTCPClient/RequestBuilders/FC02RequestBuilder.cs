using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModbusTotalKit;

namespace ModbusTCPClient
{
    public class FC02RequestBuilder : RequestBuilder
    {
        public FC02RequestBuilder(ModbusServer modbusServer) : base(modbusServer)
        {
            RequestBuilderName = "FC02 - Read discrete inputs";

            FormatSelectionEnabled = false;
            AddressCountEnabled = true;
            DataRowsReadOnly = true;

            RebuildDataRows();

            BuildModbusRequest();
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
            request[7] = 2;     // Function code
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
                int currentByte = i / 8;
                int currentBit = i % 8;

                byte valueByte = ModbusResponce[9 + currentByte];
                bool bitValue = valueByte.GetBit(currentBit);

                DataRows[i].DataValue = bitValue.ToString();
            }

            return true;
        }
    }
}
