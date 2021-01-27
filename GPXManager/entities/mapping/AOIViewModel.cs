using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
namespace GPXManager.entities.mapping
{
     public class AOIViewModel
    {
        public ObservableCollection<AOI> AOICollection{ get; set; }
        private AOIRepository AOIs { get; set; }

        public AOIViewModel()
        {
            AOIs = new AOIRepository();
            AOICollection = new ObservableCollection<AOI>(AOIs.AOIs);
            AOICollection.CollectionChanged += AOICollection_CollectionChanged;
        }



        private void AOICollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        AOI newAOI = AOICollection[newIndex];
                        if (AOIs.Add(newAOI))
                        {
                            CurrentEntity = newAOI;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<AOI> tempListOfRemovedItems = e.OldItems.OfType<AOI>().ToList();
                        AOIs.Delete(tempListOfRemovedItems[0].ID);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<AOI> tempList = e.NewItems.OfType<AOI>().ToList();
                        AOIs.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }
        public List<AOI> GetAllAOI()
        {
            return AOICollection.ToList();
        }
        public bool AOINameExist(string name)
        {
            return AOICollection.Where(t => t.Name == name).FirstOrDefault() != null; 
        }
        public AOI CurrentEntity { get; set; }

        public AOI GetAOI(int id)
        {
            CurrentEntity = AOICollection.FirstOrDefault(n => n.ID== id);
            return CurrentEntity;

        }

        public AOI GetAOI(string name)
        {
            CurrentEntity = AOICollection.FirstOrDefault(n => n.Name == name);
            return CurrentEntity;
        }
        public int Count
        {
            get { return AOICollection.Count; }
        }

        public void AddRecordToRepo(AOI aoi)
        {
            if (aoi == null)
                throw new ArgumentNullException("Error: The argument is Null");
            AOICollection.Add(aoi);
        }

        public void UpdateRecordInRepo(AOI aoi)
        {
            if (aoi.ID == 0)
                throw new Exception("Error: ID cannot be null");

            int index = 0;
            while (index < AOICollection.Count)
            {
                if (AOICollection[index].ID == aoi.ID)
                {
                    AOICollection[index] = aoi;
                    break;
                }
                index++;
            }
        }

        public void DeleteRecordFromRepo(int id)
        {
            if (id == 0)
                throw new Exception("Record ID cannot be null");

            int index = 0;
            while (index < AOICollection.Count)
            {
                if (AOICollection[index].ID == id)
                {
                    AOICollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
        public int NextRecordNumber
        {
            get
            {
                if (AOICollection.Count == 0)
                {
                    return 1;
                }
                else
                {
                    return AOIs.MaxRecordNumber() + 1;
                }
            }
        }
        public EntityValidationResult ValidateAOI(AOI aoi, bool isNew, string oldName)
        {
            EntityValidationResult evr = new EntityValidationResult();

            if (isNew && (aoi.Name == null || aoi.Name.Length < 2))
            {
                evr.AddMessage("Device name must be at least 2 letters long");
            }

            if (isNew && (aoi.ID == 0))
            {
                evr.AddMessage("AOI ID cannot be zero");
            }

            if (!isNew && aoi.Name.Length > 0
                && oldName != aoi.Name
                && AOINameExist(aoi.Name))
                evr.AddMessage("AOI name already used");


            if (isNew && aoi.Name.Length > 0 && AOINameExist(aoi.Name))
            {
                evr.AddMessage("AOI name already used");
            }

            return evr;
        }
    }
}
