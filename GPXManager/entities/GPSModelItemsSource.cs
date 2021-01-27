using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
namespace GPXManager.entities
{
    class GPSModelItemsSource:IItemsSource
    {
        public ItemCollection GetValues()
        {
            var models = new ItemCollection();
            foreach (var item in Entities.GPSViewModel.GPSModels)
            {
                models.Add(item);
            }
            return models;
        }
    }
}
