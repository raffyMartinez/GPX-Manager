using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    class ImportGPXEventArg:EventArgs
    {
        public int ImportedCount { get; set; }
        public string GPXFileName { get; set; }
        public GPS GPS { get; set; }
    }
}
