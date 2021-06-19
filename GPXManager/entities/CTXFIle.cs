using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;
using System.IO;

namespace GPXManager.entities
{
    public class CTXFileSummaryView
    {
        public CTXFileSummaryView(CTXFile f)
        {
            Identifier = f.RowID;
            DateStart = "";
            DateEnd = "";
            if(f.DateStart!=null)
            {
                DateStart = ((DateTime)f.DateStart).ToString("MMM-dd-yyyy HH:mm");
            }
            if (f.DateEnd != null)
            {
                DateEnd = ((DateTime)f.DateEnd).ToString("MMM-dd-yyyy HH:mm");
            }

            if(DateEnd.Length>0 && DateStart.Length>0)
            {
                Duration = ((DateTime)f.DateEnd - (DateTime)f.DateStart).ToString();
            }

            ErrorConvertingToXML = f.ErrorConvertingToXML;
            WaypointsForSet = f.SetGearPtCount;
            WaypointsForHaul = f.RetrieveGearPtCount;
            TrackpointsCount = f.TrackPtCount;
            User = f.UserName;
            Gear = f.Gear;
            LandingSite = f.LandingSite;
            Version = f.AppVersion;
            XML = f.XML;
            CTXFile = f;
            CTXFileName = f.CTXFileName;
            DeviceID = f.DeviceID;
            DownloadedFromServer = f.IsDownloadedFromServer;
            TrackingInterval = f.TrackingInterval;

        }
        public int? TrackingInterval { get; internal set; }
        public bool ErrorConvertingToXML { get; set; }
        public string Duration { get; internal set; }
        public string CTXFileName { get; internal set; }
        public string DeviceID{ get; internal set; }
        public string Version { get; internal set; }
        public string User { get;internal set; }
        public int Identifier { get; internal set; }
        public string DateStart { get; internal set; }
        public string DateEnd { get; internal set; }
        public int? WaypointsForSet { get; internal set; }
        public int? WaypointsForHaul { get; internal set; }

        public int? TrackpointsCount { get; internal set; }

        public string Gear { get; internal set; }
        public string LandingSite { get; internal set; }

        public string XML { get; internal set; }

        public CTXFile CTXFile { get; internal set; }
        public bool DownloadedFromServer { get;  set; }
    }
    public class CTXFile
    {
        public FileInfo FileInfo { get; set; }
        public RemoteFileInfo RemoteFileInfo { get; set; }

        public bool ErrorConvertingToXML { get; set; }
        public bool IsDownloaded { get; set; }

        public string CTXFileName { get; set; }

        public string UserName { get; set; }
        public string Gear { get; set; }
        public string LandingSite { get; set; }
        public DateTime? DateStart { get; set; }
        public DateTime? DateEnd { get; set; }

        public DateTime? Date
        {
            get
            {
                if(DateStart!=null)
                {
                    return DateStart;
                }
                else if(DateEnd!=null)
                {
                    return DateEnd;
                }
                else
                {
                    return null;
                }
            }
        }

        public int? TrackPtCount { get; set; }
        public DateTime? TrackTimeStampStart { get; set; }
        public DateTime? TrackTimeStampEnd { get; set; }
        public int? SetGearPtCount { get; set; }
        public int? RetrieveGearPtCount { get; set; }

        public int? NumberOfTrips { get; set; }
        public int RowID { get; set; }
        public string DeviceID { get; set; }
        public string FileName { get; set; }

        public DateTime CTXFileTimeStamp { get; set; }
        public DateTime DateAdded { get; set; }
        public string XML { get; set; }

        public int? TrackingInterval { get; set; }
        public string AppVersion { get; set; }

        public bool DownloadFile { get; set; }

        public bool TrackExtracted { get; set; }
        public bool IsDownloadedFromServer { get; set; }

        public override string ToString()
        {
            if (RemoteFileInfo != null)
            {
                return RemoteFileInfo.Name;
            }
            else
            {
                return "";
            }
        }
    }
}
