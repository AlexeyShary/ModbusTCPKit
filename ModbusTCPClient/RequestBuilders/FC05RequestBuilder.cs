using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModbusTotalKit;

namespace ModbusTCPClient
{
    public class FC05RequestBuilder : RequestBuilder
    {
        public FC05RequestBuilder(ModbusServer modbusServer) : base(modbusServer)
        {
            RequestBuilderName = "FC05 - Write single coil";

            FormatSelectionEnabled = false;
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
            request[7] = 5;     // Function code
            request[8] = startRegister[0];     // Start register
            request[9] = startRegister[1];     // Start register

            try
            {
                if (Convert.ToInt16(DataRows[0].DataValue) != 0 && Convert.ToInt16(DataRows[0].DataValue) != 1)
                {
                    ModbusRequest = null;
                    return;
                }

                if (Convert.ToBoolean(Convert.ToInt16(DataRows[0].DataValue)))
                    request[10] = 255;
            }
            catch
            {
                try
                {
                    if (Convert.ToBoolean(DataRows[0].DataValue))
                        request[10] = 255;
                }
                catch { ModbusRequest = null; return; }
            }

            ModbusRequest = request;
        }
    }
}
