using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WinSCP;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities
{

    public class CTXFileRepository
    {
        public delegate void XMLFileFromDownloadedCTX(CTXFileRepository s, TransferEventArgs e);
        public event XMLFileFromDownloadedCTX XMLFileFromDownloadedCTXCreated;

        public delegate void XMLFileFromImportedCTX(CTXFileRepository s, CTXFileImportEventArgs e);
        public event XMLFileFromImportedCTX XMLFileFromImportedCTXCreated;

        private SessionOptions _sessionOptions;
        private List<CTXFile> _ctxFileList;
        public List<CTXFile> CTXFiles { get; set; }
        public CTXFileRepository()
        {
            _ctxFileList = getFiles();
            CTXFiles = _ctxFileList;
        }
        public string getXML(int RowID)
        {
            string xml = "";

            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select xml from ctxFiles where RowID={RowID}";
                    using (OleDbCommand getXML = new OleDbCommand(query, conection))
                    {
                        xml = (string)getXML.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message);
                }
            }
            return xml;
        }
        private List<CTXFile> getFiles()
        {
            //Logger.Log($"about to start getting ctx from database");
            var list = new List<CTXFile>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    //string query = $"Select * from ctxFiles";
                    string query = @"Select RowID, FileName, DeviceID, CTXFilename, UserName, Gear,
                                     LandingSite, DateAdded, CTXFileTimeStamp, AppVersion,
                                     ErrorConvertingToXML, IsDownloadedFromServer, DateStart, DateEnd,
                                     TrackPts, SetGearPts, RetrieveGearPts, TrackTimeStampStart, TrackTimeStampEnd,
                                     TrackingInterval, TrackExtracted from ctxFiles";

                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        list.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            CTXFile ctxfile = new CTXFile();
                            ctxfile.RowID = (int)dr["RowID"];
                            ctxfile.FileName = dr["FileName"].ToString();
                            ctxfile.DeviceID = dr["DeviceID"].ToString();
                            ctxfile.XML = "";
                            //ctxfile.XML = dr["xml"].ToString();
                            ctxfile.CTXFileName = dr["CTXFileName"].ToString();
                            ctxfile.UserName = dr["UserName"].ToString();
                            ctxfile.Gear = dr["Gear"].ToString();
                            ctxfile.LandingSite = dr["LandingSite"].ToString();
                            ctxfile.DateAdded = DateTime.Parse(dr["DateAdded"].ToString());
                            ctxfile.CTXFileTimeStamp = DateTime.Parse(dr["CTXFileTimeStamp"].ToString());
                            ctxfile.AppVersion = dr["AppVersion"].ToString();
                            ctxfile.ErrorConvertingToXML = (bool)dr["ErrorConvertingToXML"];
                            ctxfile.IsDownloadedFromServer = (bool)dr["IsDownloadedFromServer"];
                            ctxfile.TrackExtracted = (bool)dr["TrackExtracted"];

                            if (dr["DateStart"].ToString().Length > 0)
                            {
                                ctxfile.DateStart = DateTime.Parse(dr["DateStart"].ToString());
                            }

                            if (dr["DateEnd"].ToString().Length > 0)
                            {
                                ctxfile.DateEnd = DateTime.Parse(dr["DateEnd"].ToString());
                            }

                            if (dr["TrackPts"].ToString().Length > 0)
                            {
                                ctxfile.TrackPtCount = (int)dr["TrackPts"];
                            }
                            if (dr["SetGearPts"].ToString().Length > 0)
                            {
                                ctxfile.SetGearPtCount = (int)dr["SetGearPts"];
                            }
                            if (dr["RetrieveGearPts"].ToString().Length > 0)
                            {
                                ctxfile.RetrieveGearPtCount = (int)dr["RetrieveGearPts"];
                            }
                            if (dr["TrackTimeStampStart"].ToString().Length > 0)
                            {
                                ctxfile.TrackTimeStampStart = DateTime.Parse(dr["TrackTimeStampStart"].ToString());
                            }
                            if (dr["TrackTimeStampEnd"].ToString().Length > 0)
                            {
                                ctxfile.TrackTimeStampEnd = DateTime.Parse(dr["TrackTimeStampEnd"].ToString());
                            }
                            if (dr["TrackingInterval"] != DBNull.Value)
                            {
                                ctxfile.TrackingInterval = (int)dr["TrackingInterval"];
                            }

                            list.Add(ctxfile);
                            //Logger.Log($"Added ctxfile ID: {ctxfile.RowID}");
                        }
                    }
                }
                catch (OleDbException dbex)
                {
                    switch (dbex.ErrorCode)
                    {
                        case -2147217865:
                            //table not found
                            CreateTable();
                            break;
                        case -2147217904:
                            //No value given for one or more required parameters.
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);

                }

            }
            return list;
        }

        private void CreateTable()
        {
            using (var conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                string sql = @"CREATE TABLE ctxFiles 
                                (
                                RowID Int NOT NULL PRIMARY KEY,
                                DeviceID VarChar,
                                UserName VarChar,
                                Gear VarChar,
                                LandingSite VarChar,
                                DateStart DateTime,
                                DateEnd DateTime,
                                NumberOfTrips Int,
                                FileName VarChar,
                                TrackPts Int,
                                TrackTimeStampStart DateTime,
                                TrackTimeStampEnd DateTime,
                                SetGearPts Int,
                                RetrieveGearPts Int,
                                xml Memo,
                                CTXFileName VarChar,
                                AppVersion VarChar,
                                ErrorConvertingToXML Bit,    
                                CTXFileTimeStamp DateTime,
                                IsDownloadedFromServer Bit,
                                DateAdded DateTime,
                                TrackingInterval Int,
                                TrackExtracted Bit
                                )";
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;
                cmd.CommandText = sql;

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (OleDbException)
                {
                    //ignore
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }

                cmd.Connection.Close();
                conn.Close();
            }
        }
        public int MaxRecordNumber()
        {
            int max_rec_no = 0;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT Max(RowID) AS max_id FROM ctxFiles";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }
        public bool Add(CTXFile f)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Insert into ctxFiles 
                            (RowID, DeviceID, UserName, Gear, LandingSite, 
                            DateStart, DateEnd, NumberOfTrips, TrackPts, TrackTimeStampStart, 
                            TrackTimeStampEnd, SetGearPts, RetrieveGearPts, FileName, XML, 
                            CTXFileName, AppVersion, ErrorConvertingToXML, CTXFileTimeStamp, IsDownloadedFromServer, 
                            TrackExtracted, DateAdded, TrackingInterval)
                           Values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    update.Parameters.Add("@rowID", OleDbType.Integer).Value = f.RowID;

                    if (f.DeviceID == null)
                    {
                        update.Parameters.Add("@deviceID", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@deviceID", OleDbType.VarChar).Value = f.DeviceID;
                    }

                    if (f.UserName == null)
                    {
                        update.Parameters.Add("@user", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@user", OleDbType.VarChar).Value = f.UserName;
                    }


                    if (f.Gear == null)
                    {
                        update.Parameters.Add("@gear", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@gear", OleDbType.VarChar).Value = f.Gear;
                    }

                    if (f.LandingSite == null)
                    {
                        update.Parameters.Add("@landingSite", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@landingSite", OleDbType.VarChar).Value = f.LandingSite;
                    }


                    if (f.DateStart == null)
                    {
                        update.Parameters.Add("@start", OleDbType.Date).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@start", OleDbType.Date).Value = f.DateStart;
                    }

                    if (f.DateEnd == null)
                    {
                        update.Parameters.Add("@end", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@end", OleDbType.VarChar).Value = f.DateEnd;
                    }

                    if (f.NumberOfTrips == null)
                    {
                        update.Parameters.Add("@numbnerTrips", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@numbnerTrips", OleDbType.Integer).Value = f.NumberOfTrips;
                    }


                    if (f.TrackPtCount == null)
                    {
                        update.Parameters.Add("@trackCount", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@trackCount", OleDbType.Integer).Value = f.TrackPtCount;
                    }

                    if (f.TrackTimeStampStart == null)
                    {
                        update.Parameters.Add("@tracktimestart", OleDbType.Date).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@tracktimestart", OleDbType.Date).Value = f.TrackTimeStampStart;
                    }

                    if (f.TrackTimeStampEnd == null)
                    {
                        update.Parameters.Add("@tracktimeend", OleDbType.Date).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@tracktimeend", OleDbType.Date).Value = f.TrackTimeStampEnd;
                    }

                    if (f.SetGearPtCount == null)
                    {
                        update.Parameters.Add("@setCount", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@setCount", OleDbType.Integer).Value = f.SetGearPtCount;
                    }

                    if (f.RetrieveGearPtCount == null)
                    {
                        update.Parameters.Add("@retreiveCount", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@retreiveCount", OleDbType.Integer).Value = f.RetrieveGearPtCount;
                    }

                    if (f.FileName == null)
                    {
                        update.Parameters.Add("@fileName", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@fileName", OleDbType.VarChar).Value = f.FileName;
                    }

                    if (f.XML == null)
                    {
                        update.Parameters.Add("@xml", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@xml", OleDbType.VarChar).Value = f.XML;
                    }

                    if (f.CTXFileName == null)
                    {
                        update.Parameters.Add("@ctxFileName", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@ctxFileName", OleDbType.VarChar).Value = f.CTXFileName;
                    }

                    if (f.AppVersion == null)
                    {
                        update.Parameters.Add("@appVersion", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@appVersion", OleDbType.VarChar).Value = f.AppVersion;
                    }

                    update.Parameters.Add("@convert_xml_error", OleDbType.Boolean).Value = f.ErrorConvertingToXML;
                    update.Parameters.Add("@fileTimeStamp", OleDbType.Date).Value = f.CTXFileTimeStamp;
                    update.Parameters.Add("@isDownloadedFromServer", OleDbType.Boolean).Value = f.IsDownloadedFromServer;
                    update.Parameters.Add("@isTrackExtracted", OleDbType.Boolean).Value = f.TrackExtracted;
                    update.Parameters.Add("@dateAdded", OleDbType.Date).Value = f.DateAdded;
                    if (f.TrackingInterval == null)
                    {
                        update.Parameters.Add("@trackingInterval", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@trackingInterval", OleDbType.Integer).Value = f.TrackingInterval;
                    }

                    try
                    {
                        success = update.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException dbex)
                    {

                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            return success;
        }

        public List<CTXFile> GetFilesForImportCTXByFolder(string folder)
        {
            _ctxFileList.Clear();
            var list = GetFilesImportCTXByFolder1(folder);
            //return GetFilesImportCTXByFolder1(folder);
            return list;
        }
        private List<CTXFile> GetFilesImportCTXByFolder1(string folder)
        {

            var folderName = System.IO.Path.GetFileName(folder);
            var files = Directory.GetFiles(folder).Select(s => new FileInfo(s));

            if (files.Any())
            {
                foreach (var file in files)
                {
                    if (file.Extension.ToLower() == ".ctx")
                    {
                        string ctxFile = $@"{Global.Settings.CTXBackupFolder}\{Path.GetFileName(file.FullName)}";
                        if (!File.Exists(ctxFile))
                        {
                            var fi = file.CopyTo(ctxFile);
                        }
                        _ctxFileList.Add(new CTXFile { FileInfo = file, IsDownloaded = Entities.CTXFileViewModel.Exists(file.Name) });
                    }
                }
            }

            foreach (var dir in Directory.GetDirectories(folder))
            {
                GetFilesImportCTXByFolder1(dir);
            }
            return _ctxFileList;
        }

        public bool Update(CTXFile f)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();


                using (OleDbCommand update = conn.CreateCommand())
                {


                    if (f.DeviceID == null)
                    {
                        update.Parameters.Add("@deviceID", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@deviceID", OleDbType.VarChar).Value = f.DeviceID;
                    }

                    if (f.UserName == null)
                    {
                        update.Parameters.Add("@user", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@user", OleDbType.VarChar).Value = f.UserName;
                    }


                    if (f.Gear == null)
                    {
                        update.Parameters.Add("@gear", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@gear", OleDbType.VarChar).Value = f.Gear;
                    }

                    if (f.LandingSite == null)
                    {
                        update.Parameters.Add("@landingSite", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@landingSite", OleDbType.VarChar).Value = f.LandingSite;
                    }


                    if (f.DateStart == null)
                    {
                        update.Parameters.Add("@start", OleDbType.Date).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@start", OleDbType.Date).Value = f.DateStart;
                    }

                    if (f.DateEnd == null)
                    {
                        update.Parameters.Add("@end", OleDbType.Date).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@end", OleDbType.Date).Value = f.DateEnd;
                    }

                    if (f.NumberOfTrips == null)
                    {
                        update.Parameters.Add("@numbnerTrips", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@numbnerTrips", OleDbType.Integer).Value = f.NumberOfTrips;
                    }


                    if (f.TrackPtCount == null)
                    {
                        update.Parameters.Add("@trackCount", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@trackCount", OleDbType.Integer).Value = f.TrackPtCount;
                    }

                    if (f.TrackTimeStampStart == null)
                    {
                        update.Parameters.Add("@tracktimestart", OleDbType.Date).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@tracktimestart", OleDbType.Date).Value = f.TrackTimeStampStart;
                    }

                    if (f.TrackTimeStampEnd == null)
                    {
                        update.Parameters.Add("@tracktimeend", OleDbType.Date).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@tracktimeend", OleDbType.Date).Value = f.TrackTimeStampEnd;
                    }

                    if (f.SetGearPtCount == null)
                    {
                        update.Parameters.Add("@setCount", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@setCount", OleDbType.Integer).Value = f.SetGearPtCount;
                    }

                    if (f.RetrieveGearPtCount == null)
                    {
                        update.Parameters.Add("@retreiveCount", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@retreiveCount", OleDbType.Integer).Value = f.RetrieveGearPtCount;
                    }

                    if (f.FileName == null)
                    {
                        update.Parameters.Add("@fileName", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@fileName", OleDbType.VarChar).Value = f.FileName;
                    }

                    if (f.XML == null)
                    {
                        update.Parameters.Add("@xml", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@xml", OleDbType.VarChar).Value = f.XML;
                    }

                    if (f.CTXFileName == null)
                    {
                        update.Parameters.Add("@ctxFileName", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@ctxFileName", OleDbType.VarChar).Value = f.CTXFileName;
                    }

                    if (f.AppVersion == null)
                    {
                        update.Parameters.Add("@appVersion", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@appVersion", OleDbType.VarChar).Value = f.AppVersion;
                    }

                    update.Parameters.Add("@convert_xml_error", OleDbType.Boolean).Value = f.ErrorConvertingToXML;
                    update.Parameters.Add("@fileTimeStamp", OleDbType.Date).Value = f.CTXFileTimeStamp;
                    update.Parameters.Add("@isDownloadedFromServer", OleDbType.Boolean).Value = f.IsDownloadedFromServer;
                    update.Parameters.Add("@isTrackExtracted", OleDbType.Boolean).Value = f.TrackExtracted;
                    update.Parameters.Add("@dateAdded", OleDbType.Date).Value = f.DateAdded;
                    if (f.TrackingInterval == null)
                    {
                        update.Parameters.Add("@trackingInterval", OleDbType.Integer).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@trackingInterval", OleDbType.Integer).Value = f.TrackingInterval;
                    }
                    update.Parameters.Add("@rowID", OleDbType.Integer).Value = f.RowID;

                    update.CommandText = @"UPDATE ctxFiles SET 
                            DeviceID = @deviceID, 
                            UserName = @user, 
                            Gear = @gear, 
                            LandingSite = @landingSite, 
                            DateStart = @start, 
                            DateEnd = @end, 
                            NumberOfTrips = @numbnerTrips, 
                            TrackPts = @trackCount, 
                            TrackTimeStampStart = @tracktimestart, 
                            TrackTimeStampEnd = @tracktimeend, 
                            SetGearPts = @setCount, 
                            RetrieveGearPts = @retreiveCount, 
                            FileName = @fileName, 
                            XML = @xml, 
                            CTXFileName = @ctxFileName, 
                            AppVersion = @appVersion, 
                            ErrorConvertingToXML = @convert_xml_error, 
                            CTXFileTimeStamp = @fileTimeStamp, 
                            IsDownloadedFromServer = @isDownloadedFromServer, 
                            TrackExtracted = isTrackExtracted,
                            DateAdded = @dateAdded,
                            TrackingInterval = @trackingInterval
                            Where RowID = @rowID";

                    try
                    {
                        success = update.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException dbex)
                    {

                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            return success;
        }

        public bool Delete(int id)
        {
            return true;
        }
        public List<CTXFile> GetServerContent(string url, string user, string pwd, bool download = false)
        {
            LastError = "";
            _sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Ftp,
                HostName = url,
                UserName = user,
                Password = pwd,
                PortNumber = 21,
            };

            List<CTXFile> list = new List<CTXFile>();

            using (var session = new Session())
            {
                try
                {
                    // Connect
                    session.Open(_sessionOptions);

                    // Enumerate files
                    var options = EnumerationOptions.EnumerateDirectories | EnumerationOptions.AllDirectories;
                    IEnumerable<RemoteFileInfo> fileInfos = session.EnumerateRemoteFiles("/CTData", null, options);

                    foreach (var fileInfo in fileInfos)
                    {
                        try
                        {
                            CTXFile f = new CTXFile { IsDownloaded = Entities.CTXFileViewModel.Exists(fileInfo.Name), RemoteFileInfo = fileInfo, CTXFileName = fileInfo.Name };
                            f.CTXFileTimeStamp = f.RemoteFileInfo.LastWriteTime;
                            list.Add(f);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    if (ex.Message == "Connection failed.\r\nConnection failed.")
                    {
                        LastError = "Connection failed";
                    }
                    else
                    {
                        LastError = ex.Message;
                    }
                    return null;
                }

            }

            return list;
        }

        public string LastXMLFromCTXFile { get; private set; }

        public bool ErrorConvertingToXML { get; private set; }
        public string LastError { get; private set; }


        public static bool ExtractXMLFromCTX(string inCTX)//, string downloadLocation)
        {
            string args = $@"/datafile {inCTX} /exportxml  {inCTX}.xml";
            string command = $@"{Global.Settings.PathToCybertrackerExe}\ct3";
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(command, args);
            psi.UseShellExecute = false;
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            psi.ErrorDialog = true;
            p.StartInfo = psi;
            p.Start();
            p.WaitForExit();
            return File.Exists($@"{inCTX}.xml");
        }

        public bool ImportCTXFIles(List<CTXFile> files, string importLocation)
        {
            foreach (var f in files)
            {
                string inCTX = $@"{Global.Settings.CTXDownloadFolder}\{f.FileInfo.Name}";
                string sourceCTX = $@"{f.FileInfo.FullName}";

                if (!File.Exists(inCTX))
                {
                    File.Copy(sourceCTX, inCTX, overwrite: false);
                }

                if (File.Exists(inCTX) && ExtractXMLFromCTX(inCTX))
                {
                    if (XMLFileFromImportedCTXCreated != null)
                    {
                        LastXMLFromCTXFile = $@"{inCTX}.xml";
                        if (!File.Exists(LastXMLFromCTXFile))
                        {
                            ErrorConvertingToXML = true;
                            LastError = "XML file converted from binary CTX not found";
                        }
                        CTXFileImportEventArgs e = new CTXFileImportEventArgs();
                        e.SourceFile = f.FileInfo.FullName;
                        e.ImportResultFile = inCTX;
                        XMLFileFromImportedCTXCreated(this, e);
                        if (!File.Exists($@"{Global.Settings.CTXBackupFolder}\{f.FileInfo.Name}"))
                        {
                            File.Copy($@"{inCTX}", $@"{Global.Settings.CTXBackupFolder}\{f.FileInfo.Name}", overwrite: false);
                        }
                    }
                }
            }
            return true;
        }
        public bool DownloadFromServer(List<CTXFile> files, string downloadLocation)
        {
            LastError = "";
            if (files.Count > 0)
            {
                try
                {
                    using (Session session = new Session())
                    {
                        // Connect
                        session.Open(_sessionOptions);

                        // Download files
                        TransferOptions transferOptions = new TransferOptions();
                        transferOptions.TransferMode = TransferMode.Binary;

                        foreach (var f in files)
                        {
                            try
                            {
                                var transferResult = session.GetFileToDirectory(f.RemoteFileInfo.FullName, $"{downloadLocation}");//, false, transferOptions);
                                if (transferResult.Error == null)
                                {
                                    ErrorConvertingToXML = false;
                                    string inCTX = $@"{downloadLocation}\{f.RemoteFileInfo.Name}";

                                    //string args = $@"/datafile {downloadLocation}\{f.RemoteFileInfo.Name} /exportxml  {downloadLocation}\{f.RemoteFileInfo.Name}.xml";
                                    //string command = $@"{Global.Settings.PathToCybertrackerExe}\ct3";
                                    //System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(command, args);
                                    //psi.UseShellExecute = false;
                                    //System.Diagnostics.Process p = new System.Diagnostics.Process();
                                    //psi.ErrorDialog = true;
                                    //p.StartInfo = psi;
                                    //p.Start();
                                    //p.WaitForExit();

                                    if (ExtractXMLFromCTX(inCTX))//0., downloadLocation))
                                    {
                                        if (XMLFileFromDownloadedCTXCreated != null)
                                        {
                                            LastXMLFromCTXFile = $@"{inCTX}.xml";
                                            if (!File.Exists(LastXMLFromCTXFile))
                                            {
                                                ErrorConvertingToXML = true;
                                                LastError = "XML file converted from binary CTX not found";
                                            }
                                            XMLFileFromDownloadedCTXCreated(this, transferResult);
                                        }
                                        //copy downloaded file to backup folder
                                        if (!File.Exists($@"{Global.Settings.CTXBackupFolder}\{f.RemoteFileInfo.Name}"))
                                        {
                                            File.Copy($@"{Global.Settings.CTXDownloadFolder}\{f.RemoteFileInfo.Name}", $@"{Global.Settings.CTXBackupFolder}\{f.RemoteFileInfo.Name}", overwrite: false);
                                        }

                                    }
                                    else
                                    {
                                        ErrorConvertingToXML = true;
                                        LastError = "XML file converted from binary CTX not found";
                                        XMLFileFromDownloadedCTXCreated(this, transferResult);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    LastError = ex.Message;
                }
            }

            return true;
        }

    }
}
