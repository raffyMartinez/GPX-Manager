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
using GPXManager.entities;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for TrackingIntervalDetailsWindow.xaml
    /// </summary>
    public partial class TrackingIntervalDetailsWindow : Window
    {
        private static TrackingIntervalDetailsWindow _instance;
        public TrackingIntervalDetailsWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
            dataGrid.AutoGenerateColumns = false;
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Duration", Binding = new Binding("Value")});
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Count", Binding = new Binding("Count")});
        }
        public static TrackingIntervalDetailsWindow GetInstance()
        {
            if (_instance == null) _instance = new TrackingIntervalDetailsWindow();
            return _instance;
        }
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _instance = null;
        }


        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Entities.CTXFileViewModel.GetGPSTimerIntervalFromCTX(CTXFile, false);
            var durationList = Entities.CTXFileViewModel.DurationList;
            if (durationList.Count > 0)
            {
                var query = from r in durationList
                            group r by r into g
                            select new { Count = g.Count(), Value = g.Key };

                var list = query.OrderBy(t=>t.Value).ToList();
                dataGrid.DataContext = list;

            }
            

        }
        public CTXFile CTXFile { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
