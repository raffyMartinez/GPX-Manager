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
    /// Interaction logic for EditFisherWindow.xaml
    /// </summary>
    public partial class EditFisherWindow : Window
    {
        private Fisher _fisher;
        private static EditFisherWindow _instance;
        private List<Gear> _gears = new List<Gear>();
        public EditFisherWindow()
        {
            InitializeComponent();
            Closing += OnWindowClosing;
            Closed += OnWindowClosed;
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

            //rbNone.IsChecked = true;
            cboLandingSite.ItemsSource = Entities.LandingSiteViewModel.GetAll();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            ((MainWindow)Owner).ChildFormClosed();
            _instance = null;
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //_instance = null;

        }

        public static EditFisherWindow Instance()
        {
            return _instance;
        }

        public bool IsNew { get; set; }


        public static EditFisherWindow GetInstance()
        {
            if (_instance == null) _instance = new EditFisherWindow();
            return _instance;
        }
        public List<Gear> Gears
        {
            get { return _gears; }
            set
            {
                _gears = value;
                listBoxGears.Items.Clear();
                foreach (Gear g in _gears)
                {
                    listBoxGears.Items.Add(g.ToString());
                }
            }
        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonCancel":
                    Close();
                    //((MainWindow)Owner).ChildFormClosed();
                    break;
                case "buttonOk":
                    if (textFisherName.Text.Length > 0 &&
                        listBoxBoats.Items.Count > 0 &&
                        listBoxGears.Items.Count > 0 &&
                        cboLandingSite.Text.Length > 0)
                    {
                        bool proceed = true;
                        if ((bool)rbPhone.IsChecked || (bool)rbGPS.IsChecked)
                        {
                            proceed = cboDevice.Text.Length > 0;
                        }

                        if (proceed)
                        {
                            if (IsNew)
                            {
                                Fisher f = new Fisher { Name = textFisherName.Text, FisherID = Entities.FisherViewModel.NextRecordNumber() };
                                f.Gears = _gears;
                                foreach (string item in listBoxBoats.Items)
                                {
                                    f.Vessels.Add(item);
                                }
                                if (Entities.FisherViewModel.AddRecordToRepo(f))
                                {

                                    Close();
                                    //((MainWindow)Owner).ChildFormClosed();
                                }
                            }
                            else
                            {
                                _fisher.Name = textFisherName.Text;
                                _fisher.Vessels.Clear();
                                foreach (string item in listBoxBoats.Items)
                                {
                                    _fisher.Vessels.Add(item);
                                }
                                if (Entities.FisherViewModel.UpdateRecordInRepo(_fisher))
                                {
                                    Close();

                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please provide identifier of device assigned to fisher","GPX Manager",MessageBoxButton.OK,MessageBoxImage.Information);
                        }


                    }
                    else
                    {
                        MessageBox.Show("Please provide all the information that are asked", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "buttonDeleteGears":
                    if(listBoxGears.SelectedItems.Count==1)
                    {
                        var g = _gears.FirstOrDefault(t => t.ToString() == listBoxGears.SelectedItem.ToString());
                        _gears.Remove(g);
                        listBoxGears.Items.Remove(listBoxGears.SelectedItem);

                    }
                    break;
                case "buttonAddGears":
                    EditFisherBoat(_gears); 
                    break;
                case "buttonDeleteBoat":
                    break;
                case "buttonAddBoat":
                    EditSingleItemDialog esd = new EditSingleItemDialog();
                    esd.ItemType = "name of boat";
                    if ((bool)esd.ShowDialog())
                    {
                        listBoxBoats.Items.Add(esd.ItemForEditing);
                    }
                    break;
            }
        }
        private void EditFisherBoat(List<Gear>gears = null)
        {
            SelectGearWindow sgw = new SelectGearWindow();
            sgw.Gears = gears;
            sgw.Owner = this;
            sgw.ShowDialog();
        }
        public Fisher Fisher
        {
            get { return _fisher; }
            set
            {
                IsNew = false;
                _fisher = value;
                textFisherName.Text = _fisher.Name;
                foreach (var boat in _fisher.Vessels)
                {
                    listBoxBoats.Items.Add(boat);
                }
                switch(_fisher.DeviceType)
                {
                    case DeviceType.DeviceTypeNone:
                        rbNone.IsChecked = true;
                        break;
                    case DeviceType.DeviceTypeGPS:
                        rbGPS.IsChecked = true;
                        if(_fisher.GPS!=null)
                        {
                            cboDevice.SelectedItem = _fisher.GPS;
                        }
                        break;
                    case DeviceType.DeviceTypePhone:
                        rbPhone.IsChecked = true;
                        break;
                }
                if(_fisher.LandingSite!=null)
                {
                    cboLandingSite.SelectedItem = _fisher.LandingSite;
                }
            }
        }

        private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            switch (((ListBox)sender).Name)
            {
                case "listBoxBoats":
                    EditSingleItemDialog esd = new EditSingleItemDialog();
                    string copyOfItemToEdit = listBoxBoats.SelectedItems[0].ToString();
                    esd.ItemForEditing = listBoxBoats.SelectedItems[0].ToString();
                    esd.ItemType = "name of boat";
                    if ((bool)esd.ShowDialog())
                    {
                        foreach (var item in listBoxBoats.Items)
                        {
                            if (item.ToString() == copyOfItemToEdit)
                            {
                                listBoxBoats.Items.Remove(item);
                                break;
                            }
                        }
                        listBoxBoats.Items.Add(esd.ItemForEditing);
                    }
                    break;
                case "listBoxGears":

                    break;
            }

        }

        private void OnCheckChanged(object sender, RoutedEventArgs e)
        {
            cboDevice.IsEnabled = false;    
            cboDevice.Items.Clear();


            if ((bool)rbGPS.IsChecked)
            {
                foreach (var gps in Entities.GPSViewModel.GPSCollection.OrderBy(t => t.DeviceName))
                {
                    cboDevice.Items.Add(gps);
                }
                cboDevice.IsEnabled = true;

            }
            else if ((bool)rbPhone.IsChecked)
            {
                foreach (var name in Entities.CTXFileViewModel.GetUserNames())
                {
                    cboDevice.Items.Add(name);
                }
                cboDevice.IsEnabled = true;
            }
       


        }
    }
}
