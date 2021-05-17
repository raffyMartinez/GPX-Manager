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
        public delegate void XMLFileFromCTX(CTXFileRepository s, TransferEventArgs e);
        public event XMLFileFromCTX XMLFileFromCTXCreated;

        private SessionOptions _sessionOptions;
        private List<CTXFile> _ctxFileList;
        public List<CTXFile> CTXFiles { get; set; }
        public CTXFileRepository()
        {
            _ctxFileList = getFiles();
            CTXFiles = _ctxFileList;
        }

        private List<CTXFile> getFiles()
        {
            var list = new List<CTXFile>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from ctxFiles";


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
                            ctxfile.XML = dr["xml"].ToString();
                            ctxfile.CTXFileName = dr["CTXFileName"].ToString();
                            ctxfile.UserName = dr["UserName"].ToString();
                            ctxfile.Gear = dr["Gear"].ToString();
                            ctxfile.LandingSite = dr["LandingSite"].ToString();
                            ctxfile.DateAdded = DateTime.Parse(dr["DateAdded"].ToString());
                            ctxfile.AppVersion = dr["AppVersion"].ToString();

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
                            if(dr["TrackTimeStampStart"].ToString().Length>0)
                            {
                                ctxfile.TrackTimeStampStart = DateTime.Parse(dr["TrackTimeStampStart"].ToString());
                            }
                            if (dr["TrackTimeStampEnd"].ToString().Length > 0)
                            {
                                ctxfile.TrackTimeStampEnd = DateTime.Parse(dr["TrackTimeStampEnd"].ToString());
                            }

                            list.Add(ctxfile);
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
                                DateAdded DateTime
                                )";
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;
                cmd.CommandText = sql;

                try
                {
                    cmd.ExecuteNonQuery();

                    //sql = "ALTER TABLE trips ALTER COLUMN NameOfOperator INT";
                    //cmd.CommandText = sql;
                    //cmd.ExecuteNonQuery();

                    //sql = "ALTER TABLE trips ADD CONSTRAINT fisherID_FK FOREIGN KEY (NameOfOperator) REFERENCES fishers(FisherID)";
                    //cmd.CommandText = sql;
                    //cmd.ExecuteNonQuery();
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
                var sql = $@"Insert into ctxFIles (RowID, DeviceID, UserName, Gear, LandingSite, DateStart, DateEnd, NumberOfTrips, TrackPts, TrackTimeStampStart, TrackTimeStampEnd, SetGearPts, RetrieveGearPts, FileName, XML, CTXFileName, AppVersion, DateAdded)
                           Values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    update.Parameters.Add("@rowID", OleDbType.Integer).Value = f.RowID;
                    update.Parameters.Add("@deviceID", OleDbType.VarChar).Value = f.DeviceID;

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

                    if(f.NumberOfTrips==null)
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

                    if(f.TrackTimeStampStart==null)
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


                    update.Parameters.Add("@fileName", OleDbType.VarChar).Value = f.FileName;
                    update.Parameters.Add("@xml", OleDbType.VarChar).Value = f.XML;
                    update.Parameters.Add("@ctxFileName", OleDbType.VarChar).Value = f.CTXFileName;
                    if (f.AppVersion == null)
                    {
                        update.Parameters.Add("@appVersion", OleDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@appVersion", OleDbType.VarChar).Value = f.AppVersion;
                    }

                    update.Parameters.Add("@dateAdded", OleDbType.Date).Value = f.DateAdded;

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

        public bool Update(CTXFile f)
        {
            return true;
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
                            CTXFile f = new CTXFile { IsDownloaded = Entities.CTXFileViewModel.Exists(fileInfo.Name), RemoteFileInfo = fileInfo };
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

        public string LastError { get; private set; }
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
                                    string args = $@"/datafile {downloadLocation}\{f.RemoteFileInfo.Name} /exportxml  {downloadLocation}\{f.RemoteFileInfo.Name}.xml";
                                    string command = $@"{Global.Settings.PathToCybertrackerExe}\ct3";
                                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(command, args);
                                    psi.UseShellExecute = false;
                                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                                    psi.ErrorDialog = true;
                                    p.StartInfo = psi;
                                    p.Start();
                                    p.WaitForExit();

                                    if (XMLFileFromCTXCreated != null)
                                    {
                                        LastXMLFromCTXFile = $@"{downloadLocation}\{f.RemoteFileInfo.Name}.xml";
                                        XMLFileFromCTXCreated(this, transferResult);
                                    }
                                    //copy downloaded file to backup folder
                                    File.Copy($@"{Global.Settings.CTXDownloadFolder}\{f.RemoteFileInfo.Name}",$@"{Global.Settings.CTXBackupFolder}\{f.RemoteFileInfo.Name}",overwrite:false);
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
