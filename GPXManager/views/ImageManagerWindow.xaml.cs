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
using System.IO;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Windows.Navigation;



namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for ImageManager.xaml
    /// </summary>
    public partial class ImageManagerWindow : Window
    {
        private List<FileInfo> _imageFiles;
        private BitmapImage _src;
        public ImageManagerWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            panelFiles.Children[0].Visibility = Visibility.Hidden;
        }

        private void ShowList()
        {
            panelFiles.Children.Clear();
            if (_imageFiles.Count > 0)
            {
                foreach (var item in _imageFiles)
                {

                    RadioButton rb = new RadioButton { Content = item.Name, Tag = item };
                    rb.Margin = new Thickness(5, 3, 0, 0);
                    rb.Checked += OnButtonSelected;
                    panelFiles.Children.Add(rb);
                }
                panelFiles.Children[0].Visibility = Visibility.Visible;
            }
        }

        private async void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonMetadata":
                    break;
                case "buttonRegister":

                    break;
                case "buttonSelectFolder":
                    _imageFiles = await Entities.LogbookImageViewModel.GetImagesFromFolder();
                    ShowList();
                    break;
                case "buttonRotateLeft":
                    if(_src!=null)
                    {
                        TransformedBitmap transformBmp = new TransformedBitmap();
                        transformBmp.BeginInit();
                        transformBmp.Source = _src;
                        RotateTransform transform = new RotateTransform(270);
                        transformBmp.Transform = transform;
                        transformBmp.EndInit();

                        imagePreview.Source = transformBmp;
                    }
                    break;
                case "buttonRotateRight":
                    if (_src != null)
                    {
                        TransformedBitmap transformBmp = new TransformedBitmap();
                        transformBmp.BeginInit();
                        transformBmp.Source = _src;
                        RotateTransform transform = new RotateTransform(90);
                        transformBmp.Transform = transform;
                        transformBmp.EndInit();

                        imagePreview.Source = transformBmp;


                    }
                    break;
            }
        }

        private void OnButtonSelected(object sender, RoutedEventArgs e)
        {
            imageBorder.Reset();
            FileInfo file = (FileInfo)((RadioButton)sender).Tag;


            _src = new BitmapImage();
            _src.BeginInit();
            _src.UriSource = new Uri(file.FullName, UriKind.Absolute);
            _src.EndInit();

            imagePreview.Stretch = Stretch.Uniform;
            imagePreview.Source = _src;
            
            var metadata = Entities.LogbookImageViewModel.GetImageMetadata(file);

        }
    }
}
