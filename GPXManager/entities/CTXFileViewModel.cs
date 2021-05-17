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

namespace GPXManager.entities
{
    public class CTXFileViewModel
    {
        private bool _editSucceeded;
        public delegate void XMLFileFromCTX(CTXFileViewModel s, TransferEventArgs e);
        public event XMLFileFromCTX XMLFileFromCTXCreated;
        private Dictionary<string, string> _sightingAttributesDictionary = new Dictionary<string, string>();
        public ObservableCollection<CTXFile> CTXFileCollection { get; set; }
        private CTXFileRepository ctxFileRepo { get; set; }
        public CTXFileViewModel()
        {
            ctxFileRepo = new CTXFileRepository();
            CTXFileCollection = new ObservableCollection<CTXFile>(ctxFileRepo.CTXFiles);
            CTXFileCollection.CollectionChanged += CTXFileCollection_CollectionChanged;
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

        public async Task<bool> GetFileListInServerAsync(string url, string user, string pwd)
        {
            return await Task.Run(() => GetFileListInServer(url, user, pwd));
        }
        private bool GetFileListInServer(string url, string user, string pwd)
        {
            var files = ctxFileRepo.GetServerContent(url, user, pwd);
            FilesInServer = files;
            return files != null && files.Count > 0;
        }

        public bool Exists(string ctxFileName)
        {
            return CTXFileCollection.FirstOrDefault(t => t.CTXFileName == ctxFileName) != null;
        }


        public async Task DownloadFromServerAsync(List<CTXFile> filesToDownload, string downloadlocation)
        {
            await Task.Run(() => DownloadFromServer(filesToDownload, downloadlocation));
        }
        private void DownloadFromServer(List<CTXFile> filesToDownload, string downloadlocation)
        {
            ctxFileRepo.XMLFileFromCTXCreated += CtxFileRepo_XMLFileFromCTXCreated;
            ctxFileRepo.DownloadFromServer(filesToDownload, downloadlocation);

            ctxFileRepo.XMLFileFromCTXCreated -= CtxFileRepo_XMLFileFromCTXCreated;
            CopyCTXToBackupFolder();
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

        public List<CTXFileSummaryView>LatestTripsOfUser(string userName)
        {
            var list = new List<CTXFileSummaryView>();
            if (Global.Settings.LatestTripCount != null)
            {
                foreach (var item in CTXFileCollection.Where(t=>t.UserName==userName).OrderByDescending(t => t.DateStart).Take((int)Global.Settings.LatestTripCount).ToList())
                {
                    list.Add(new CTXFileSummaryView(item));
                }
            }
            else
            {
                foreach(var item in CTXFileCollection.Where(t=>t.UserName==userName).OrderByDescending(t => t.DateStart).Take(5).ToList())
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

                if(item.user=="EBM 3")
                {

                }
                CTXUserSummary summ = new CTXUserSummary
                {
                    DateOfFirstTrip = (DateTime)list.Min(t => t.DateStart).Value,
                    DateOfLatestTrip = (DateTime)list.Max(t => t.DateStart).Value,
                    User = item.user,
                    TotalNumberOfTrips = list.Count
                };
                userSummaries.Add(summ);
            }

            return userSummaries;
        }
        private SightingAttributes GetAttributes(string xml)
        {
            SightingAttributes sa = new SightingAttributes();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            if (_sightingAttributesDictionary.Count == 0)
            {
                XmlNodeList elements = doc.SelectNodes("//E");
                foreach (XmlNode n in elements)
                {
                    var key = n.Attributes["I"].Value;
                    var val = n.Attributes["N"].Value;
                    _sightingAttributesDictionary.Add(key, val);
                }
            }

            var departureKey = _sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Depart landing site").Key;
            var returnKey = _sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Return to landing site").Key;

            var departure = doc.SelectSingleNode($"//A[@V='{departureKey}']");
            var returnHome = doc.SelectSingleNode($"//A[@V='{returnKey}']");

            sa.DeviceID = doc.SelectSingleNode("//A[@N='DeviceId']").Attributes["V"].Value;
            if (departure != null)
            {
                var departureDate = departure.ParentNode.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                var departureDTime = departure.ParentNode.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;

                sa.User = _sightingAttributesDictionary[departure.ParentNode.SelectSingleNode(".//A[@N='SelectedUser']").Attributes["V"].Value];
                sa.LandingSite = _sightingAttributesDictionary[departure.ParentNode.SelectSingleNode(".//A[@N='SelectedLandingSite']").Attributes["V"].Value];
                sa.Gear = _sightingAttributesDictionary[departure.ParentNode.SelectSingleNode(".//A[@N='Selected gear']").Attributes["V"].Value];
                sa.Start = DateTime.Parse(departureDate) + DateTime.Parse(departureDTime).TimeOfDay;
                sa.NumberOfTrips = doc.SelectNodes($"//A[@V='{departureKey}']").Count;
                if (doc.SelectSingleNode("//A[@N='AppVersion']") != null)
                {
                    sa.AppVersion = doc.SelectSingleNode("//A[@N='AppVersion']").Attributes["V"].Value;
                }
                else
                {
                    sa.AppVersion = "";
                }

                if (sa.LandingSite.Contains("Other"))
                {
                    sa.LandingSite = departure.ParentNode.SelectSingleNode(".//A[@N='OtherLandingSite']").Attributes["V"].Value;
                }

                if (sa.Gear.Contains("Other"))
                {
                    sa.Gear = departure.ParentNode.SelectSingleNode(".//A[@N='OtherGear']").Attributes["V"].Value;
                }

                if (sa.User.Contains("Other"))
                {
                    sa.User = departure.ParentNode.SelectSingleNode(".//A[@N='OtherUser']").Attributes["V"].Value;
                }


                if (returnHome != null)
                {
                    //string returnDate = "";
                    //string returnTime = "";

                    var returnDate = returnHome.ParentNode.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                    var returnTime = returnHome.ParentNode.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;

                    //foreach (XmlNode node in returnHome.ParentNode.ChildNodes)
                    //{
                    //    if (node.OuterXml.Contains("Date"))
                    //    {
                    //        returnDate = node.Attributes["V"].Value;
                    //    }

                    //    if (node.OuterXml.Contains("Time"))
                    //    {
                    //        returnTime = node.Attributes["V"].Value;
                    //    }
                    //}
                    sa.End = DateTime.Parse(returnDate) + DateTime.Parse(returnTime).TimeOfDay;
                }

                var setGearActionKey = _sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Set gear").Key;
                var retrieGearActionKey = _sightingAttributesDictionary.FirstOrDefault(x => x.Value == "Retrieve gear").Key;

                sa.SetGearPointCount = doc.SelectNodes($"//A[@V='{setGearActionKey}']").Count;
                sa.RetrieveGearPointCount = doc.SelectNodes($"//A[@V='{retrieGearActionKey}']").Count;

            }

            var tracknodes = doc.SelectNodes("//T");
            sa.TrackPointCount = tracknodes.Count;
            if (sa.TrackPointCount > 0)
            {
                string ptTime = "";
                string ptDate = "";

                ptDate = tracknodes[0].SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                ptTime = tracknodes[0].SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                //foreach (XmlNode nd in tracknodes[0].ChildNodes)
                //{
                //    if (nd.OuterXml.Contains("Date"))
                //    {
                //        ptDate = nd.Attributes["V"].Value;
                //    }

                //    if (nd.OuterXml.Contains("Time"))
                //    {
                //        ptTime = nd.Attributes["V"].Value;
                //    }
                //}

                sa.TrackTimeStampStart = DateTime.Parse(ptDate) + DateTime.Parse(ptTime).TimeOfDay;


                if (sa.TrackPointCount > 1)
                {

                    //foreach (XmlNode nd in tracknodes[tracknodes.Count - 1].ChildNodes)
                    //{
                    //    if (nd.OuterXml.Contains("Date"))
                    //    {
                    //        ptDate = nd.Attributes["V"].Value;
                    //    }

                    //    if (nd.OuterXml.Contains("Time"))
                    //    {
                    //        ptTime = nd.Attributes["V"].Value;
                    //    }
                    //}
                    ptDate = tracknodes[tracknodes.Count-1].SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                    ptTime = tracknodes[tracknodes.Count-1].SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                    sa.TrackTimeStampEnd = DateTime.Parse(ptDate) + DateTime.Parse(ptTime).TimeOfDay;
                }
            }

            return sa;
        }
        private void CtxFileRepo_XMLFileFromCTXCreated(CTXFileRepository s, TransferEventArgs e)
        {
            if (XMLFileFromCTXCreated != null)
            {
                XMLFileFromCTXCreated(this, e);
                using (StreamReader sr = File.OpenText($@"{e.Destination}.xml"))
                {
                    string xml = sr.ReadToEnd();
                    var sa = GetAttributes(xml);
                    CTXFile f = new CTXFile
                    {
                        RowID = NextRecordNumber,
                        CTXFileName = Path.GetFileName(e.FileName),
                        XML = xml,
                        FileName = $@"{e.Destination}.xml",
                        DateAdded = DateTime.Now,
                        IsDownloaded = true,

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
                    FilesInServer.FirstOrDefault(t => t.RemoteFileInfo.Name == f.CTXFileName).IsDownloaded = true;
                    AddRecordToRepo(f);
                    //File.Copy(e.Destination, $@"{Global.Settings.CTXBackupFolder}\{Path.GetFileName(e.Destination)}");
                }

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
