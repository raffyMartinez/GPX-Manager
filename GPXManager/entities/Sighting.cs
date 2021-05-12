using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class Sighting
    {
        public string ID { get; set; }
        public string DeviceID { get; set; }

        public DateTime DateTime { get; set; }
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string SelectedUser { get; set; }
        public string SelectedGear { get; set; }
        public string SelectedLandingSite { get; set; }
        public string SelectedAction { get; set; }

        public string WaypointName { get; set; }

        public string WaypointType { get; set; }

        public string Note { get; set; }

        public string OtherUser { get; set; }
        public string OtherGear { get; set; }
        public string OtherLandingSite { get; set; }

        

    }
}
