using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WinSCP;
using GPXManager.entities.mapping;

namespace GPXManager.entities
{
    public class CTXFileViewModel
    {
        private static Dictionary<string, string> _ctxDictionary = new Dictionary<string, string>();
        private bool _editSucceeded;

        public delegate void XMLFileFromImportedCTX(CTXFileViewModel s, CTXFileImportEventArgs e);
        public event XMLFileFromImportedCTX XMLFileFromImportedCTXCreated;

        public delegate void XMLReviewed(CTXFileViewModel s, CTXFileImportEventArgs e);
        public event XMLReviewed XMLofCTXReviewed;

        public delegate void XMLFileFromCTX(CTXFileViewModel s, TransferEventArgs e);
        public event XMLFileFromCTX XMLFileFromCTXCreated;

        private Dictionary<string, string> _sightingAttributesDictionary = new Dictionary<string, string>();
        public ObservableCollection<CTXFile> CTXFileCollection { get; set; }
        private CTXFileRepository ctxFileRepo { get; set; }
        public CTXFileViewModel()
        {
            ctxFileRepo = new CTXFileRepository();
            CTXFileCollection = new ObservableCollection<CTXFile>(ctxFileRepo.CTXFiles);
            var c = CTXFileCollection[CTXFileCollection.Count - 1];
            CTXFileCollection.CollectionChanged += CTXFileCollection_CollectionChanged;

            _intervals.Add(30);
            _intervals.Add(60);
            _intervals.Add(120);
            _intervals.Add(180);
            _intervals.Add(300);
            //_intervals.Add(30);
        }
        private List<int> _intervals = new List<int>();


        public List<DateTime> getCTXUserHistoryMonths(string userName)
        {
            HashSet<DateTime> userHistory = new HashSet<DateTime>();
            foreach (var item in CTXFileCollection.Where(t => t.UserName == userName).OrderBy(t => t.DateStart))
            {
                DateTime? monthYear = null;
                if (item.DateStart != null)
                {
                    monthYear = (DateTime)item.DateStart;

                }
                else if (item.DateEnd != null)
                {
                    monthYear = (DateTime)item.DateEnd;
                }

                if (monthYear != null)
                {
                    userHistory.Add(new DateTime(((DateTime)monthYear).Year, ((DateTime)monthYear).Month, 1));
                }
            }
            return userHistory.ToList();
        }

        public async Task<List<CTXFile>> GetAllAsync(
            bool checkXML = false,
            bool excludeExtracted = false,
            bool refreshReadTrack = false
            ) => await Task.Run(() => GetAll(checkXML, excludeExtracted, refreshReadTrack));
        private List<CTXFile> GetAll(
            bool checkXML = false,
            bool excludeExtracted = false,
            bool refreshReadTrack = false
            )
        {
            var list = new List<CTXFile>();
            if (checkXML)
            {
                list = CTXFileCollection.OrderBy(t => t.DateStart).ToList();
                foreach (var ctx in list)
                {
                    if (ctx.XML == "")
                    {
                        ctx.XML = GetXMLOfCTX(ctx);
                    }
                }
                //return list;
            }
            else
            {
                list = CTXFileCollection.OrderBy(t => t.DateStart).ToList();
                //return CTXFileCollection.OrderBy(t => t.DateStart).ToList();
            }

            if (refreshReadTrack) excludeExtracted = false;

            if (excludeExtracted)
            {
                return list.Where(t => t.TrackExtracted = false).ToList();
            }
            else
            {
                return list;
            }
        }
        public CTXFile GetFile(int id, bool getXML = true)
        {
            var ctx = CTXFileCollection.FirstOrDefault(t => t.RowID == id);
            if (ctx != null && getXML && ctx.XML == "")
            {
                ctx.XML = GetXMLOfCTX(ctx);
            }
            return ctx;

        }
        public CTXFile GetFile(string ctxFileName)
        {
            var ctx = CTXFileCollection.FirstOrDefault(t => t.CTXFileName == ctxFileName);
            if (ctx.XML == "")
            {
                ctx.XML = GetXMLOfCTX(ctx);
            }
            return ctx;
        }
        public CTXFile CurrentEntity { get; set; }

