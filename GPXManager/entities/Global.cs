using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Management;
using System.Xml;

namespace GPXManager.entities
{
    public static class Global
    {
        public static string MapOCXPath = $@"{AppDomain.CurrentDomain.BaseDirectory}AxInterop.MapWinGIS.dll";

        public const string UserSettingsFilename = "settings.xml";

        public static string _DefaultSettingspath =
            AppDomain.CurrentDomain.BaseDirectory +
            "\\Settings\\" + UserSettingsFilename;

        public static string _UserSettingsPath =
            AppDomain.CurrentDomain.BaseDirectory +
            "\\Settings\\UserSettings\\" +
            UserSettingsFilename;


        public static string IsValidXMLFile(string xmlFile)
        {
            string xml = File.OpenText(xmlFile).ReadToEnd();
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            {
                try
                {
                    var result = reader.Read();
                }
                catch(XmlException)
                {
                    return "Invalid XML";
                }
                catch(Exception ex)
                {
                    Logger.Log(ex);
                    return ex.Message;
                }
                return "Valid XML";
            }
        }
        public static bool MapOCXInstalled
        {
            get { return File.Exists(MapOCXPath); }
       }
        public static string MDBPath { get; internal set; }

        public static bool AppProceed { get; private set; }

        public static string ConnectionString { get; private set; }

        public static string ConnectionStringGrid25 { get; private set; }
        static Global()
        {

            // if default settings exist
            if (File.Exists(_UserSettingsPath))
            {
                Settings = Settings.Read(_UserSettingsPath);
            }
            else if (File.Exists(_DefaultSettingspath))
            {
                Settings = Settings.Read(_DefaultSettingspath);
            }


            DoAppProceed();



        }

        private static void DoAppProceed()
        {
            AppProceed = Settings != null &&
                File.Exists(Settings.MDBPath) &&
                Settings.ComputerGPXFolder != null &&
                Settings.ComputerGPXFolder.Length > 0 &&
                Directory.Exists(Settings.ComputerGPXFolder) &&
                Settings.DeviceGPXFolder !=null && 
                Settings.DeviceGPXFolder.Length > 0;    
            if (AppProceed)
            {
                MDBPath = Settings.MDBPath;
                ConnectionString = "Provider=Microsoft.JET.OLEDB.4.0;data source=" + MDBPath;
            }
            else
            {
                Logger.Log("Application settings not complete");
            }
        }

        public static bool SetSettings(string computerGPXFolder, string deviceGPXFolder, 
              string backendPath, int hoursGMTOffset, string bingAPIKey, int countLatestTrip,
              int countLatestGPXFiles)
        {
            Settings = new Settings
            {
                MDBPath = backendPath,
                ComputerGPXFolder = computerGPXFolder,
                DeviceGPXFolder = deviceGPXFolder,
                HoursOffsetGMT = hoursGMTOffset,
                BingAPIKey = bingAPIKey,
                LatestTripCount = countLatestTrip,
                LatestGPXFileCount = countLatestGPXFiles
            };

            SaveGlobalSettings();
            DoAppProceed();
            return AppProceed;
        }

        public static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub "))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
                ));
            }

            collection.Dispose();
            return devices;
        }

        public static Settings Settings { get; private set; }

        public static void SaveGlobalSettings()
        {
            Settings.Save(_DefaultSettingspath);
        }
        public static void SaveUserSettings()
        {
            Settings.Save(_UserSettingsPath);
        }
        public static bool ParsedCoordinateIsValid(string coordToParse, string x_or_y, out double result)
        {
            result = 0;
            bool success = false;
            switch (x_or_y)
            {
                case "x":
                case "X":
                case "y":
                case "Y":
                    switch (x_or_y)
                    {
                        case "x":
                        case "X":
                            if (double.TryParse(coordToParse, out double v))
                            {
                                if (v >= 0 && v <= 180)
                                {
                                    result = v;
                                    success = true;
                                }
                            }
                            break;
                        case "y":
                        case "Y":
                            if (double.TryParse(coordToParse, out v))
                            {
                                if (v >= -90 && v <= 90)
                                {
                                    result = v;
                                    success = true;
                                }
                            }
                            break;
                    }
                    break;
                default:
                    throw new Exception("Error: expected value must either be x,X,y,Y");

            }


            return success;
        }
        public static bool ParsedDateIsValid(string dateToParse, out DateTime result)
        {
            bool success = false;
            result = DateTime.Now;
            if (DateTime.TryParse(dateToParse, out DateTime inDate))
            {
                if (inDate <= DateTime.Now)
                {
                    result = inDate;
                    success = true;
                }
            }
            return success;
        }
        public static Stream ToStream(this string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

    }
}
