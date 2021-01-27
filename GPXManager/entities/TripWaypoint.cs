using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit;
using NSAP_ODK.Entities;
namespace GPXManager.entities
{
    public class TripWaypointLite
    {
        public TripWaypointLite() { }
        public TripWaypointLite(TripWaypoint wpt)
        {
            RowID = wpt.RowID;
            WaypointType = wpt.WaypointType;
            SetNumber = wpt.SetNumber;
            Waypoint = wpt.Waypoint;
            TripID = wpt.Trip.TripID;
        }
        public int RowID { get; set; }
        public string WaypointType { get; set; }
        public int SetNumber { get; set; }
        public Waypoint Waypoint { get; set; }

        public int TripID { get; set; }
    }
    public class TripWaypoint
    {
        public DateTime? _timeStamp;
        public  Trip Trip{get;set;}
        [ReadOnly(true)]
        public int RowID { get; set; }

        [ItemsSource(typeof(WaypointItemsSource))]
        public string WaypointName { get; set; }

        [ItemsSource(typeof(WaypointTypeItemsSource))]
        public string WaypointType { get; set; }

        public DateTime TimeStampAdjusted { get; internal set; }


        public DateTime? TimeStamp { 
            get { return _timeStamp; }
            set 
            {
                _timeStamp = value;
                TimeStampAdjusted = ((DateTime)_timeStamp).AddHours(Global.Settings.HoursOffsetGMT);
            }
        }
        public int SetNumber { get; set; }

        public Waypoint Waypoint { get; set; }

        [ReadOnly(true)]
        public string WaypointGPXFileName { get; set; }


    }
}
