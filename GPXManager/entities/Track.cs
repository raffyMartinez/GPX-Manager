using Gavaghan.Geodesy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GPXManager.entities
{
    /// <summary>
    /// Represents a GPS-track or -route.
    /// </summary>
    public class Track
    {
        #region Types
        /// <summary>
        /// Holds all kinds of statistics of a track.
        /// </summary>
        public class TrackStatistics
        {

            public Waypoint WayPointStart { get; set; }
            public Waypoint WayPointEnd { get; set; }
            /// <summary>
            /// Amount of time where a speed threshold between waypoints is exceeded.
            /// </summary>
            public TimeSpan TimeInMotion { get; set; }
            /// <summary>
            /// Average speed of the track parts with motion.
            /// </summary>
            public double AverageSpeedInMotion { get; set; }
            /// <summary>
            /// Total average speed including breaks.
            /// </summary>
            public double AverageSpeed { get; set; }
            /// <summary>
            /// Track length in km.
            /// </summary>
            public double Length { get; set; }
            /// <summary>
            /// Total meters climbed.
            /// </summary>
            public double AbsoluteClimb { get; set; }
            /// <summary>
            /// Total meters decended.
            /// </summary>
            public double AbsoluteDescent { get; set; }
            /// <summary>
            /// Produces a string summarizing the track statistics.
            /// </summary>
            /// <returns>String summary.</returns>
            /// 
            public TimeSpan Duration { get; set; }
            public override string ToString()
            {
                return $"{Length:0.00} km at avg. speed of {AverageSpeed:0.00} km/h ({AverageSpeedInMotion:0.00} km/h in motion), {AbsoluteClimb:0.0} m up and {AbsoluteDescent:0.0} m down.";
            }
        }
        #endregion

        #region Private Fields
        private TrackStatistics statisics;
        private double motionSpeedThreshold = 1.7;
        #endregion

        #region Public Properties

        public GPS GPS { get; set; }
        public string FileName { get; set; }

        public string FullFileName
        {
            get
            {
                return $@"{GPS.Device.Disks[0].Caption}\{GPS.Folder}\{FileName}";
            }
        }

        /// <summary>
        /// All waypoints of the track.
        /// </summary>
        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();
        /// <summary>
        /// True if object represents a route, false if it represents a track.
        /// </summary>
        public bool IsRoute { get; set; }
        /// <summary>
        /// Name of the track.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Track statistics.
        /// </summary>
        /// <remarks>Statistics are computed when the property is called for the first time. Needs to be reset if waypoints are changed.</remarks>
        public TrackStatistics Statistics
        {
            get
            {
                if (statisics == null)
                {
                    ComputeStatistics();
                }
                return statisics;
            }
        }
        #endregion


        public List<WaypointLocalTime> TrackPtsInLocalTine
        {
            get
            {
                if (Waypoints.Count > 0)
                {
                    var pts = new List<WaypointLocalTime>();
                    foreach (var wpt in Waypoints)
                    {
                        pts.Add(new WaypointLocalTime(wpt));
                    }
                    return pts;
                }
                else
                {
                    return null;
                }
            }
        }
        public void ResetStatistics()
        {
            this.statisics = null;
        }
        /// <summary>
        /// Split a track at all jumps where the distance between two waypoints exceeds a threshold.
        /// </summary>
        /// <param name="distanceThreshold">Distance threshold in </param>
        /// <returns>Splitted tracks.</returns>
        /// <remarks>Track names are preserved and a running number is appended.</remarks>
        public List<Track> SplitAtDistanceJumps(double distanceThreshold)
        {
            List<Track> splitTracks = new List<Track>();
            Track currentTrack = new Track();
            splitTracks.Add(currentTrack);
            currentTrack.Name = this.Name + "_" + splitTracks.Count;
            // go though points and start a new track if distance treashold is exceeded.
            for (int i = 0; i < Waypoints.Count - 1; i++)
            {
                currentTrack.Waypoints.Add(Waypoints[i]);
                double distance = Waypoint.ComputeDistance(Waypoints[i], Waypoints[i + 1], out double elevationChange);
                if (distanceThreshold < distance)
                {
                    currentTrack = new Track();
                    splitTracks.Add(currentTrack);
                    currentTrack.Name = this.Name + "_" + splitTracks.Count;
                }
            }
            // take care of last point
            double lastDistance = Waypoint.ComputeDistance(Waypoints[Waypoints.Count - 2], Waypoints[Waypoints.Count - 1], out double lastElevationChange);
            if (lastDistance > distanceThreshold)
            {
                currentTrack = new Track();
                splitTracks.Add(currentTrack);
                currentTrack.Name = this.Name + "_" + splitTracks.Count;
            }
            currentTrack.Waypoints.Add(Waypoints[Waypoints.Count - 1]);

            return splitTracks;
        }

        private void ComputeStatistics()
        {
            if (Waypoints.Count > 1)
            {
                TrackStatistics trackStatistics = new TrackStatistics();
                for (int i = 0; i < Waypoints.Count - 1; i++)
                {
                    Waypoint w1 = Waypoints[i];
                    Waypoint w2 = Waypoints[i + 1];
                    double distance = Waypoint.ComputeDistance(w1, w2, out double elevationChange);
                    if (elevationChange < 0)
                    {
                        trackStatistics.AbsoluteDescent -= elevationChange;
                    }
                    else
                    {
                        trackStatistics.AbsoluteClimb += elevationChange;
                    }
                    TimeSpan timeBetween = w2.Time - w1.Time;
                    w1.Speed = distance / (1000.0 * timeBetween.TotalHours);
                    if (w1.Speed > motionSpeedThreshold)
                    {
                        trackStatistics.TimeInMotion += timeBetween;
                    }
                    trackStatistics.Length += distance;
                }
                trackStatistics.WayPointStart = Waypoints[0];
                trackStatistics.WayPointEnd = Waypoints[Waypoints.Count-1];
                trackStatistics.Length /= 1000; // convert to km
                trackStatistics.Duration = Waypoints[Waypoints.Count - 1].Time - Waypoints[0].Time;
                trackStatistics.AverageSpeed = trackStatistics.Length / trackStatistics.Duration.TotalHours;
                trackStatistics.AverageSpeedInMotion = trackStatistics.Length / trackStatistics.TimeInMotion.TotalHours;
                statisics = trackStatistics;
            }
        }

        public void Read(string xml, bool stayInGMT = false)
        {
            Waypoints.Clear();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNodeList nameNode = doc.GetElementsByTagName("name");
            Name = nameNode.Item(0).InnerText;

            XmlNodeList parentNode = doc.GetElementsByTagName("trkpt");

            Waypoint wpt = null;
            int x = 1;
            foreach (XmlNode pt in parentNode)
            {
                if (stayInGMT)
                {
                    wpt = new Waypoint
                    {
                        Latitude = double.Parse(pt.Attributes["lat"].Value),
                        Longitude = double.Parse(pt.Attributes["lon"].Value),

                        Time = (DateTime)DateTimeOffset.Parse(pt.InnerText).DateTime,
                        Name = x.ToString()
                    };
                }
                else
                {
                    wpt = new Waypoint
                    {
                        Latitude = double.Parse(pt.Attributes["lat"].Value),
                        Longitude = double.Parse(pt.Attributes["lon"].Value),

                        //BEWARE: pt.inner text is time in GMT and when parsed
                        //it converts it to local time which could surprise you
                        Time = DateTime.Parse(pt.InnerText),
                        Name = x.ToString()
                    };
                }
                Waypoints.Add(wpt);
                x++;
            }

            var wpts = Waypoints.OrderBy(t => t.Time).ToList();
            Waypoints = wpts;
            XMLString = xml;
        }

        public  string XMLString { get; internal set; }
        public  string SerializeToString(GPS gps, DateTime timeStamp,string gpxFileName)
        {
            XmlDocument doc = new XmlDocument();

            XmlElement root = doc.CreateElement("gpx");
            root.SetAttribute("xmlns", "http://www.topografix.com/GPX/1/1");
            root.SetAttribute("creator", "GPXManager");
            root.SetAttribute("gpx_file_source", gpxFileName);
            root.SetAttribute("gps", $"{gps.Code} {gps.DeviceName}");
            root.SetAttribute("timestamp", $"{timeStamp.ToString("MMM-dd-yyyy HH:mm")}");

            XmlElement trk = doc.CreateElement("trk");

            XmlElement trackName = doc.CreateElement("name");
            trackName.InnerText = Name;
            trk.AppendChild(trackName);

            XmlElement trackSegments = doc.CreateElement("trkseg");
            foreach (var wpt in Waypoints)
            {
                XmlElement trackPoint = doc.CreateElement("trkpt");
                trackPoint.SetAttribute("lat", wpt.Latitude.ToString());
                trackPoint.SetAttribute("lon", wpt.Longitude.ToString());
                trackPoint.AppendChild(doc.CreateElement("ele")).InnerXml = "0";

                //appended time is GMT which is denoted by "Z" at the end of the time format string
                trackPoint.AppendChild(doc.CreateElement("time")).InnerXml = wpt.Time.ToString("yyyy-MM-ddTHH:mm:ssZ");

                trackSegments.AppendChild(trackPoint);
            }

            trk.AppendChild(trackSegments);
            root.AppendChild(trk);

            XMLString = root.OuterXml;
            return XMLString;
        }
        public void Read(XmlReader reader)
        {
            Waypoints.Clear();
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    if (reader.Name == "trkpt" || reader.Name == "rtept")
                    {
                        Waypoint wp = new Waypoint();
                        wp.Read(reader);
                        Waypoints.Add(wp);
                    }
                    if (reader.Name == "name")
                    {
                        reader.Read();
                        this.Name = reader.Value.Trim();
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement && (reader.Name == "trk" || reader.Name == "rte"))
                {
                    break;
                }
            }
        }
    }
}
