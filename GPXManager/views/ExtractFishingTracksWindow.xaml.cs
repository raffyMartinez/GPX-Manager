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
using GPXManager.entities;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for ExtractFishingTracksWindow.xaml
    /// </summary>
    public partial class ExtractFishingTracksWindow : Window
    {
        private DispatcherTimer _timer;
        private int _trackCount;
        private int _timerSeconds;
        public ExtractFishingTracksWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            progressBar.Visibility = Visibility.Collapsed;
            labelProgress.Visibility = Visibility.Collapsed;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer();
            chkShowInMap.IsEnabled = false;
            if (entities.mapping.MapWindowManager.MapWindowForm != null)
            {
                chkShowInMap.IsEnabled = true;
            }
        }

        private async void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonOk":
                    _timer.Interval = new TimeSpan(0, 0, 1);
                    _timer.Tick += OnTimerTick;
                    _timer.Start();

                    progressBar.Visibility = Visibility.Visible;
                    progressBar.IsIndeterminate = true;
                    labelProgress.Visibility = Visibility.Visible;
                    Entities.ExtractedFishingTrackViewModel.TrackExtractedFromSourceCreated += ExtractedFishingTrackViewModel_TrackExtractedFromSourceCreated;

                    labelProgress.Content = "Getting xml data of tracks";
                    var list = await Entities.ExtractedFishingTrackViewModel.ExtractTracksFromSourcesAsync(
                        (bool)chkSave.IsChecked,
                        (bool)chkShowInMap.IsChecked,
                        true,
                        (bool)chkRefresh.IsChecked,
                        (bool)chkLogTracks.IsChecked
                        );

                    Entities.ExtractedFishingTrackViewModel.TrackExtractedFromSourceCreated -= ExtractedFishingTrackViewModel_TrackExtractedFromSourceCreated;
                    
                    if ((bool)chkShowInMap.IsChecked)
                    {
                        //entities.mapping.MapWindowManager.MapExtractedFishingTracksShapefile(Entities.ExtractedFishingTrackViewModel.ExtractedFishingTracksSF);
                        entities.mapping.MapWindowManager.AddExtractedTracksLayer(true);
                    }
                    _timer.Stop();
                    progressBar.Visibility = Visibility.Collapsed;

                    ((MainWindow)Owner).ShowExtractedFishingTracksFromGearHauling();
                    labelProgress.Content = $"Finished extracting {_trackCount} tracks in {_timerSeconds} seconds";
                    _timer.Tick -= OnTimerTick;
                    break;
                case "buttonCancel":

                    Close();
                    break;
            }

        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            _timerSeconds++;
        }

        private void ExtractedFishingTrackViewModel_TrackExtractedFromSourceCreated(entities.mapping.ExtractedFishingTrackViewModel s, entities.mapping.ExtractTrackEventArgs e)
        {
            //progressBar.Dispatcher.BeginInvoke
            //    (
            //      DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
            //      {
            //          progressBar.Value = 0;
            //                              //do what you need to do on UI Thread
            //                              return null;
            //      }
            //    ), null);

            labelProgress.Dispatcher.BeginInvoke
                (
                  DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                  {
                      _trackCount = e.Counter;
                      switch (e.Context)
                      {
                          case "Saved track":
                              labelProgress.Content = $"Saved track # {_trackCount}";
                              break;
                          case "Extracted track":
                              string sourceType = "GPX";
                              if (e.ExtractedFishingTrack.TrackSourceType == entities.mapping.ExtractedTrackSourceType.TrackSourceTypeCTX)
                              {
                                  sourceType = "CTX";
                              }
                              labelProgress.Content = $"Created track # {_trackCount} from {sourceType}";
                              break;
                      }

                      //do what you need to do on UI Thread
                      return null;


                  }
                 ), null);
        }
    }
}
