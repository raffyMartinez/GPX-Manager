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

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for EditSingleItemDialog.xaml
    /// </summary>
    public partial class EditSingleItemDialog : Window
    {
        public EditSingleItemDialog()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }
        public string ItemType { get; set; }
        public string ItemForEditing { get; set; }
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Title = $"Provide {ItemType}";
            textItem.Text = ItemForEditing;
            labelForEditing.Content = Title;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonCancel":
                    DialogResult = false;
                    break;
                case "buttonOk":
                    if (textItem.Text.Length > 0)
                    {
                        ItemForEditing = textItem.Text;
                        DialogResult = true; ;
                    }
                    break;
            }
        }
    }
}
