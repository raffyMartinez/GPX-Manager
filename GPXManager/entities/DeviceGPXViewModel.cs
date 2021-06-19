using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Linq;


namespace GPXManager.entities
{
    public class DeviceGPXViewModel
    {

        private bool _editSuccess;

        private bool _updateExtractedStatus = false;
        public ObservableCollection<DeviceGPX> DeviceGPXCollection { get; set; }
        private DeviceGPXRepository DeviceWaypointGPXes { get; set; }

        public DeviceGPXViewModel()
        {
            DeviceWaypointGPXes = new DeviceGPXRepository();
            DeviceGPXCollection = new ObservableCollection<DeviceGPX>(DeviceWaypointGPXes.DeviceGPXes);
            DeviceGPXCollection.CollectionChanged += DeviceWptGPXCollection_CollectionChanged;
            ConvertDeviceGPXInArchiveToGPXFile();
            GPXBackupFolder = "GPXBackup";
        }

        public List<GPSDataSummary> GetGPSDataSummaries()
        {
            var list = new List<GPSDataSummary>();
            var gpsUnits = Entities.GPSViewModel.GetAll();
            foreach (var item in Entities.GPSViewModel.GetAll())
            {
                int count500 = 0;
                int countLess500 = 0;
                var listTracks = DeviceGPXCollection
                    .Where(t => t.GPS.DeviceID == item.DeviceID && t.GPXType == "track")
                    .ToList();

                foreach (var deviceGPX in listTracks)
                {
                    foreach (var track in Entities.TrackViewModel.ReadTracksFromXML(deviceGPX))
                    {
                        if (track.Statistics.Length <= 0.5)
                        {
                            ++countLess500;
                        }
                        else
                        {
                            ++count500;
                        }
                    }

                }

                var listWpts = DeviceGPXCollection
                    .Where(t => t.GPS.DeviceID == item.DeviceID && t.GPXType == "waypoint")
                    .ToList();



                var summaryItem = new GPSDataSummary { GPS = item, NumberOfSavedTracks = listTracks.Count, NumberOfSavedWaypoints = listWpts.Count, NumberTrackLength500m = count500, NumberTrackLengthLess500m = countLess500 };


                list.Add(summaryItem);
            }

            return list;
        }

        public Dictionary<GPS, List<GPXFile>> ArchivedGPXFiles { get; private set; } = new Dictionary<GPS, List<GPXFile>>();


        /// <summary>
        /// checks if the backup location exists. It not, it will create one.
        /// </summary>
        /// <param name="folderForBackup"></param>
        public void CheckGPXBackupFolder()
        {
            string folderForBackup = Global.Settings.ComputerGPXFolder;
            if (!Directory.Exists($@"{folderForBackup}\{GPXBackupFolder}"))
            {
                Directory.CreateDirectory($@"{folderForBackup}\{GPXBackupFolder}");
            }
        }

        public List<DateTime> GetAllMonthYear(DateTime? startingDate = null)
        {
            DateTime earliest;
            if (startingDate == null)
            {
                earliest = DeviceGPXCollection.OrderBy(t => t.TimeRangeStart).First().TimeRangeStart;
            }
            else
            {
                earliest = (DateTime)startingDate;
            }
            var latest = DeviceGPXCollection.OrderByDescending(t => t.TimeRangeStart).First().TimeRangeStart;
            var dates = new List<DateTime>();

            while (earliest <= latest)
            {
                dates.Add(new DateTime(earliest.Year, earliest.Month, 1));
                earliest = earliest.AddMonths(1);
            }
            return dates;

        }

