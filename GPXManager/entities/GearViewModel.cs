using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
namespace GPXManager.entities
{
    public class GearViewModel
    {
        private bool _operationSucceeded = false;
        public ObservableCollection<Gear> GearCollection { get; set; }
        private GearRepository Gears { get; set; }

        public void Serialize(string fileName)
        {
            SerializeGear serialize = new SerializeGear { Gears = GearCollection.ToList() };
            serialize.Save(fileName);
        }
        public GearViewModel()
        {
            Gears = new GearRepository();
            GearCollection = new ObservableCollection<Gear>(Gears.Gears);
            GearCollection.CollectionChanged += Gears_CollectionChanged;
        }

        private void Gears_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        Gear newGear = GearCollection[newIndex];
                        if (Gears.Add(newGear))
                        {
                            CurrentEntity = newGear;
                            _operationSucceeded = true;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        List<Gear> tempListOfRemovedItems = e.OldItems.OfType<Gear>().ToList();
                        _operationSucceeded = Gears.Delete(tempListOfRemovedItems[0].Code);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        List<Gear> tempList = e.NewItems.OfType<Gear>().ToList();
                        _operationSucceeded = Gears.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }
        public List<Gear> GetAllGears()
        {
            return GearCollection.ToList();
        }
        public bool GearNameExist(string gearName)
        {
            foreach (Gear g in GearCollection)
            {
                if (g.Name == gearName)
                {
                    return true;
                }
            }
            return false;
        }

        public bool GearCodeExist(string gearCode)
        {
            foreach (Gear g in GearCollection)
            {
                if (g.Code == gearCode)
                {
                    return true;
                }
            }
            return false;
        }
        public Gear GetGear(string code)
        {
            CurrentEntity = GearCollection.FirstOrDefault(n => n.Code == code);
            return CurrentEntity;
        }
        public Gear CurrentEntity { get; private set; }
        public int Count
        {
            get { return GearCollection.Count; }
        }

        public bool AddRecordToRepo(Gear gear)
        {
            if (gear == null)
                throw new ArgumentNullException("Error: The argument is Null");

            GearCollection.Add(gear);

            return _operationSucceeded;
        }

        public bool UpdateRecordInRepo(Gear gear)
        {
            if (gear.Code == null)
                throw new Exception("Error: ID cannot be null");

            int index = 0;
            while (index < GearCollection.Count)
            {
                if (GearCollection[index].Code == gear.Code)
                {
                    GearCollection[index] = gear;
                    break;
                }
                index++;
            }

            return _operationSucceeded;
        }

        public bool DeleteRecordFromRepo(string code)
        {
            if (code == null)
                throw new Exception("Record ID cannot be null");

            int index = 0;
            while (index < GearCollection.Count)
            {
                if (GearCollection[index].Code == code)
                {
                    GearCollection.RemoveAt(index);
                    break;
                }
                index++;
            }

            return _operationSucceeded;
        }
    }
}
