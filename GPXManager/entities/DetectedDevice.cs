using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class DetectedDevice
    {
        public GPS GPS { get; set; }
        public string[] CapabilityDescriptions { get; set; }
        public string Caption { get; set; }
        public string DeviceID { get; set; }
        public List<Disk> Disks { get; set; }
        public int Index { get; set; }
        public List<string> Drives { get; set; }
        public int BytesPerSector { get; set; }
        public string CreationClassName {get;set;}
        public string Description { get; set; }
        public string InterfaceType { get; set; }
        public bool MediaLoaded { get; set; }
        public string MediaType { get; set; }
        public string Model { get; set; }

        public string Name { get; set; }

        public int Partitions { get; set; }

        public string Signature { get; set; }

        public long Size { get; set; }

        public string Status { get; set; }
        public int TotalCylinders { get; set; }
        public int TotalHeads { get; set; }
        public int TotalSectors { get; set; }
        public int TotalTracks { get; set; }

        public string GPSID { get; set; }

        public int TracksPerCylinder { get; set; }

        public int SectorsPerTrack { get; set; }

        public string PNPDeviceID { get; set; }
        public string SerialNumber { get; set; }

        public string DriveSummary 
        { 
            get
            {
                var diskSize = "";

                if (Disks.Count == 1)
                {
                    var disk = Disks[0];
                    diskSize = FileSizeFormatter.FormatSize(disk.Size);
                    return $"Drive: {disk.Caption}\r\n" +
                        $"Volume: {disk.VolumeName}\r\n" +
                        $"Size: {diskSize}\r\n" +
                        $"FileSystem: {disk.FileSystem}\r\n" +
                        $"Serial number: {SerialNumber}\r\n" +
                        $"PNP DeviceID: {PNPDeviceID}\r\n" +
                        $"GPSID: {GPSID}";
                }
                else
                {
                    var summary = "";
                    int count = 1;
                    foreach(Disk d in Disks)
                    {
                        diskSize = FileSizeFormatter.FormatSize(d.Size);
                        //summary += $"Disk:{count}\r\n Drive: {d.Caption}\r\nVolume: {d.VolumeName}\r\nSize: {diskSize}\r\nFileSystem: {d.FileSystem}\r\n\r\n";
                        summary += $"Drive: {d.Caption}\r\n" +
                                    $"Volume: {d.VolumeName}\r\n" +
                                    $"Size: {diskSize}\r\n" +
                                    $"FileSystem: {d.FileSystem}\r\n+" +
                                    $"Serial number: {SerialNumber}\r\n" +
                                    $"PNP DeviceID: {PNPDeviceID}\r\n" +
                                    $"GPSID: {GPSID}";
                        count++;
                    }
                    return summary;
                }
            }
        }
    }
}
