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
    /// Interaction logic for ReviewXMLDialog.xaml
    /// </summary>
    public partial class ReviewXMLDialog : Window
    {
        public ReviewXMLDialog()
        {
            InitializeComponent();
            Loaded += ReviewXMLDialog_Loaded;
            Closing += ReviewXMLDialog_Closing;
        }

        private void ReviewXMLDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Entities.CTXFileViewModel.XMLofCTXReviewed -= CTXFileViewModel_XMLofCTXReviewed;
        }

        private void ReviewXMLDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Entities.CTXFileViewModel.XMLofCTXReviewed += CTXFileViewModel_XMLofCTXReviewed;
            progressBarr.Maximum = Entities.CTXFileViewModel.CountCTXFileWithNoXML();
            progressBarr.Value = 0;
            progressBarr.Visibility = Visibility.Collapsed;
            progressLabel.Content = "";
        }

        private void CTXFileViewModel_XMLofCTXReviewed(CTXFileViewModel s, CTXFileImportEventArgs e)
        {
            progressBarr.Dispatcher.BeginInvoke
                (DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                {
                    if (e.Context == "reviewing")
                    {
                        progressBarr.Value = e.XMLReviewedCount;
                    }
                    else if (e.Context == "saving")
                    {
                        progressBarr.Value = e.XMLReviewdSaveCount;
                    }
                    else if (e.Context == "done")
                    {
                        progressBarr.Value = 0;
                    }
                    return null;
                }), null);


            progressLabel.Dispatcher.BeginInvoke
            (
                DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                {
                    if (e.Context == "reviewing")
                    {
                        progressLabel.Content = $"Reviewed XML {progressBarr.Value}";
                    }
                    else if (e.Context == "saving")
                    {
                        progressLabel.Content = $"Saved recovered XML {progressBarr.Value}";
                    }
                    else if (e.Context == "done")
                    {
                        progressLabel.Content = $"Finished reviewing XML";
                    }
                    return null;
                }
             ), null);
        }

        private async void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Content)
            {
                case "Ok":
                    progressBarr.Visibility = Visibility.Visible;
                    await Entities.CTXFileViewModel.ReviewXMLAsync();
                    break;
                case "Cancel":
                    Close();
                    break;
            }
        }
    }
}
