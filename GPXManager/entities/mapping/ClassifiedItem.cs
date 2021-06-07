using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public class ClassifiedItem
    {
        public string Caption { get; set; }
        public ShapeDrawingOptions DrawingOptions { get; set; }
        public float PointSize { get; set; }
        public uint FillColor { get; set; }

        public ClassifiedItem(string caption)
        {
            Caption = caption;
        }
    }
}
