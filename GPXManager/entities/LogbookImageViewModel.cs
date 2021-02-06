using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using Ookii.Dialogs.Wpf;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace GPXManager.entities
{
    public class LogbookImageViewModel
    {
        public bool EditSuccess { get; private set; }
        public ObservableCollection<LogbookImage> ImageCollection { get; set; }
        private LogBookImageRepository LogBookImages { get; set; }

        private List<FileInfo> _imageLists;

        public LogbookImageViewModel()
        {
            LogBookImages = new LogBookImageRepository();
            ImageCollection = new ObservableCollection<LogbookImage>(LogBookImages.LogbookImages);
            ImageCollection.CollectionChanged += ImageCollection_CollectionChanged;


        }

        public LogbookImage CurrentEntity { get; set; }

        public LogbookImage GetImage(string fileName)
        {
            return ImageCollection.Where(t => t.FileName == fileName).FirstOrDefault();
        }
        public async Task<List<FileInfo>> GetImagesFromFolder()
        {
            List<FileInfo> thisList = new List<FileInfo>();
            VistaFolderBrowserDialog vfbd = new VistaFolderBrowserDialog
            {
                Description = "Locate folder with files of images of logbook data",
                UseDescriptionForTitle = true,
                SelectedPath = Global.Settings.LogImagesFolder
            };
            if ((bool)vfbd.ShowDialog() && System.IO.Directory.Exists(vfbd.SelectedPath))
            {
                thisList = await Task.Run(() => GetImagesByFoldereRecursive(vfbd.SelectedPath, true));
            }
            return thisList;
        }

        public EntityValidationResult EntityValidated(LogbookImage image, bool isNew)
        {
            var result = new EntityValidationResult();


            return result;


        }
        public string MetadataFlatText(string fileName)
        {
            var m = GetImageMetadata(fileName);
            string s = $"File name: {m.FileName}\r\n";
            s += $"File type: {m.FileType}\r\n";
            s += $"Size: {m.FileSize} ({m.FileSize.ToSize(FileSizeFormatExtension.SizeUnits.MB)}MB)\r\n";
            s += $"Height: {m.Height}\r\n";
            s += $"Width: {m.Width}\r\n";
            s += $"Camera: {m.Make}\r\n";
            s += $"Model: {m.Model}\r\n";
            s += $"Data precision: {m.DataPrecision}\r\n";
            s += $"Exposure time: {m.ExposureTime}\r\n\r\n";
            s += $"Date created: {m.Original.ToString("dd-MMM-yyyy HH:mm:ss")}\r\n";
            s += $"Date digitized: {m.Digitized.ToString("dd-MMM-yyyy HH:mm:ss")}\r\n";
            s += $"Date modified: {m.Modified.ToString("dd-MMM-yyyy HH:mm:ss")}\r\n\r\n\r\n";
            s += $"Comment: {m.Comment} ";

            return s;
        }

        public string GetImageCommentMetadata(string fileName)
        {
            return GetImageMetadata(fileName).Comment;
        }
        public ImageMetadata GetImageMetadata(string fileName)
        {
            ImageMetadata metadata = null;
            if (fileName.Length > 0)
            {
                var directories = ImageMetadataReader.ReadMetadata(fileName);

                // print out all metadata
                metadata = new ImageMetadata();
                foreach (var directory in directories)
                    foreach (var tag in directory.Tags)
                    {
                        switch ($"{directory.Name}-{tag.Name}")
                        {
                            case "JPEG-Image Height":
                                var arr = tag.Description.Split(' ');
                                metadata.Height = int.Parse(arr[0]);
                                break;
                            case "JPEG-Image Width":
                                arr = tag.Description.Split(' ');
                                metadata.Width = int.Parse(arr[0]);
                                break;
                            case "JPEG-Data Precision":
                                metadata.DataPrecision = tag.Description;
                                break;
                            case "Exif IFD0-Make":
                                metadata.Make = tag.Description;
                                break;
                            case "Exif IFD0-Model":
                                metadata.Model = tag.Description;
                                break;
                            case "Exif IFD0-Orientation":
                                metadata.Orientation = tag.Description;
                                break;
                            case "Exif IFD0-Windows XP Comment":
                                metadata.Comment = tag.Description;
                                break;
                            case "Exif SubIFD-Exposure Time":
                                metadata.ExposureTime = tag.Description;
                                break;
                            case "Exif SubIFD-Date/Time Digitized":

                                metadata.Digitized = GetDateTimeFromMetadataTag(tag.Description);
                                break;
                            case "Exif SubIFD-Date/Time Original":
                                metadata.Original = GetDateTimeFromMetadataTag(tag.Description);
                                break;
                            case "File Type-Detected File Type Name":
                                metadata.FileType = tag.Description;
                                break;
                            case "File-File Name":
                                metadata.FileName = tag.Description;
                                break;
                            case "File-File Size":
                                arr = tag.Description.Split(' ');
                                metadata.FileSize = int.Parse(arr[0]);
                                break;
                            case "File-File Modified Date":
                                metadata.Modified = GetDateTimeFromMetadataTag(tag.Description, false);
                                break;
                        }
                        Console.WriteLine($"{directory.Name} - {tag.Name} = {tag.Description}");
                    }
            }
            return metadata;
        }

        public ImageSource ImageRotate(BitmapImage src, int rotationDegrees)
        {
            TransformedBitmap transformBmp = null;
            if (src != null)
            {
                transformBmp = new TransformedBitmap();
                transformBmp.BeginInit();
                transformBmp.Source = src;
                RotateTransform transform = new RotateTransform(rotationDegrees);
                transformBmp.Transform = transform;
                transformBmp.EndInit();
            }
            return transformBmp;
        }

        public bool IgnoreImage(LogbookImage image)
        {
            return AddRecordToRepo(image);

        }

        private DateTime GetDateTimeFromMetadataTag(string tag, bool pureNumeric = true)
        {
            var arr = tag.Split(new char[] { ' ', ':', '+' });
            if (pureNumeric)
            {
                return new DateTime
                    (
                    int.Parse(arr[0]),
                    int.Parse(arr[1]),
                    int.Parse(arr[2]),
                    int.Parse(arr[3]),
                    int.Parse(arr[4]),
                    int.Parse(arr[5])
                    );
            }
            else
            {
                int offset = int.Parse(arr[7]);
                string date = $"{arr[2]}-{arr[1]}-{arr[9]} {arr[3]}:{arr[4]}:{arr[5]}";
                return DateTime.Parse(date).AddHours(offset);
            }
        }
        private List<FileInfo> GetImagesByFoldereRecursive(string folder, bool? first = false)
        {

            if ((bool)first)
            {
                _imageLists = new List<FileInfo>();
            }
            var files = System.IO.Directory.GetFiles(folder).Select(s => new FileInfo(s));
            if (files.Any())
            {
                foreach (var file in files)
                {
                    string ext = file.Extension.ToLower();
                    if (ext == ".jpg" || ext == "*.jpeg" || ext == "png")
                    {
                        _imageLists.Add(file);
                    }
                }
            }
            foreach (var dir in System.IO.Directory.GetDirectories(folder))
            {
                GetImagesByFoldereRecursive(dir);
            }

            return _imageLists;
        }
        private void ImageCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EditSuccess = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        LogbookImage newImage = ImageCollection[newIndex];

                        if (LogBookImages.Add(newImage))
                        {
                            CurrentEntity = newImage;
                            EditSuccess = true;
                        }

                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {

                        List<LogbookImage> tempListOfRemovedItems = e.OldItems.OfType<LogbookImage>().ToList();
                        EditSuccess = LogBookImages.Delete(tempListOfRemovedItems[0].Comment);

                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<LogbookImage> tempList = e.NewItems.OfType<LogbookImage>().ToList();
                        EditSuccess = LogBookImages.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }


        public bool AddImageCommentToMetadata(string file)
        {
            bool success = false;
            var jpeg = new JpegMetadataAdapter(file);
            if (jpeg.Metadata != null)
            {
               
                try
                {
                    string comment = jpeg.Metadata.Comment;
                    if (comment == null || !comment.Contains("GPXManager"))
                    {
                        string newComment = $"GPXManager-{Guid.NewGuid().ToString()}";
                        jpeg.Metadata.Comment = newComment;
                        jpeg.Metadata.Title = "Logbook image for GPX Manager";
                        jpeg.Save();              // Saves the jpeg in-place
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return success;
        }
        public bool AddRecordToRepo(LogbookImage image)
        {
            if (image == null)
                throw new ArgumentNullException("Error: The argument is Null");

            image.Comment = GetImageCommentMetadata(image.FileName);
            
            if (image.Comment.Contains("GPXManager"))
            {
                ImageCollection.Add(image);
            }
            

            return EditSuccess;
        }

        public bool UpdateRecordInRepo(LogbookImage image)
        {
            if (image.FileName == null)
                throw new Exception("Error: Filename cannot be null");

            int index = 0;
            while (index < ImageCollection.Count)
            {
                if (ImageCollection[index].FileName == image.FileName)
                {
                    ImageCollection[index] = image;
                    break;
                }
                index++;
            }
            return EditSuccess;
        }

        public bool DeleteRecordFromRepo(string fileName)
        {
            if (fileName == null)
                throw new Exception("Filename cannot be null");

            int index = 0;
            while (index < ImageCollection.Count)
            {
                if (ImageCollection[index].FileName == fileName)
                {
                    ImageCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
            return EditSuccess;
        }
    }
}
