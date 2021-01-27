using MapWinGIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities.mapping
{
    
    public class globalMapping
    {
        private static string _appPath = "";
       public static fad3MappingMode MappingMode { get; set; }

        static globalMapping()
        {
            _appPath = System.Windows.Forms.Application.StartupPath;
            GeoProjection = new GeoProjection();
            GeoProjection.SetWgs84();

        }
        public static string ApplicationPath
        {
            get { return _appPath; }
        }

        public static GeoProjection GeoProjection { get; set; }
    }

}
