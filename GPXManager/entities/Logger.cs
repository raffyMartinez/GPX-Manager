using System;
using System.IO;
using System.Threading.Tasks;
namespace GPXManager.entities
{
    public enum LogType
    {
        Logfile,
        ItemSets_csv,
        Gear_csv,
        Species_csv,
        EffortSpec_csv,
        SizeMeasure_csv,
        VesselName_csv,
        FMACode_csv,
        FishingGroundCode_csv,
        MajorGrid_csv,
        InlandGridCells_csv,
        FMAID_csv
    }
    public static class Logger
    {
        private static LogType _logType = LogType.Logfile;
        private static string _filepath = "";

        public static string LogTypeModifier { get; set; }

        public static LogType LogType
        {
            get { return _logType; }
            set
            {
                _logType = value;
                switch (_logType)
                {
                    case LogType.Logfile:
                        _filepath = $"{AppDomain.CurrentDomain.BaseDirectory}/log.txt";
                        break;

                    default:
                        break;
                }

                if (_logType != LogType.Logfile && File.Exists(_filepath))
                {
                    File.Delete(_filepath);
                }
            }
        }

        
        public static string FilePath
        {
            get { return _filepath; }
            set
            {
                if (value.Length > 0)
                    _filepath = value;
            }
        }

        public static void SetFilePathToDefault()
        {
            _filepath = $"{AppDomain.CurrentDomain.BaseDirectory}/log.txt";
        }

        static Logger()
        {
            SetFilePathToDefault();
        }


        public static async Task LogAsync(string s, string filePath="")
        {
            if (filePath.Length>0)
            {
                if(File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    await writer.WriteLineAsync(s);
                }
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(_filepath, true))
                {
                    await writer.WriteLineAsync(s);
                }
            }
        }

        public static void Log(string s, bool simpleLog = false)
        {
            using (StreamWriter writer = new StreamWriter(_filepath, true))
            {
                if (!simpleLog)
                {
                    writer.WriteLine($"Message: {s} Date :{DateTime.Now.ToString()}");
                }
                else
                {
                    writer.WriteLine(s);
                }
            }
        }

        public static void Log(string s, Exception ex)
        {
            using (StreamWriter writer = new StreamWriter(_filepath, true))
            {
                writer.WriteLine($"Log message:{s}\r\nError: {ex.Message}\r\n{ex.StackTrace}\r\n Date :{DateTime.Now.ToString()}");
            }
        }

        public static void Log(Exception ex)
        {
            using (StreamWriter writer = new StreamWriter(_filepath, true))
            {
                writer.WriteLine($"Error: {ex.Message}\r\n{ex.StackTrace}\r\n Date :{DateTime.Now.ToString()}");
            }
        }
    }
}