using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class Fisher
    {
        public int FIsherID { get; set; }
        public string Name { get; set; }
        public List<string> Vessels { get; set; }

        public string VesselList()
        {
            string list = "";
            foreach(var item in Vessels)
            {
                list += (item + ",");
            }
            return list.Trim(',');
        }
    }
}
