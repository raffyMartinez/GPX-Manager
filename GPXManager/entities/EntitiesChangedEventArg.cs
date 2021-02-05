using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public enum TypeOfChange
    {
        Added,
        Edited,
        Deleted
    }
    class EntitiesChangedEventArg:EventArgs
    {
        public TypeOfChange TypeOfChange { get; set; }
        public object Entity { get; set; }
    }
}
