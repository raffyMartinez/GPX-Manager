
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
namespace GPXManager.entities
{
    public class FisherItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            var fishers = new ItemCollection();
            foreach (var item in Entities.FisherViewModel.GetAll())
            {
                fishers.Add(item.FisherID, item.Name);
            }
            return fishers;
        }
    }
}
