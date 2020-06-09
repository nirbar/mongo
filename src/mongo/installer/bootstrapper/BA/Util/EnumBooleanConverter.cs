using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MongoDB.Bootstrapper.BA.Util
{
    public class EnumBooleanConverter : BaseConverter, IValueConverter
    {
        public EnumBooleanConverter()
        { }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;

            return Enum.Parse(targetType, parameterString);
        }
    }
}
