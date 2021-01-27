using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GPXManager.entities
{
    public class TrackViewModel
    {
        public Dictionary<GPS, List<Track>> Tracks { get;  internal set; }
        public TrackViewModel()
        {
            Tracks = new Dictionary<GPS, List<Track>>();
        }

        public List<Track> ReadTracksFromXML(DeviceGPX deviceGPX)
        {
            string xml = deviceGPX.GPX;
            GPS gps = deviceGPX.GPS;
            string fileName = deviceGPX.Filename;
            var trackList = new List<Track>();
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "trk")
                        {
                            Track trk = new Track();
                            trk.GPS = gps;
                            trk.FileName = Path.GetFileName(fileName);
                            trk.IsRoute = false;
                            trk.Read(reader);

                            if (!Tracks.ContainsKey(gps))
                            {
                                Tracks.Add(gps, new List<Track>());
                                Tracks[gps].Add(trk);
                            }
                            else
                            {
                                if (Tracks[gps].Where(t => t.Name == trk.Name).FirstOrDefault() == null)
                                {
                                    Tracks[gps].Add(trk);
                                }
                            }

                            trackList.Add(trk);
                        }
                    }
                }
            }
            return trackList;
        }
        public List<Track> ReadTracksFromFile(string filename,GPS gps)
        {
            if(Tracks==null)
            {
                Tracks = new Dictionary<GPS, List<Track>>();
            }
            var trackList = new List<Track>();

            using (XmlReader reader = XmlReader.Create(filename))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "trk")
                        {
                            Track trk = new Track();
                            trk.GPS = gps;
                            trk.FileName = Path.GetFileName(filename);
                            trk.IsRoute = false;
                            trk.Read(reader);
                            if(!Tracks.ContainsKey(gps))
                            {
                                Tracks.Add(gps, new List<Track>());
                                Tracks[gps].Add(trk);
                            }
                            else
                            {
                                if(Tracks[gps].Where(t=>t.Name==trk.Name).FirstOrDefault()==null)
                                {
                                    Tracks[gps].Add(trk);
                                }
                            }
                            trackList.Add(trk);
                        }
                    }
                }
            }
            return trackList;
        }
    }
}
