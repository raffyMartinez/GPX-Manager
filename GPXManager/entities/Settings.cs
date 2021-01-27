using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GPXManager.entities
{
    public class Settings
    {
        private int? _latestTripCount = null;
        private int? _latestGPXFileCount = null;

        public int? LatestGPXFileCount
        {
            get
            {
                if (_latestGPXFileCount == null)
                {
                    return 5;
                }
                else
                {
                    return _latestGPXFileCount;
                }
            }
            set { _latestGPXFileCount = value; }
        }
        public int? LatestTripCount 
        { 
            get
            {
                if(_latestTripCount==null)
                {
                    return 5;
                }
                else
                {
                    return _latestTripCount;
                }
            }
            set { _latestTripCount = value; } 
        }
        public string BingAPIKey { get; set; }
        public string MDBPath { get; set; }
        public string ComputerGPXFolder { get; set; }
        public string DeviceGPXFolder { get; set; }

        public int HoursOffsetGMT { get; set; }
        //public List<string> Setting2 { get; set; }

        public void Save(string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(Settings));
                xmls.Serialize(sw, this);
            }
        }
        public static Settings Read(string filename)
        {
            using (StreamReader sw = new StreamReader(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(Settings));
                return xmls.Deserialize(sw) as Settings;
            }
        }
    }
}
