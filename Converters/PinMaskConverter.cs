﻿// Converters/PinMaskConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace MiddleBooth.Converters
{
    public class PinMaskConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string pin)
            {
                return new string('*', pin.Length);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}