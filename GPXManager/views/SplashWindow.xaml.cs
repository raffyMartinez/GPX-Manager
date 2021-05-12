using GPXManager.entities;
using GPXManager.entities.mapping;
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
using System.Windows.Threading;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        private DispatcherTimer _timer;
        private int _entityCount;
        public SplashWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }
        private void OnTimerTick(object sender, EventArgs e)
        {

            _timer.Stop();
            Close();

        }
        private async Task LoadEntitiesAsync()
        {
            await Task.Run(() => LoadEntities());
        }

        private void UpdateProgress(string entity,bool isDone=false)
        {

            _entityCount++;

            UpdateProgressBar.Dispatcher.BeginInvoke
                (DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                {
                    UpdateProgressBar.Value = _entityCount;
                    return null;
                }), null);

            LabelProgress.Dispatcher.BeginInvoke
            (
                DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                {
                    if (isDone)
                    {
                           LabelProgress.Content = "All entities loaded";
                    }
                    else
                    {
                        LabelProgress.Content = $"Loading {entity} entities";
                    }
                    return null;
                }
             ), null);
        }
        private void LoadEntities()
        {
            UpdateProgress("GPS");
            Entities.GPSViewModel = new GPSViewModel();
            UpdateProgress("detected devices");
            Entities.DetectedDeviceViewModel = new DetectedDeviceViewModel();
            UpdateProgress("GPX");
            Entities.GPXFileViewModel = new GPXFileViewModel();
            UpdateProgress("gears");
            Entities.GearViewModel = new GearViewModel();
            UpdateProgress("trips");
            Entities.TripViewModel = new TripViewModel();
            UpdateProgress("waypoints");
            Entities.WaypointViewModel = new WaypointViewModel();
            UpdateProgress("tracks");
            Entities.TrackViewModel = new TrackViewModel();
            UpdateProgress("trip waypoints");
            Entities.TripWaypointViewModel = new TripWaypointViewModel();
            UpdateProgress("device GPX");
            Entities.DeviceGPXViewModel = new DeviceGPXViewModel();
            UpdateProgress("AOIs");
            Entities.AOIViewModel = new AOIViewModel();
            UpdateProgress("logbook images");
            Entities.LogbookImageViewModel = new LogbookImageViewModel();
            UpdateProgress("landing sites");
            Entities.LandingSiteViewModel = new LandingSiteViewModel();
            UpdateProgress("fishers");
            Entities.FisherViewModel = new FisherViewModel();
            UpdateProgress("cybertracker data");
            Entities.CTXFileViewModel = new CTXFileViewModel();
            UpdateProgress("fisher device assignment");
            Entities.FisherDeviceAssignmentViewModel = new FisherDeviceAssignmentViewModel();


        }
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
            UpdateProgressBar.Value = 0;
            UpdateProgressBar.Maximum = 13;
            await LoadEntitiesAsync();
            UpdateProgress("", true);
            _timer.Start();
        }
    }
}
