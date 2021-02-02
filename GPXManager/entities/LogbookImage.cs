using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class LogbookImage
    {
        public string FileName { get; set; }
        public GPS GPS { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public string Notes { get; set; }
        public Gear Gear { get; set; }

        public Trip Trip { get; set; }
    }
}
