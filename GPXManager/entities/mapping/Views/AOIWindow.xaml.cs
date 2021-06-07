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
using System.IO;
using MapWinGIS;

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
        //private Shapefile _gridShapefile;
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

        //public Shapefile GridShapefile
        //{
        //    get { return _gridShapefile; }
        //    set
        //    {

        //        _gridShapefile = value;
        //    }
        //}

        public void ShowAOIList()
        {
            rowAOIList.Height = new GridLength(1, GridUnitType.Star);

            dataGridAOIs.Columns.Clear();
            dataGridAOIs.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("ID") });
            //dataGridAOIs.Columns.Add(new DataGridCheckBoxColumn { Header = "Visibility", Binding = new Binding("Visibility") });
            dataGridAOIs.Columns.Add(new DataGridTextColumn { Header = "AOI name", Binding = new Binding("Name") });
            dataGridAOIs.Columns.Add(new DataGridTextColumn { Header = "Grid size", Binding = new Binding("GridSize") });
            dataGridAOIs.Columns.Add(new DataGridCheckBoxColumn { Header = "Select", Binding = new Binding("Selected") });
            SetDataGridContext();

            foreach (var aoi in Entities.AOIViewModel.AOICollection)
            {
                var h = aoi.MapLayerHandle = MapWindowManager.MapLayersHandler.AddLayer(aoi.ShapeFile, aoi.Name, uniqueLayer: true, layerKey: aoi.ShapeFile.Key);
                aoi.AOIHandle = h;
                var sf = (Shapefile)MapWindowManager.MapLayersHandler[h].LayerObject;

                sf.DefaultDrawingOptions.LineColor = new MapWinGIS.Utils().ColorByName(MapWinGIS.tkMapColor.Red);
                sf.DefaultDrawingOptions.LineWidth = 2f;
                sf.DefaultDrawingOptions.FillVisible = false;


            }

            buttonOk.IsEnabled = false;
        }

        private void SetDataGridContext()
        {
            dataGridAOIs.DataContext = Entities.AOIViewModel.AOICollection.OrderBy(t => t.Name).ToList();
        }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            Entities.AOIViewModel.UnloadAllAOIBouindaries();
            Entities.AOIViewModel.UnloadAllGrids();
            Owner.Focus();
            MapWindowManager.ResetCursor();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

        }


        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            switch (btn.Name)
            {
                case "buttonProcessGrid":
                case "buttonShowGrid":
                case "buttonFormatMaps":
                    if (Entities.AOIViewModel.CountSelected() > 0)
                    {
                        switch (btn.Name)
                        {
                            case "buttonShowGrid":
                                Entities.AOIViewModel.SetGridFilenamesOfCommonSize();
                                if (Entities.AOIViewModel.CommonGridSizes.Count == 1)
                                {
                                    foreach (var aoi in Entities.AOIViewModel.GetSelectedAOIs())
                                    {
                                        var file = aoi.GetGridFileNameOfGridSize(Entities.AOIViewModel.CommonGridSizes[0]);
                                        aoi.CreateGridFromFileName(file);
                                    }
                                }
                                SetDataGridContext();
                                break;
                            case "buttonProcessGrid":
                                var griddedAOICount = Entities.AOIViewModel.GetSelectedAOIs().Count(t => t.SubGrids != null);
                                if (griddedAOICount == Entities.AOIViewModel.CountSelected())
                                {
                                    ShowGridMappingWindow(mulitpleAOIs: true);
                                }
                                else
                                {
                                    MessageBox.Show("All selected AOIs must have a grid", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                break;
                            case "buttonFormatMaps":
                                FormatGridMapWindow fmw = new FormatGridMapWindow(_aoi);
                                fmw.ShowDialog();
                                break;
                        }
                    }
                    else
                    {
                        MessageBox.Show("At least one AOI must be selected", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    break;
                case "buttonCancel":
                    MapWindowManager.MapLayersHandler.RemoveLayer(AOIManager._hAOI);
                    Close();
                    break;
                case "buttonOk":
                    if (textBoxAOIName.Text.Length > 0)
                    {
                        _aoi = AOIManager.SaveAOI(textBoxAOIName.Text);

                        if (_aoi != null)
                        {
                            Close();
                        }
                    }
                    else if (_editingAOI)
                    {
                        _aoi = AOIManager.SaveAOI(_aoi.Name, true);
                        buttonOk.IsEnabled = _aoi == null;

                    }
                    break;
            }
        }

        private void ShowGridMappingWindow(bool mulitpleAOIs = false)
        {
            if (MapWindowManager.MapLayersHandler.IsLayerLoadedInMap("extracted_tracks"))
            {
                GridMappingWindow gmw = GridMappingWindow.GetInstance();
                gmw.MultipleAOIs = mulitpleAOIs;
                if (!mulitpleAOIs)
                {
                    gmw.AOI = _aoi;
                }
                gmw.Owner = this;
                if (gmw.Visibility == Visibility.Visible)
                {
                    gmw.BringIntoView();
                }
                else
                {
                    gmw.Show();
                }
            }
            else
            {
                MessageBox.Show("Extracted tracks is not in the map", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void OnMenuClick(object sender, RoutedEventArgs e)
        {
            switch (((MenuItem)sender).Name)
            {
                case "menuFormatMap":
                    bool proceed = false;
                    foreach (var aoi in Entities.AOIViewModel.GetAllAOI())
                    {

                        if (aoi.GridIsLoaded && aoi.GridMapping.HasGriddedData)
                        {
                            proceed = true;
                            break;
                        }
                    }


                    if (!proceed)
                    {
                        MessageBox.Show("There is no gridded map", "GPXC Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        FormatGridMapWindow fmw = new FormatGridMapWindow(_aoi);
                        fmw.ShowDialog();
                    }

                    break;
                case "menuEditGrid":
                    MakeGrid();
                    menuEditGrid.IsEnabled = false;
                    menuShowGrid.IsEnabled = false;
                    menuRemoveGrid.IsEnabled = true;
                    break;
                case "menuGridMapping":
                    ShowGridMappingWindow();
                    break;
                case "menuAOIZoom":
                    if (_aoi != null)
                    {
                        MapWindowManager.ZoomToShapeFileExtent(_aoi.ShapeFile);
                    }
                    break;
                case "menuAOIEditExtent":
                    if (_aoi != null)
                    {
                        _editingAOI = true;
                        AOIManager.Edit(_aoi);
                        buttonOk.IsEnabled = true;
                    }
                    break;
                case "menuRemoveGrid":
                    if (_aoi != null)
                    {
                        //MapWindowManager.MapLayersHandler.RemoveLayer(_aoi.MapLayerHandle);
                        MapWindowManager.MapLayersHandler.RemoveLayer(_aoi.GridLayerName);
                        _aoi.GridIsLoaded = false;
                        menuRemoveGrid.IsEnabled = false;
                        menuShowGrid.IsEnabled = !_aoi.GridIsLoaded;
                        menuEditGrid.IsEnabled = !_aoi.GridIsLoaded;
                        SetDataGridContext();
                    }
                    break;
                case "menuShowGrid":

                    if (_aoi.GridFileName != null && _aoi.GridFileName.Length > 0 && File.Exists(_aoi.GridFileName))
                    {
                        var files = Entities.AOIViewModel.GetAOISubGridFileNames(_aoi);
                        if (files.Count > 1)
                        {
                            var selectedFile = "";
                            SelectGridFileWindow sgw = new SelectGridFileWindow();
                            sgw.GridFiles = files;
                            if ((bool)sgw.ShowDialog())
                            {
                                selectedFile = sgw.SelectedFile;
                                menuGridMapping.IsEnabled = _aoi.CreateGridFromFileName(selectedFile);
                            }

                        }
                        else
                        {
                            menuGridMapping.IsEnabled = _aoi.CreateGridFromFileName(_aoi.GridFileName);
                        }



                    }
                    else
                    {
                        MakeGrid();
                    }
                    SetDataGridContext();
                    menuShowGrid.IsEnabled = !_aoi.GridIsLoaded;
                    menuEditGrid.IsEnabled = menuShowGrid.IsEnabled;
                    menuRemoveGrid.IsEnabled = _aoi.GridIsLoaded;
                    break;
            }


        }

        private void MakeGrid()
        {
            MakeAOIGridWindow mgw = MakeAOIGridWindow.GetInstance();
            mgw.AOI = _aoi;
            mgw.Owner = this;
            if (mgw.Visibility == Visibility.Visible)
            {
                mgw.BringIntoView();
            }
            else
            {
                mgw.Show();
            }
        }
        public void GridIsLoaded()
        {
            menuGridMapping.IsEnabled = true;
            SetDataGridContext();
        }
        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (menuGridMapping.IsEnabled)
            {
                menuFormatMap.IsEnabled = _aoi.GridMapping.IsBerriedMapped ||
                    _aoi.GridMapping.IsCPUEMapped ||
                    _aoi.GridMapping.IsFishingInternsityMapped ||
                    _aoi.GridMapping.IsUndersizedMapped;
            }
        }

        private void OnGridSelectedCellChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            menuRemoveGrid.IsEnabled = false;
            menuShowGrid.IsEnabled = true;
            menuGridMapping.IsEnabled = false;
            menuEditGrid.IsEnabled = false;
            menuFormatMap.IsEnabled = false;
            if (dataGridAOIs.SelectedCells.Count == 1)
            {
                DataGridCellInfo cell = dataGridAOIs.SelectedCells[0];
                _gridRow = dataGridAOIs.Items.IndexOf(cell.Item);
                _aoi = (AOI)dataGridAOIs.Items[_gridRow];
                menuGridMapping.IsEnabled = _aoi.GridIsLoaded;
                menuRemoveGrid.IsEnabled = _aoi.GridIsLoaded;
                menuEditGrid.IsEnabled = _aoi.GridFileName != null && _aoi.GridFileName.Length > 0 && !_aoi.GridIsLoaded;
                menuShowGrid.IsEnabled = !_aoi.GridIsLoaded;

                menuFormatMap.IsEnabled = _aoi.GridMapping.IsBerriedMapped ||
                    _aoi.GridMapping.IsCPUEMapped ||
                    _aoi.GridMapping.IsFishingInternsityMapped ||
                    _aoi.GridMapping.IsUndersizedMapped;

            }

        }
    }
}
