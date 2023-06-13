using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusTCPClient
{
    public class ManualRequestBuilder : RequestBuilder
    {
        public ManualRequestBuilder(ModbusServer modbusServer) : base(modbusServer)
        {
            RequestBuilderName = "Manual request builder";

            FormatSelectionEnabled = false;
            AddressCountEnabled = false;
            DataRowsReadOnly = true;

            IsManualRequestBuilder = true;
        }

        protected override void BuildModbusRequest()
        {
            
        }
    }
}
