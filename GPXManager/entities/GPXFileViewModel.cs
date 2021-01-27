using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using MapWinGIS;
using Xceed.Wpf.Toolkit;

namespace GPXManager.entities
{
    public class GPXFileViewModel
    {
        private List<GPS> _gpsFinishedReadingFiles = new List<GPS>();
        public ObservableCollection<GPXFile> GPXFileCollection { get; set; }
        public GPXFileViewModel()
        {
            GPXFileCollection = new ObservableCollection<GPXFile>();
            GPXFileCollection.CollectionChanged += GPXFileCollection_CollectionChanged;
        }

        public GPXFile ConvertToGPXFile(DeviceGPX deviceGPX )
        {
            GPXFile gpxFile = new GPXFile(deviceGPX.Filename);
            gpxFile.GPS = deviceGPX.GPS;
            gpxFile.ComputeStats(deviceGPX);
            gpxFile.XML = deviceGPX.GPX;
            return gpxFile;
        }
        public Dictionary<DateTime,List<GPXFile>>FilesByMonth(GPS gps)
        {
            return GPXFileCollection
                .Where(g=>g.GPS.DeviceID==gps.DeviceID)
                .OrderBy(m=>m.DateRangeStart)
                .GroupBy(o => o.MonthYear)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        public void MarkAllNotShownInMap()
        {
            //foreach (var item in GPXFileCollection.Where(t => t.ShownInMap))
            foreach (var item in GPXFileCollection)
            {
                item.ShownInMap = false;
            }
        }

        public List<WaypointLocalTime> GetWaypointsMatch(GPXFile trackFile, out List<GPXFile> gpxFiles)
        {
            gpxFiles = new List<GPXFile>();
            var thisList = new List<WaypointLocalTime>();
            foreach (var g in GPXFileCollection
                .Where(t => t.GPXFileType == GPXFileType.Waypoint)
                .Where(t=>t.GPS.DeviceID==trackFile.GPS.DeviceID)
                )
            {
                foreach (var wpt in g.NamedWaypointsInLocalTime)
                {
                    if (wpt.Time >= trackFile.DateRangeStart && wpt.Time <= trackFile.DateRangeEnd)
                    {
                        thisList.Add(wpt);
                        if (!gpxFiles.Contains(g))
                        {
                            gpxFiles.Add(g);
                        }
                    }
                }
            }

            return thisList;
        }
        public List<GPXFile>GetFiles(string deviceID)
        {
            GetFilesFromDevice(Entities.DetectedDeviceViewModel.GetDevice(deviceID));
            return GPXFileCollection.Where(t => t.GPS.DeviceID == deviceID).ToList();
        }

        public GPXFile GetFile (GPS gps, string fileName)
        {
            return GPXFileCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .Where(t => t.FileName == fileName)
                .FirstOrDefault();
        }

        public List<GPXFile> GetFilesEx(string gpsID, DateTime monthYear)
        {
            GetFilesFromDevice(Entities.DetectedDeviceViewModel.GetDevice(gpsID));
            return GPXFileCollection
                .Where(t => t.GPS.DeviceID == gpsID)
                .Where(t => t.DateRangeStart > monthYear)
                .Where(t => t.DateRangeEnd < monthYear.AddMonths(1))
                .ToList();
            //return thisList;
        }
        public List<GPXFile> GetFiles(string deviceID, DateTime monthYear)
        {
            GetFilesFromDevice(Entities.DetectedDeviceViewModel.GetDevice(deviceID));
            return GPXFileCollection
                .Where(t => t.GPS.DeviceID == deviceID)
                .Where(t => t.DateRangeStart > monthYear)
                .Where(t => t.DateRangeEnd < monthYear.AddMonths(1))
                .ToList();
            //return thisList;
        }

        public void Clear()
        {
            GPXFileCollection = new ObservableCollection<GPXFile>();
        }
        public bool Contains(GPXFile file)
        {
            return GPXFileCollection
               .Where(t => t.GPS.DeviceID == file.GPS.DeviceID)
               .Where(t => t.FileName == file.FileName).FirstOrDefault() != null;
        }
        public GPXFile CurrentEntity { get; set; }
        private void GPXFileCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        GPXFile newFile = GPXFileCollection[newIndex];
                        CurrentEntity = newFile;

                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        //List<GPS> tempListOfRemovedItems = e.OldItems.OfType<GPS>().ToList();
                        //GPSes.Delete(tempListOfRemovedItems[0].Code);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        //List<GPS> tempList = e.NewItems.OfType<GPS>().ToList();
                        //GPSes.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }
        public int Count
        {
            get { return GPXFileCollection.Count; }
        }

        public List<GPXFile>LatestTrackFileUsingGPS(GPS gps, int latestCount=5)
        {
           return Entities.GPXFileViewModel.GetFiles(gps.DeviceID)
                .Where(t => t.TrackCount > 0)
                .OrderByDescending(t => t.DateRangeStart)
                .Take(latestCount)
                .ToList();
        }

        public List<GPXFile> LatestWaypointFileUsingGPS(GPS gps, int latestCount = 5)
        {
            return Entities.GPXFileViewModel.GetFiles(gps.DeviceID)
                 .Where(t => t.WaypointCount > 0)
                 .OrderByDescending(t => t.DateRangeStart)
                 .Take(latestCount)
                 .ToList();
        }
        public List<FileInfo>GetGPXFilesFromGPS(DetectedDevice device)
        {
            if (device.GPS != null)
            {
                var folder = $@"{device.Disks[0].Caption}\{device.GPS.Folder}";
                if (Directory.Exists(folder))
                {
                    return new DirectoryInfo(folder)
                      .EnumerateFiles()
                      .Where(f => f.Extension == ".gpx").ToList();
                }
            }
            return null;
        }

        public void GetFilesFromDevice(DetectedDevice device)
        {
            if (!_gpsFinishedReadingFiles.Contains(device.GPS))
            {
                var gpxFolder = $"{device.Disks[0].Caption }\\{ device.GPS.Folder}";
                if (Directory.Exists(gpxFolder))
                {
                    List<FileInfo> myFiles = new DirectoryInfo(gpxFolder)
                        .EnumerateFiles()
                        .Where(f => f.Extension == ".gpx").ToList();


                    foreach (var file in myFiles)
                    {
                        string xml = File.OpenText(file.FullName).ReadToEnd();
                        DeviceGPX dg = new DeviceGPX { GPX = xml, Filename = file.Name,GPS = device.GPS };
                        Entities.WaypointViewModel.ReadWaypointsFromFile(dg);
                        GPXFile gf = new GPXFile(file)
                        {
                            GPS = device.GPS,
                            DriveName = device.Disks[0].Caption,
                            XML = xml
                        };


                        if (!Contains(gf))
                        {
                            Add(gf);
                            gf.ComputeStats();
                        }
                    }
                }
                _gpsFinishedReadingFiles.Add(device.GPS);
            }
        }
        public void Add(GPXFile file)
        {
            if (file == null)
                throw new ArgumentNullException("Error: The argument is Null");
            GPXFileCollection.Add(file);
        }

        public void Update(GPXFile file)
        {
            if (file.FileName == null)
                throw new Exception("Error: File name cannot be null");

            int index = 0;
            while (index < GPXFileCollection.Count)
            {
                if (GPXFileCollection[index].FileName == file.FileName)
                {
                    GPXFileCollection[index] = file;
                    break;
                }
                index++;
            }
        }

        public void Delete(string fileName)
        {
            if (fileName == null)
                throw new Exception("File name cannot be null");

            int index = 0;
            while (index < GPXFileCollection.Count)
            {
                if (GPXFileCollection[index].FileName == fileName)
                {
                    GPXFileCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
