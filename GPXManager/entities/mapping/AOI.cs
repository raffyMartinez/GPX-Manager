using MapWinGIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities.mapping
{
    public class AOI
    {
        public double UpperLeftX { get; set; }
        public double UpperLeftY { get; set; }
        public double LowerRightX { get; set; }
        public double LowerRightY { get; set; }
        public string Name { get; set; }

        public int MapLayerHandle { get; set; } = -1;
        public int ID { get; set; }

        public bool Visibility { get; set; }

        public Shapefile ShapeFile
        {
            get 
            {
                return ShapefileFactory.AOIShapefileFromAOI(this);
            }
        }
    }
}
