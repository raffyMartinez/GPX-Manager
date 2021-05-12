using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace GPXManager.entities
{
    public class LandingSiteViewModel
    {
        private bool _editSuccess;
        public ObservableCollection<LandingSite> LandingSiteCollection { get; set; }
        private LandingSiteRepository LandingSites { get; set; }
        public LandingSiteViewModel()
        {
            LandingSites = new LandingSiteRepository();
            LandingSiteCollection = new ObservableCollection<LandingSite>(LandingSites.LandingSites);
            LandingSiteCollection.CollectionChanged += LandingSiteCollection_CollectionChanged;
        }

        public List<LandingSite>GetAll()
        {
            return LandingSiteCollection.OrderBy(t => t.Name).ToList();
        }
        public LandingSite GetLandingSite(int id)
        {
            return LandingSiteCollection.FirstOrDefault(t => t.ID == id);
        }
        public LandingSite CurrentEntity { get; set; }
        private void LandingSiteCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _editSuccess = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        LandingSite newLandingSite = LandingSiteCollection[newIndex];

                        if (LandingSites.Add(newLandingSite))
                        {
                            CurrentEntity = newLandingSite;
                            _editSuccess = true;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {

                        List<LandingSite> tempListOfRemovedItems = e.OldItems.OfType<LandingSite>().ToList();
                        if (LandingSites.Delete(tempListOfRemovedItems[0].ID))
                        {
                            _editSuccess = true;
                            CurrentEntity = null;
                        }

                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<LandingSite> tempList = e.NewItems.OfType<LandingSite>().ToList();
                        if (LandingSites.Update(tempList[0]))
                        {
                            _editSuccess = true;
                            CurrentEntity = tempList[0];
                        }
                    }
                    break;
            }
        }

        public int NextRecordNumber()
        {
            int rv;
            if (LandingSiteCollection.Count == 0)
            {
                rv = 1;
            }
            else
            {
                rv = LandingSites.MaxRecordNumber() + 1; ;
            }
            return rv;
        }
        public bool AddRecordToRepo(LandingSite ls)
        {
            if (ls == null)
                throw new ArgumentNullException("Error: The argument is Null");

            LandingSiteCollection.Add(ls);

            return _editSuccess;
        }

        public bool UpdateRecordInRepo(LandingSite ls)
        {
            if (ls==null)
                throw new Exception("Error: Landing site cannot be null");

            int index = 0;
            while (index < LandingSiteCollection.Count)
            {
                if (LandingSiteCollection[index].ID == ls.ID)
                {
                    LandingSiteCollection[index] = ls;
                    break;
                }
                index++;
            }
            return _editSuccess;
        }

        public void DeleteRecordFromRepo(LandingSite ls)
        {
            if (ls == null)
                throw new Exception("Landing site cannot be null");

            int index = 0;
            while (index < LandingSiteCollection.Count)
            {
                if (LandingSiteCollection[index].ID == ls.ID)
                {
                    LandingSiteCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
