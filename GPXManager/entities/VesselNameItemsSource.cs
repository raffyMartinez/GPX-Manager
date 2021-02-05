using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace GPXManager.entities
{
    class VesselNameItemsSource:IItemsSource
    {
        public ItemCollection GetValues()
        {
            ItemCollection vesselNames = new ItemCollection();
            if (Entities.FisherViewModel.SelectedTripVesselNameList != null)
            {
                foreach (var name in Entities.FisherViewModel.SelectedTripVesselNameList)
                {
                    vesselNames.Add(name);
                }
            }
            return vesselNames;
        }
    }
}
