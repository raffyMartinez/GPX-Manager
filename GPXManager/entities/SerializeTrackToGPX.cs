using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
namespace GPXManager.entities
{
    public static class SerializeTrackToGPX
    {

        public static string XMLString { get; internal set; }
        public static string SerializeToString()
        {
            XmlDocument doc = new XmlDocument();

            XmlElement root = doc.CreateElement("gpx");
            root.SetAttribute("xmlns", "http://www.topografix.com/GPX/1/1");
            root.SetAttribute("creator", "GPXManager");

            XmlElement trk = doc.CreateElement("trk");
            
            XmlElement trackName = doc.CreateElement("name");
            trackName.InnerText = TrackName;
            trk.AppendChild(trackName);

            XmlElement trackSegments = doc.CreateElement("trkseg");
            foreach(var wpt in Waypoints)
            {
                XmlElement trackPoint = doc.CreateElement("trkpt");
                trackPoint.SetAttribute("lat", wpt.Latitude.ToString());
                trackPoint.SetAttribute("lon", wpt.Longitude.ToString());
                trackPoint.AppendChild(doc.CreateElement("ele")).InnerXml = "0";
                trackPoint.AppendChild(doc.CreateElement("time")).InnerXml = wpt.Time.ToString("yyyy-MM-ddTHH:mm:ssZ");
                trackSegments.AppendChild(trackPoint);
            }

            trk.AppendChild(trackSegments);
            root.AppendChild(trk);

            XMLString = root.OuterXml;
            return XMLString;
        }

        public static List<Waypoint> Waypoints { get; set; }
        public static string TrackName { get; set; }
    }
}
