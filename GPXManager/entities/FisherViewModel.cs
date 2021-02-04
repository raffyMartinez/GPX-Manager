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
        public bool EditSuccess { get; private set; }
        public ObservableCollection<Fisher> FisherCollection { get; set; }
        private FisherRepository Fishers{ get; set; }

        public Fisher CurrentEntity { get; private set; }

        public Fisher GetFisher(int ID)
        {
            return FisherCollection.Where(t => t.FIsherID == ID).FirstOrDefault();
        }
        public FisherViewModel()
        {
            Fishers = new FisherRepository();
            FisherCollection = new ObservableCollection<Fisher>(Fishers.Fishers);
            FisherCollection.CollectionChanged += FisherCollection_CollectionChanged;
        }

        private void FisherCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EditSuccess = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        Fisher newFisher= FisherCollection[newIndex];

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
                        EditSuccess = Fishers.Delete(tempListOfRemovedItems[0]);

                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<Fisher> tempList = e.NewItems.OfType<Fisher>().ToList();
                        EditSuccess = Fishers.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
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
            if (fisher.FIsherID== 0)
                throw new Exception("Error: ID must be greater than zero");

            int index = 0;
            while (index < FisherCollection.Count)
            {
                if (FisherCollection[index].FIsherID== fisher.FIsherID)
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
                if (FisherCollection[index].FIsherID == fisher.FIsherID)
                {
                    FisherCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
