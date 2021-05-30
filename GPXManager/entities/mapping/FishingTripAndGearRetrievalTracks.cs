using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;
namespace GPXManager.entities.mapping
{
 /// <summary>
 /// encapsulates a fishing trip represented by the shapefile and any tracks of gear retrieval
 /// </summary>
    public class FishingTripAndGearRetrievalTracks
    {
        public Shapefile TripShapefile { get; set; }
        public List<DetectedTrack> GearRetrievalTracks { get; set; }

        public int Handle { get; set; }
    }
}