        public List<GPXFile> GetDeviceGPX(GPS gps, GPXFileType gpxType, DateTime monthYear)
        {
            return ArchivedGPXFiles[gps].Where(t => t.GPXFileType == gpxType && t.MonthYear == monthYear).ToList();
        }
        public string GPXBackupFolder { get; private set; }
        public int BackupGPXToDrive()
        {
            string folderForBackup = Global.Settings.ComputerGPXFolder;
            int count = 0;
            DirectoryInfo backupDir;
            DirectoryInfo gpsBackupDir;
            DirectoryInfo gpsBackupDirMonth;
            if (!Directory.Exists($@"{folderForBackup}\{GPXBackupFolder}"))
            {
                backupDir = Directory.CreateDirectory($@"{folderForBackup}\{GPXBackupFolder}");
            }
            else
            {
                backupDir = new DirectoryInfo(($@"{folderForBackup}\{GPXBackupFolder}"));
            }
            foreach (var gps in Entities.GPSViewModel.GPSCollection.OrderBy(t => t.DeviceName))
            {

                string gpsDirectory = $@"{backupDir.FullName}\{gps.DeviceName}";
                if (Directory.Exists(gpsDirectory))
                {
                    gpsBackupDir = new DirectoryInfo(gpsDirectory);
                }
                else
                {
                    gpsBackupDir = Directory.CreateDirectory(gpsDirectory);
                }

                if (Entities.DeviceGPXViewModel.ArchivedGPXFiles.Keys.Contains(gps))
                {
                    foreach (var gpxFile in Entities.DeviceGPXViewModel.ArchivedGPXFiles[gps])
                    {
                        var fileTimeStart = gpxFile.DateRangeStart;
                        var monthYear = new DateTime(fileTimeStart.Year, fileTimeStart.Month, 1).ToString("MMM-yyyy");
                        if (Directory.Exists($@"{gpsBackupDir.FullName}\{monthYear}"))
                        {
                            gpsBackupDirMonth = new DirectoryInfo($@"{gpsBackupDir.FullName}\{monthYear}");
                        }
                        else
                        {
                            gpsBackupDirMonth = Directory.CreateDirectory($@"{gpsBackupDir.FullName}\{monthYear}");
                        }

                        string fileToBackup = $@"{gpsBackupDirMonth.FullName}\{gpxFile.FileName}";
                        if (File.Exists(fileToBackup))
                        {
                            if (CreateMD5(gpxFile.XML) != CreateMD5(File.OpenText(fileToBackup).ReadToEnd()))
                            {
                                var pattern = (Path.GetFileNameWithoutExtension(fileToBackup) + "*.gpx");
                                var files = Directory.GetFiles(gpsBackupDirMonth.FullName, pattern);
                                string versionedFile = $@"{Path.ChangeExtension(fileToBackup, null)}_{((int)(DateTime.Now.ToOADate() * 1000)).ToString()}.gpx";
                                using (StreamWriter sw = File.CreateText(versionedFile))
                                {
                                    sw.Write(gpxFile.XML);
                                }
                            }

                        }
                        else
                        {
                            using (StreamWriter sw = File.CreateText(fileToBackup))
                            {
                                sw.Write(gpxFile.XML);
                                count++;
                            }
                        }
                    }
                }
            }
            return count;
        }

        public List<GPXFile> GetGPXFiles(List<GPS> selectedGPS, List<DateTime> selectedDates)
        {
            var list = new List<GPXFile>();
            foreach (var gps in selectedGPS)
            {
                if (ArchivedGPXFiles.Keys.Contains(gps))
                {
                    foreach (var item in ArchivedGPXFiles[gps])
                    {
                        foreach (var date in selectedDates)
                        {
                            if (item.DateRangeStart >= date && item.DateRangeStart < date.AddMonths(1))
                            {
                                list.Add(item);
                                break;
                            }
                        }
                    }
                }
            }
            return list;
        }



        public void RefreshArchivedGPXCollection(GPS gps)
        {
            ConvertDeviceGPXInArchiveToGPXFile(gps); ;
        }

