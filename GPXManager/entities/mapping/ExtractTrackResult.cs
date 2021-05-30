using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities.mapping
{
    public class ExtractTrackResult
    {
        public List<ExtractedFishingTrack> ExtractedTracks { get; set; }
        public bool Success { get; set; }
        public ExtractedTrackSourceType SourceType { get; set; }
        public int SourceID { get; set; }


    }
}
