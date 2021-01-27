using Gavaghan.Geodesy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace GPXManager.entities
{

    public class WaypointLocalTime:IWaypoint
    {
        public WaypointLocalTime(Waypoint wpt)
        {
            Latitude = wpt.Latitude;
            Longitude = wpt.Longitude;
            Name = wpt.Name;
            Time = wpt.Time.AddHours(Global.Settings.HoursOffsetGMT);
        }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Name { get; set; }
        public DateTime Time { get; set; }
    }
    public class Waypoint:IWaypoint
    {
        #region Public Properties


        public string GPXFileName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; } = double.NaN;
        public double Speed { get; set; }
        public string Name { get; set; }


        public DateTime Time { get; set; }

        public override string ToString()
        {
            return Name;
        }
        #endregion

        #region Private Fields
        private static GeodeticCalculator geoCal = new GeodeticCalculator();
        #endregion

        #region Public Methods
        public static double ComputeDistance(Waypoint w1, Waypoint w2, out double elevationChange)
        {
            GlobalCoordinates startCoords = new GlobalCoordinates(w1.Latitude, w1.Longitude);
            GlobalCoordinates endCoords = new GlobalCoordinates(w2.Latitude, w2.Longitude);
            GeodeticCurve curve = geoCal.CalculateGeodeticCurve(Ellipsoid.WGS84, startCoords, endCoords);
            elevationChange = 0;
            if (!double.IsNaN(w1.Elevation) && !double.IsNaN(w2.Elevation))
            {
                elevationChange = w2.Elevation - w1.Elevation;
            }
            GeodeticMeasurement measurement = new GeodeticMeasurement(curve, elevationChange);
            return measurement.PointToPointDistance;
        }
        #endregion

        #region Private and internal methods
        internal void Read(XmlReader reader)
        {
            string latStr = reader.GetAttribute("lat");
            if (latStr != null && latStr != "")
            {
                Latitude = Convert.ToDouble(latStr, CultureInfo.InvariantCulture);
            }
            string lonStr = reader.GetAttribute("lon");
            if (lonStr != null && lonStr != "")
            {
                Longitude = Convert.ToDouble(lonStr, CultureInfo.InvariantCulture);
            }
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    if (reader.Name == "ele")
                    {
                        reader.Read();
                        Elevation = Convert.ToDouble(reader.Value.Trim(), CultureInfo.InvariantCulture);
                    }
                    if (reader.Name == "time")
                    {
                        reader.Read();
                        string datetimestring = reader.Value.Trim();
                        Time = ParseDateTimeString(datetimestring);
                    }
                    if(reader.Name =="name")
                    {
                        reader.Read();
                        Name = reader.Value;
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement && (reader.Name == "trkpt" || reader.Name == "rtept" || reader.Name == "wpt"))
                {
                    break;
                }
            }
        }

        private DateTime ParseDateTimeString(string datetimestring)
        {
            string[] elems = datetimestring.Split('-', 'T', ':', 'Z');
            if (elems.Length == 3)
            {
                return new DateTime(Convert.ToInt32(elems[0]), Convert.ToInt32(elems[1]), Convert.ToInt32(elems[2]));
            }
            return new DateTime(Convert.ToInt32(elems[0]),
                Convert.ToInt32(elems[1]), 
                Convert.ToInt32(elems[2]), 
                Convert.ToInt32(elems[3]),
                Convert.ToInt32(elems[4]),
                Convert.ToInt32(elems[5]));
        }
        #endregion
    }
}
