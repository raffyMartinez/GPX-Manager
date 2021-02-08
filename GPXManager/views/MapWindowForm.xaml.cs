using AxMapWinGIS;
using GPXManager.entities;
using GPXManager.entities.mapping;
using GPXManager.entities.mapping.Views;
using MapWinGIS;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using Xceed.Wpf.AvalonDock.Controls;
using WindowMenuItem = System.Windows.Controls.MenuItem;
using System.Windows.Controls.Primitives;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for MapWindowForm.xaml
    /// </summary>
    /// 

    public partial class MapWindowForm : Window
    {
        private static MapWindowForm _instance;

        public static MapWindowForm Instance { get { return _instance; } }
        public AxMapWinGIS.AxMap MapControl { get; set; }
        public MapWindowForm()
        {
            InitializeComponent();
            Closing += OnWindowClosing;
        }


        public System.Windows.Controls.Control LayerSelector { get; set; }
        public MapLayer CurrentLayer { get; set; }

        public List<int> SelectedShapeIndexes { get; set; }

        public MapInterActionHandler MapInterActionHandler { get; private set; }
        public MapLayersHandler MapLayersHandler { get; private set; }
        public static MapWindowForm GetInstance()
        {
            if (_instance == null) _instance = new MapWindowForm();
            return _instance;
        }


        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (!MapWindowManager.MapStateFileExists)
            {
                SaveMapState();
            }
            _instance = null;
            this.SavePlacement();

            GPXMappingManager.RemoveAllFromMap();
            TripMappingManager.Cleanup();
            GPXMappingManager.Cleanup();
            ParentWindow.ResetDataGrids();
            MapWindowManager.CleanUp();
            ParentWindow.Focus();
        }

        

        public GPXFile GPXFile { get; set; }
        public MainWindow ParentWindow { get; set; }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Integration.WindowsFormsHost host = new System.Windows.Forms.Integration.WindowsFormsHost();

            MapControl = new AxMapWinGIS.AxMap();
            host.Child = MapControl;
            MapGrid.Children.Add(host);

            MapLayersHandler = new MapLayersHandler(MapControl);
            MapInterActionHandler = new MapInterActionHandler(MapControl, MapLayersHandler);
            MapControl.ZoomBehavior = tkZoomBehavior.zbDefault;

            if (MapWindowManager.MapStateFileExists)
            {
                MapWindowManager.RestoreMapState(this);
                menuMapTilesVisible.IsChecked = MapControl.TileProvider != tkTileProvider.ProviderNone;
                menuMapTilesSelectProvider.IsEnabled = MapControl.TileProvider != tkTileProvider.ProviderNone;
            }
            else
            {
                menuMapTilesSelectProvider.IsEnabled = false;
            }

            if (MapLayersHandler.get_MapLayer("Coastline") != null)
            {
                menuMapCoastlineVisible.IsChecked = MapLayersHandler.get_MapLayer("Coastline").Visible;
            }


            MapWindowManager.ResetCursor();

            MapInterActionHandler.ShapesSelected += OnMapShapeSelected;
            MapLayersHandler.CurrentLayer += OnMapCurrentLayer;
            MapLayersHandler.OnLayerVisibilityChanged += MapLayersHandler_OnLayerVisibilityChanged;
            GPXMappingManager.MapInteractionHandler = MapInterActionHandler;
            TripMappingManager.MapInteractionHandler = MapInterActionHandler;

            SetButtonEnabled();

        }

        private void MapLayersHandler_OnLayerVisibilityChanged(MapLayersHandler s, LayerEventArg e)
        {
            switch (e.LayerName)
            {
                case "Coastline":
                    menuMapCoastlineVisible.IsChecked = e.LayerVisible;
                    break;
            }
        }

        private void OnMapCurrentLayer(MapLayersHandler s, LayerEventArg e)
        {
            CurrentLayer = s.CurrentMapLayer;
            SetButtonEnabled();
        }



        private void OnMapShapeSelected(MapInterActionHandler s, LayerEventArg e)
        {
            Title = "not found";
            if (CurrentLayer.LayerType == "ShapefileClass" && LayerSelector != null)
            {
                SelectedShapeIndexes = e.SelectedIndexes.ToList();
                var sf = (Shapefile)CurrentLayer.LayerObject;
                int fileNameField = sf.FieldIndexByName["Filename"];
                int gpsField = sf.FieldIndexByName["GPS"];

                switch (LayerSelector.GetType().Name)
                {
                    case "DataGrid":
                        var dataGrid = (System.Windows.Controls.DataGrid)LayerSelector;
                        string fileName = sf.CellValue[fileNameField, SelectedShapeIndexes[0]];
                        string gps = sf.CellValue[gpsField, SelectedShapeIndexes[0]];
                        string itemGPS = "";
                        string itemFilename = "";
                        foreach (var item in dataGrid.Items)
                        {
                            switch (dataGrid.Name)
                            {
                                case "dataGridTrips":
                                    Trip trip = (Trip)item;
                                    itemGPS = trip.GPS.DeviceName;
                                    itemFilename = trip.Track.FileName;
                                    break;
                                case "dataGridGPXFiles":
                                    GPXFile gpxFile = (GPXFile)item;
                                    itemGPS = gpxFile.GPS.DeviceName;
                                    itemFilename = gpxFile.FileName;
                                    break;
                            }
                            if (itemGPS == gps && itemFilename == fileName)
                            {
                                dataGrid.SelectedItem = item;
                                dataGrid.ScrollIntoView(item);
                                break;
                            }
                        }
                        break;
                }
            }
        }

        public string CoastlineShapeFile_FileName { get; set; }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        private void SelectTileProvider()
        {
            SelectTileProviderWindow stpw = new SelectTileProviderWindow();
            if (stpw.ShowDialog() == true)
            {
                MapControl.TileProvider = (tkTileProvider)Enum.Parse(typeof(tkTileProvider), stpw.TileProviderID.ToString());
            }
        }

        private void SaveMapState()
        {
            if (MapWindowManager.SaveMapState() == false)
            {
                System.Windows.MessageBox.Show(MapWindowManager.LastError, "GPXManager", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OnMenuClick(object sender, RoutedEventArgs e)
        {
            switch (((WindowMenuItem)sender).Name)
            {
                case "menuEdit":
                    break;
                case "menuAddLayerBoundaryLGU":
                    string feedfBack = "";
                    MapWindowManager.AddLGUBoundary(out feedfBack);
                    if (feedfBack.Length > 0)
                    {
                        System.Windows.MessageBox.Show(feedfBack, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "menuMapTilesSelectProvider":
                    SelectTileProvider();
                    break;
                case "menuClose":
                    Close();
                    break;
                case "menuSaveMapState":
                    SaveMapState();
                    break;
                case "menuAOICreate":
                    var aoiw = new AOIWindow();
                    aoiw.Owner = this;
                    aoiw.AddNewAOI();
                    aoiw.Show();
                    AOIManager.AddNew();
                    break;
                case "menuAOIList":
                    aoiw = new AOIWindow();
                    aoiw.ShowAOIList();
                    aoiw.Owner = this;
                    aoiw.Show();
                    break;
                case "menuIslandLabels":
                    break;
            }
        }

        private void OnMenuChecked(object sender, RoutedEventArgs e)
        {
            var menuItem = (WindowMenuItem)sender;
            switch (menuItem.Name)
            {
                case "menuMapCoastlineVisible":

                    var coast = MapLayersHandler.get_MapLayer("Coastline");
                    if (coast == null)
                    {
                        MapWindowManager.LoadCoastline(MapWindowManager.CoastLineFile);
                        coast = MapLayersHandler.get_MapLayer("Coastline");
                    }
                    if (coast != null)
                    {
                        MapLayersHandler.EditLayer(coast.Handle, coast.Name, menuItem.IsChecked);
                    }
                    else
                    {
                        if(File.Exists(MapWindowManager.CoastLineFile))
                        {
                            System.Windows.MessageBox.Show("Coastline file could not be loaded as a GIS Shapefile", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Coastline file could not be found", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    break;
                case "menuMapTilesVisible":
                    if (menuItem.IsChecked)
                    {
                        menuMapTilesSelectProvider.IsEnabled = true;
                        if (MapControl.TileProvider == tkTileProvider.ProviderNone)
                        {
                            SelectTileProvider();
                        }
                    }
                    else
                    {
                        MapControl.TileProvider = tkTileProvider.ProviderNone;
                        menuMapTilesSelectProvider.IsEnabled = false;
                    }
                    break;
            }
        }

        private void ToBeImplemented(string usage)
        {
            System.Windows.MessageBox.Show($"The {usage} functionality is not yet implemented", "Placeholder and not yet working", MessageBoxButton.OK, MessageBoxImage.Information); ;
        }

        private void OnToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            tkCursorMode cursorMode = tkCursorMode.cmNone;
            tkCursor cursor = tkCursor.crsrArrow;


            switch (((System.Windows.Controls.Button)sender).Name)
            {
                case "buttonDataScreen":
                    Visibility = Visibility.Hidden;
                    if (MapWindowManager.MapLayersWindow != null)
                    {
                        MapWindowManager.MapLayersWindow.Visibility = Visibility.Hidden;
                    }
                    if (MapWindowManager.ShapeFileAttributesWindow != null)
                    {
                        MapWindowManager.ShapeFileAttributesWindow.Visibility = Visibility.Hidden;
                    }

                    ParentWindow.Focus();
                    break;
                case "buttonExit":
                    Close();
                    break;
                case "buttonRuler":
                    cursorMode = tkCursorMode.cmMeasure;
                    cursor = tkCursor.crsrCross;
                    break;
                case "buttonPan":
                    cursorMode = tkCursorMode.cmPan;
                    cursor = tkCursor.crsrSizeAll;
                    break;
                case "buttonZoomPlus":
                    cursorMode = tkCursorMode.cmZoomIn;
                    cursor = tkCursor.crsrCross;
                    MakeCursor("zoom_plus");
                    break;
                case "buttonZoomMinus":
                    cursorMode = tkCursorMode.cmZoomOut;
                    cursor = tkCursor.crsrCross;
                    break;
                case "buttonSelect":
                    cursorMode = tkCursorMode.cmSelection;
                    cursor = tkCursor.crsrHand;
                    break;
                case "buttonSelectNone":
                    MapLayersHandler.ClearAllSelections();
                    break;
                case "buttonAttributes":
                    ShapeFileAttributesWindow sfw = ShapeFileAttributesWindow.GetInstance(MapWindowManager.MapInterActionHandler);
                    if (sfw.Visibility == Visibility.Visible)
                    {
                        sfw.BringIntoView();
                    }
                    else
                    {
                        sfw.Owner = this;
                        sfw.ShapeFile = MapLayersHandler.CurrentMapLayer.LayerObject as Shapefile;
                        sfw.ShowShapeFileAttribute();
                        sfw.Show();
                    }
                    MapWindowManager.ShapeFileAttributesWindow = sfw;
                    break;
                case "buttonGears":
                    ToBeImplemented("mapping options");
                    break;
                case "buttonUploadCloud":
                    ToBeImplemented("upload to cloud");
                    break;
                case "buttonCalendar":
                    ToBeImplemented("calendar");
                    break;
                case "buttonTrack":
                    ToBeImplemented("track");
                    break;
                case "buttonGPS":
                    ToBeImplemented("gps");
                    break;
                case "buttonLayers":
                    var mlw = MapLayersWindow.GetInstance();

                    if (mlw.Visibility == Visibility.Visible)
                    {
                        mlw.BringIntoView();
                        mlw.Focus();
                    }
                    else
                    {
                        mlw.ParentForm = this;
                        mlw.Owner = this;
                        mlw.MapLayersHandler = MapWindowManager.MapLayersHandler;
                        mlw.Show();
                    }

                    break;
                case "buttonAddLayer":
                    ToBeImplemented("add a layer");
                    break;

            }


            MapControl.CursorMode = cursorMode;
            MapControl.MapCursor = cursor;
        }

        private void SetButtonEnabled()
        {
            double buttonOpacityDisabled = .20d;

            buttonTrack.IsEnabled = false;

            if (CurrentLayer != null)
            {
                buttonTrack.IsEnabled = CurrentLayer.LayerType == "ShapefileClass" &&
                    ((Shapefile)CurrentLayer.LayerObject).ShapefileType == ShpfileType.SHP_POLYLINE;
            }



            if(!buttonTrack.IsEnabled)
            {
                buttonTrack.Opacity = buttonOpacityDisabled;
            }
            else
            {
                buttonTrack.Opacity = 1;
            }
        }
        private void OnToolbarButtonChecked(object sender, RoutedEventArgs e)
        {

            ToggleButton tb = (ToggleButton)sender;
            switch(tb.Name)
            {
                case "buttonTrack":
                    if((bool)tb.IsChecked)
                    {
                        if(MapWindowManager.TrackGPXFile!=null)
                        {
                            List<int> handles = new List<int>();
                            var sf = ShapefileFactory.GPXTrackVertices(MapWindowManager.TrackGPXFile,out handles);
                            MapWindowManager.MapLayersHandler.AddLayer(sf, "Vertices", uniqueLayer: true,layerKey:sf.Key,rejectIfExisting:true);
                            MapWindowManager.MapLayersWindow.RefreshCurrentLayer();
                        }
                        else
                        {
                            ((Shapefile)CurrentLayer.LayerObject).DefaultDrawingOptions.VerticesVisible = true;
                        }
                    }
                    else
                    {
                        if (MapWindowManager.TrackGPXFile != null)
                        {
                            GPXMappingManager.RemoveGPXTrackVertices();
                        }
                        else
                        {
                            ((Shapefile)CurrentLayer.LayerObject).DefaultDrawingOptions.VerticesVisible = false;
                        }

                    }
                    MapControl.Redraw();
                    break;
            }
        }
    }
}
