using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class CTXUserSummary
    {
        public DateTime DateOfFirstTrip { get; set; }
        public DateTime DateOfLatestTrip { get; set; }

        public string User { get; set; }

        public int TotalNumberOfTrips { get; set; }
    }
}
