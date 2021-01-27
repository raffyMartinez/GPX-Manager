using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GPXManager.entities
{
    public class GPSWaypointSet
    {
        public GPS GPS { get; set; }
        public string FullFileName { get; set; }

        public string FileName
        {
            get
            {
                return Path.GetFileName(FullFileName);
            }
        }
        public List<Waypoint> Waypoints { get; set; }
    }
}
