using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for AOIWindow.xaml
    /// </summary>
    public partial class AOIWindow : Window
    {
        private int _gridRow;
        private AOI _aoi;
        private bool _editingAOI;
        public AOIWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
            ResetView();
        }
        private void ResetView()
        {
            rowAOIName.Height = new GridLength(0);
            rowAOIList.Height = new GridLength(0);
        }

        public void AddNewAOI()
        {
            rowAOIName.Height = new GridLength(60);
        }

        public void ShowAOIList()
        {
            rowAOIList.Height = new GridLength(1, GridUnitType.Star);

            dataGridAOIs.Columns.Clear();
            dataGridAOIs.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("ID") });
            dataGridAOIs.Columns.Add(new DataGridCheckBoxColumn { Header = "Visibility", Binding = new Binding("Visibility") });
            dataGridAOIs.Columns.Add(new DataGridTextColumn { Header = "AOI name", Binding = new Binding("Name") });
            dataGridAOIs.DataContext = Entities.AOIViewModel.AOICollection.OrderBy(t => t.Name).ToList();
            
            foreach(var aoi in Entities.AOIViewModel.AOICollection)
            {
                aoi.MapLayerHandle = MapWindowManager.MapLayersHandler.AddLayer(aoi.ShapeFile, aoi.Name, uniqueLayer: true, layerKey: "aoi_boundary"); 
            }

            buttonOk.IsEnabled = false;
            //buttonCancel.Content = "Close";
        }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            MapWindowManager.ResetCursor();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonCancel":
                    MapWindowManager.MapLayersHandler.RemoveLayer(AOIManager._hAOI);
                    Close();
                    break;
                case "buttonOk":
                    if(textBoxAOIName.Text.Length>0)
                    {
                        _aoi = AOIManager.SaveAOI(textBoxAOIName.Text);
                        
                        if (_aoi != null)
                        {
                            Close();
                        }
                    }
                    else if(_editingAOI)
                    {
                        _aoi = AOIManager.SaveAOI(_aoi.Name, true);
                         buttonOk.IsEnabled = _aoi==null;
                        
                    }
                    break;
            }
        }

        private void OnMenuClick(object sender, RoutedEventArgs e)
        {
            switch(((MenuItem)sender).Name)
            {
                case "menuAOIZoom":
                    if (_aoi != null)
                    {
                        MapWindowManager.ZoomToShapeFileExtent(_aoi.ShapeFile);
                    }
                    break;
                case "menuAOIEditExtent":
                    if(_aoi !=null)
                    {
                        _editingAOI = true;
                        AOIManager.Edit(_aoi);
                        buttonOk.IsEnabled = true;
                    }
                    break;
                case "menuAOIRemove":
                    if (_aoi != null)
                    {
                        MapWindowManager.MapLayersHandler.RemoveLayer(_aoi.MapLayerHandle);
                    }
                        break;
            }
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void OnGridSelectedCellChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGridAOIs.SelectedCells.Count == 1)
            {
                DataGridCellInfo cell = dataGridAOIs.SelectedCells[0];
                _gridRow = dataGridAOIs.Items.IndexOf(cell.Item);
                _aoi = (AOI)dataGridAOIs.Items[_gridRow];
                
            }

        }
    }
}
