using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities.mapping
{
    public class ExtractTrackEventArgs:EventArgs
    {
        public string Context { get; set; }
        public ExtractedFishingTrack ExtractedFishingTrack { get; set; }        

        public int Counter { get; set; }
    
    }
}
