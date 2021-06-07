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
using System.Drawing.Drawing2D;
using MapWinGIS;


namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for FormatGridMapWindow.xaml
    /// </summary>
    public partial class FormatGridMapWindow : Window
    {
        private List<CategoryItem> _categoryItems = new List<CategoryItem>();
        private List<double> _dataValues = new List<double>();
        private ShapefileCategories _categories = new ShapefileCategories();
        private Dictionary<string, int> _sheetMapSummary = new Dictionary<string, int>();
        private ColorScheme _scheme = new ColorScheme();
        private List<AOI> _selectedAOIs = new List<AOI>();
        public FormatGridMapWindow()
        {
            InitializeComponent();
            Loaded += OnFormLoaded;
        }
        public FormatGridMapWindow(AOI aoi)
        {
            InitializeComponent();
            Loaded += OnFormLoaded;
            AOI = aoi;
        }

        public AOI AOI { get; set; }
        private void OnFormLoaded(object sender, RoutedEventArgs e)
        {
            icbColorScheme.ComboStyle = usercontrols.ImageComboStyle.ColorSchemeGraduated;
            icbColorScheme.ColorSchemes = MapWindowManager.MapLayersHandler.LayerColors;
            if (icbColorScheme.Items.Count > 0)
            {
                icbColorScheme.SelectedIndex = 0;
            }
            _selectedAOIs = Entities.AOIViewModel.GetSelectedAOIs();

            HashSet<string> gridColumnNames = new HashSet<string>();
            foreach (var aoi in _selectedAOIs)
            {
                if (aoi.BerriedGridColumn != null)
                {
                    gridColumnNames.Add(aoi.BerriedGridColumn);
                }

                if (aoi.CPUEGridColumn != null)
                {
                    gridColumnNames.Add(aoi.CPUEGridColumn);
                }

                if (aoi.EffortGridColumn != null)
                {
                    gridColumnNames.Add(aoi.EffortGridColumn);
                }

                if (aoi.UndersizedGridColumn != null)
                {
                    gridColumnNames.Add(aoi.UndersizedGridColumn);
                }
            }

            var list = gridColumnNames.ToList();
            list.Sort();
            foreach (var item in list)
            {
                cboGridColumnNames.Items.Add(item);
            }

        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Content)
            {
                case "Categorize":
                    int result = 0;
                    var isInt = int.TryParse(txtCategoryCount.Text, out result);

                    if (cboGridColumnNames.SelectedItem != null && isInt && result > 0)
                    {
                        bool categorizationSuccess = DoJenksFisherCategorization(icbColorScheme.SelectedIndex, _selectedAOIs.Count> 1);
                        if (categorizationSuccess)
                        {
                            dg.AutoGenerateColumns = false;
                            dg.CanUserAddRows = false;
                            dg.Columns.Clear();
                            dg.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") });
                            dg.Columns.Add(new DataGridTextColumn { Header = "Size", Binding = new Binding("ClassSize") });
                            dg.Columns.Add(new DataGridTextColumn { Header = "Legend", Binding = new Binding("Color") });
                            dg.DataContext = _categoryItems;

                            int r = 0;
                            foreach (var item in dg.Items)
                            {
                                var cell = dg.GetCell(r, 2);
                                if (cell != null)
                                {
                                    var ci = (CategoryItem)item;
                                    if (ci.Name != "nullCategory")
                                    {
                                        var color = ci.Color;

                                        cell.Background = new SolidColorBrush(ToMediaColor(color));
                                        cell.Content = "";
                                    }
                                }
                                r++;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select column name and provide number of classess", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "Ok":
                    foreach (var aoi in _selectedAOIs)
                    {
                        if (AssignClassificationToGrid(aoi))
                        {
                            ClassificationType classificationType = ClassificationType.JenksFisher;
                            MapWindowManager.MapLayersHandler[aoi.GridHandle].ClassificationType = classificationType;
                            MapWindowManager.MapControl.Redraw();
                            Close();
                        }
                    }
                    break;
                case "Cancel":
                    Close();
                    break;
            }
        }

        private bool AssignClassificationToGrid(AOI aoi)
        {
            int counter = 0;
            int idx = aoi.SubGrids.FieldIndexByName[cboGridColumnNames.SelectedItem.ToString()];
            aoi.SubGrids.Categories = _categories;
            aoi.SubGrids.Categories.ClassificationField = idx;
            for (int x = 0; x < aoi.SubGrids.NumShapes; x++)
            {
                for (int c = 0; c < _categories.Count; c++)
                {
                    if (_categories.Item[c].MinValue != null)
                    {
                        double min = (double)_categories.Item[c].MinValue;
                        double max = (double)_categories.Item[c].MaxValue;

                        double? valueToCategorize = aoi.SubGrids.CellValue[idx, x];
                        if (valueToCategorize != null)
                        {
                            if (valueToCategorize >= min && valueToCategorize < max)
                            {
                                aoi.SubGrids.ShapeCategory[x] = c;

                                _sheetMapSummary[_categories.Item[c].Name]++;
                                counter++;
                                break;
                            }
                        }
                        else
                        {
                            aoi.SubGrids.ShapeCategory[x] = _nullCategoryIndex;
                            break;
                        }
                    }
                }
            }
            return counter > 0;
        }
        public Color ToMediaColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }


        public List<double> ComputeDataValues(bool multiAOI = false)
        {
            var listValues = new List<double>();
            foreach (var aoi in _selectedAOIs)
            {
                for (int y = 0; y < aoi.SubGrids.NumShapes; y++)
                {
                    double? v = aoi.SubGrids.CellValue[AOI.SubGrids.FieldIndexByName[cboGridColumnNames.SelectedItem.ToString()], y];
                    if (v != null && v > 0)
                    {
                        listValues.Add((double)v);
                    }
                }
            }

            for (int x = 0; x < AOI.SubGrids.NumShapes; x++)
            {
                double? v = AOI.SubGrids.CellValue[AOI.SubGrids.FieldIndexByName[cboGridColumnNames.SelectedItem.ToString()], x];
                if (v != null && v > 0)
                {
                    listValues.Add((double)v);
                }
            }

            return listValues;
        }

        public System.Drawing.Color AddCategory(double min, double max)
        {
            var cat = new ShapefileCategory();
            cat.MinValue = min;
            cat.MaxValue = max;
            cat.Name = _categories.Count.ToString();
            cat.ValueType = tkCategoryValue.cvRange;
            _categories.Add2(cat);

            cat.DrawingOptions.FillColor = _scheme.get_GraduatedColor((double)(_categories.Count) / double.Parse(txtCategoryCount.Text));
            cat.DrawingOptions.LineColor = cat.DrawingOptions.FillColor;
            cat.DrawingOptions.LineWidth = 1.1F;
            cat.DrawingOptions.LineVisible = false;
            _sheetMapSummary.Add(cat.Name, 0);

            return Colors.UintToColor(cat.DrawingOptions.FillColor);
        }
        private bool DoJenksFisherCategorization(int colorIndex, bool multiAOI = false)
        {

            if (txtCategoryCount.Text.Length > 0)
            {
                _sheetMapSummary.Clear();
                _categoryItems.Clear();
                _dataValues = ComputeDataValues(multiAOI);
                _dataValues.Sort();
                var listBreaks = JenksFisher.CreateJenksFisherBreaksArray(_dataValues, int.Parse(txtCategoryCount.Text));
                var n = 0;
                var lower = listBreaks.Min();
                var upper = 0D;
                int row;

                System.Drawing.Color color;
                _categories.Clear();

                ColorBlend blend = (ColorBlend)icbColorScheme.ColorSchemes.List[colorIndex];
                _scheme = ColorSchemes.ColorBlend2ColorScheme(blend);

                //make categories from the breaks defined in Jenk's-Fisher's
                //add the category range and color to a datagridview
                CategoryItem ci;
                foreach (var item in listBreaks)
                {
                    if (n > 0)
                    {
                        upper = item;
                        color = AddCategory(lower, upper);
                        ci = new CategoryItem();
                        ci.Upper = item;
                        ci.Color = color;
                        ci.Lower = lower;
                        ci.ClassSize = GetClassSize(lower, upper).ToString();
                        lower = item;
                        _categoryItems.Add(ci);
                    }
                    n++;
                }
                //add the last category to the datagridview
                color = AddCategory(upper, _dataValues.Max() + 1);
                ci = new CategoryItem();
                ci.Color = color;
                ci.Lower = listBreaks.Max();
                ci.Upper = _dataValues.Max();
                ci.ClassSize = GetClassSize(listBreaks.Max(), 0, true).ToString();
                _categoryItems.Add(ci);

                //add an empty null category
                AddNullCategory();

                return _categoryItems.Count > 0;
            }
            else
            {
                MessageBox.Show("Specify number of categories", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return false;
        }

        private ShapefileCategory _nullCategory;
        private int _nullCategoryIndex;
        public void AddNullCategory()
        {
            _nullCategory = new ShapefileCategory();
            _nullCategory.Name = "nullCategory";
            //_nullCategory.DrawingOptions.FillColor = new Utils().ColorByName(tkMapColor.White);
            _nullCategory.DrawingOptions.FillVisible = false;
            _nullCategory.DrawingOptions.LineVisible = false;
            _categories.Add2(_nullCategory);
            _nullCategoryIndex = _categories.Count - 1;
            _sheetMapSummary.Add("Null", 0);
        }
        private int GetClassSize(double value1, double value2 = 0, bool greaterThan = false)
        {
            int count = 0;
            if (!greaterThan)
            {
                foreach (var item in _dataValues)
                {
                    if (item >= value1 && item < value2)
                    {
                        count++;
                    }
                    else if (item == value2)
                    {
                        break;
                    }
                }
            }
            else
            {
                foreach (var item in _dataValues)
                {
                    if (item >= value1)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

    }
}
