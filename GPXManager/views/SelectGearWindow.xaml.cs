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
    /// Interaction logic for SelectGearWindow.xaml
    /// </summary>
    public partial class SelectGearWindow : Window
    {
        public SelectGearWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        public List<Gear> Gears { get; set; } = new List<Gear>();
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (Gear g in Entities.GearViewModel.GetAllGears().OrderBy(t => t.Name))
            {
                panelGears.Children.Add(new CheckBox { Content = g.ToString(), Tag = g, Margin = new Thickness(3) });
            }

            if(Gears.Count>0)
            {
                foreach (Gear g in Gears)
                {
                    foreach (CheckBox chk in panelGears.Children)
                    {
                        if(g.ToString()==chk.Content.ToString())
                        {
                            chk.IsChecked = true;
                            break;
                        }
                    }
                }
            }
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonOk":
                    int counter = 0;
                    List<Gear> gears = new List<Gear>();
                    foreach(CheckBox  c in panelGears.Children)
                    {
                        if((bool)c.IsChecked)
                        {
                            gears.Add((Gear)c.Tag);
                            counter++;
                        }
                    }
                    if(counter==0)
                    {
                        MessageBox.Show("Select at least one gear", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ((EditFisherWindow)Owner).Gears = gears;
                        Close();
                    }
                    break;
                case "buttonCancel":
                    Close();
                    break;
            }
        }
    }
}
