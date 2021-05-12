using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System.IO;
using GPXManager.entities;
using System.Linq;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
namespace GPXManager.entities
{
    public static class ImportGPSData
    {
        private static int _count;
        private static int _gpxCount;
        public static string ImportMessage { get; internal set; }
        public static int ImportCount { get; private set; }

        public static int GPXCount { get { return _gpxCount; } }
        public static int? StartGPSNumbering { get; set; }

        public static int? EndGPSNumbering { get; set; }

        public static string GPSNameStart { get; set; }

        public static EventHandler<ImportGPXEventArg> ImportGPXEvent;

        /// <summary>
        /// imnport GPX data into the database by scanning folder and subfolder for numbered folders
        /// The folder will be assumed to be a gps but need not be precisely named as long as the number agree 
        /// wiht the GPX naming scheme
        /// </summary>
        /// <param name="gpsStartName"></param>
        /// <returns></returns>
        public static async Task<bool> ImportGPXAsync()
        {
            _count = 0;
            bool success = false;
            VistaFolderBrowserDialog vfbd = new VistaFolderBrowserDialog
            {
                Description = "Locate folder with GPX files",
                UseDescriptionForTitle = true,
                SelectedPath = Global.Settings.ComputerGPXFolder
            };
            if ((bool)vfbd.ShowDialog() && Directory.Exists(vfbd.SelectedPath))
            {
                if (StartGPSNumbering != null && 
                    ((int)StartGPSNumbering) >= 0 && 
                    EndGPSNumbering != null &&
                    ((int)EndGPSNumbering) >= 0 &&
                    GPSNameStart.Length>0)
                {
                    await ImportGPXByFolderAsync(vfbd.SelectedPath,first:true);
                    ImportCount = _count;
                }
                else
                {
                    ImportCount = ImportGPX(vfbd.SelectedPath, first: true);
                }
                if (ImportCount > 0)
                {
                    ImportMessage= $"{ImportCount} GPX files imported to database";
                    success = true;
                }
                else
                {
                    ImportMessage = "No GPX files were imported to the database";
                }
            }
            return success;
        }

        /// <summary>
        /// assume that the selected folder will contain subfolders with numbered folder names. 
        /// GPS that all the folders will refer will be similarly named, but with differing numbers
        /// The number will point to a specific GPS so folder names need not be precise. The number will be concatenated to
        /// a string that referes to an LGU so CON 11 which then points to a GPS already stored in the database
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="in_gps"></param>
        /// <param name="gpsNameStart">starting part of a gps name for example CON = concepcion </param>
        /// <param name="first"></param>
        /// <param name=""></param>
        /// <returns></returns>


