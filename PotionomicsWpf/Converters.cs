using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace PotionomicsWpf
{
    public abstract class AbstractBooleanConverter<T> : IValueConverter
    {
        private T TrueValue { get; }
        private T FalseValue { get; }

        public AbstractBooleanConverter(T trueValue, T falseValue) 
        {
            TrueValue = trueValue;
            FalseValue = falseValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (b)
                    return TrueValue;
                else
                    return FalseValue;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is T t)
            {
                if (t.Equals(TrueValue))
                    return true;
                else if (t.Equals(FalseValue))
                    return false;
            }
            return DependencyProperty.UnsetValue;
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : AbstractBooleanConverter<bool>
    {
        public InverseBooleanConverter() : base(false, true)
        {
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : AbstractBooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed)
        {
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBooleanToVisibilityConverter : AbstractBooleanConverter<Visibility>
    {
        public InverseBooleanToVisibilityConverter() : base(Visibility.Collapsed, Visibility.Visible)
        {
        }
    }
}
