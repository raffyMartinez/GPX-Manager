using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace GPXManager.entities
{
    public class TripViewModel
    {
        private bool _operationSucceeded = false;
        public ObservableCollection<Trip> TripCollection { get; set; }
        private TripRepository Trips{ get; set; }

        public void Serialize(string fileName)
        {
            List <TripEdited> trips= new List<TripEdited>();
            foreach(var trip in TripCollection)
            {
                var tripWpts = new List<TripWaypointLite>();
                foreach(var tripWpt in Entities.TripWaypointViewModel.GetAllTripWaypoints(trip.TripID))
                {
                    tripWpts.Add(new TripWaypointLite(tripWpt));
                }
                trip.TripWaypoints = tripWpts;
                trips.Add(new TripEdited(trip));

            }

            SerializeTrips serialize = new SerializeTrips { Trips = trips };
            serialize.Save(fileName);
        }
        public TripViewModel()
        {
            Trips = new TripRepository();
            TripCollection = new ObservableCollection<Trip>(Trips.Trips);
            TripCollection.CollectionChanged += Trips_CollectionChanged;
        }

        private void Trips_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        Trip newItem = TripCollection[newIndex];
                        if (Trips.Add(newItem))
                        {
                            CurrentEntity = newItem;
                            _operationSucceeded = true;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        List<Trip> tempListOfRemovedItems = e.OldItems.OfType<Trip>().ToList();
                        _operationSucceeded = Trips.Delete(tempListOfRemovedItems[0].TripID);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        List<Trip> tempList = e.NewItems.OfType<Trip>().ToList();
                        _operationSucceeded = Trips.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }
        public List<Trip> GetAllTrips()
        {
            return TripCollection.ToList();
        }

        public List<DateTime>GetMonthYears()
        {
            var tripMonthYears = new List<DateTime>();
            foreach(Trip t in TripCollection)
            {
                DateTime monthYear = new DateTime(t.DateTimeDeparture.Year ,t.DateTimeDeparture.Month,1);
                if(!tripMonthYears.Contains(monthYear))
                {
                    tripMonthYears.Add(monthYear);
                }
            }
            return tripMonthYears;
        }
        public bool ClearRepository()
        {
            return Trips.ClearTable();
        }
        public int NextRecordNumber
        {
            get
            {
                if (TripCollection.Count == 0)
                {
                    return 1;
                }
                else
                {
                    return Trips.MaxRecordNumber() + 1;
                }
            }
        }

        public Trip GetLastTripOfDevice(string deviceID)
        {
            return TripCollection
                .Where(t=>t.DeviceID==deviceID)
                .OrderByDescending(t => t.TripID).FirstOrDefault();
        }
        public List<Trip>GetAllTrips(string deviceID)
        {
            return TripCollection.Where(t => t.DeviceID == deviceID).ToList();
        }

        public List<Trip> GetTrips(GPS gps, string gpxFileName)
        {
            return TripCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .Where(t => t.GPXFileName == gpxFileName)
                .ToList();
        }
        public List<Trip> GetTrips(GPS gps, DateTime dateOfTrip)
        {
            return TripCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .Where(t => t.DateTimeDeparture > dateOfTrip)
                .Where(t => t.DateTimeDeparture < dateOfTrip.AddDays(1))
                .ToList();
        }
        public Trip GetTrip(int tripID)
        {
            CurrentEntity = TripCollection.FirstOrDefault(n => n.TripID == tripID);
            return CurrentEntity;
        }
        public Trip CurrentEntity { get; private set; }
        public int Count
        {
            get { return TripCollection.Count; }
        }

        public List<Trip>TripsUsingGPSByDate(GPS gps, DateTime date)
        {
            return Entities.TripViewModel.TripCollection
                                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                                .Where(t => t.DateTimeDeparture.Date == date.Date).ToList();
        }
        public List<Trip>LatestTripsUsingGPS(GPS gps, int countLatestTrips=5)
        {
            return Entities.TripViewModel.TripCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .OrderByDescending(t => t.DateAdded)
                .Take(countLatestTrips).ToList();
        }
        public List<Trip>TripsUsingGPSByMonth(GPS gps, DateTime month)
        {
            return TripCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .Where(t => t.DateTimeDeparture > month)
                .Where(t => t.DateTimeDeparture < month.AddMonths(1))
                .OrderBy(t => t.DateTimeDeparture).ToList();
        }
        public Dictionary<DateTime, List<Trip>> TripArchivesByMonth(GPS gps)
        {
            var d =  TripCollection
                .Where(g => g.GPS.DeviceID == gps.DeviceID)
                .OrderBy(m => m.DateTimeDeparture)
                .GroupBy(o => o.MonthYear)
                .ToDictionary(g => g.Key, g => g.ToList());

            return d;
        }

        public void MarkAllNotShownInMap()
        {
            foreach(var item in TripCollection.Where(t => t.ShownInMap))
            {
                item.ShownInMap = false;
            }
        }

        public bool AddRecordToRepo(Trip trip)
        {
            if (trip == null)
                throw new ArgumentNullException("Error: The argument is Null");

            trip.DateAdded = DateTime.Now;
            TripCollection.Add(trip);

            return _operationSucceeded;
        }

        public bool UpdateRecordInRepo(Trip trip)
        {
            if (trip.TripID == 0)
                throw new Exception("Error: ID cannot be null");

            int index = 0;
            while (index < TripCollection.Count)
            {
                if (TripCollection[index].TripID == trip.TripID)
                {
                    TripCollection[index] = trip;
                    break;
                }
                index++;
            }

            return _operationSucceeded;
        }

        public bool DeleteRecordFromRepo(int tripID)
        {
            if (tripID == 0)
                throw new Exception("Trip ID cannot be null");

            int index = 0;
            while (index < TripCollection.Count)
            {
                if (TripCollection[index].TripID == tripID)
                {
                    TripCollection.RemoveAt(index);
                    break;
                }
                index++;
            }

            return _operationSucceeded;
        }

        public EntityValidationResult ValidateTrip(Trip trip, bool isNew)
        {
            EntityValidationResult evr = new EntityValidationResult();

            if (trip.OperatorName== null || trip.OperatorName.Length<3)
            {
                evr.AddMessage("Operator name must be at least 3 letters long");
            }

            if (trip.VesselName== null || trip.VesselName.Length < 3)
            {
                evr.AddMessage("Vessel name 3 letters long");
            }

            if (trip.Gear == null && trip.OtherGear==null)
            {
                evr.AddMessage("Gear or gear other name cannnot be both empty");
            }

            if (trip.DateTimeDeparture == null || trip.DateTimeDeparture > DateTime.Now)
            {
                evr.AddMessage("Date and time of departure cannot be empty and cannot be in the future");
            }
            else if (trip.DateTimeArrival== null || trip.DateTimeArrival>DateTime.Now)
            {
                evr.AddMessage("Date and time of arrival cannot be empty and cannot be in the future");
            }
            else if(trip.DateTimeDeparture >= trip.DateTimeArrival)
            {
                evr.AddMessage("Date and time of departure must be before date and time of arrival");
            }


            int? overlapID = null;
            foreach (var tripItem in TripCollection
                .Where(t=>t.TripID!= trip.TripID)
                .Where(t=>t.DeviceID==trip.DeviceID))
            {
                if (trip.DateTimeDeparture >= tripItem.DateTimeDeparture && trip.DateTimeDeparture <= tripItem.DateTimeArrival)
                {
                    overlapID = tripItem.TripID;
                    break;
                }
                else if (trip.DateTimeArrival >= tripItem.DateTimeDeparture && trip.DateTimeArrival <= tripItem.DateTimeArrival)
                {
                    overlapID = tripItem.TripID;
                    break;
                }
                else if (trip.DateTimeDeparture <= tripItem.DateTimeDeparture && trip.DateTimeArrival >= tripItem.DateTimeArrival)
                {
                    overlapID = tripItem.TripID;
                    break;
                }
            }
            if(overlapID!=null)
            {
                evr.AddMessage($"This trip overlaps with trip ID {overlapID}");
            }
            

            return evr;
        }
    }
}