        private static async Task ImportGPXByFolderAsync(string folder, GPS in_gps = null,  bool? first=false, int yearStartProcess = 2019)
        {
            EventHandler<ImportGPXEventArg> importEvent = ImportGPXEvent;
            if (importEvent != null)
            {
                ImportGPXEventArg e = new ImportGPXEventArg
                {
                    Intent = "start"
                };

                importEvent(null, e);
            }
            await Task.Run(() => ImportGPXByFolder(folder, in_gps, first,yearStartProcess));
            Logger.LogType = LogType.Logfile;
        }
        private static int ImportGPXByFolder(string folder, GPS in_gps = null,  bool? first=false, int yearStartProcess = 2019 )
        {



            if ((bool)first)
            {
                Logger.LogType = LogType.ImportGPXfFromFolder;
                _count = 0;
                _gpxCount = 0;

            }

            GPS gps = null;
            GPS current_gps = null;

            if(in_gps!=null)
            {
                gps = in_gps;
                current_gps = in_gps;
            }
            //Logger.Log($"processing folder: {folder}");

            var folderName = System.IO.Path.GetFileName(folder);
            //if (ImportGPSData.GPSNameStart.Length > 0)
            //{

                string result = GetNumericPartOfFolderName(folderName);
                if (result.Length > 0)
                {
                    int numericPart = int.Parse(result);
                    if (numericPart >= ImportGPSData.StartGPSNumbering && numericPart <= ImportGPSData.EndGPSNumbering)
                    {
                        gps = Entities.GPSViewModel.GetGPS($"{ImportGPSData.GPSNameStart} {GetNumericPartOfFolderName(folderName)}");
                    }
                    else
                    {
                        return 0;
                    }
                }
                //int numericPart = int.Parse(GetNumericPartOfFolderName(folderName));

            //}
            //else
            //{
            //    gps = Entities.GPSViewModel.GetGPSByName(folderName);
            //}

            if (gps != null)
            {
                current_gps = gps;
            }
            else if (gps == null && in_gps != null)
            {
                current_gps = in_gps;
            }


            var files = Directory.GetFiles(folder).Select(s => new FileInfo(s));

            if (files.Any())
            {
                foreach (var file in files)
                {
                    if (file.Extension.ToLower() == ".gpx")
                    {
                        //var folderName = System.IO.Path.GetFileName(folder);
                        //if(ImportGPSData.GPSNameStart.Length>0)
                        //{

                        //    string result = GetNumericPartOfFolderName(folderName);
                        //    if (result.Length > 0)
                        //    {
                        //        int numericPart = int.Parse(result);
                        //        if (numericPart >= ImportGPSData.StartGPSNumbering && numericPart <= ImportGPSData.EndGPSNumbering)
                        //        {
                        //            gps = Entities.GPSViewModel.GetGPS($"{ImportGPSData.GPSNameStart} {GetNumericPartOfFolderName(folderName)}");
                        //        }
                        //        else
                        //        {
                        //            return 0;
                        //        }
                        //    }
                        //    //int numericPart = int.Parse(GetNumericPartOfFolderName(folderName));

                        //}
                        //else {
                        //    gps = Entities.GPSViewModel.GetGPSByName(folderName);
                        //}

                        //if (gps != null)
                        //{
                        //    current_gps = gps;
                        //}
                        //else if (gps == null && in_gps != null)
                        //{
                        //    current_gps = in_gps;
                        //}

                        if (current_gps != null)
                        {
                            _gpxCount++;
                            GPXFile g = new GPXFile(file);
                            g.GPS = current_gps;
                            if (g.ComputeStats())
                            {
                                if (g.DateRangeStart.Year >= yearStartProcess)
                                {
                                    DeviceGPX d = new DeviceGPX
                                    {
                                        Filename = file.Name,
                                        GPS = current_gps,
                                        GPX = g.XML,
                                        GPXType = g.GPXFileType == GPXFileType.Track ? "track" : "waypoint",
                                        RowID = Entities.DeviceGPXViewModel.NextRecordNumber,
                                        MD5 = CreateMD5(g.XML),
                                        TimeRangeStart = g.DateRangeStart,
                                        TimeRangeEnd = g.DateRangeEnd
                                    };

                                    string fileProcessed = $@"{current_gps.DeviceName}:{file.FullName}";


                                    DeviceGPX saved = Entities.DeviceGPXViewModel.GetDeviceGPX(d);
                                    if (saved == null)
                                    {
                                        if (Entities.DeviceGPXViewModel.AddRecordToRepo(d))
                                        {
                                            _count++;
                                            fileProcessed += "  (ADDED)";

                                            EventHandler<ImportGPXEventArg> importEvent = ImportGPXEvent;
                                            if(importEvent!=null)
                                            {
                                                ImportGPXEventArg e = new ImportGPXEventArg
                                                {
                                                    Intent="gpx saved",
                                                    GPS = current_gps,
                                                    ImportedCount = _count,
                                                    GPXFileName = file.Name
                                                };

                                                importEvent(null, e);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (saved.MD5 != d.MD5 && d.TimeRangeEnd > saved.TimeRangeEnd)
                                        {
                                            if(Entities.DeviceGPXViewModel.UpdateRecordInRepo(d))
                                            {
                                                EventHandler<ImportGPXEventArg> importEvent = ImportGPXEvent;
                                                if (importEvent != null)
                                                {
                                                    ImportGPXEventArg e = new ImportGPXEventArg
                                                    {
                                                        Intent = "gpx file modified",
                                                        GPS = current_gps,
                                                        ImportedCount = _count,
                                                        GPXFileName = file.Name
                                                    };

                                                    importEvent(null, e);
                                                }
                                            }
                                            fileProcessed += " (MODIFIED ADDED)";
                                        }
                                        else
                                        {
                                            EventHandler<ImportGPXEventArg> importEvent = ImportGPXEvent;
                                            if (importEvent != null)
                                            {
                                                ImportGPXEventArg e = new ImportGPXEventArg
                                                {
                                                    Intent = "gpx file duplicate",
                                                    GPS = current_gps,
                                                    ImportedCount = _count,
                                                    GPXFileName = file.Name
                                                };

                                                importEvent(null, e);
                                            }
                                            fileProcessed += "  (DUPLICATE)";
                                        }
                                    }
                                    //Console.WriteLine(fileProcessed);
                                    Logger.Log(fileProcessed);
                                }
                                else
                                {
                                    Logger.Log($"GPX file {file.FullName} time is {g.DateRangeEnd.ToString("MMM-dd-yyyy")} and is not saved");
                                }
                            }
                            else
                            {
                                Logger.Log($"Error computing stats for GPX file {file.FullName} and is not saved");
                            }
                        }
                    }
                }
            }

            foreach (var dir in Directory.GetDirectories(folder))
            {
                ImportGPXByFolder(dir, current_gps);
            }
            return _count;
        }

   
        private static string CreateMD5(string input)
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
        private static string GetNumericPartOfFolderName(string folderName)
        {
            string numericPart="";
            for(int x=0; x<folderName.Length;x++)
            {
                if(folderName[x]>='0' && folderName[x]<='9')
                {
                    numericPart += folderName[x];
                }
            }
            if(numericPart.Length>0)
            {
                int n = int.Parse(numericPart);
                if (n >= ImportGPSData.StartGPSNumbering && n <= ImportGPSData.EndGPSNumbering)
                {
                    return n.ToString();
                }
                else
                {
                    return "";
                }
            }
            return numericPart;
        }
        private static int ImportGPX(string folder, GPS in_gps = null, bool first=false, int startYear=2019)
        {
            if(first)
            {
                _count = 0;
            }
            GPS gps = null;
            GPS current_gps = null; 
            var files = Directory.GetFiles(folder).Select(s => new FileInfo(s));
            if (files.Any())
            {
                foreach(var file in files)
                {
                    if(file.Extension.ToLower()==".gpx")
                    {
                        var folderName = System.IO.Path.GetFileName(folder);
                        if (ImportGPSData.GPSNameStart.Length > 0)
                        {
                            gps = Entities.GPSViewModel.GetGPS($"{ImportGPSData.GPSNameStart} {GetNumericPartOfFolderName(folderName)}");
                        }
                        else
                        {
                            gps = Entities.GPSViewModel.GetGPSByName(folderName);
                        }

                        if (gps!=null)
                        {
                            current_gps = gps;    
                        }
                        else if(gps==null && in_gps!=null)
                        {
                            current_gps = in_gps;
                        } 

                        if (current_gps != null)
                        {
                            GPXFile g = new GPXFile(file);
                            g.GPS = current_gps;
                            if (g.ComputeStats())
                            {
                                if (g.DateRangeStart.Year >= startYear)
                                {
                                    DeviceGPX d = new DeviceGPX
                                    {
                                        Filename = file.Name,
                                        GPS = current_gps,
                                        GPX = g.XML,
                                        GPXType = g.GPXFileType == GPXFileType.Track ? "track" : "waypoint",
                                        RowID = Entities.DeviceGPXViewModel.NextRecordNumber,
                                        MD5 = CreateMD5(g.XML),
                                        TimeRangeStart = g.DateRangeStart,
                                        TimeRangeEnd = g.DateRangeEnd
                                    };

                                    string fileProcessed = $@"{current_gps.DeviceName}:{file.FullName}";

                                    DeviceGPX saved = Entities.DeviceGPXViewModel.GetDeviceGPX(d);
                                    if (saved == null)
                                    {
                                        if (Entities.DeviceGPXViewModel.AddRecordToRepo(d))
                                        {
                                            _count++;
                                            fileProcessed += "  (ADDED)";
                                        }
                                    }
                                    else
                                    {
                                        if (saved.MD5 != d.MD5 && d.TimeRangeEnd > saved.TimeRangeEnd)
                                        {
                                            Entities.DeviceGPXViewModel.UpdateRecordInRepo(d);
                                            fileProcessed += " (MODIFIED ADDED)";
                                        }
                                        else
                                        {
                                            fileProcessed += "  (DUPLICATE)";
                                        }
                                    }
                                    Console.WriteLine(fileProcessed);
                                }
                                else
                                {
                                    Logger.Log($"GPX year for {file.FullName} is less than {startYear}");
                                }
                            }
                            else
                            {
                                Logger.Log($"Error in computing stats for {file.FullName}");
                            }
                        }
                    }
                }
            }

            foreach(var dir in Directory.GetDirectories(folder))
            {
                ImportGPX(dir, current_gps);
            }
            return _count;
        }
        public static bool ImportGPS()
        {
            bool success = false;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Import GPS with XML";
            ofd.Filter = "XML file (*.xml)|*.xml|All file type (*.*)|*.*";
            ofd.DefaultExt = ".xml";
            if ((bool)ofd.ShowDialog() && File.Exists(ofd.FileName))
            {

                int importCount = Entities.GPSViewModel.ImportGPS(ofd.FileName, out string message);
                if (importCount > 0)
                {
                    ImportMessage = $"{importCount} GPS was imported into the database";
                    success= true;
                }
                else
                {
                    if (message != "Valid XML")
                    {
                        ImportMessage = $"{message}\r\n\r\nNo GPS was imported into the database";
                    }
                    else
                    {
                        ImportMessage = "No GPS was imported into the database";
                    }
                }
            }
            return success;
        }
    }
}
