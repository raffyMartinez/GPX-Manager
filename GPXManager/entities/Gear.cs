using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class Gear
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string BaseGearCode { get; set; }
        public bool IsGeneric { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
