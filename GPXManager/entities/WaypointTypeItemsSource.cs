using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
namespace GPXManager.entities
{
    class WaypointTypeItemsSource:IItemsSource
    {
        public ItemCollection GetValues()
        {
            var types = new ItemCollection();
            types.Add("Set");
            types.Add("Haul");
            return types;
        }
    }
}
