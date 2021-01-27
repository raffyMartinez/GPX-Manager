using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Diagnostics;
using System.IO;

namespace GPXManager.entities
{
    public class WaypointViewModel
    {
        public WaypointViewModel()
        {
            Waypoints = new Dictionary<GPS, List<GPSWaypointSet>>();
        }


        public List<Waypoint> GetWayppoints(GPS gps, string fileName)
        {
            return Waypoints[gps].Where(t => t.FullFileName == fileName).FirstOrDefault().Waypoints;
        }
        public Waypoint GetWaypoint(string name, GPS gps)
        {
            Waypoint wpt=null;
            foreach(var item in Waypoints[gps])
            {
                var w = item.Waypoints.Where(t => t.Name == name).FirstOrDefault();
                if (w!=null)
                {
                    wpt =w;
                    break;
                }
            }
            return wpt;
        }

        public int Count { get { return Waypoints.Count; } }
        public Dictionary<GPS, List<GPSWaypointSet>> Waypoints { get; internal set; }

        public void ReadWaypointsFromRepository()
        {

            var dict = Entities.DeviceGPXViewModel.DeviceGPXCollection
                .GroupBy(t => t.GPS)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach(var set in dict)
            {
                var listGPSWptSet = new List<GPSWaypointSet>();
                foreach (var gpsWptGPX in set.Value)
                {
                    var wpts = new List<Waypoint>();
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(gpsWptGPX.GPX);

                    XmlNodeList parentNodes = xmlDoc.GetElementsByTagName("wpt");
                    foreach (XmlNode pt in parentNodes)
                    {
                        Waypoint wpt = new Waypoint
                        {
                            Latitude = double.Parse(pt.Attributes["lat"].Value),
                            Longitude = double.Parse(pt.Attributes["lon"].Value)
                        };
                        foreach (XmlNode childNode in pt.ChildNodes)
                        {
                            switch (childNode.Name)
                            {
                                case "time":
                                    wpt.Time = DateTime.Parse(childNode.InnerText).AddHours(Global.Settings.HoursOffsetGMT * -1);
                                    break;
                                case "name":
                                    wpt.Name = childNode.InnerText;
                                    Console.WriteLine($"wpt name and time:{wpt.Name}-{wpt.Time.ToString()}");
                                    break;
                            }
                        }
                        wpts.Add(wpt);
                    }
                    listGPSWptSet.Add(new GPSWaypointSet { Waypoints = wpts, FullFileName = gpsWptGPX.Filename, GPS = gpsWptGPX.GPS });
                    Console.WriteLine($"waypoint count: {wpts.Count} gps: {gpsWptGPX.GPS.DeviceName} filename:{gpsWptGPX.Filename}");
                    
                }
                Waypoints.Add(set.Key, listGPSWptSet);
            }
        }


        public List<Waypoint> ReadWaypointFromXML(string xml, string filename)
        {
            List<Waypoint> wpts = new List<Waypoint>();
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "wpt")
                        {
                            Waypoint namedWaypoint = new Waypoint();
                            namedWaypoint.Read(reader);
                            namedWaypoint.GPXFileName = Path.GetFileName(filename);
                            wpts.Add(namedWaypoint);
                        }
                    }
                }
            }
            return wpts;
        }

        public List<Waypoint>ReadWaypointFromDeviceGPX(DeviceGPX deviceGPX)
        {
            return ReadWaypointFromXML(deviceGPX.GPX, deviceGPX.Filename);
        }
        //public List<Waypoint>ReadWaypointFromDeviceGPX1(DeviceGPX deviceGPX)
        //{
        //    List<Waypoint> wpts = new List<Waypoint>();
        //    using (XmlReader reader = XmlReader.Create(new StringReader(deviceGPX.GPX)))
        //    {
        //        while (reader.Read())
        //        {
        //            if (reader.IsStartElement())
        //            {
        //                if (reader.Name == "wpt")
        //                {
        //                    Waypoint namedWaypoint = new Waypoint();
        //                    namedWaypoint.Read(reader);
        //                    namedWaypoint.GPXFileName = Path.GetFileName(deviceGPX.Filename);
        //                    wpts.Add(namedWaypoint);
        //                }
        //            }
        //        }
        //    }
        //    return wpts;
        //}

        public List<Waypoint> ReadWaypointsFromFile( DeviceGPX deviceGPX=null, bool waypointsOnly=false)
        {
            GPS gps = deviceGPX.GPS;
            string filename = deviceGPX.Filename;
            List<Waypoint> wpts = new List<Waypoint>();

            using (XmlReader reader = XmlReader.Create(new StringReader(deviceGPX.GPX)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "wpt")
                        {
                            Waypoint namedWaypoint = new Waypoint();
                            namedWaypoint.Read(reader);
                            namedWaypoint.GPXFileName = Path.GetFileName(deviceGPX.Filename);
                            wpts.Add(namedWaypoint);
                        }
                    }
                }
            }

            if (!waypointsOnly)
            {
                if (wpts.Count > 0)
                {
                    GPSWaypointSet gws = new GPSWaypointSet
                    {
                        GPS = gps,
                        FullFileName = filename,
                        Waypoints = wpts
                    };


                    if (Waypoints.Count == 0)
                    {
                        var list = new List<GPSWaypointSet>();
                        list.Add(gws);
                        Waypoints.Add(gps, list);
                    }
                    else
                    {
                        if (Waypoints.ContainsKey(gps))
                        {
                            if (Waypoints[gps].Where(t => t.FileName == gws.FileName).FirstOrDefault() == null)
                            {
                                Waypoints[gps].Add(gws);
                            }

                        }
                        else
                        {
                            var list = new List<GPSWaypointSet>();
                            list.Add(gws);
                            Waypoints.Add(gps, list);
                        }
                    }
                }

            }
            return wpts;
        }

    }
}
