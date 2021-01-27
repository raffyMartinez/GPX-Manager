using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.ComponentModel;
using System.Security.Policy;

namespace GPXManager.entities
{
    [DisplayName("GPS")]
    public class GPSEdited
    {
        public GPSEdited() { }
        public GPSEdited(GPS gps)
        {
            DeviceID = gps.DeviceID;
            DeviceName = gps.DeviceName;
            Code = gps.Code;
            Brand = gps.Brand;
            Model = gps.Model;
            Folder = gps.Folder;
            //PNPDeviceID = gps.PNPDeviceID;
            //VolumeName = gps.VolumeName;
        }

        [ReadOnly(true)]
        public string DeviceID { get; set; }
        public string DeviceName { get; set; }

        [ReadOnly(true)]
        public string Code { get; set; }

        public string Brand { get; set; }

        public string Model { get; set; }

        public string Folder { get; set; }

      
       // public string PNPDeviceID { get; set; }

       //public string VolumeName { get; set; }
    }
    public class GPS
    {
        private DetectedDevice _device;
        public GPS()
        {
        
        }
        public GPS(string deviceID, string deviceName, string code, string brand, 
            string model, string folder)
        {
            DeviceID = deviceID;
            DeviceName = deviceName;
            Code = code;
            Brand = brand;
            Model = model;
            Folder = folder;
        }

        [ReadOnly(true)]
        public string DeviceID { get; set; }
        public string DeviceName { get; set; }

        public string Code { get; set; }

        public string Brand { get; set; }

        public string Model { get; set; }

        public string Folder { get; set; }

       

        public DetectedDevice Device{
            get
            {
                if (_device == null)
                {
                    _device = Entities.DetectedDeviceViewModel.GetDevice(DeviceID);
                }
                return _device;
            } 
            set { _device= value; } }

        public override string ToString()
        {
            return DeviceName;
        }
    }
}
