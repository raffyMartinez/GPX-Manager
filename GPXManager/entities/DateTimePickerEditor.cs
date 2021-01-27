using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace NSAP_ODK.Entities
{
    public class DateTimePickerEditor : DateTimePicker, ITypeEditor
    {
        public DateTimePickerEditor()
        {
            Format = DateTimeFormat.Custom;
            //FormatString = "dd.MM.yyyy";
            FormatString = "MMM-dd-yyyy";

            TimePickerVisibility = System.Windows.Visibility.Collapsed;
            ShowButtonSpinner = false;
            AutoCloseCalendar = true;
            DefaultValue = DateTime.Parse("1/1/2010");
        }

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            Binding binding = new Binding("Value");
            binding.Source = propertyItem;
            binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;

            BindingOperations.SetBinding(this, ValueProperty, binding);
            return this;
        }
    }
}
