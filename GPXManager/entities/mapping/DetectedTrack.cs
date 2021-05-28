using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public class DetectedTrack
    {
        public ExtractedFishingTrack ExtractedFishingTrack { get; set; }
        public Shape Shape { get; set; }
        public bool Accept { get; set; }

        public double Length { get; set; }

        public override string ToString()
        {
            return $"{ExtractedFishingTrack.Start.ToString("MMM-dd-yyyy HH:mm")}: Length - {Length.ToString("N2")} Points - {ExtractedFishingTrack.TrackPointCountOriginal}";
        }
    }
}
