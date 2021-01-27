using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace GPXManager.entities
{
    public  class SerializeGPS
    {
        public  List<GPSEdited> GPSList { get; set; }
        public  void Save(string fileName)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(SerializeGPS));
                xmls.Serialize(sw, this);
            }
        }
    }
}
