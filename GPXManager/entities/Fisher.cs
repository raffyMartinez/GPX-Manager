using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class Fisher
    {
        public int FisherID { get; set; }
        public string Name { get; set; }
        public List<string> Vessels { get; set; } = new List<string>();

        public string VesselListCSV
        {
            get
            {
                string list = "";
                foreach (var item in Vessels)
                {
                    list += $"{item}, ";
                }
                return list.Trim(',', ' ');
            }
        }
        public string VesselList
        {
            get
            {
                string list = "";
                foreach (var item in Vessels)
                {
                    list += $"{item}|";
                }
                return list.Trim('|');
            }
        }
    }
}
