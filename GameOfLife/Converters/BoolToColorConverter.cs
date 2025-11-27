using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GameOfLife.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAlive)
            {
                return isAlive ? Colors.Black : Colors.White;
            }
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
