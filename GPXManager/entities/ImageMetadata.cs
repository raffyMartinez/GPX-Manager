using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class ImageMetadata
    {
        public int Height { get; set; }
        public int Width { get; set; }

        public string DataPrecision { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Comment { get; set; }
        public string Orientation { get; set; }

        public DateTime Digitized { get; set; }

        public string ExposureTime { get; set; }

        public DateTime Original { get; set; }

        public string FileType { get; set; }

        public string FileName { get; set; }

        public Int64 FileSize { get; set; }

        public DateTime Modified { get; set; }
    }
}