        public void MarkAllNotShownInMap()
        {
            foreach (GPS gps in ArchivedGPXFiles.Keys)
            {
                foreach (GPXFile file in ArchivedGPXFiles[gps])
                {
                    file.ShownInMap = false;
                }
            }
        }
        private void ConvertDeviceGPXInArchiveToGPXFile(GPS gps = null)
        {
            if (gps == null)
            {
                foreach (var item in DeviceGPXCollection)
                {
                    //if(item.GPX=="")
                    //{
                    //    item.GPX = DeviceWaypointGPXes.getXML(item.RowID);
                    //}
                    var gpxFile = Entities.GPXFileViewModel.ConvertToGPXFile(item);
                    AddToDictionary(gpxFile.GPS, gpxFile);
                }
            }
            else
            {
                List<GPXFile> gpxFiles = new List<GPXFile>();
                if (ArchivedGPXFiles.Keys.Contains(gps) && ArchivedGPXFiles[gps].Count > 0)
                {
                    gpxFiles = ArchivedGPXFiles[gps];
                }
                foreach (var item in DeviceGPXCollection.Where(t => t.GPS.DeviceID == gps.DeviceID))
                {
                    if (item.GPX == "")
                    {
                        item.GPX = DeviceWaypointGPXes.getXML(item.RowID);
                    }
                    var gpxFile = Entities.GPXFileViewModel.ConvertToGPXFile(item);
                    if (!gpxFiles.Contains(gpxFile))
                    {
                        AddToDictionary(gpxFile.GPS, gpxFile);
                    }
                }
            }
        }

        private void AddToDictionary(GPS gps, GPXFile gpxFile)
        {
            if (!ArchivedGPXFiles.Keys.Contains(gps))
            {
                ArchivedGPXFiles.Add(gps, new List<GPXFile>());
            }
            if (ArchivedGPXFiles[gps].FirstOrDefault(t => t.FileName == gpxFile.FileName) == null)
            {
                ArchivedGPXFiles[gps].Add(gpxFile);
            }
        }

        public List<WaypointLocalTime> GetWaypoints(GPXFile wayppointFile)
        {
            if (wayppointFile.TrackCount == 0 && wayppointFile.NamedWaypointsInLocalTime.Count > 0)
            {
                return wayppointFile.NamedWaypointsInLocalTime;
            }
            return null;
        }
        public List<WaypointLocalTime> GetWaypointsMatch(GPXFile trackFile, out List<GPXFile> gpxFiles)
        {
            gpxFiles = new List<GPXFile>();
            var thisList = new List<WaypointLocalTime>();
            foreach (var g in ArchivedGPXFiles[trackFile.GPS].Where(t => t.GPXFileType == GPXFileType.Waypoint))
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

        public DeviceGPX GetDeviceGPX(int id)
        {
            var gpx = DeviceGPXCollection.Where(t => t.RowID == id).FirstOrDefault();
            if (gpx.GPX == "")
            {
                gpx.GPX = DeviceWaypointGPXes.getXML(id);
            }
            return gpx;

        }

        public DeviceGPX GetDeviceGPX(DeviceGPX deviceGPX)
        {
            var gpx = DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == deviceGPX.GPS.DeviceID)
                .Where(t => t.Filename == deviceGPX.Filename)
                .FirstOrDefault();

            if (gpx.GPX == "")
            {
                gpx.GPX = DeviceWaypointGPXes.getXML(gpx.RowID);
            }
            return gpx;
        }

        public DeviceGPX GetDeviceGPX(GPS gps, string fileName)
        {
            var gpx = DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .Where(t => t.Filename == Path.GetFileName(fileName))
                .FirstOrDefault();

            if (gpx.GPX == "")
            {
                gpx.GPX = DeviceWaypointGPXes.getXML(gpx.RowID);
            }
            return gpx;
        }

