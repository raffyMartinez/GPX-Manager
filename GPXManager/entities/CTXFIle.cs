using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;

namespace GPXManager.entities
{
    public class CTXFIle
    {
        public RemoteFileInfo RemoteFileInfo { get; set; }

        public bool IsDownloaded { get; set; }

        public string CTXFileName { get; set; }

        public string UserName { get; set; }
        public string Gear { get; set; }
        public string LandingSite { get; set; }
        public DateTime? DateStart { get; set; }
        public DateTime? DateEnd { get; set; }

        public int? TrackPtCount { get; set; }
        public DateTime? TrackTimeStampStart { get; set; }
        public DateTime? TrackTimeStampEnd { get; set; }
        public int? SetGearPtCount { get; set; }
        public int? RetrieveGearPtCount { get; set; }
        public int RowID { get; set; }
        public string DeviceID { get; set; }
        public string FileName { get; set; }
        public DateTime DateAdded { get; set; }
        public string XML { get; set; }

        public string AppVersion { get; set; }

        public bool DownloadFile { get; set; }

        public override string ToString()
        {
            return RemoteFileInfo.Name;
        }
    }
}
