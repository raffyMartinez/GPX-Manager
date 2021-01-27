using GPXManager.entities;
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
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Windows.Documents.Serialization;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for EditTripWaypointsWindow.xaml
    /// </summary>
    public partial class EditTripWaypointsWindow : Window,IDisposable
    {
        private TripWaypoint _tripWaypoint;
        private Waypoint _oldWaypoint;
        public EditTripWaypointsWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            this.SavePlacement();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        public int TripWaypointID { get; set; }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

            if (IsNew)
            {
                _tripWaypoint = new TripWaypoint
                {
                    RowID = Entities.TripWaypointViewModel.NextRecordNumber,
                    Trip = Trip,
                    WaypointType = "Set",
                    SetNumber = Entities.TripWaypointViewModel.NextSetNumber(Trip),
                };
                labelTitle.Content = "Add a waypoint";
                Title = "Add trip waypoint";
                
            }
            else
            {
                var wptForEditing = Entities.TripWaypointViewModel.GetTripWaypoint(TripWaypointID);
                _tripWaypoint = new TripWaypoint
                {
                    RowID = wptForEditing.RowID,
                    SetNumber = wptForEditing.SetNumber,
                    Trip = wptForEditing.Trip,
                    WaypointName = wptForEditing.WaypointName,
                    WaypointType = wptForEditing.WaypointType,
                    TimeStamp = wptForEditing.TimeStamp,
                    Waypoint = wptForEditing.Waypoint,
                    WaypointGPXFileName = wptForEditing.WaypointGPXFileName
                };
                _oldWaypoint = wptForEditing.Waypoint;
                _oldWaypoint.Name = _tripWaypoint.WaypointName;
                labelTitle.Content = "Edit a trip waypoint";
                Title = "Edit trip waypoint";
            }

            PropertyGrid.AutoGenerateProperties = false;
            PropertyGrid.SelectedObject = _tripWaypoint;

            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "WaypointName", DisplayName = "Waypoint name", DisplayOrder = 1, Description = "Name of waypoint" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "WaypointType", DisplayName = "Waypoint type", DisplayOrder = 2, Description = "Select if waypoint is for setting or hauling of gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "TimeStampAdjusted", DisplayName = "Timestamp", DisplayOrder = 3, Description = "Timestamp of waypoint" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "SetNumber", DisplayName = "Set #", DisplayOrder = 4, Description = "Set number" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "Trip", DisplayName = "Trip ID", DisplayOrder = 5, Description = "Trip identifier" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "WaypointGPXFileName", DisplayName = "Source of waypoint", DisplayOrder = 6, Description = "GPX source" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "RowID", DisplayName = "ID", DisplayOrder = 7, Description = "Identifier" });



        }

        public void Dispose()
        {

        }
        public Trip Trip { get; set; }

        
        public bool IsNew { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonOK":
                    TripWaypoint tw = new TripWaypoint
                    {
                        Trip = _tripWaypoint.Trip,
                        WaypointName = _tripWaypoint.WaypointName,
                        WaypointType = _tripWaypoint.WaypointType,
                        TimeStamp = _tripWaypoint.TimeStamp,
                        RowID = _tripWaypoint.RowID,
                        SetNumber = _tripWaypoint.SetNumber,
                        Waypoint = _tripWaypoint.Waypoint,
                        WaypointGPXFileName = _tripWaypoint.WaypointGPXFileName
                    };

                    var result = Entities.TripWaypointViewModel.ValidateTrip(tw, IsNew, _oldWaypoint);
                    if (result.ErrorMessage.Length == 0 )
                    {
                        bool saveSuccess;
                        if(IsNew)
                        {
                            saveSuccess = Entities.TripWaypointViewModel.AddRecordToRepo(tw);
                        }
                        else
                        {
                            saveSuccess = Entities.TripWaypointViewModel.UpdateRecordInRepo(tw);
                        }

                        DialogResult = saveSuccess;
                    }
                    else
                    {
                        MessageBox.Show(result.ErrorMessage, "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    break;
                case "buttonCancel":
                    DialogResult = false;
                    break;
            }
        }

        private void OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            var prp = (PropertyItem)e.OriginalSource;
            switch (prp.PropertyName)
            {
                case "WaypointName":
                    if (Trip != null)
                    {
                        var wpt = Entities.WaypointViewModel.GetWaypoint(prp.Value.ToString(), Trip.GPS);
                        _oldWaypoint = ((TripWaypoint)PropertyGrid.SelectedObject).Waypoint;
                        ((TripWaypoint)PropertyGrid.SelectedObject).Waypoint = wpt;
                        ((TripWaypoint)PropertyGrid.SelectedObject).TimeStamp = wpt.Time.AddHours(Global.Settings.HoursOffsetGMT);
                        ((TripWaypoint)PropertyGrid.SelectedObject).WaypointGPXFileName = wpt.GPXFileName;
                        PropertyGrid.Update();
                    }
                    break;
            }
        }
    }
}
