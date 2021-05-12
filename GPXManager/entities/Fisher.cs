using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public enum DeviceType
    {
        DeviceTypeNone,
        DeviceTypeGPS,
        DeviceTypePhone
    }
    public class Fisher
    {
        private List<string> _gearCodes = new List<string>();

        public DeviceType DeviceType { get; set; }

        public string DeviceTypeString
        {
            get
            {
                string dt = "None";
                switch (DeviceType)
                {
                    case DeviceType.DeviceTypeGPS:
                        dt = "GPS";
                        break;
                    case DeviceType.DeviceTypePhone:
                        dt = "Phone";
                        break;
                }
                return dt;
            }
        }



        public int FisherID { get; set; }
        public string Name { get; set; }
        public List<string> Vessels { get; set; } = new List<string>();
        public List<string> GearCodes
        {
            get { return _gearCodes; }
            set
            {
                _gearCodes = value;
                foreach (var item in _gearCodes)
                {
                    if (item.Length > 0)
                    {
                        Gears.Add(Entities.GearViewModel.GetGear(item));
                    }
                }
            }
        }

        public List<Gear> Gears { get; set; } = new List<Gear>();
        public GPS GPS
        {
            get
            {
                if (DeviceType != DeviceType.DeviceTypeGPS)
                {
                    return null;
                }
                else
                {
                    return Entities.GPSViewModel.GetGPSEx(DeviceIdentifier);
                }

            }
        }
        public string PhoneUserName
        {
            get
            {
                if (DeviceType != DeviceType.DeviceTypePhone)
                {
                    return null;
                }
                else
                {
                    return DeviceIdentifier;
                }
            }

        }

        public string DeviceIdentifier { get; set; }
        public string CSV
        {
            get
            {
                string list = "";
                foreach (var g in Gears)
                {
                    list += $"{g.ToString()}, ";
                }
                return list.Trim(',', ' ');
            }
        }
        public LandingSite LandingSite { get; set; }
        public string VesselListCSV
        {
            get
            {
                string list = "";
                foreach (var item in Vessels)
                {
                    list += $"{item}, ";
                }
                return list.Trim(',', ' ');
            }
        }
        public string VesselList
        {
            get
            {
                string list = "";
                foreach (var item in Vessels)
                {
                    list += $"{item}|";
                }
                return list.Trim('|');
            }
        }
    }
}
