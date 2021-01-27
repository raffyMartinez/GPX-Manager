using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
   public class DetectDeviceEventArg:EventArgs
    {
        public string Message { get; set; }
        public string GPSId { get; set; }

        public bool HasDetectError { get; set; }

        public string DriveName { get; set; }
    }
}
