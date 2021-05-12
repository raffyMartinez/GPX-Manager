using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class SightingAttributes
    {
        public string DeviceID { get; set; }
        public string User { get; set; }
        public string Gear { get; set; }
        public string LandingSite { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public int? TrackPointCount { get; set; }

        public DateTime? TrackTimeStampStart { get; set; }
        public DateTime? TrackTimeStampEnd { get; set; }
        public int? SetGearPointCount { get; set; }
        public int? RetrieveGearPointCount { get; set; }

        public string AppVersion { get; set; }
    }
}
