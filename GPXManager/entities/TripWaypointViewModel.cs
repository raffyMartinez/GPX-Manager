using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.AccessControl;

namespace GPXManager.entities
{
    public class TripWaypointViewModel
    {
        private bool _operationSucceeded = false;
        public ObservableCollection<TripWaypoint> TripWaypointCollection { get; set; }
        private TripWaypointRepository TripWaypoints { get; set; }

        public bool ClearRepository()
        {
            return TripWaypoints.ClearTable();
        }
        public int NextSetNumber(Trip trip)
        {
            if(TripWaypointCollection.Count==0)
            {
                return 1;
            }
            else
            {
                var thistrip = TripWaypointCollection
                    .Where(t => t.Trip.TripID == trip.TripID)
                    .OrderByDescending(t => t.Trip.TripID)
                    .LastOrDefault();

                if(thistrip==null)
                {
                    return 1;
                }
                else
                {
                    return thistrip.SetNumber + 1;
                }
            }

        }
        public TripWaypointViewModel()
        {
            TripWaypoints = new TripWaypointRepository();
            TripWaypointCollection = new ObservableCollection<TripWaypoint>(TripWaypoints.TripWaypoints);
            TripWaypointCollection.CollectionChanged += TripWaypoints_CollectionChanged;
        }

        private void TripWaypoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        TripWaypoint newItem = TripWaypointCollection[newIndex];
                        if (TripWaypoints.Add(newItem))
                        {
                            CurrentEntity = newItem;
                            _operationSucceeded = true;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        List<TripWaypoint> tempListOfRemovedItems = e.OldItems.OfType<TripWaypoint>().ToList();
                        _operationSucceeded = TripWaypoints.Delete(tempListOfRemovedItems[0].RowID);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        List<TripWaypoint> tempList = e.NewItems.OfType<TripWaypoint>().ToList();
                        _operationSucceeded = TripWaypoints.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }
        public List<TripWaypoint> GetAllTripWaypoints()
        {
            return TripWaypointCollection.ToList();
        }

        public TripWaypoint GetTripWaypoint(TripWaypoint tpw)
        {
            return TripWaypointCollection
                .Where(t => t.Trip.GPS.DeviceID == tpw.Trip.GPS.DeviceID)
                .Where(t => t.WaypointName == tpw.WaypointName).FirstOrDefault();
        }
        public int NextRecordNumber
        {
            get
            {
                if (TripWaypointCollection.Count == 0)
                {
                    return 1;
                }
                else
                {
                    return TripWaypoints.MaxRecordNumber() + 1;
                }
            }
        }

        public List<TripWaypoint> GetAllTripWaypoints(int tripID)
        {
            return TripWaypointCollection.Where(t => t.Trip.TripID == tripID).ToList();
        }
        public TripWaypoint GetTripWaypoint(int rowID)
        {
            CurrentEntity = TripWaypointCollection.FirstOrDefault(n => n.RowID == rowID);
            return CurrentEntity;
        }
        public TripWaypoint CurrentEntity { get; private set; }
        public int Count
        {
            get { return TripWaypointCollection.Count; }
        }

        public bool AddRecordToRepo(TripWaypoint tw)
        {
            if (tw == null)
                throw new ArgumentNullException("Error: The argument is Null");

            TripWaypointCollection.Add(tw);

            return _operationSucceeded;
        }


        public bool UpdateRecordInRepo(TripWaypoint tw)
        {
            if (tw.RowID == 0)
                throw new Exception("Error: ID cannot be null");

            int index = 0;
            while (index < TripWaypointCollection.Count)
            {
                if (TripWaypointCollection[index].RowID == tw.RowID)
                {
                    TripWaypointCollection[index] = tw;
                    break;
                }
                index++;
            }

            return _operationSucceeded;
        }

        public bool DeleteRecordFromRepo(int rowID)
        {
            if (rowID == 0)
                throw new Exception("Trip ID cannot be null");

            int index = 0;
            while (index < TripWaypointCollection.Count)
            {
                if (TripWaypointCollection[index].RowID == rowID)
                {
                    TripWaypointCollection.RemoveAt(index);
                    break;
                }
                index++;
            }

            return _operationSucceeded;
        }

        public EntityValidationResult ValidateTrip(TripWaypoint tw, bool isNew, Waypoint oldWaypoint=null)
        {
            EntityValidationResult evr = new EntityValidationResult();

            if (tw.WaypointName == null || tw.WaypointName.Length < 1)
            {
                evr.AddMessage("Waypoint name must be at least 1 character long");
            }

            if(tw.TimeStamp <= tw.Trip.DateTimeDeparture || tw.TimeStamp >= tw.Trip.DateTimeArrival)
            {
                evr.AddMessage("Waypoint timestamp must be within departure and arrival time of trip");
            }

            if (tw.WaypointType == null || tw.WaypointType.Length < 3)
            {
                evr.AddMessage("Waypoint type is not valid");
            }

            if(tw.SetNumber==0)
            {
                evr.AddMessage("Set number is not valid");
            }

            if(!isNew && tw.Waypoint.Name != oldWaypoint.Name)
            {
                if(GetTripWaypoint(tw)!=null)
                {
                    evr.AddMessage("Waypoint already in use");
                }
            }

            if (isNew && GetTripWaypoint(tw) != null)
            {
                evr.AddMessage("Waypoint already in use");
            }




            return evr;
        }
    }
}
