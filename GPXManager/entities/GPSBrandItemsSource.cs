using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace GPXManager.entities
{
    class GPSBrandItemsSource:IItemsSource
    {
        public ItemCollection GetValues()
        {
            var brands = new ItemCollection();
            foreach(var item in Entities.GPSViewModel.GPSBrands)
            {
                brands.Add(item);
            }
            return brands;
        }
    }
}
