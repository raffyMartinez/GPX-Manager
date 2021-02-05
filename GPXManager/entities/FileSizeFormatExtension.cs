using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public static class FileSizeFormatExtension
    {

            public enum SizeUnits
            {
                Byte, KB, MB, GB, TB, PB, EB, ZB, YB
            }

            public static string ToSize(this Int64 value, SizeUnits unit)
            {
                return (value / (double)Math.Pow(1024, (Int64)unit)).ToString("0.00");
            }
        
    }
}
