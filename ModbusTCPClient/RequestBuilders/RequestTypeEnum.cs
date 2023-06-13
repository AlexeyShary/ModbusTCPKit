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
    public enum RequestTypeEnum
    {
        [Description("FC01 - Read coils")]
        ReadCoils,
        [Description("FC02 - Read discrete inputs")]
        ReadDiscreteInputs,
        [Description("FC03 - Read holding registers")]
        ReadHoldingRegisters,
        [Description("FC04 - Read input registers")]
        ReadInputRegisters,
        [Description("FC05 - Write single coil")]
        WriteCoil,
        [Description("FC06 - Write single register")]
        WriteRegister,
        [Description("FC15 - Write coils")]
        WriteCoils,
        [Description("FC16 - Write registers")]
        WriteRegisters,
        [Description("Manual request build")]
        ManualRequestBuild
    }

    public class RequestTypeEnumDescriptionValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var type = typeof(RequestTypeEnum);
                var name = Enum.GetName(type, value);
                FieldInfo fi = type.GetField(name);
                var descriptionAttrib = (DescriptionAttribute)
                    Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));

                return descriptionAttrib.Description;
            }
            catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
