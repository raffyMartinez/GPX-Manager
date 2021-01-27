using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
namespace GPXManager.entities
{
   public class TripWaypointRepository
    {
        public List<TripWaypoint> TripWaypoints { get; set; }

        public TripWaypointRepository()
        {
            TripWaypoints = getTripWaypoints();
        }

        private List<TripWaypoint> getTripWaypoints()
        {
            List<TripWaypoint> thisList = new List<TripWaypoint>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from trip_waypoints";

                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        thisList.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            DateTime timestamp = (DateTime)dr["TimeStamp"];
                            TripWaypoint t = new TripWaypoint
                            {
                                Trip = Entities.TripViewModel.GetTrip(int.Parse(dr["TripID"].ToString())),
                                RowID = int.Parse(dr["RowID"].ToString()),
                                WaypointName = dr["WaypointName"].ToString(),
                                SetNumber = int.Parse(dr["SetNumber"].ToString()),
                                TimeStamp = timestamp,
                                WaypointType = dr["WaypointType"].ToString(),
                                WaypointGPXFileName = dr["WaypointGPXFileName"].ToString(),
                                Waypoint = new Waypoint {
                                    Latitude = double.Parse(dr["Latitude"].ToString()), 
                                    Longitude = double.Parse(dr["Longitude"].ToString()),
                                    Time = timestamp }
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
        public bool ClearTable()
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from trip_waypoints";
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
        public int MaxRecordNumber()
        {
            int max_rec_no = 0;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT Max(RowID) AS max_id FROM trip_waypoints";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }

        public bool Add(TripWaypoint t)
        {
            bool success = false;

            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Insert into trip_waypoints (TripID,RowID,WaypointName,WaypointType,[TimeStamp],SetNumber,latitude,longitude,DateAdded,WaypointGPXFileName)
                           Values
                           (
                               {t.Trip.TripID},
                               {t.RowID},
                              '{t.WaypointName}',
                              '{t.WaypointType}',
                              '{t.TimeStamp}',
                               {t.SetNumber},
                               {t.Waypoint.Latitude},
                               {t.Waypoint.Longitude},
                              '{DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss")}',
                              '{t.WaypointGPXFileName}' 
                           )";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Update(TripWaypoint t)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update trip_waypoints set
                                TripID = {t.Trip.TripID},
                                WaypointName = '{t.WaypointName}',
                                WaypointType = '{t.WaypointType}',
                                [TimeStamp] = '{t.TimeStamp}',
                                SetNumber = {t.SetNumber},
                                Longitude = {t.Waypoint.Longitude},
                                Latitude = {t.Waypoint.Latitude},
                                WaypointGPXFileName = '{t.WaypointGPXFileName}'
                            WHERE RowID = {t.RowID}";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Delete(int rowID)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from trip_waypoints where RowID={rowID}";
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
