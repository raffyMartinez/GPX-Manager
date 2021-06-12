using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities.mapping
{
    public class ShapefileFieldSummary
    {
        public ShapefileFieldSummary() { }

        public ShapefileFieldSummary(object list, string type, string name)
        {
            Name = name;
            Type = type;
            switch (type)
            {
                case "Int":
                    var listInt = (List<int>)list;
                    Max = listInt.Max();
                    Min = listInt.Min();
                    Sum = listInt.Sum();
                    Count = listInt.Count()+1;
                    Average = listInt.Average();
                    MaxAsString = ((int)Max).ToString();
                    MinAsString = ((int)Min).ToString();
                    SumAsString = ((int)Sum).ToString();
                    AverageAsString = ((int)Average).ToString();
                    break;
                case "Double":
                    var listDouble = (List<Double>)list;
                    Max = listDouble.Max();
                    Min = listDouble.Min();
                    Sum = listDouble.Sum();
                    Count = listDouble.Count()+1;
                    Average = listDouble.Average();
                    MaxAsString = ((double)Max).ToString("000.00");
                    MinAsString = ((double)Min).ToString("000.00");
                    SumAsString = ((double)Sum).ToString("000.00");
                    AverageAsString = ((double)Average).ToString("000.00");
                    break;

                case "DateTime":
                    var dt = (List<DateTime>)list;
                    First = dt.Min();
                    Last = dt.Max();
                    Count = dt.Count()+1;
                    MaxAsString = ((DateTime)Last).ToString("MMM-dd-yyyy HH:mm");
                    MinAsString = ((DateTime)First).ToString("MMM-dd-yyyy HH:mm");
                    TimeSpan = (DateTime)Last - (DateTime)First;
                    SumAsString = ((TimeSpan)TimeSpan).ToString();
                    AverageAsString = "";
                    break;
            }
        }

        public Dictionary<string, int> FieldEntriesAndCount { get; set; } = new Dictionary<string, int>();
        public int Count { get; private set; }
        public string MaxAsString { get; private set; }
        public string MinAsString { get; private set; }
        public string SumAsString { get; private set; }
        public string AverageAsString { get; private set; }
        public string Type { get; private set; }
        public string Name { get; private set; }
        public double? Max { get; private set; }
        public double? Min { get; private set; }

        public DateTime? First { get; private set; }
        public DateTime? Last { get; private set; }
        public double? Sum { get; private set; }
        public TimeSpan? TimeSpan { get; private set; }

        public double? Average { get; set; }
    }
}