        private void CTXFileCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _editSucceeded = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        CTXFile newItem = CTXFileCollection[newIndex];
                        if (ctxFileRepo.Add(newItem))
                        {
                            CurrentEntity = newItem;
                            _editSucceeded = true;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        List<CTXFile> tempListOfRemovedItems = e.OldItems.OfType<CTXFile>().ToList();
                        _editSucceeded = ctxFileRepo.Delete(tempListOfRemovedItems[0].RowID);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        List<CTXFile> tempList = e.NewItems.OfType<CTXFile>().ToList();
                        _editSucceeded = ctxFileRepo.Update(tempList[0]);
                    }
                    break;
            }
        }

        public string LastRepositoryError()
        {
            return ctxFileRepo.LastError;
        }



        public List<CTXFileSummaryView> GetCTXFilesSummaryView(DateTime downloadDate)
        {
            List<CTXFileSummaryView> files = new List<CTXFileSummaryView>();
            foreach (var item in CTXFileCollection
                .Where(t => t.DateAdded > downloadDate && t.DateAdded < downloadDate.AddDays(1))
                .OrderBy(t => t.DateAdded))
            {
                files.Add(new CTXFileSummaryView(item));
            }
            return files;
        }
        public List<string> GetCTXDownloadMonthYear()
        {
            HashSet<string> list = new HashSet<string>();
            foreach (var item in CTXFileCollection.OrderBy(t => t.DateAdded))
            {
                list.Add(item.DateAdded.ToString("MMM-dd-yyyy"));
            }
            return list.ToList();
        }
        public List<string> GetUserNames()
        {
            var grouped = CTXFileCollection
                .OrderBy(t => t.UserName)
                .Where(t => t.UserName != null && t.UserName.Length > 0)
                .GroupBy(t => t.UserName)
                .Select(g => new { user = g.Key });

            var list = new List<string>();
            foreach (var item in grouped)
            {
                list.Add(item.user);
            }
            return list;
        }
        public List<CTXFile> FilesInServer { get; private set; }

        public List<CTXFile> FilesForImporting { get; private set; }

        public async Task<bool> GetFileListInServerAsync(string url, string user, string pwd)
        {
            return await Task.Run(() => GetFileListInServer(url, user, pwd));
        }
        private bool GetFileListInServer(string url, string user, string pwd)
        {
            try
            {
                var files = ctxFileRepo.GetServerContent(url, user, pwd);
                if (files != null)
                {
                    FilesInServer = files.OrderBy(t => t.CTXFileTimeStamp).ToList();
                    return files != null && files.Count > 0;
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public bool Exists(string ctxFileName)
        {
            return CTXFileCollection.FirstOrDefault(t => t.CTXFileName == ctxFileName) != null;
        }

        public int GetGPSTimerIntervalFromCTX(CTXFile ctxfile, bool? saveToDatabase)
        {
            if (ctxfile.XML == null || ctxfile.XML.Length == 0)
            {
                return -1;
            }
            else
            {

                ctxfile.TrackingInterval = GetGPSTimerIntervalFromCTX(ctxfile.XML);

                if (saveToDatabase == null)
                {
                    return (int)ctxfile.TrackingInterval;
                }
                else if ((bool)saveToDatabase && UpdateRecordInRepo(ctxfile))
                {
                    return (int)ctxfile.TrackingInterval;
                }
                return -1;
            }
        }

        public List<(int, double)> IntervalGroup { get; internal set; }
        public string GetXMLOfCTX(CTXFile ctx)
        {
            return GetXML(ctx.RowID);
        }

        public async Task<int> ReviewXMLAsync()
        {
            return await Task.Run(() => ReviewXML());
        }
        public int CountCTXFileWithNoXML()
        {
            return CTXFileCollection.Count(t => t.XML.Length == 0);
        }
        public int ReviewXML()
        {
            List<CTXFile> reviewedFiles = new List<CTXFile>();
            foreach (var item in CTXFileCollection)
            {
                if (item.XML.Length == 0)
                {
                    var binaryCTX = $@"{Global.Settings.CTXDownloadFolder}\{item.CTXFileName}";
                    if (!File.Exists(binaryCTX))
                    {
                        binaryCTX = $@"{Global.Settings.CTXBackupFolder}\{item.CTXFileName}";
                    }

                    if (File.Exists(binaryCTX))
                    {
                        if (CTXFileRepository.ExtractXMLFromCTX(binaryCTX))
                        {
                            if (File.Exists($@"{binaryCTX}.xml"))
                            {
                                item.XML = File.ReadAllText($@"{binaryCTX}.xml");
                                if (item.XML.Length > 0)
                                {
                                    reviewedFiles.Add(item);
                                    if (XMLofCTXReviewed != null)
                                    {
                                        XMLofCTXReviewed(this, new CTXFileImportEventArgs { XMLReviewedCount = reviewedFiles.Count, Context = "reviewing" });
                                    }
                                }

                            }
                        }

                    }

                }

            }

            int counter = 0;
            foreach (var c in reviewedFiles)
            {
                if (Entities.CTXFileViewModel.UpdateRecordInRepo(c))
                {
                    counter++;
                    if (XMLofCTXReviewed != null)
                    {
                        XMLofCTXReviewed(this, new CTXFileImportEventArgs { XMLReviewdSaveCount = counter, Context = "saving" });
                    }
                }
            }

            if (XMLofCTXReviewed != null)
            {
                XMLofCTXReviewed(this, new CTXFileImportEventArgs { Context = "done" });
            }

            return counter;
        }
        public string GetXML(int RowID)
        {
            return ctxFileRepo.getXML(RowID);
        }

        public List<double> DurationList { get; internal set; } = new List<double>();
        public int GetGPSTimerIntervalFromCTX(string xml)
        {
            DurationList.Clear();
            if (xml.Length == 0)
            {
                return -1;
            }
            else
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                var tracknodes = doc.SelectNodes("//T");
                if (tracknodes.Count < 2)
                {
                    return 0;
                }
                else
                {

                    int counter = 0;
                    DateTime wptDateTime2;
                    DateTime wptDateTime1 = DateTime.Now;
                    foreach (XmlNode node in tracknodes)
                    {

                        var wptDate = node.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                        var wptTime = node.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                        if (counter > 0)
                        {
                            wptDateTime2 = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;
                            var interval = (wptDateTime2 - wptDateTime1);
                            DurationList.Add((wptDateTime2 - wptDateTime1).TotalSeconds);
                        }
                        wptDateTime1 = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;
                        counter++;
                    }

                    var query = from r in DurationList
                                group r by r into g
                                select new { Count = g.Count(), Value = g.Key };


                    var top = query.OrderByDescending(t => t.Count).First();

                    foreach (int intv in _intervals)
                    {

                        if (Math.Abs(intv - top.Value) < 5)
                        {
                            return intv;
                        }

                    }
                }
            }
            return 0;
        }
        public async Task ImportCTXFilesAsync(List<CTXFile> filesToDownload, string importLocation)
        {
            LastDownloadedCTXFiles.Clear();
            await Task.Run(() => ImportCTXFiles(filesToDownload, importLocation));
        }
        private void ImportCTXFiles(List<CTXFile> filesToImport, string importLocation)
        {
            ctxFileRepo.XMLFileFromImportedCTXCreated += CtxFileRepo_XMLFileFromImportedCTXCreated;
            ctxFileRepo.ImportCTXFIles(filesToImport, importLocation);
            ctxFileRepo.XMLFileFromImportedCTXCreated -= CtxFileRepo_XMLFileFromImportedCTXCreated;
        }



        public async Task DownloadFromServerAsync(List<CTXFile> filesToDownload, string downloadlocation)
        {
            await Task.Run(() => DownloadFromServer(filesToDownload, downloadlocation));
        }
        public List<CTXFile> LastDownloadedCTXFiles { get; private set; } = new List<CTXFile>();
        private void DownloadFromServer(List<CTXFile> filesToDownload, string downloadlocation)
        {
            ctxFileRepo.XMLFileFromDownloadedCTXCreated += CtxFileRepo_XMLFileFromCTXCreated;
            ctxFileRepo.DownloadFromServer(filesToDownload, downloadlocation);

            ctxFileRepo.XMLFileFromDownloadedCTXCreated -= CtxFileRepo_XMLFileFromCTXCreated;
            //CopyCTXToBackupFolder();
        }

        public int NextRecordNumber
        {
            get
            {
                if (CTXFileCollection.Count == 0)
                {
                    return 1;
                }
                else
                {
                    return ctxFileRepo.MaxRecordNumber() + 1;
                }
            }
        }

        public void CopyCTXToBackupFolder()
        {
            if (Directory.Exists(Global.Settings.CTXBackupFolder))
            {
            }
        }
        private static string setWaypointKey { get; set; }
        private static string haulWaypointKey { get; set; }
        private static void FillCTXDictionary(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNodeList elements = doc.SelectNodes("//E");
            foreach (XmlNode n in elements)
            {
                var key = n.Attributes["I"].Value;
                var val = n.Attributes["N"].Value;
                _ctxDictionary.Add(key, val);
            }
            setWaypointKey = _ctxDictionary.FirstOrDefault(x => x.Value == "Set gear").Key;
            haulWaypointKey = _ctxDictionary.FirstOrDefault(x => x.Value == "Retrieve gear").Key;
        }

        public List<TrackWaypoint> TrackWaypointsFromTrip(int id)
        {
            List<TrackWaypoint> list = new List<TrackWaypoint>();
            string xml = CTXFileCollection.FirstOrDefault(t => t.RowID == id)?.XML;
            if (xml != null)
            {
                Waypoint wptBefore = null;
                DateTime timeBefore = DateTime.Now;
                TrackWaypoint tw = null;
                if (xml != null && xml.Length > 0)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);
                    var tracknodes = doc.SelectNodes("//T");

                    double lat = 0;
                    double lon = 0;
                    int counter = 1;
                    foreach (XmlNode node in tracknodes)
                    {

                        lat = double.Parse(node.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                        lon = double.Parse(node.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                        var wptDate = node.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                        var wptTime = node.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                        var dateTime = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;

                        if (counter > 1)
                        {
                            var wptNow = new Waypoint { Longitude = lon, Latitude = lat, Name = counter.ToString(), Elevation = 0, Time = dateTime };
                            double elevChange;
                            double distance = Waypoint.ComputeDistance(wptBefore, wptNow, out elevChange);
                            TimeSpan timeElapsed = dateTime - timeBefore;
                            double speed = distance / timeElapsed.TotalMinutes;
                            tw = new TrackWaypoint { Speed = speed, Distance = distance, Waypoint = wptNow };
                        }

                        wptBefore = new Waypoint { Longitude = lon, Latitude = lat, Name = counter.ToString(), Elevation = 0, Time = dateTime };
                        timeBefore = dateTime;

                        if (counter == 1)
                        {
                            tw = new TrackWaypoint { Speed = 0, Distance = 0, Waypoint = wptBefore };
                        }

                        list.Add(tw);
                        counter++;
                    }
                    return list;
                }
            }
            return null;
        }
        public List<GearWaypoint> GearWaypointsFromTrip(int id)
        {
            string xml = CTXFileCollection.FirstOrDefault(t => t.RowID == id)?.XML;
            if (xml != null)
            {
                List<GearWaypoint> list = new List<GearWaypoint>();
                if (xml != null && xml.Length > 0)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);
                    var wptNodes = doc.SelectNodes("//A[@N='Waypoint name']");
                    if (_ctxDictionary.Count == 0)
                    {
                        FillCTXDictionary(xml);
                    }

                    foreach (XmlNode nd in wptNodes)
                    {

                        string wptType = "";
                        string name = nd.ParentNode.SelectSingleNode(".//A[@N='Waypoint name']").Attributes["V"].Value;
                        double? lat = null; double? lon = null;
                        if (nd.ParentNode.SelectSingleNode(".//A[@N='Latitude']") != null)
                        {
                            lat = double.Parse(nd.ParentNode.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                        }
                        if (nd.ParentNode.SelectSingleNode(".//A[@N='Longitude']") != null)
                        {
                            lon = double.Parse(nd.ParentNode.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                        }
                        if (lat != null || lon != null)
                        {
                            var pt_type = nd.ParentNode.SelectSingleNode(".//A[@N='WaypointType']").Attributes["V"].Value;
                            if (pt_type == setWaypointKey)
                            {
                                wptType = "Setting";
                            }
                            else
                            {
                                wptType = "Hauling";
                            }
                            var wptDate = nd.ParentNode.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                            var wptTime = nd.ParentNode.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                            var dateTime = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;
                            var wpt = new Waypoint { Latitude = (double)lat, Longitude = (double)lon, Name = name, Time = dateTime };
                            list.Add(new GearWaypoint { WaypointType = wptType, Waypoint = wpt });
                        }
                        else
                        {

                        }
                    }
                    return list;
                }
            }
            return null;
        }

        public List<CTXFileSummaryView> TripsOfUserByMonth(string monthYear, string userName)
        {
            var list = new List<CTXFileSummaryView>();

            foreach (var item in CTXFileCollection.Where(t => t.UserName == userName &&
                t.Date != null &&
                t.Date >= DateTime.Parse(monthYear) && t.Date < DateTime.Parse(monthYear).AddMonths(1))
                .OrderByDescending(t => t.Date).ToList())
            {
                list.Add(new CTXFileSummaryView(item));
            }

            return list;
        }
        public List<CTXFileSummaryView> LatestTripsOfUser(string userName)
        {
            var list = new List<CTXFileSummaryView>();
            if (Global.Settings.LatestTripCount != null)
            {
                foreach (var item in CTXFileCollection.Where(t => t.UserName == userName).OrderByDescending(t => t.DateStart).Take((int)Global.Settings.LatestTripCount).ToList())
                {
                    list.Add(new CTXFileSummaryView(item));
                }
            }
            else
            {
                foreach (var item in CTXFileCollection.Where(t => t.UserName == userName).OrderByDescending(t => t.DateStart).Take(5).ToList())
                {
                    list.Add(new CTXFileSummaryView(item));
                }
            }
            return list;
        }
        public List<CTXUserSummary> GetUserSummary()
        {
            var userSummaries = new List<CTXUserSummary>();
            var grouped = CTXFileCollection
                .OrderBy(t => t.UserName)
                .Where(t => t.UserName != null && t.UserName.Length > 0)
                .GroupBy(t => t.UserName)
                .Select(g => new { user = g.Key });

            foreach (var item in grouped)
            {
                var list = CTXFileCollection.Where(t => t.UserName == item.user).ToList(); ;

                try
                {
                    CTXUserSummary summ = new CTXUserSummary
                    {
                        DateOfFirstTrip = (DateTime)list.Min(t => t.DateStart).Value,
                        DateOfLatestTrip = (DateTime)list.Max(t => t.DateStart).Value,
                        User = item.user,
                        TotalNumberOfTrips = list.Count
                    };
                    userSummaries.Add(summ);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    return null;
                }
            }

            return userSummaries;
        }

        private SightingAttributes GetAttributes(string xml)
        {
            Dictionary<string, string> sightingAttributesDictionary = new Dictionary<string, string>();
            SightingAttributes sa = new SightingAttributes();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);


            XmlNodeList elements = doc.SelectNodes("//E");
            foreach (XmlNode n in elements)
            {
                var key = n.Attributes["I"].Value;
                var val = n.Attributes["N"].Value;
                sightingAttributesDictionary.Add(key, val);
            }


            XmlNodeList sightings = doc.SelectNodes("//S");
            if (sightings.Count > 0)
            {
                foreach (XmlNode s in sightings)
                {
                    if (sa.DeviceID == null || sa.DeviceID.Length == 0)
                    {
                        sa.DeviceID = s.SelectSingleNode(".//A[@N='DeviceId']").Attributes["V"].Value;
                    }
                    if (sa.User == null || sa.User.Length == 0)
                    {
                        if (s.SelectSingleNode(".//A[@N='SelectedUser']") != null)
                        {
                            sa.User = sightingAttributesDictionary[s.SelectSingleNode(".//A[@N='SelectedUser']").Attributes["V"].Value];
                        }
                        //sa.User = sightingAttributesDictionary[s.ParentNode.SelectSingleNode(".//A[@N='SelectedUser']").Attributes["V"].Value];
                    }
                    if (sa.LandingSite == null || sa.LandingSite.Length == 0)
                    {
                        if (s.SelectSingleNode(".//A[@N='SelectedLandingSite']") != null)
                        {
                            sa.LandingSite = sightingAttributesDictionary[s.SelectSingleNode(".//A[@N='SelectedLandingSite']").Attributes["V"].Value];
                        }
                        //sa.LandingSite = sightingAttributesDictionary[s.ParentNode.SelectSingleNode(".//A[@N='SelectedLandingSite']").Attributes["V"].Value];
                    }
                    if (sa.Gear == null || sa.Gear.Length == 0)
                    {
                        if (s.SelectSingleNode(".//A[@N='Selected gear']") != null)
                        {
                            sa.Gear = sightingAttributesDictionary[s.SelectSingleNode(".//A[@N='Selected gear']").Attributes["V"].Value];
                        }
                    }
                }



                if (sightings[0].SelectSingleNode(".//A[@N='AppVersion']") != null)
                {
                    sa.AppVersion = sightings[0].SelectSingleNode(".//A[@N='AppVersion']").Attributes["V"].Value;
                }
                else
                {
                    sa.AppVersion = "";
                }

                if (sa.LandingSite != null && sa.LandingSite.Contains("Other") && doc.SelectSingleNode("//A[@N='OtherLandingSite']") != null)
                {
                    sa.LandingSite = doc.SelectSingleNode("//A[@N='OtherLandingSite']").Attributes["V"].Value;
                }

                if (sa.Gear != null && sa.Gear.Contains("Other") && doc.SelectSingleNode("//A[@N='OtherGear']") != null)
                {

                    sa.Gear = doc.SelectSingleNode("//A[@N='OtherGear']").Attributes["V"].Value;
                }

                if (sa.User != null && sa.User.Contains("Other") && doc.SelectSingleNode("//A[@N='OtherUser']") != null)
                {
                    sa.User = doc.SelectSingleNode("//A[@N='OtherUser']").Attributes["V"].Value;
                }


                var departureKey = sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Depart landing site").Key;
                var returnKey = sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Return to landing site").Key;

                var departures = doc.SelectNodes($"//A[@V='{departureKey}']");
                var returnHomes = doc.SelectNodes($"//A[@V='{returnKey}']");

                if (departures != null)
                {
                    string departureDate = "";
                    string departureDTime = "";
                    string returnDate = "";
                    string returnTime = "";
                    DateTime? departureDateTime = null;
                    DateTime? returnDateTime = null;
                    int y = 0;
                    foreach (XmlNode d in departures)
                    {
                        if (d.SelectSingleNode("//A[@N='Date']") != null)
                        {
                            departureDate = d.SelectSingleNode("//A[@N='Date']").Attributes["V"].Value;

                            if (d.SelectSingleNode("//A[@N='Time']") != null)
                            {
                                departureDTime = d.SelectSingleNode("//A[@N='Time']").Attributes["V"].Value;
                                if (departureDate.Length > 0 && departureDTime.Length > 0)
                                {
                                    departureDateTime = DateTime.Parse(departureDate) + DateTime.Parse(departureDTime).TimeOfDay;
                                }
                            }
                            if (y == 0)
                            {
                                sa.Start = departureDateTime;
                            }
                            else
                            {
                                if (departureDateTime > sa.Start)
                                {
                                    sa.Start = departureDateTime;
                                }
                            }
                            y++;
                        }
                    }

                    y = 0;
                    foreach (XmlNode d in returnHomes)
                    {
                        if (d.SelectSingleNode("//A[@N='Date']") != null)
                        {
                            returnDate = d.SelectSingleNode("//A[@N='Date']").Attributes["V"].Value;

                            if (d.SelectSingleNode("//A[@N='Time']") != null)
                            {
                                returnTime = d.SelectSingleNode("//A[@N='Time']").Attributes["V"].Value;
                                if (returnDate.Length > 0 && returnTime.Length > 0)
                                {
                                    returnDateTime = DateTime.Parse(returnDate) + DateTime.Parse(returnTime).TimeOfDay;
                                }
                            }
                            if (y == 0)
                            {
                                sa.End = returnDateTime;
                            }
                            else
                            {
                                if (returnDateTime > sa.End)
                                {
                                    sa.End = returnDateTime;
                                }
                            }
                            y++;
                        }
                    }





                    sa.NumberOfTrips = doc.SelectNodes($"//A[@V='{departureKey}']").Count;

                    var setGearActionKey = sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Set gear").Key;
                    var retrieGearActionKey = sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Retrieve gear").Key;

                    sa.SetGearPointCount = doc.SelectNodes($"//A[@V='{setGearActionKey}']").Count;
                    sa.RetrieveGearPointCount = doc.SelectNodes($"//A[@V='{retrieGearActionKey}']").Count;
                }
            }

            var tracknodes = doc.SelectNodes("//T");
            sa.TrackPointCount = tracknodes.Count;
            if (sa.TrackPointCount > 0)
            {
                string ptDate = tracknodes[0].SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                string ptTime = tracknodes[0].SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                sa.TrackTimeStampStart = DateTime.Parse(ptDate) + DateTime.Parse(ptTime).TimeOfDay;


                if (sa.TrackPointCount > 1)
                {
                    ptDate = tracknodes[tracknodes.Count - 1].SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                    ptTime = tracknodes[tracknodes.Count - 1].SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                    sa.TrackTimeStampEnd = DateTime.Parse(ptDate) + DateTime.Parse(ptTime).TimeOfDay;
                }
            }

            return sa;
        }
        private SightingAttributes GetAttributes2(string xml)
        {
            Dictionary<string, string> sightingAttributesDictionary = new Dictionary<string, string>();
            SightingAttributes sa = new SightingAttributes();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);


            XmlNodeList elements = doc.SelectNodes("//E");
            foreach (XmlNode n in elements)
            {
                var key = n.Attributes["I"].Value;
                var val = n.Attributes["N"].Value;
                sightingAttributesDictionary.Add(key, val);
            }


            XmlNodeList sightings = doc.SelectNodes("//S");
            if (sightings.Count > 0)
            {
                foreach (XmlNode s in sightings)
                {
                    if (sa.DeviceID == null || sa.DeviceID.Length == 0)
                    {
                        sa.DeviceID = s.SelectSingleNode(".//A[@N='DeviceId']").Attributes["V"].Value;
                    }
                    if (sa.User == null || sa.User.Length == 0)
                    {
                        if (s.SelectSingleNode(".//A[@N='SelectedUser']") != null)
                        {
                            sa.User = sightingAttributesDictionary[s.SelectSingleNode(".//A[@N='SelectedUser']").Attributes["V"].Value];
                        }
                        //sa.User = sightingAttributesDictionary[s.ParentNode.SelectSingleNode(".//A[@N='SelectedUser']").Attributes["V"].Value];
                    }
                    if (sa.LandingSite == null || sa.LandingSite.Length == 0)
                    {
                        if (s.SelectSingleNode(".//A[@N='SelectedLandingSite']") != null)
                        {
                            sa.LandingSite = sightingAttributesDictionary[s.SelectSingleNode(".//A[@N='SelectedLandingSite']").Attributes["V"].Value];
                        }
                        //sa.LandingSite = sightingAttributesDictionary[s.ParentNode.SelectSingleNode(".//A[@N='SelectedLandingSite']").Attributes["V"].Value];
                    }
                    if (sa.Gear == null || sa.Gear.Length == 0)
                    {
                        if (s.SelectSingleNode(".//A[@N='Selected gear']") != null)
                        {
                            sa.Gear = sightingAttributesDictionary[s.SelectSingleNode(".//A[@N='Selected gear']").Attributes["V"].Value];
                        }
                        //sa.Gear = sightingAttributesDictionary[s.ParentNode.SelectSingleNode(".//A[@N='Selected gear']").Attributes["V"].Value];
                    }

                    if (sa.DeviceID != null && sa.User != null && sa.LandingSite != null && sa.Gear != null)
                    {
                        break;
                    }
                }


                //sa.User = doc.SelectSingleNode("//A[@N='SelectedUser']").Attributes["V"].Value;
                //sa.LandingSite = doc.SelectSingleNode("//A[@N='SelectedLandingSite']").Attributes["V"].Value;
                //sa.Gear = doc.SelectSingleNode("//A[@N='Selected gear']").Attributes["V"].Value;
                if (sightings[0].SelectSingleNode(".//A[@N='AppVersion']") != null)
                {
                    sa.AppVersion = sightings[0].SelectSingleNode(".//A[@N='AppVersion']").Attributes["V"].Value;
                }
                else
                {
                    sa.AppVersion = "";
                }

                if (sa.LandingSite != null && sa.LandingSite.Contains("Other") && doc.SelectSingleNode("//A[@N='OtherLandingSite']") != null)
                {
                    sa.LandingSite = doc.SelectSingleNode("//A[@N='OtherLandingSite']").Attributes["V"].Value;
                }

                if (sa.Gear != null && sa.Gear.Contains("Other") && doc.SelectSingleNode("//A[@N='OtherGear']") != null)
                {

                    sa.Gear = doc.SelectSingleNode("//A[@N='OtherGear']").Attributes["V"].Value;
                }

                if (sa.User != null && sa.User.Contains("Other") && doc.SelectSingleNode("//A[@N='OtherUser']") != null)
                {
                    sa.User = doc.SelectSingleNode("//A[@N='OtherUser']").Attributes["V"].Value;
                }


                var departureKey = sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Depart landing site").Key;
                var returnKey = sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Return to landing site").Key;

                var departure = doc.SelectSingleNode($"//A[@V='{departureKey}']");
                var returnHome = doc.SelectSingleNode($"//A[@V='{returnKey}']");

                if (departure != null)
                {
                    var departureDate = departure.ParentNode.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                    var departureDTime = departure.ParentNode.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;

                    sa.Start = DateTime.Parse(departureDate) + DateTime.Parse(departureDTime).TimeOfDay;
                    sa.NumberOfTrips = doc.SelectNodes($"//A[@V='{departureKey}']").Count;





                    if (returnHome != null)
                    {
                        var returnDate = returnHome.ParentNode.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                        var returnTime = returnHome.ParentNode.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;

                        sa.End = DateTime.Parse(returnDate) + DateTime.Parse(returnTime).TimeOfDay;
                    }

                    var setGearActionKey = sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Set gear").Key;
                    var retrieGearActionKey = sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Retrieve gear").Key;

                    sa.SetGearPointCount = doc.SelectNodes($"//A[@V='{setGearActionKey}']").Count;
                    sa.RetrieveGearPointCount = doc.SelectNodes($"//A[@V='{retrieGearActionKey}']").Count;
                }
            }

            var tracknodes = doc.SelectNodes("//T");
            sa.TrackPointCount = tracknodes.Count;
            if (sa.TrackPointCount > 0)
            {
                string ptDate = tracknodes[0].SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                string ptTime = tracknodes[0].SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                sa.TrackTimeStampStart = DateTime.Parse(ptDate) + DateTime.Parse(ptTime).TimeOfDay;


                if (sa.TrackPointCount > 1)
                {
                    ptDate = tracknodes[tracknodes.Count - 1].SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                    ptTime = tracknodes[tracknodes.Count - 1].SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                    sa.TrackTimeStampEnd = DateTime.Parse(ptDate) + DateTime.Parse(ptTime).TimeOfDay;
                }
            }

            return sa;
        }



        public void GetFilesFromDrive(string folderName)
        {
            FilesForImporting = ctxFileRepo.GetFilesForImportCTXByFolder(folderName).OrderBy(t => t.FileInfo.LastWriteTime).ToList();
        }
        private void CtxFileRepo_XMLFileFromImportedCTXCreated(CTXFileRepository s, CTXFileImportEventArgs e)
        {
            if (XMLFileFromImportedCTXCreated != null)
            {
                CTXFile f = null;
                XMLFileFromImportedCTXCreated(this, e);
                string xmlFile = $@"{e.ImportResultFile}.xml";
                if (File.Exists(xmlFile))
                {
                    f = GetCTXFileAttributesFromXML(xmlFile, new FileInfo(e.ImportResultFile), isFromServer: false);
                }
                else
                {
                    f = new CTXFile
                    {
                        RowID = NextRecordNumber,
                        CTXFileName = Path.GetFileName(e.SourceFile),
                        DateAdded = DateTime.Now,
                        IsDownloaded = true,
                        ErrorConvertingToXML = true
                    };

                    // $"Could not find {e.Destination}.xml file";
                }
                if (AddRecordToRepo(f))
                {
                    FilesForImporting.FirstOrDefault(t => t.FileInfo.Name == f.CTXFileName).IsDownloaded = true;
                    LastDownloadedCTXFiles.Add(f);
                }

            }
        }

        private CTXFile GetCTXFileAttributesFromXML(string xmlFile, FileInfo ctxFile, bool isFromServer)
        {
            CTXFile f = null;
            using (StreamReader sr = File.OpenText(xmlFile))
            {
                string xml = sr.ReadToEnd();
                var sa = GetAttributes(xml);
                f = new CTXFile
                {
                    RowID = NextRecordNumber,
                    CTXFileName = ctxFile.Name,
                    XML = xml,
                    FileName = $@"{ctxFile.Name}.xml",
                    DateAdded = DateTime.Now,
                    IsDownloaded = true,
                    ErrorConvertingToXML = false,
                    CTXFileTimeStamp = ctxFile.LastWriteTime,
                    IsDownloadedFromServer = isFromServer,
                    TrackingInterval = GetGPSTimerIntervalFromCTX(xml),

                    DeviceID = sa.DeviceID,
                    UserName = sa.User,
                    Gear = sa.Gear,
                    LandingSite = sa.LandingSite,
                    DateStart = sa.Start,
                    DateEnd = sa.End,
                    NumberOfTrips = sa.NumberOfTrips,
                    TrackPtCount = sa.TrackPointCount,
                    TrackTimeStampStart = sa.TrackTimeStampStart,
                    TrackTimeStampEnd = sa.TrackTimeStampEnd,
                    SetGearPtCount = sa.SetGearPointCount,
                    RetrieveGearPtCount = sa.RetrieveGearPointCount,
                    AppVersion = sa.AppVersion
                };

            }
            return f;

        }

        public List<DateTime> GetDownloadDates()
        {
            HashSet<DateTime> dates = new HashSet<DateTime>();
            foreach (var item in LastDownloadedCTXFiles.OrderBy(t => t.DateAdded))
            {
                dates.Add(item.DateAdded.Date);
            }
            return dates.ToList();
        }
        private void CtxFileRepo_XMLFileFromCTXCreated(CTXFileRepository s, TransferEventArgs e)
        {
            if (XMLFileFromCTXCreated != null)
            {
                CTXFile f = null;
                XMLFileFromCTXCreated(this, e);
                string xmlFile = $@"{e.Destination}.xml";
                if (File.Exists(xmlFile))
                {
                    f = GetCTXFileAttributesFromXML(xmlFile, new FileInfo(e.Destination), isFromServer: true);
                }
                else
                {
                    f = new CTXFile
                    {
                        RowID = NextRecordNumber,
                        CTXFileName = Path.GetFileName(e.FileName),
                        DateAdded = DateTime.Now,
                        IsDownloaded = true,
                        ErrorConvertingToXML = true
                    };

                    // $"Could not find {e.Destination}.xml file";
                }
                if (AddRecordToRepo(f))
                {
                    FilesInServer.FirstOrDefault(t => t.RemoteFileInfo.Name == f.CTXFileName).IsDownloaded = true;
                    LastDownloadedCTXFiles.Add(f);
                }
                //AddRecordToRepo(f);
            }
        }

        public bool AddRecordToRepo(CTXFile f)
        {
            if (f == null)
                throw new ArgumentNullException("Error: The argument is Null");

            CTXFileCollection.Add(f);

            return _editSucceeded;
        }

        public bool UpdateRecordInRepo(CTXFile f)
        {
            if (f == null)
                throw new Exception("Error: The argument is Null");

            int index = 0;
            while (index < CTXFileCollection.Count)
            {
                if (CTXFileCollection[index].RowID == f.RowID)
                {
                    CTXFileCollection[index] = f;
                    break;
                }
                index++;
            }
            return _editSucceeded;
        }

        public bool DeleteRecordFromRepo(int id)
        {
            if (id == 0)
                throw new Exception("Record ID cannot be null");

            int index = 0;
            while (index < CTXFileCollection.Count)
            {
                if (CTXFileCollection[index].RowID == id)
                {
                    CTXFileCollection.RemoveAt(index);
                    break;
                }
                index++;
            }

            return _editSucceeded;
        }
    }
}
