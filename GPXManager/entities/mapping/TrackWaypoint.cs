using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities.mapping
{
    public class TrackWaypoint
    {
        public double Speed { get; set; }
        public Waypoint Waypoint { get; set; }

        public double Distance { get; set; }
    }
}
