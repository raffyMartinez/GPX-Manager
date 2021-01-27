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
            Loaded += ShapeFileAttributesWindow_Loaded;
            _mapInterActionHandler = mapInterActionHandler;
            _mapInterActionHandler.ShapesSelected += _mapInterActionHandler_ShapesSelected;
            _mapInterActionHandler.SelectionCleared += _mapInterActionHandler_SelectionCleared;
            _mapInterActionHandler.MapLayersHandler.CurrentLayer += MapLayersHandler_CurrentLayer;
            _mapInterActionHandler.MapLayersHandler.AllSelectionsCleared += MapLayersHandler_AllSelectionsCleared;

        }

        private void _mapInterActionHandler_SelectionCleared(object sender, EventArgs e)
        {
            dataGridAttributes.SelectedItems.Clear();
        }

        private void MapLayersHandler_AllSelectionsCleared(object sender, EventArgs e)
        {
            dataGridAttributes.SelectedItems.Clear();
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
            foreach(DataRowView item in dataGridAttributes.Items )
            {
                if (item.Row.Field<int>("MWShapeID") == e.SelectedIndexes[0])
                {
                    dataGridAttributes.SelectedItem = item;
                }
            }
        }

        
        private void CleanUp()
        {
            _instance = null;
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
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CleanUp();
            this.SavePlacement();
            MapWindowManager.ShapeFileAttributesWindow = null;
            MapWindowManager.MapWindowForm.Focus();
            _instance = null;
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

        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                DataColumn col = ((DataRowView)e.AddedItems[0]).Row.Table.Columns["MWShapeID"];
                if (col != null)
                {
                    List<int> selectedIDs = new List<int>();
                    foreach (DataRowView row in ((DataGrid)sender).SelectedItems)
                    {
                        selectedIDs.Add(row.Row.Field<int>(col));
                    }
                    MapWindowManager.SelectedAttributeRows = selectedIDs;
                }
            }
        }
    }
}
