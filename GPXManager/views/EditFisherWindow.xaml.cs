﻿using System;
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
        private static EditFisherWindow _instance;
        public EditFisherWindow()
        {
            InitializeComponent();
        }

        public static EditFisherWindow Instance()
        {
            return _instance;
        }

        public static EditFisherWindow GetInstance()
        {
            if (_instance == null) _instance = new EditFisherWindow();
            return _instance;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonCancel":
                    Close();
                    break;
                case "buttonOk":
                    if(textFisherName.Text.Length>0 && listBoxBoats.Items.Count>0)
                    {
                        Fisher f = new Fisher {Name = textFisherName.Text,FisherID = Entities.FisherViewModel.NextRecordNumber()};
                        foreach(string item in listBoxBoats.Items)
                        {
                            f.Vessels.Add(item);
                        }
                        if (Entities.FisherViewModel.AddRecordToRepo(f))
                        {
                            
                            Close();
                            ((MainWindow)Owner).Focus();
                        }
                    }
                    break;
                case "buttonDelete":
                    break;
                case "buttonAdd":
                    EditSingleItemDialog esd = new EditSingleItemDialog();
                    esd.ItemType = "name of boat";
                    if((bool)esd.ShowDialog())
                    {
                        listBoxBoats.Items.Add(esd.ItemForEditing);
                    }
                    break;
            }
        }

        private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditSingleItemDialog esd = new EditSingleItemDialog();
            string copyOfItemToEdit = listBoxBoats.SelectedItems[0].ToString();
            esd.ItemForEditing = listBoxBoats.SelectedItems[0].ToString();
            esd.ItemType = "name of boat";
            if ((bool)esd.ShowDialog())
            {
                foreach(var item in listBoxBoats.Items)
                {
                    if(item.ToString()==copyOfItemToEdit)
                    {
                        listBoxBoats.Items.Remove(item);
                        break;
                    }
                }
                listBoxBoats.Items.Add(esd.ItemForEditing);
            }
        }
    }
}
