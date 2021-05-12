using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class GPSDataSummary
    {
        public GPS GPS { get; set; }
        public int NumberOfSavedTracks { get; set; }
        public int NumberOfSavedWaypoints { get; set; }

        public int NumberTrackLength500m { get; set; }
        public int NumberTrackLengthLess500m { get; set; }
    }
}
