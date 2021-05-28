using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;
namespace GPXManager.entities.mapping
{
    public class TripAndHauls
    {
        public Shapefile Shapefile { get; set; }
        public List<DetectedTrack> Tracks { get; set; }

        public int Handle { get; set; }
    }
}
