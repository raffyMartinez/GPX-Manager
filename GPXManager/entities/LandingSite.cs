using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class LandingSite
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }

        public string Municipality { get; set; }
        public string Province { get; set; }

        public override string ToString()
        {
            return $"{Name}, {Municipality}, {Province}";
        }
    }
}
