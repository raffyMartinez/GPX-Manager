using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace GPXManager.entities
{
    public class TripCalendarDay
    {
        public GPS GPS { get; set; }
        public DateTime TripDate { get; set; }
        public Trip Trip { get; set; }
        public List<int?> NumberOfTripsPerDay { get; set; }
    }
    public class TripCalendarViewModel
    {
        private int _numberOfDays;
        public DataTable DataTable { get; set; }

        public List<TripCalendarDay> TripCalendarDays { get; set; }

        public DateTime MonthYear { get; set; }
        public TripCalendarViewModel(DateTime monthYear)
        {
            MonthYear = monthYear;
            TripCalendarDays = new List<TripCalendarDay>();
            _numberOfDays = DateTime.DaysInMonth(monthYear.Year, monthYear.Month);
            if(Entities.TripViewModel.Count>0)
            {
                TripCalendarDay calendarDay = null;
                foreach(var gps in Entities.GPSViewModel.GPSCollection)
                {
                    var trips = Entities.TripViewModel.TripCollection
                        .Where(t => t.GPS.DeviceID == gps.DeviceID)
                        .Where(t=>t.DateTimeDeparture.Year==MonthYear.Year)
                        .Where(t=>t.DateTimeDeparture.Month==MonthYear.Month)
                        .OrderBy(t=>t.DateTimeDeparture)
                        .ToList();

                    foreach(var trip in trips)
                    {
                        calendarDay = new TripCalendarDay
                        {
                            GPS = gps,
                            TripDate = trip.DateTimeDeparture,
                            Trip = trip
                        };

                        TripCalendarDays.Add(calendarDay);
                    }
                }
            }
            BuildCalendar();
        }

        public void BuildCalendar()
        {
            DataTable = new DataTable();
            DataTable.Columns.Add("GPS");
            DataTable.Columns.Add("GpsDeviceID");

            for (int n = 1; n <= _numberOfDays; n++)
            {
                DataTable.Columns.Add(n.ToString());
            }

            foreach (var gps in Entities.GPSViewModel.GPSCollection)
            {
                var row = DataTable.NewRow();
                row["GPS"] = gps.DeviceName;
                row["GPSDeviceID"] = gps.DeviceID;
                for(int n = 1; n <= _numberOfDays; n++)
                {
                    var trip = TripCalendarDays
                                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                                .Where(t => t.TripDate.Day == n)
                                .FirstOrDefault();
                    row[n.ToString()] = trip == null ? "" : "x";
                }
                DataTable.Rows.Add(row);
            }
        }

    }
}