        public List<GPXFile> ArchivedFilesByGPSByMonth(GPS gps, DateTime month)
        {
            return ArchivedGPXFiles[gps]
                .Where(t => t.DateRangeStart >= month)
                .Where(t => t.DateRangeEnd <= month.AddMonths(1))
                .OrderByDescending(t => t.DateRangeStart)
                .OrderByDescending(t => t.TrackCount)
                .ToList();


        }
        public DeviceGPX GetDeviceGPX(GPS gps)
        {
            var gpx = DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .FirstOrDefault();

            if (gpx.GPX == "")
            {
                gpx.GPX = DeviceWaypointGPXes.getXML(gpx.RowID);
            }
            return gpx;
        }
        public List<GPS> GetAllGPS()
        {
            return DeviceGPXCollection.GroupBy(t => t.GPS).Select(g => g.First().GPS).OrderBy(t => t.DeviceName).ToList();
        }
        public DeviceGPX GetDeviceGPX(GPS gps, DateTime month_year)
        {
            var gpx = DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .FirstOrDefault();
            if (gpx.GPX == "")
            {
                gpx.GPX = DeviceWaypointGPXes.getXML(gpx.RowID);
            }
            return gpx;
        }

        public List<DateTime> GetMonthsInArchive(GPS gps)
        {
            return DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .OrderBy(t => t.TimeRangeStart)
                .GroupBy(t => t.TimeRangeStart.ToString("MMM-yyyy"))
                .Select(g => g.First().TimeRangeStart)
                .ToList();
        }

        public List<DeviceGPX> GetAllDeviceWaypointGPX()
        {
            var gpxes = DeviceGPXCollection.ToList();

            foreach (var gpx in gpxes)
            {
                if (gpx.GPX == "")
                {
                    gpx.GPX = DeviceWaypointGPXes.getXML(gpx.RowID);
                }
            }

            return gpxes;
        }


        public List<DeviceGPX> GetAllDeviceWaypointGPX(GPS gps)
        {
            var gpxes = DeviceGPXCollection.Where(t => t.GPS.DeviceID == gps.DeviceID).ToList();

            foreach (var gpx in gpxes)
            {
                if (gpx.GPX == "")
                {
                    gpx.GPX = DeviceWaypointGPXes.getXML(gpx.RowID);
                }
            }

            return gpxes;
        }

