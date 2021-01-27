using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities.mapping
{
    public class MapLayerSequence
    {
        public MapLayer MapLayer{ get; set; }
        public int Sequence { get; set; }

        public override string ToString()
        {
            return $"{Sequence} - {MapLayer.Name}";
        }
    }
}
