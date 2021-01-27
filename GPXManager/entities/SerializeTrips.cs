using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace GPXManager.entities
{
    public class SerializeTrips
    {
        public List<TripEdited> Trips { get; set; }
        public void Save(string fileName)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(SerializeTrips));
                xmls.Serialize(sw, this);
            }
        }
    }
}
