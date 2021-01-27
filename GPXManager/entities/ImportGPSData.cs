using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System.IO;



namespace GPXManager.entities
{
    public static class ImportGPSData
    {
        public static string ImportMessage { get; internal set; }
        public static int ImportCount { get; private set; }

        public static int? StartGPSNumbering { get; set; }

        public static int? EndGPSNumbering { get; set; }

        public static string GPSNameStart { get; set; }


        
        /// <summary>
        /// imnport GPX data into the database by scanning folder and subfolder for numbered folders
        /// The folder will be assumed to be a gps but need not be precisely named as long as the number agree 
        /// wiht the GPX naming scheme
        /// </summary>
        /// <param name="gpsStartName"></param>
        /// <returns></returns>
        public static bool ImportGPX()
        {
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
                    ImportCount = Entities.DeviceGPXViewModel.ImportGPXByLGUFolder(vfbd.SelectedPath, GPSNameStart);
                }
                else
                {
                    ImportCount = Entities.DeviceGPXViewModel.ImportGPX(vfbd.SelectedPath, first: true);
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