        public DeviceGPX CurrentEntity { get; set; }
        private void DeviceWptGPXCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _editSuccess = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        DeviceGPX newWPTGPX = DeviceGPXCollection[newIndex];
                        if (DeviceWaypointGPXes.Add(newWPTGPX))
                        {
                            CurrentEntity = newWPTGPX;
                            _editSuccess = true;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<DeviceGPX> tempListOfRemovedItems = e.OldItems.OfType<DeviceGPX>().ToList();
                        DeviceWaypointGPXes.Delete(tempListOfRemovedItems[0].RowID);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<DeviceGPX> tempList = e.NewItems.OfType<DeviceGPX>().ToList();
                        if (_updateExtractedStatus)
                        {
                            _editSuccess = DeviceWaypointGPXes.Update(tempList[0].RowID, tempList[0].TrackIsExtracted);
                            _updateExtractedStatus = false;
                        }
                        else
                        {
                            _editSuccess = DeviceWaypointGPXes.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                        }
                        
                    }
                    break;
            }
        }
        public bool ClearRepository()
        {
            return DeviceWaypointGPXes.ClearTable();
        }

        public int Count
        {
            get { return DeviceGPXCollection.Count; }
        }

        public bool AddRecordToRepo(DeviceGPX gpx)
        {
            int oldCount = DeviceGPXCollection.Count;
            if (gpx == null)
                throw new ArgumentNullException("Error: The argument is Null");
            DeviceGPXCollection.Add(gpx);
            if (_editSuccess)
            {
                GPXFile gpxFile = Entities.GPXFileViewModel.GetFile(gpx.GPS, gpx.Filename);
                if (gpxFile == null)
                {
                    gpxFile = new GPXFile(gpx.Filename) { GPS = gpx.GPS, XML = gpx.GPX };
                    gpxFile.ComputeStats(gpx);


                    Entities.GPXFileViewModel.Add(gpxFile);
                }
                AddToDictionary(gpx.GPS, gpxFile);
            }
            return DeviceGPXCollection.Count > oldCount;
        }



        public bool UpdateRecordInRepo(DeviceGPX gpx, bool updateExtractedStatus=false)
        {
            _updateExtractedStatus = updateExtractedStatus;
            if (gpx.RowID == 0)
                throw new Exception("Error: ID cannot be null");

            int index = 0;
            while (index < DeviceGPXCollection.Count)
            {
                if (DeviceGPXCollection[index].RowID == gpx.RowID)
                {
                    DeviceGPXCollection[index] = gpx;
                    break;
                }
                index++;
            }
            return _editSuccess;
        }

        private string AddGPSSourceToGPX(string gpx, GPS gps)
        {
            bool hasGPS = false;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(gpx);
            var nd = doc.GetElementsByTagName("gpx");
            foreach (XmlNode child in nd[0].ChildNodes)
            {
                hasGPS = child.Name == "gps";
                if (hasGPS)
                {
                    break;
                }
            }
            if (!hasGPS)
            {
                XmlElement gpsChild = doc.CreateElement("gps", doc.DocumentElement.NamespaceURI);
                gpsChild.InnerText = gps.DeviceID;
                nd[0].AppendChild(gpsChild);
            }
            return doc.OuterXml;

        }


        /// <summary>
        /// Saves gpx files in device to database
        /// </summary>
        /// <param name="device"></param>
        public bool SaveDeviceGPXToRepository(DetectedDevice device)
        {
            bool successSave = false;
            foreach (var file in Entities.GPXFileViewModel.GetGPXFilesFromGPS(device))
            {
                string content;
                using (StreamReader sr = File.OpenText(file.FullName))
                {
                    content = sr.ReadToEnd();
                    content = AddGPSSourceToGPX(content, device.GPS);
                    var gpxFileName = Path.GetFileName(file.FullName);
                    var dwg = GetDeviceGPX(device.GPS, gpxFileName);
                    GPXFile gpxFile = Entities.GPXFileViewModel.GetFile(device.GPS, gpxFileName);
                    var gpxType = gpxFile.GPXFileType == GPXFileType.Track ? "track" : "waypoint";
                    if (dwg == null)
                    {
                        successSave = AddRecordToRepo(
                            new DeviceGPX
                            {
                                GPS = device.GPS,
                                Filename = gpxFileName,
                                GPX = content,
                                RowID = NextRecordNumber,
                                MD5 = CreateMD5(content),
                                GPXType = gpxType,
                                TimeRangeStart = gpxFile.DateRangeStart,
                                TimeRangeEnd = gpxFile.DateRangeEnd
                            }
                        );
                    }
                    else
                    {
                        var deviceMD5 = CreateMD5(content);
                        if (CreateMD5(dwg.GPX) != deviceMD5)
                        {
                            successSave = UpdateRecordInRepo(new DeviceGPX
                            {
                                GPS = dwg.GPS,
                                GPX = content,
                                Filename = dwg.Filename,
                                RowID = dwg.RowID,
                                MD5 = deviceMD5,
                                GPXType = gpxType,
                                TimeRangeStart = gpxFile.DateRangeStart,
                                TimeRangeEnd = gpxFile.DateRangeEnd
                            });
                        }
                    }
                }
            }
            return successSave;
        }
        public void SaveDeviceGPXToRepository()
        {
            foreach (var device in Entities.DetectedDeviceViewModel.DetectedDeviceCollection)
            {
                if (device.GPS != null)
                {
                    SaveDeviceGPXToRepository(device);
                }
            }
        }
        public string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        public int NextRecordNumber
        {
            get
            {
                if (DeviceGPXCollection.Count == 0)
                {
                    return 1;
                }
                else
                {
                    return DeviceWaypointGPXes.MaxRecordNumber() + 1;
                }
            }
        }
        public void DeleteRecordFromRepo(int rowID)
        {
            if (rowID == 0)
                throw new Exception("Record ID cannot be null");

            int index = 0;
            while (index < DeviceGPXCollection.Count)
            {
                if (DeviceGPXCollection[index].RowID == rowID)
                {
                    DeviceGPXCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
