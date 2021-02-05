using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
namespace GPXManager.entities
{
    public class FisherViewModel
    {
        public delegate void EntityChangedEvent(object sender, EventArgs e);
        private List<string> _selectedTripVesselNameList;
        public event EntityChangedEvent EntitiesChanged;
        public bool EditSuccess { get; private set; }
        public ObservableCollection<Fisher> FisherCollection { get; set; }
        private FisherRepository Fishers { get; set; }


        public void SelectedTripFisherID (int fisherID)
        {
            _selectedTripVesselNameList = GetFisherBoats(fisherID);
        }

        public List<string> SelectedTripVesselNameList { get { return _selectedTripVesselNameList; } }
        public Fisher CurrentEntity { get; private set; }

        public Fisher GetFisher(int ID)
        {
            return FisherCollection.Where(t => t.FisherID == ID).FirstOrDefault();
        }
        
        public List<string>GetFisherBoats(int fisherID)
        {
            return FisherCollection.FirstOrDefault(t => t.FisherID == fisherID).Vessels;
        }

        public List<Fisher> GetAll()
        {
            return FisherCollection.OrderBy(t => t.Name).ToList();
        }
        public FisherViewModel()
        {
            Fishers = new FisherRepository();
            FisherCollection = new ObservableCollection<Fisher>(Fishers.Fishers);
            FisherCollection.CollectionChanged += FisherCollection_CollectionChanged;
        }

        private void FisherCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TypeOfChange c = TypeOfChange.Added;
            EditSuccess = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        Fisher newFisher = FisherCollection[newIndex];

                        if (Fishers.Add(newFisher))
                        {
                            CurrentEntity = newFisher;
                            EditSuccess = true;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {

                        List<Fisher> tempListOfRemovedItems = e.OldItems.OfType<Fisher>().ToList();
                        if(Fishers.Delete(tempListOfRemovedItems[0]))
                        {
                            EditSuccess = true;
                            CurrentEntity = null;
                        }
                        c = TypeOfChange.Deleted;

                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<Fisher> tempList = e.NewItems.OfType<Fisher>().ToList();
                        if (Fishers.Update(tempList[0]))
                        {
                            EditSuccess = true;
                            CurrentEntity = tempList[0];
                        }
                        c = TypeOfChange.Edited;
                    }
                    break;
            }

            if (EditSuccess && EntitiesChanged != null)
            {
                EntitiesChangedEventArg ece = new EntitiesChangedEventArg
                {
                    TypeOfChange = c,
                    Entity = CurrentEntity
                };
                EntitiesChanged(this, ece);
            }
        }
        public int NextRecordNumber()
        {
            int rv;
            if (FisherCollection.Count == 0)
            {
                rv = 1;
            }
            else
            {
                rv = Fishers.MaxRecordNumber() + 1; ;
            }
            return rv;
        }
        public bool AddRecordToRepo(Fisher fisher)
        {
            if (fisher == null)
                throw new ArgumentNullException("Error: The argument is Null");

            FisherCollection.Add(fisher);

            return EditSuccess;
        }

        public bool UpdateRecordInRepo(Fisher fisher)
        {
            if (fisher.FisherID == 0)
                throw new Exception("Error: ID must be greater than zero");

            int index = 0;
            while (index < FisherCollection.Count)
            {
                if (FisherCollection[index].FisherID == fisher.FisherID)
                {
                    FisherCollection[index] = fisher;
                    break;
                }
                index++;
            }
            return EditSuccess;
        }

        public void DeleteRecordFromRepo(Fisher fisher)
        {
            if (fisher == null)
                throw new Exception("Fisher cannot be null");

            int index = 0;
            while (index < FisherCollection.Count)
            {
                if (FisherCollection[index].FisherID == fisher.FisherID)
                {
                    FisherCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
