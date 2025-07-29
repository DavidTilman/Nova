using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.Client;
public class GainToColorConverter : IValueConverter
{


    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double gain)
        {
            return gain >= 0
                ? (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"] // Success-like
                : (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }

        throw new ArgumentException("Value must be a double representing gain.", nameof(value));

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
