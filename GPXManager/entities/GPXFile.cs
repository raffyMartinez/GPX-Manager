
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace GPXManager.entities
{

    public enum GPXFileType
    {
        Common,
        Track,
        Waypoint
    }
    public class GPXFile
    {
        private int _tripCount;
        private bool _isArchived;
        public GPXFile(string fileName)
        {
            FileName = fileName;
        }
        public GPXFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            FileName = fileInfo.Name;
            TimeStampUTC = fileInfo.CreationTimeUtc;
            DateModifiedUTC = fileInfo.LastWriteTimeUtc;
            Size = fileInfo.Length;
            XML = File.OpenText(FileInfo.FullName).ReadToEnd();
        }
        public DateTime MonthYear
        {
            get
            {
                return new DateTime(DateRangeStart.Year, DateRangeStart.Month, 1);
            }
        }
        public FileInfo FileInfo { get; set; }
        public string FileName { get; set; }
        public int WaypointCount { get; internal set; }
        public int TrackCount{ get; internal set; }

        public int TrackPointsCount { get; internal set; }

        public GPXFileType GPXFileType
        {
            get
            {
                if(TrackCount>0 && WaypointCount>0)
                {
                    return GPXFileType.Common;
                }
                else if(TrackCount>0)
                {
                    return GPXFileType.Track;
                }
                else
                {
                    return GPXFileType.Waypoint;
                }
            }
        }

        public double TrackLength { get; internal set; }
        public TimeSpan TimeInMotion { get; internal set; }
        public DateTime TimeStampUTC { get; set; }
        public DateTime DateModifiedUTC { get; set; }
        public string DriveName { get; set; }
        public GPS GPS { get; set; }

        public List<Track> Tracks { get; internal set; }
        public List<WaypointLocalTime> TrackWaypoinsInLocalTime { get; internal set; } = new List<WaypointLocalTime>();
        public  List<int> ShapeIndexes { get; set; }

        public int TripCount
        {
            get 
            {
                return Entities.TripViewModel.TripCollection.Count(t=>t.GPXFileName==FileName); 
            }
            set
            {
                _tripCount = value;
            }
        }
        public string XML { get; set; }
        public List<WaypointLocalTime> NamedWaypointsInLocalTime { get; internal set; } = new List<WaypointLocalTime>();

        public bool IsArchived
        {
            get
            {
                var exist = Entities.DeviceGPXViewModel.GetDeviceGPX(GPS, FileName) != null; 
                return Entities.DeviceGPXViewModel.GetDeviceGPX(GPS, FileName) != null;
            }
            set { _isArchived = value; }
        }
        public bool SavedToDatabase { get; internal set; }
        public long Size { get; set; }
        public bool ShownInMap { get; set; }
        public string DateRange { get; internal set; }
        public TimeSpan? TimeSpan { get; internal set; }

        public string TimeSpanHourMinute { get; internal set; }
        public DateTime DateRangeStart { get; internal set; }
        public DateTime DateRangeEnd { get; internal set; }
        public void ComputeStats(DeviceGPX deviceGPX = null)
        {
            DateTime? trkDateStart = null;
            DateTime? trkDateEnd = null;
            DateTime? wptDateStart = null; 
            DateTime? wptDateEnd = null;


            List<Track> tracks;
            if (deviceGPX != null && deviceGPX.GPX .Length > 0)
            {
                tracks = Entities.TrackViewModel.ReadTracksFromXML(deviceGPX);
            }
            else
            {
                tracks = Entities.TrackViewModel.ReadTracksFromFile($"{FileInfo.FullName}", GPS);
            }
            TrackCount = tracks.Count;
            Tracks = tracks;
            if(TrackCount>0)
            {
                trkDateStart = tracks[0].Waypoints[0].Time.AddHours(Global.Settings.HoursOffsetGMT) ;
                trkDateEnd = tracks[TrackCount-1].Waypoints[tracks[TrackCount - 1].Waypoints.Count-1].Time.AddHours(Global.Settings.HoursOffsetGMT);

                int count = 1;
                foreach (var trk in tracks)
                {
                    TrackPointsCount += trk.Waypoints.Count;
                    TrackLength += trk.Statistics.Length;
                    TimeInMotion += trk.Statistics.TimeInMotion;

                    foreach(var pt in trk.Waypoints)
                    {
                        pt.Name = count++.ToString();
                        TrackWaypoinsInLocalTime.Add(new WaypointLocalTime(pt));
                    }
                }
            }


            if (deviceGPX != null || (deviceGPX==null && XML!=null))
            {


                List<Waypoint> waypoints = null;
                if (deviceGPX != null)
                {
                    waypoints = Entities.WaypointViewModel.ReadWaypointsFromFile(deviceGPX, true);
                }
                else if(XML != null & XML.Length>0)
                {
                    waypoints = Entities.WaypointViewModel.ReadWaypointFromXML(XML,FileName);
                }
                if(waypoints==null)
                {
                    WaypointCount = 0;
                }
                else
                {
                    WaypointCount = waypoints.Count;
                }
                if (WaypointCount > 0)
                {
                    wptDateStart = waypoints[0].Time.AddHours(Global.Settings.HoursOffsetGMT);
                    wptDateEnd = waypoints[WaypointCount - 1].Time.AddHours(Global.Settings.HoursOffsetGMT);

                    foreach (var pt in waypoints)
                    {
                        NamedWaypointsInLocalTime.Add(new WaypointLocalTime(pt));
                    }
                }
            }
            else
            {
                if (Entities.WaypointViewModel.Waypoints.Count > 0 && Entities.WaypointViewModel.Waypoints.ContainsKey(GPS))
                {
                    var waypoints = Entities.WaypointViewModel.Waypoints[GPS]
                        .Where(t => t.FileName == FileInfo.Name)
                        .FirstOrDefault();
                    if (waypoints == null)
                    {
                        WaypointCount = 0;
                    }
                    else
                    {
                        WaypointCount = waypoints.Waypoints.Count;
                    }

                    if (WaypointCount > 0)
                    {
                        wptDateStart = waypoints.Waypoints[0].Time.AddHours(Global.Settings.HoursOffsetGMT);
                        wptDateEnd = waypoints.Waypoints[WaypointCount - 1].Time.AddHours(Global.Settings.HoursOffsetGMT);

                        foreach (var pt in waypoints.Waypoints)
                        {
                            NamedWaypointsInLocalTime.Add(new WaypointLocalTime(pt));
                        }
                    }
                }
            }


            if(trkDateStart != null && wptDateStart != null)
            {
                DateRangeStart = (DateTime)trkDateStart > (DateTime)wptDateStart ? (DateTime)wptDateStart : (DateTime)trkDateStart;
            }
            else if(trkDateStart!=null)
            {
                DateRangeStart = (DateTime)trkDateStart;
            }
            else
            {
                DateRangeStart = (DateTime)wptDateStart;
            }

            if (trkDateEnd != null && wptDateEnd != null)
            {
                DateRangeEnd = (DateTime)trkDateEnd > (DateTime)wptDateEnd ? (DateTime)trkDateEnd : (DateTime)wptDateEnd;
            }
            else if (trkDateEnd != null)
            {
                DateRangeEnd = (DateTime)trkDateEnd;
            }
            else
            {
                DateRangeEnd = (DateTime)wptDateEnd;
            }

            //if (DateRangeEnd.Date == DateRangeStart.Date)
            //{
            //    DateRange = $"{DateRangeStart.ToString("MMM-dd-yyyy HH:mm")}";
            //}
            //else
            //{
            //    DateRange = $"{DateRangeStart.ToString("MMM-dd-yyyy HH:mm")} - {DateRangeEnd.ToString("MMM-dd-yyyy HH:mm")}";
            //}
            DateRange = $"{DateRangeStart.ToString("MMM-dd-yyyy HH:mm")} - {DateRangeEnd.ToString("MMM-dd-yyyy HH:mm")}";

            if (trkDateStart != null && trkDateEnd != null)
            {
                TimeSpan = trkDateEnd - trkDateStart;
                var h = (int)(((TimeSpan)TimeSpan).TotalMinutes / 60);
                var m = ((TimeSpan)TimeSpan).TotalMinutes % 60;
                TimeSpanHourMinute = $"{h.ToString("000")}:{m.ToString("N1")}";
            }
        }
        public string SizeFormatted { get { return FileSizeFormatter.FormatSize(Size); } }

    }
}
