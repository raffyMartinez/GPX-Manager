using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Management;
using System.IO;
using System.Runtime.InteropServices;

namespace GPXManager.entities
{
    public class DetectedDeviceViewModel
    {

        public delegate void DetectDeviceHandler(DetectedDeviceViewModel s, DetectDeviceEventArg e);
        public event DetectDeviceHandler DeviceDetected;

        public ObservableCollection<DetectedDevice> DetectedDeviceCollection { get; set; }
        public DetectedDeviceViewModel()
        {
            DetectedDeviceCollection = new ObservableCollection<DetectedDevice>();
            DetectedDeviceCollection.CollectionChanged += DetectedDeviceCollection_CollectionChanged;
        }
        public List<DetectedDevice> GetAllDevices()
        {
            return DetectedDeviceCollection.ToList();
        }

        public DetectedDevice GetDeviceEx(string diskFileID)
        {
            CurrentEntity = DetectedDeviceCollection.FirstOrDefault(t => t.Disks[0].GPSID == diskFileID);
            return CurrentEntity;
        }
        public DetectedDevice GetDevice(string identifier)
        {
            CurrentEntity = DetectedDeviceCollection.FirstOrDefault(n => n.GPSID == identifier);

            if(CurrentEntity==null)
            {
                CurrentEntity = DetectedDeviceCollection.FirstOrDefault(n => n.PNPDeviceID == identifier);
            }
            return CurrentEntity;
        }

        public string DetectedUSBDeviceScanResult { get; set; }

