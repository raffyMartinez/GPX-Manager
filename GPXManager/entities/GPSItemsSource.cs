using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
namespace GPXManager.entities
{
    class GPSItemsSource:IItemsSource
    {
        public ItemCollection GetValues()
        {
            var items = new ItemCollection();
            foreach (var item in Entities.GPSViewModel.GPSCollection)
            {
                items.Add(item.Code,item.DeviceName);
            }
            return items;
        }
    }
}
