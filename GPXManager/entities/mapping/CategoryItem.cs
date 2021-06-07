using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GPXManager.entities.mapping
{
    class CategoryItem
    {
        public double Upper { get; set; }
        public double Lower { get; set; }
        public Color Color { get; set; }
        public string ClassSize { get; set; }

        public string Name { get { return $"{Lower} - {Upper}"; } }
    }
}