        public int ScanUSBDevices()
        {
            string detectedDriveName="";
            int deviceCount = 0;
            string gpsID;
            bool hasDetectError;
            var drives = new ManagementObjectSearcher("select * from Win32_DiskDrive where InterfaceType='USB'").Get();
            if (drives.Count > 0)
            {

                foreach (ManagementObject drive in drives)
                {
                    
                    gpsID = "";
                    DetectedUSBDeviceScanResult = "";
                    hasDetectError = false;
                    if (int.Parse(drive.Properties["Partitions"].Value.ToString()) > 0)
                    {
                        var device = new DetectedDevice
                        {
                            SerialNumber = drive.Properties["SerialNumber"].Value.ToString(),
                            Caption = drive.Properties["Caption"].Value.ToString(),
                            Index = int.Parse(drive.Properties["Index"].Value.ToString()),
                            InterfaceType = drive.Properties["InterfaceType"].Value.ToString(),
                            Model = drive.Properties["Model"].Value.ToString(),
                            Partitions = int.Parse(drive.Properties["Partitions"].Value.ToString()),
                            PNPDeviceID = drive.Properties["PNPDeviceID"].Value.ToString(),
                            Signature = drive.Properties["Signature"].Value.ToString(),
                            Size = long.Parse(drive.Properties["Size"].Value.ToString()),
                            Status = drive.Properties["Status"].Value.ToString(),
                            BytesPerSector = int.Parse(drive.Properties["BytesPerSector"].Value.ToString()),
                            CreationClassName = drive.Properties["CreationClassName"].Value.ToString(),
                            Description = drive.Properties["Description"].Value.ToString(),
                            DeviceID = drive.Properties["DeviceID"].Value.ToString(),
                            MediaLoaded = bool.Parse(drive.Properties["MediaLoaded"].Value.ToString()),
                            Name = drive.Properties["Name"].Value.ToString(),
                            SectorsPerTrack = int.Parse(drive.Properties["SectorsPerTrack"].Value.ToString()),
                            TotalHeads = int.Parse(drive.Properties["TotalHeads"].Value.ToString()),
                            TotalCylinders = int.Parse(drive.Properties["TotalCylinders"].Value.ToString()),
                            TotalSectors = int.Parse(drive.Properties["TotalSectors"].Value.ToString()),
                            TracksPerCylinder = int.Parse(drive.Properties["TracksPerCylinder"].Value.ToString())
                        };


                        var mo = new ManagementObject("Win32_PhysicalMedia.Tag='" + drive["DeviceID"] + "'");

                        foreach (ManagementObject partition in new ManagementObjectSearcher
                            ("ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + drive["DeviceID"] +
                            "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get())
                        {
                            foreach (ManagementObject disk in new ManagementObjectSearcher
                                ("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + partition["DeviceID"] +
                                "'} WHERE AssocClass = Win32_LogicalDiskToPartition").Get())

                            {
                                detectedDriveName = "";
                                Disk dsk = null;
                                try
                                {
                                    dsk = new Disk
                                    {
                                        Caption = disk.Properties["Caption"].Value.ToString(),
                                        Compressed = bool.Parse(disk.Properties["Compressed"].Value.ToString()),
                                        Description = disk.Properties["Description"].Value.ToString(),
                                        //FreeSpace = long.Parse(drive.Properties["FreeSpace"].Value.ToString()),
                                        Size = long.Parse(drive.Properties["Size"].Value.ToString()),
                                        DeviceID = disk.Properties["DeviceID"].Value.ToString(),
                                        FileSystem = disk.Properties["FileSystem"].Value.ToString(),
                                        VolumeSerialNumber = disk.Properties["VolumeSerialNumber"].Value.ToString(),
                                        VolumeName = disk.Properties["VolumeName"].Value.ToString()
                                    };
                                }
                                catch(Exception ex)
                                {
                                    Logger.Log(ex);
                                    dsk = null;
                                }


                                try
                                {

                                    var gpsIDs = Directory.GetFiles($"{dsk.Caption}", "*.gpsid");
                                    int id_count = gpsIDs.Length;
                                    detectedDriveName = dsk.Caption;

                                    if (id_count == 1)
                                    {
                                        //dsk.GPSID = Path.GetFileName(gpsIDs[0]);
                                        gpsID=Path.GetFileNameWithoutExtension(gpsIDs[0]);
                                        device.GPSID = gpsID;
                                    }
                                    else if(id_count>1)
                                    {
                                        hasDetectError = true;
                                        DetectedUSBDeviceScanResult = $"Multiple gpsids detected in Drive {detectedDriveName}";
                                        Logger.Log(DetectedUSBDeviceScanResult);
                                    }
                                    
                                }
                                    //else if(id_count==0)
                                    //{
                                    //    throw new Exception("gpsid file is missing");
                                    //}
                                    //else
                                    //{
                                    //    throw new Exception("Cannot have more than 1 gpsid file");
                                    //}
                                
                                catch (Exception ex)
                                {
                                    Logger.Log(ex);
                                    dsk = null;
                                }


                                if (device.Disks == null)
                                {
                                    device.Disks = new List<Disk>();
                                }
                                device.Disks.Add(dsk);

                            }
                        }
                        if (GetDevice(device.PNPDeviceID) == null)
                        {
                            AddDevice(device);
                        }
                        deviceCount++;
                    }

                    if(DeviceDetected!=null)
                    {
                        DetectDeviceEventArg e = new DetectDeviceEventArg
                        {
                            Message = DetectedUSBDeviceScanResult,
                            GPSId = gpsID,
                            HasDetectError = hasDetectError,
                            DriveName = detectedDriveName
                        };

                        DeviceDetected(this, e);
                    }
                }
                return deviceCount;
            }
            else
            {
                return 0;
            }
        }
        public DetectedDevice CurrentEntity { get; set; }
        private void DetectedDeviceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        DetectedDevice newDevice = DetectedDeviceCollection[newIndex];
                        CurrentEntity = newDevice;

                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        //List<GPS> tempListOfRemovedItems = e.OldItems.OfType<GPS>().ToList();
                        //GPSes.Delete(tempListOfRemovedItems[0].Code);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        //List<GPS> tempList = e.NewItems.OfType<GPS>().ToList();
                        //GPSes.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }
        public int Count
        {
            get { return DetectedDeviceCollection.Count; }
        }

        public void AddDevice(DetectedDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("Error: The argument is Null");

                DetectedDeviceCollection.Add(device);
                var diskFileID = device.GPSID;
            //var gps = Entities.GPSViewModel.GetGPSEx(device.SerialNumber);
            if (diskFileID != null)
            {
                var gps = Entities.GPSViewModel.GetGPSEx(diskFileID);
                if (gps != null)
                {
                    device.GPS = gps;
                    //if(gps.PNPDeviceID==null || gps.PNPDeviceID.Length==0)
                    //{
                    //    gps.PNPDeviceID = device.PNPDeviceID;
                    //    gps.VolumeName = device.Disks[0].VolumeName;
                    //    Entities.GPSViewModel.UpdateRecordInRepo(gps);
                    //}
                }
            }
            
        }

        public void UpdateDeviceInCollection(DetectedDevice device)
        {
            if (device.SerialNumber == null)
                throw new Exception("Error: Serial number cannot be null");

            int index = 0;
            while (index < DetectedDeviceCollection.Count)
            {
                if (DetectedDeviceCollection[index].DeviceID == device.DeviceID)
                {
                    DetectedDeviceCollection[index] = device;
                    break;
                }
                index++;
            }
        }

        public bool DeleteDeviceFromCollection(string deviceID)
        {
            if (deviceID == null)
                throw new Exception("Serial number cannot be null");

            int index = 0;
            int deviceCountBeforeDelete = DetectedDeviceCollection.Count;
            while (index < DetectedDeviceCollection.Count)
            {
                if (DetectedDeviceCollection[index].DeviceID == deviceID)
                {
                    DetectedDeviceCollection.RemoveAt(index);
                    return deviceCountBeforeDelete > DetectedDeviceCollection.Count;
                }
                index++;
            }
            return false;
        }

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const int FILE_SHARE_READ = 0x1;
        const int FILE_SHARE_WRITE = 0x2;
        const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
        public bool EjectDrive(DetectedDevice device, out string statusMessage)
        {
            var driveLetter = device.Disks[0].Caption.Trim(':');
            if (DeleteDeviceFromCollection(device.DeviceID))
            {
                string path = @"\\.\" + driveLetter + @":";
                IntPtr handle = CreateFile(path, GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, 0x3, 0, IntPtr.Zero);

                if ((long)handle == -1)
                {
                    //MessageBox.Show("Unable to open drive " + driveLetter);
                    statusMessage = $"Unable to open drive {driveLetter}";
                    return false;
                }

                int dummy = 0;

                Entities.DeviceGPXViewModel.SaveDeviceGPXToRepository(device);

                DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0,
                    IntPtr.Zero, 0, ref dummy, IntPtr.Zero);

                CloseHandle(handle);

                //MessageBox.Show("OK to remove drive.");
                statusMessage = $"OK to remove drive.";
                return true;
            }
            else
            {
                statusMessage = $"Unable to open drive {driveLetter}";
                return false;
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr CreateFile
            (string filename, uint desiredAccess,
                uint shareMode, IntPtr securityAttributes,
                int creationDisposition, int flagsAndAttributes,
                IntPtr templateFile);
        [DllImport("kernel32")]
        private static extern int DeviceIoControl
            (IntPtr deviceHandle, uint ioControlCode,
                IntPtr inBuffer, int inBufferSize,
                IntPtr outBuffer, int outBufferSize,
                ref int bytesReturned, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
