using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
namespace GPXManager.entities
{
    public class GearItemsSource:IItemsSource
    {
        public ItemCollection GetValues()
        {
            var gears = new ItemCollection();
            foreach (var item in Entities.GearViewModel.GearCollection)
            {
                gears.Add(item.Code, item.Name);
            }
            return gears;
        }
    }
}
