using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class FisherDeviceAssignment
    {
        public Fisher Fisher { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? RetunDate { get; set; }
        public string DeviceID { get; set; }

        public int RowID { get; set; }
    }
}
