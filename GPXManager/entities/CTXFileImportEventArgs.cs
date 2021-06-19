using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class CTXFileImportEventArgs:EventArgs
    {
        public string SourceFile { get; set; }
        public string ImportResultFile { get; set; }

        public int XMLReviewedCount { get; set; }

        public int XMLReviewdSaveCount { get; set; }

        public string Context { get; set; }

    }
}
