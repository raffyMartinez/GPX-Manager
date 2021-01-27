using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace GPXManager.entities
{
    class WaypointItemsSource:IItemsSource
    {
        public ItemCollection GetValues()
        {
            var wpts = new ItemCollection();
            if(Entities.WaypointViewModel.Waypoints.Count==0)
            {
                Entities.WaypointViewModel.ReadWaypointsFromRepository();
            }

            if (Entities.WaypointViewModel.Count > 0)
            {
                foreach (var item in Entities.WaypointViewModel.Waypoints[Entities.GPSViewModel.CurrentEntity])
                {
                    foreach (var wpt in item.Waypoints
                        .Where(t => t.Time.AddHours(Global.Settings.HoursOffsetGMT) > Entities.TripViewModel.CurrentEntity.DateTimeDeparture)
                        .Where(t => t.Time.AddHours(Global.Settings.HoursOffsetGMT) < Entities.TripViewModel.CurrentEntity.DateTimeArrival)
                       )
                    {
                        wpts.Add(wpt.Name);
                    }
                }
            }
            return wpts;
        }
    }
}
