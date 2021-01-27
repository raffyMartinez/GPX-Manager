using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
   public interface IWaypoint
    {
        double Latitude { get; set; }
        double Longitude { get; set; }
        string Name { get; set; }
        DateTime Time { get; set; }
    }
}
