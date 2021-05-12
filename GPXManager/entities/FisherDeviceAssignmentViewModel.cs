using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace GPXManager.entities
{
    public class FisherDeviceAssignmentViewModel
    {
        public ObservableCollection<FisherDeviceAssignment> FisherDeviceAssignmentCollection { get; set; }
        private FisherDeviceAssignmentRepository FisherDeivieAssignments { get; set; }

        private bool _editSuccess;

        public FisherDeviceAssignment CurrentEntity { get; set; }
        public FisherDeviceAssignmentViewModel()
        {
            FisherDeivieAssignments = new FisherDeviceAssignmentRepository();
            FisherDeviceAssignmentCollection = new ObservableCollection<FisherDeviceAssignment>(FisherDeivieAssignments.FisherDeviceAssignments);
            FisherDeviceAssignmentCollection.CollectionChanged += FisherDeviceAssignmentCollection_CollectionChanged;
        }

        private void FisherDeviceAssignmentCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _editSuccess = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        FisherDeviceAssignment newFDA = FisherDeviceAssignmentCollection[newIndex];

                        _editSuccess = FisherDeivieAssignments.Add(newFDA);
                        if (_editSuccess)
                        {
                            CurrentEntity = newFDA;
                        }

                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<FisherDeviceAssignment> tempListOfRemovedItems = e.OldItems.OfType<FisherDeviceAssignment>().ToList();
                        _editSuccess = FisherDeivieAssignments.Delete(tempListOfRemovedItems[0].RowID);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<FisherDeviceAssignment> tempList = e.NewItems.OfType<FisherDeviceAssignment>().ToList();
                        _editSuccess = FisherDeivieAssignments.Update(tempList[0]);
                    }
                    break;
            }
        }

        public bool AddRecordToRepo(FisherDeviceAssignment fda)
        {
            if (fda == null)
                throw new ArgumentNullException("Error: The argument is Null");

            FisherDeviceAssignmentCollection.Add(fda);

            return _editSuccess;
        }

        public bool UpdateRecordInRepo(FisherDeviceAssignment fda)
        {
            if (fda == null)
                throw new Exception("Error: The argument is Null");

            int index = 0;
            while (index < FisherDeviceAssignmentCollection.Count)
            {
                if (FisherDeviceAssignmentCollection[index].RowID == fda.RowID)
                {
                    FisherDeviceAssignmentCollection[index] = fda;
                    break;
                }
                index++;
            }
            return _editSuccess;
        }

        public bool DeleteRecordFromRepo(int id)
        {
            if (id == 0)
                throw new Exception("Record ID cannot be null");

            int index = 0;
            while (index < FisherDeviceAssignmentCollection.Count)
            {
                if (FisherDeviceAssignmentCollection[index].RowID == id)
                {
                    FisherDeviceAssignmentCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
            return _editSuccess;
        }
    }
}
