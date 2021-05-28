using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MapWinGIS;
namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for SelectedGridRowsSummaryWindow.xaml
    /// </summary>
    public partial class SelectedGridRowsSummaryWindow : Window
    {
        private static SelectedGridRowsSummaryWindow _instance;
        private Shapefile _sf;
        private Dictionary<int, object> _dictSFColumns = new Dictionary<int, object>();
        private Dictionary<int, string> _dictNameFieldIndexType = new Dictionary<int, string>();
        public SelectedGridRowsSummaryWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
            Closed += OnWindowClosed;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            //_sf = null;
            //this.SavePlacement();
            //_instance = null;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        public static SelectedGridRowsSummaryWindow Instance()
        {
            return _instance;
        }
        public static SelectedGridRowsSummaryWindow GetInstance()
        {
            if (_instance == null) _instance = new SelectedGridRowsSummaryWindow();
            return _instance;
        }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _sf = null;
            this.SavePlacement();
            _instance = null;
        }

        public void GetSelectedShapes()
        {
            var listShapeIndexes = MapWindowManager.MapLayersHandler.SelectedShapesIndexes();
            if (listShapeIndexes != null)
            {
                foreach (int item in listShapeIndexes)
                {
                    foreach (var key in _dictSFColumns.Keys)
                    {
                        switch (_dictNameFieldIndexType[key])
                        {
                            case "Double":
                                ((List<double>)_dictSFColumns[key]).Add(_sf.CellValue[key, item]);
                                break;
                            case "Int":
                                ((List<int>)_dictSFColumns[key]).Add(_sf.CellValue[key, item]);
                                break;
                            case "DateTime":
                                ((List<DateTime>)_dictSFColumns[key]).Add(_sf.CellValue[key, item]);
                                break;
                        }
                    }
                }
            }

            List<ShapefileFieldSummary> summary = new List<ShapefileFieldSummary>();
            foreach (var key in _dictSFColumns.Keys)
            {
                summary.Add(new ShapefileFieldSummary(_dictSFColumns[key], _dictNameFieldIndexType[key], _sf.Field[key].Name));
            }
            summaryGrid.AutoGenerateColumns = false;
            summaryGrid.DataContext = summary;
        }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            summaryGrid.Columns.Add(new DataGridTextColumn { Header = "Property", Binding = new Binding("Name") });
            summaryGrid.Columns.Add(new DataGridTextColumn { Header = "Number of items", Binding = new Binding("Count") });
            summaryGrid.Columns.Add(new DataGridTextColumn { Header = "Sum", Binding = new Binding("SumAsString") });
            summaryGrid.Columns.Add(new DataGridTextColumn { Header = "Minimum", Binding = new Binding("MinAsString") });
            summaryGrid.Columns.Add(new DataGridTextColumn { Header = "Maximum", Binding = new Binding("MaxAsString") });
            summaryGrid.Columns.Add(new DataGridTextColumn { Header = "Average", Binding = new Binding("AverageAsString") });
            _sf = (Shapefile)MapWindowManager.MapLayersHandler.CurrentMapLayer.LayerObject;
            for (int x = 0; x < _sf.NumFields; x++)
            {
                switch (_sf.Field[x].Type)
                {
                    case FieldType.DOUBLE_FIELD:
                        _dictNameFieldIndexType.Add(x, "Double");
                        _dictSFColumns.Add(x, new List<double>());
                        break;
                    case FieldType.INTEGER_FIELD:
                        _dictNameFieldIndexType.Add(x, "Int");
                        _dictSFColumns.Add(x, new List<int>());
                        break;
                    case FieldType.DATE_FIELD:
                        _dictNameFieldIndexType.Add(x, "DateTime");
                        _dictSFColumns.Add(x, new List<DateTime>());
                        break;
                }

            }


        }

        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
