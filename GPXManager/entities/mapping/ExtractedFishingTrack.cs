using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public enum ExtractedTrackSourceType
    {
        TrackSourceTypeNone,
        TrackSourceTypeCTX,
        TrackSourceTypeGPX

    }
    public class ExtractedFishingTrack
    {
        public Shape Segment { get; set; }
        public int ID { get; set; }
        public DateTime DateAdded { get; set; }

        public CTXFile SourceCTXFile
        {
            get
            {
                if(TrackSourceType==ExtractedTrackSourceType.TrackSourceTypeCTX)
                {
                    return Entities.CTXFileViewModel.GetFile(ID);
                }
                else
                {
                    return null;
                }
            }
        }

        public DeviceGPX SourceGPXFile
        {
            get
            {
                if (TrackSourceType == ExtractedTrackSourceType.TrackSourceTypeGPX)
                {
                    return Entities.DeviceGPXViewModel.GetDeviceGPX(ID);
                }
                else
                {
                    return null;
                }
            }
        }
        public string TrackSourceTypeToString
        {
            get
            {
                if(TrackSourceType==ExtractedTrackSourceType.TrackSourceTypeCTX)
                {
                    return "CTX";
                }
                else
                {
                    return "GPX";
                }
            }
        }
        public ExtractedTrackSourceType TrackSourceType { get; set; }
        public int TrackSourceID { get; set; }
        public string SerializedTrack { get; set; }
        public Shape TrackOriginal { get; set; }
        public Shape SegmentSimplified { get; set; }
        public double LengthOriginal { get; set; }
        public double LengthSimplified { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public double AverageSpeed { get; set; }
        public int TrackPointCountOriginal { get; set; }
        public int TrackPointCountSimplified { get; set; }

        public string SerializedTrackUTM { get; set; }

        public bool FromDatabase { get; set; }
        public string DeviceName { get; set; }
        public string Duration
        {
            get
            {
                return (End - Start).ToString();
            }
        }

        public List<double> SpeedAtWaypoints { get; set; } = new List<double>();

    }
}
