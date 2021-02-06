using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class LogbookImage
    {
        public string Title
        {
            get
            {
                if (Ignore)
                {
                    return FileName;
                }
                else
                {
                    return $"{System.IO.Path.GetFileName(FileName)} - {GPS.DeviceName} {((DateTime)Start).ToString("dd-MMM-yyyy")}";
                }
            }
        }
        public string Comment { get; set; }
        public string FileName { get; set; }
        public GPS GPS { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public string Notes { get; set; }
        public Gear Gear { get; set; }

        public Trip Trip { get; set; }

        public int FisherID { get; set; }

        public Fisher Fisher { get { return Entities.FisherViewModel.GetFisher(FisherID); } }

        public string Boat { get; set; }

        public bool Ignore { get; set; }

        public DateTime DateAddedToDatabase { get; set; }

        public bool TripWithTrack
        {
            get
            {
                Trip = Entities.TripViewModel.GetTrip(Trip.TripID);
                if(Trip.GPXFileName==null || Trip.GPXFileName.Length==0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

    }
}
