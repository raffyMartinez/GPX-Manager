using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class Disk
    {
        public string Caption { get; set; }
        public bool Compressed { get; set; }
        public string VolumeSerialNumber { get; set; }
        public string VolumeName { get; set; }
        public bool VolumeDirty { get; set; }
        public long Size { get; set; }
        public long FreeSpace { get; set; }
        public string Description { get; set; }
        public string FileSystem { get; set; }

        public string DeviceID { get; set; }

        public string GPSID { get; set; }
    }
}
