using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Data;
using System.Globalization;
using System.Reflection;

namespace ModbusTCPClient
{
    public enum SingleRegisterFormatEnum
    {
        Binary,
        UShort,
        Short,
        Hex,
    }

    public enum MultipleRegistersFormatEnum
    {
        Binary,
        UShort,
        Short,
        Hex,
        Float
    }

    public enum FloatByteOrderEnum
    {
        [Description("0-1-2-3")]
        NoSwap,
        [Description("1-0-3-2")]
        ByteSwap,
        [Description("2-3-0-1")]
        WoradSwap,
        [Description("3-2-1-0")]
        ByteAndWordSwap
    }
}
