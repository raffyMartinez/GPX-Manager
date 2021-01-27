using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GongSolutions.Wpf;
using GongSolutions.Wpf.DragDrop;
using System.Collections.ObjectModel;

namespace GPXManager.entities.mapping
{
    public class MapLayersViewModel:IDropTarget
    {
        public delegate void LayerReadHandler(MapLayersViewModel s, LayerEventArg e);                 //an event that is raised when a layer from the mapcontrol is retrieved
        public event LayerReadHandler LayerRead;

        public delegate void LayerRemovedHandler(MapLayersViewModel s, LayerEventArg e);
        public event LayerRemovedHandler LayerRemoved;

        public delegate void CurrentLayerHandler(MapLayersViewModel s, LayerEventArg e);              //event raised when a layer is selected from the list found in the layers form
        public event CurrentLayerHandler CurrentLayer;



        public ObservableCollection<MapLayer> MapLayerCollection{ get; set; }
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            MapWindowManager.MapWindowForm.Title = "Draggin' over";
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            MapWindowManager.MapWindowForm.Title = "Droppin'";
        }

        public MapLayer FirstLayer()
        {
            return MapLayerCollection[0];
        }

        public void RefreshCollection()
        {
            if (MaplayersHandler != null)
            {
                
                foreach (MapLayer ly in MaplayersHandler.MapLayerDictionary.Values.OrderByDescending(t => t.LayerPosition))
                {
                    MapLayerCollection.Add(ly);
                }
            }
            else
            {
                throw new ArgumentNullException("Error: Maplayershandler is null");
            }
        }


        public MapLayersHandler MaplayersHandler { get;  private set; }
        public MapLayersViewModel(MapLayersHandler mapLayersHandler)
        {
            MaplayersHandler = mapLayersHandler;
            MaplayersHandler.LayerRead += MaplayersHandler_LayerRead;
            MaplayersHandler.LayerRemoved += MaplayersHandler_LayerRemoved;
            MaplayersHandler.CurrentLayer += MaplayersHandler_CurrentLayer;
            MapLayerCollection = new ObservableCollection<MapLayer>();
            MapLayerCollection.CollectionChanged += MapLayerCollection_CollectionChanged;
            RefreshCollection();
        }

        private void MaplayersHandler_CurrentLayer(MapLayersHandler s, LayerEventArg e)
        {
            if (CurrentLayer != null)
            {
                //fill up the event argument class with the layer item
                LayerEventArg lp = new LayerEventArg(s.CurrentMapLayer.Handle, s.CurrentMapLayer.Name, s.CurrentMapLayer.Visible, s.CurrentMapLayer.VisibleInLayersUI, s.CurrentMapLayer.LayerType);
                CurrentLayer(this, lp);
                
            }
        }

        private void MapLayerCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            
        }

        private void MaplayersHandler_LayerRemoved(MapLayersHandler s, LayerEventArg e)
        {
            int index = 0;
            while (index < MapLayerCollection.Count)
            {
                if (MapLayerCollection[index].Handle == e.LayerHandle)
                {
                    MapLayerCollection.RemoveAt(index);

                    if (LayerRemoved != null)
                    {
                        LayerEventArg lp = new LayerEventArg(e.LayerHandle, layerRemoved: true);
                        LayerRemoved(this, lp);
                    }
                    break;
                }
                index++;
            }
        }

        public void CleanUp()
        {
            MapLayerCollection.Clear();
            MapLayerCollection = null;
            MaplayersHandler.LayerRead -= MaplayersHandler_LayerRead;
            MaplayersHandler.LayerRemoved -= MaplayersHandler_LayerRemoved;
            MaplayersHandler = null;
        }

        private void MaplayersHandler_LayerRead(MapLayersHandler s, LayerEventArg e)
        {

            MapLayerCollection.Insert(0, s.CurrentMapLayer);

            if (LayerRead != null)
            {

                var item = MaplayersHandler.MapLayerDictionary[e.LayerHandle];


                LayerEventArg lp = new LayerEventArg(item.Handle, item.Name, item.Visible, item.VisibleInLayersUI, item.LayerType);
                LayerRead(this, lp);
            }
        }
    }
}
