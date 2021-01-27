using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities
{
    public class TripRepository
    {
        private string _nullString = "null";
        public List<Trip> Trips{ get; set; }

        public TripRepository()
        {
            Trips = getTrips();
        }

        private List<Trip> getTrips()
        {
            List<Trip> thisList = new List<Trip>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from trips";

                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        thisList.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            var track = new Track();
                            var gpx = dr["TrackGPX"].ToString();
                            if (gpx.Length > 0)
                            {
                                track.Read(gpx);
                                track.FileName = dr["GPXFileName"].ToString();
                                track.GPS = Entities.GPSViewModel.GetGPSEx(dr["DeviceID"].ToString());
                            }

                            Trip t = new Trip
                            {
                                DeviceID = dr["DeviceID"].ToString(),
                                TripID = int.Parse(dr["TripID"].ToString()),
                                DateTimeDeparture = (DateTime)dr["DateTimeDeparted"],
                                DateTimeArrival = (DateTime)dr["DateTimeArrived"],
                                OperatorName = dr["NameOfOperator"].ToString(),
                                VesselName = dr["NameOfFishingBoat"].ToString(),
                                Gear = dr["Gear"] == DBNull.Value ? null : Entities.GearViewModel.GetGear(dr["Gear"].ToString()),
                                OtherGear = dr["OtherGear"].ToString(),
                                Track = track,
                                Notes = dr["Notes"].ToString(),
                                GPXFileName = dr["GPXFileName"].ToString(),
                                XML = gpx,
                                DateAdded = (DateTime)dr["DateAdded"]
                            };
                            thisList.Add(t);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                return thisList;
            }
        }

        public int MaxRecordNumber()
        {
            int max_rec_no = 0;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT Max(TripID) AS max_id FROM trips";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }

        public bool Add(Trip t)
        {
            bool success = false;

            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Insert into trips (DeviceID,TripID,DateTimeDeparted,DateTimeArrived,
                            NameOfOperator,NameOfFishingBoat,Gear,OtherGear,TrackGPX,Notes,DateAdded,GPXFileName)
                           Values
                           (
                              '{t.DeviceID}',
                               {t.TripID},
                              '{t.DateTimeDeparture}',
                              '{t.DateTimeArrival}',
                              '{t.OperatorName}',
                              '{t.VesselName}',
                              '{(t.Gear==null?_nullString:t.Gear.Code)}',
                              '{t.OtherGear}',
                              '{t.Track.XMLString}',
                              '{t.Notes}',
                              '{t.DateAdded.ToString("MMM-dd-yyyy HH:mm:ss")}',
                              '{t.GPXFileName}' 
                           )";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Update(Trip t)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update trips set
                                DateTimeDeparted = '{t.DateTimeDeparture}',
                                DateTimeArrived = '{t.DateTimeArrival}',
                                NameOfOperator = '{t.OperatorName}',
                                NameOfFishingBoat = '{t.VesselName}',
                                Gear = '{(t.Gear == null ? _nullString : t.Gear.Code)}',
                                OtherGear = '{t.OtherGear}',
                                TrackGPX = '{t.Track.XMLString}',
                                Notes = '{t.Notes}',
                                GPXFileName = '{t.Track.FileName}'
                            WHERE TripID = {t.TripID}";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }
        public bool ClearTable()
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from trips";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    try
                    {
                        update.ExecuteNonQuery();
                        success = true;
                    }
                    catch (OleDbException)
                    {
                        success = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        success = false;
                    }
                }
            }
            return success;
        }
        public bool Delete(int tripID)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from trips where TripID={tripID}";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    try
                    {
                        success = update.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException)
                    {
                        success = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        success = false;
                    }
                }
            }
            return success;
        }
    }
}
