using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
using GPXManager.views;
using MapWinGIS;

namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for ShapeFileAttributesWindow.xaml
    /// </summary>
    public partial class ShapeFileAttributesWindow : Window
    {
        private MapInterActionHandler _mapInterActionHandler;
        private static ShapeFileAttributesWindow _instance;
        public ShapeFileAttributesWindow(MapInterActionHandler mapInterActionHandler)
        {
            InitializeComponent();
            Closing += OnWindowClosing;
            Closed += OnWindowClosed;
            Loaded += ShapeFileAttributesWindow_Loaded;

            _mapInterActionHandler = mapInterActionHandler;
            _mapInterActionHandler.ShapesSelected += _mapInterActionHandler_ShapesSelected;
            _mapInterActionHandler.SelectionCleared += _mapInterActionHandler_SelectionCleared;
            _mapInterActionHandler.MapLayersHandler.CurrentLayer += MapLayersHandler_CurrentLayer;
            _mapInterActionHandler.MapLayersHandler.OnVisibilityExpressionSet += MapLayersHandler_OnVisibilityExpressionSet;
            _mapInterActionHandler.MapLayersHandler.AllSelectionsCleared += MapLayersHandler_AllSelectionsCleared;
            //dataGridAttributes.SelectionChanged += OnDataGridAttributes_SelectionChanged;

        }

        private void MapLayersHandler_OnVisibilityExpressionSet(MapLayersHandler s, LayerEventArg e)
        {
            //ShowShapeFileAttribute();
            dataGridAttributes.SelectedItems.Clear();
            //dataGridAttributes.ItemsSource = null;
            //ShapeFile = (Shapefile)MapWindowManager.MapLayersHandler[e.LayerHandle].LayerObject;
            ShapeFile = e.Shapefile;
            ShowShapeFileAttribute();
        }

        public bool ShowSummaryOfSelectedItems { get; set; }



        private void _mapInterActionHandler_SelectionCleared(object sender, EventArgs e)
        {
            dataGridAttributes.SelectedItems.Clear();
            ShowShapeFileAttribute();

        }

        private void MapLayersHandler_AllSelectionsCleared(object sender, EventArgs e)
        {
            dataGridAttributes.SelectedItems.Clear();
            ShowShapeFileAttribute();
        }

        private void ShapeFileAttributesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MapWindowManager.ShapeFileAttributesWindow = this;
        }

        private void MapLayersHandler_CurrentLayer(MapLayersHandler s, LayerEventArg e)
        {
            ShapeFile = s.CurrentMapLayer.LayerObject as Shapefile;
            ShowShapeFileAttribute();
        }

        private void _mapInterActionHandler_ShapesSelected(MapInterActionHandler s, LayerEventArg e)
        {
            if (((Shapefile)MapWindowManager.MapLayersHandler[e.LayerHandle].LayerObject).FieldIndexByName["MWShapeID"] >= 0)
            {
                _gridWaslClicked = false;
                if (e.SelectedIndexes.Count() == 1)
                {
                    _gridWaslClicked = true;
                    if (dataGridAttributes.Items.Count == 1)
                    {
                        
                        dataGridAttributes.DataContext = ShapefileAttributeTableManager.SetupAttributeTable(ShapeFile, true);
                        for (int x = 0; x < e.SelectedIndexes.Count(); x++)
                        {
                            foreach (DataRowView item in dataGridAttributes.Items)
                            {
                                if (item.Row.Field<int>("MWShapeID") == e.SelectedIndexes[0])
                                {
                                    //dataGridAttributes.SelectedItem = item;
                                    dataGridAttributes.SelectedItems.Add(item);
                                    //break;
                                }
                            }
                        }
                        var t = dataGridAttributes.LayoutTransform;
                        
                    }
                    else
                    {
                        foreach (DataRowView item in dataGridAttributes.Items)
                        {

                            if (item.Row.Field<int>("MWShapeID") == e.SelectedIndexes[0])
                            {
                                dataGridAttributes.SelectedItem = item;
                                dataGridAttributes.ScrollIntoView(dataGridAttributes.SelectedItem);
                            }
                        }
                    }
                }
                else
                {
                    //dataGridAttributes.DataContext = ShapefileAttributeTableManager.SetupAttributeTable(ShapeFile,true);
                    ShowShapeFileAttribute();
                    for (int x = 0; x < e.SelectedIndexes.Count(); x++)
                    {
                        foreach (DataRowView item in dataGridAttributes.Items)
                        {
                            if (item.Row.Field<int>("MWShapeID") == e.SelectedIndexes[0])
                            {
                                //dataGridAttributes.SelectedItem = item;
                                dataGridAttributes.SelectedItems.Add(item);
                                //break;
                            }
                        }
                    }
                }
            }

        }


        private void CleanUp()
        {
            _mapInterActionHandler.ShapesSelected -= _mapInterActionHandler_ShapesSelected;
            _mapInterActionHandler.SelectionCleared -= _mapInterActionHandler_SelectionCleared;
            _mapInterActionHandler.MapLayersHandler.CurrentLayer -= MapLayersHandler_CurrentLayer;
            _mapInterActionHandler.MapLayersHandler.AllSelectionsCleared -= MapLayersHandler_AllSelectionsCleared;
            _mapInterActionHandler = null;


        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        private void OnWindowClosed(object sender, EventArgs e)
        {
            //CleanUp();
            //this.SavePlacement();
            //MapWindowManager.ShapeFileAttributesWindow = null;
            //if (MapWindowManager.MapWindowForm != null)
            //{
            //    MapWindowManager.MapWindowForm.Focus();
            //}
            _instance = null;
        }
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CleanUp();
            this.SavePlacement();
            MapWindowManager.ShapeFileAttributesWindow = null;
            MapWindowManager.MapWindowForm.Focus();
            //_instance = null;
        }

        public static ShapeFileAttributesWindow GetInstance(MapInterActionHandler mapInterActionHandler)
        {
            if (_instance == null) _instance = new ShapeFileAttributesWindow(mapInterActionHandler);
            return _instance;
        }

        private void OnButtonCLick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public Shapefile ShapeFile { get; set; }

        public void ShowShapeFileAttribute()
        {
            dataGridAttributes.DataContext = ShapefileAttributeTableManager.SetupAttributeTable(ShapeFile);
            labelTitle.Content = ShapefileAttributeTableManager.DataCaption;

        }

        private void OnDataGridAttributes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemCounts = dataGridAttributes.SelectedItems.Count;
            //if (itemCounts > 1 && ShowSummaryOfSelectedItems)
            if (itemCounts > 1)
            {
                var summaryWindow = SelectedGridRowsSummaryWindow.GetInstance();
                summaryWindow.Owner = this;
                if (summaryWindow.Visibility == Visibility.Visible)
                {
                    summaryWindow.BringIntoView();
                }
                else
                {
                    summaryWindow.Show();
                }
            }
            else
            {

            }

        }
        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridAttributes.SelectedItems.Count >= 1)
            {

                try
                {
                    DataColumn col = ((DataRowView)e.AddedItems[0]).Row.Table.Columns["MWShapeID"];
                    if(col==null)
                    {
                        return;
                        //Global.Settings.
                    }
                    if (col != null)
                    {
                        List<int> selectedIDs = new List<int>();
                        foreach (DataRowView row in ((DataGrid)sender).SelectedItems)
                        {
                            selectedIDs.Add(row.Row.Field<int>(col));
                        }

                        if (_gridWaslClicked)
                        {
                            MapWindowManager.SelectedAttributeRows = selectedIDs;
                            _gridWaslClicked = false;
                        }

                        if (dataGridAttributes.SelectedItems.Count == 1)
                        {
                            SelectedGridRowsSummaryWindow.Instance()?.Close();
                        }
                        else
                        {
                            var summaryWindow = SelectedGridRowsSummaryWindow.GetInstance();
                            summaryWindow.Owner = this;
                            if (summaryWindow.Visibility == Visibility.Visible)
                            {
                                summaryWindow.BringIntoView();
                            }
                            else
                            {
                                summaryWindow.Show();
                                summaryWindow.GetSelectedShapes();
                            }

                        }
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    //ignore
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }

        private bool _gridWaslClicked = false;
        private void OnDataGridNouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _gridWaslClicked = true;
            }
        }

        private void OnDataGridPreivewNouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton ==MouseButtonState.Pressed)
            {
                _gridWaslClicked = true;
            }
        }
    }
}
