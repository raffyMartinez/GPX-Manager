using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class DeviceGPX
    {
        public GPS GPS { get; set; }
        public string Filename { get; set; }
        public string GPX { get; set; }
        public int RowID { get; set; }

        public string GPXType { get; set; }

        public string MD5 { get; set; }

        public DateTime TimeRangeStart { get; set; }
        public DateTime TimeRangeEnd { get; set; }
    }
}
