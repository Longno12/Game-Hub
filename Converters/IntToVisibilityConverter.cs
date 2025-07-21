using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EnhancedGameHub.Converters
{
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                bool isInverse = parameter as string == "inverse";

                if (isInverse)
                {
                    // If inverse, visible when count is 0
                    return count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    // If not inverse, visible when count is > 0
                    return count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}