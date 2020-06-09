using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MongoDB.Bootstrapper.BA.Util
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : BaseConverter, IValueConverter
    {
        public InverseBooleanConverter()
        { }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((targetType != typeof(bool)) && (targetType != typeof(Nullable<bool>)))
            {
                throw new InvalidOperationException("The target must be a boolean");
            }

            return !(bool?)value ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !((bool?)value ?? true);
        }
    }
}