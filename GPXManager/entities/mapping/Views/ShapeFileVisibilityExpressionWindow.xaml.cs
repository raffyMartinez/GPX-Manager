using System;
using System.Collections.Generic;
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
    /// Interaction logic for ShapeFileVisibilityExpressionWindow.xaml
    /// </summary>
    public partial class ShapeFileVisibilityExpressionWindow : Window
    {
        private Dictionary<int, object> _dictSFColumns = new Dictionary<int, object>();
        private KeyValuePair<string, int> _selectedGridItem;
        private bool _isString;
        public ShapeFileVisibilityExpressionWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Owner.Focus();
        }

        private void GetFields()
        {
            for (int n = 0; n < Shapefile.NumFields; n++)
            {
                listBoxFields.Items.Add(Shapefile.Field[n].Name);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            lblResult.Content = "";
            GetFields();
            if (Shapefile.VisibilityExpression.Length > 0)
            {
                textQuery.Text = Shapefile.VisibilityExpression;
            }
        }

        public Shapefile Shapefile { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            switch (btn.Content)
            {
                case "Apply":
                    bool proceed = true;
                    if(textQuery.Text.Length==0)
                    {

                        //Shapefile.VisibilityExpression = "";
                        //MapWindowManager.ShapeFileVisibilityExpression = "";
                        proceed = true;
                    }
                    else if(TestExpression())
                    {
                        //Shapefile.VisibilityExpression = textQuery.Text;
                        proceed = true;
                        //MapWindowManager.ShapeFileVisibilityExpression = textQuery.Text;
                    }
                    //else
                    //{
                    //    proceed = false;
                    //}
                    if (proceed)
                    {
                        //MapWindowManager.ShapeFileVisibilityExpression = textQuery.Text;
                        MapWindowManager.MapLayersHandler.VisibilityExpression(textQuery.Text);
                        //MapWindowManager.MapControl.Redraw();
                    }
                    break;
                case "Reset":
                    Shapefile.VisibilityExpression = "";
                    MapWindowManager.MapControl.Redraw();
                    Close();
                    break;
                case "Ok":
                    if (TestExpression())
                    {
                        //MapWindowManager.MapLayersHandler.VisibilityExpression(textQuery.Text, ExpressionTarget);
                        //Shapefile.VisibilityExpression = textQuery.Text;
                        //MapWindowManager.MapControl.Redraw();
                        MapWindowManager.ShapeFileVisibilityExpression = textQuery.Text;
                        Close();
                    }
                    break;
                case "Cancel":
                    Close();
                    break;
                case "<":
                case "<=":
                case ">=":
                case ">":
                case "=":
                case "<>":
                case "AND":
                case "OR":
                case "NOT":
                case "(":
                case ")":
                    textQuery.Text += $" {btn.Content} ";
                    break;
                case "Get values":
                    break;
                case "Test":
                    TestExpression();
                    break;
                case "Clear":
                    textQuery.Clear();
                    break;

            }
        }

        public VisibilityExpressionTarget ExpressionTarget { get; set; }
        private bool TestExpression()
        {
            var success = false;
            MapWinGIS.Table tbl = Shapefile.Table;
            if (textQuery.Text == string.Empty)
            {
                return true;
            }
            else
            {
                object result = null;
                string err = string.Empty;

                if (tbl.Query(textQuery.Text, ref result, ref err))
                {
                    lblResult.Foreground = Brushes.Green;
                    int[] arr = result as int[];
                    if (arr != null)
                    {
                        lblResult.Content = "Number of shapes = " + arr.Length.ToString();

                        // updating shapefile selection
                        //if (_selectionMode)
                        //{
                        //    ArrayList options = new ArrayList();
                        //    options.Add("1 - New selection");
                        //    options.Add("2 - Add to selection");
                        //    options.Add("3 - Exclude from selection");
                        //    options.Add("4 - Invert in selection");
                        //    string s = string.Format("Number of shapes = {0}. Choose the way to update selection", arr.Length);
                        //}
                    }
                    success = (arr != null && arr.Length > 0);
                }
                else
                {
                    if (err.ToLower() == "selection is empty")
                    {
                        lblResult.Foreground = Brushes.Blue;
                        lblResult.Content = err;
                    }
                    else
                    {
                        lblResult.Foreground = Brushes.Red;
                        lblResult.Content = err;
                    }
                }
            }
            return success;
        }
        private void OnlistBoxFields_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var lb = (ListBox)sender;
            if (lb.SelectedIndex < 0) return;

            textQuery.Text += "[" + listBoxFields.SelectedItem + "] ";
        }

        private void OnListBoxSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            ShowValues(listBoxFields.SelectedIndex);
        }

        private void ShowValues(int fieldIndex)
        {
            if (Shapefile.NumFields - 1 < fieldIndex)
            {
                return;
            }
            _isString = Shapefile.get_Field(fieldIndex).Type == FieldType.STRING_FIELD;
            MapWinGIS.Table tbl = Shapefile.Table;

            ShapefileFieldSummary sfs = new ShapefileFieldSummary();
            for (int x = 0; x < Shapefile.NumShapes; x++)
            {
                string rowEntry = "";
                if (Shapefile.CellValue[fieldIndex, x] != null)
                {
                    if (_isString)
                    {
                        rowEntry = (string)Shapefile.CellValue[fieldIndex, x];
                        if (rowEntry.Length == 0)
                        {
                            rowEntry = "[Blank]";
                        }
                    }
                    else
                    {
                        Object obj = Shapefile.CellValue[fieldIndex, x];
                        rowEntry = obj.ToString();
                    }
                }

                if (!sfs.FieldEntriesAndCount.Keys.Contains(rowEntry))
                {
                    sfs.FieldEntriesAndCount.Add(rowEntry, 1);
                }
                else
                {
                    sfs.FieldEntriesAndCount[rowEntry]++;
                }
            }

            dgvValues.DataContext = sfs.FieldEntriesAndCount.OrderBy(t => t.Key);
            dgvValues.Columns.Clear();
            dgvValues.Columns.Add(new DataGridTextColumn { Header = "Cell value", Binding = new Binding("Key") });
            dgvValues.Columns.Add(new DataGridTextColumn { Header = "Count", Binding = new Binding("Value") });


        }

        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isString)
            {
                textQuery.Text += $"\"{_selectedGridItem.Key}\"";
            }
            else
            {
                textQuery.Text += $"{_selectedGridItem.Key}";
            }
        }

        private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvValues.SelectedItem != null)
            {
                _selectedGridItem = (KeyValuePair<string, int>)dgvValues.SelectedItem;
            }
        }
    }
}
