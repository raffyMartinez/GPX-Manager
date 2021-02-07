using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
namespace GPXManager.entities
{
    public class LogBookImageRepository
    {
        public LogBookImageRepository()
        {
            //UpdateTable();
            LogbookImages = getLogbookImages();
        }
        public List<LogbookImage> LogbookImages { get; set; }
        public List<LogbookImage> getLogbookImages()
        {
            var thisList = new List<LogbookImage>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from logbook_image";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        thisList.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            LogbookImage item = new LogbookImage();
                            item.FileName = dr["FileName"].ToString();
                            item.GPS = Entities.GPSViewModel.GetGPS(dr["GPSID"].ToString());
                            item.Start = (DateTime)dr["DateStart"];
                            item.End = (DateTime)dr["DateEnd"];
                            item.FisherID = int.Parse(dr["FisherID"].ToString());
                            item.Boat = dr["Boat"].ToString();
                            item.Gear = Entities.GearViewModel.GetGear(dr["GearID"].ToString());
                            item.DateAddedToDatabase = (DateTime)dr["DateAdded"];
                            item.Ignore = false;
                            item.Trip = Entities.TripViewModel.GetTrip(int.Parse(dr["TripID"].ToString()));
                            item.Comment = dr["ID"].ToString();
                            thisList.Add(item);
                        }
                    }
                }
                catch (OleDbException dbex)
                {
                    if (dbex.ErrorCode == -2147217865)
                    {
                        //table not found so we create one
                        CreateTable();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            thisList.AddRange(GetIgnoredImages());
            return thisList;
        }

        public List<LogbookImage> GetIgnoredImages()
        {
            var thisList = new List<LogbookImage>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from logbook_image_ignore";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        thisList.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            LogbookImage item = new LogbookImage();
                            item.FileName = dr["FileName"].ToString();
                            item.DateAddedToDatabase = (DateTime)dr["DateAdded"];
                            item.Ignore = true;
                            thisList.Add(item);
                        }
                    }
                }
                catch (OleDbException dbex)
                {
                    if (dbex.ErrorCode == -2147217865)
                    {
                        //table not found so we create one
                        CreateTable();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }

            return thisList;
        }

        public bool Add(LogbookImage image)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = "";
                if (image.Ignore)
                {
                    sql = $@"Insert into logbook_image_ignore(ID, FileName,DateAdded ) Values
                             ('{image.Comment}', '{image.FileName}', '{DateTime.Now}')";
                }
                else
                {
                    sql = $@"Insert into logbook_image (
                            ID,    
                            FileName, 
                            GPSID,
                            DateStart,
                            DateEnd,        
                            GearID,
                            DateAdded,
                            FisherID,
                            Boat,
                            TripID )
                           Values (
                            '{image.Comment}',
                            '{image.FileName}',
                            '{image.GPS.DeviceID}',
                            '{image.Start}', 
                            '{image.End}',
                            '{image.Gear.Code}',
                            '{DateTime.Now.ToString("dd-MMMM-yyyyy HH:mm:ss")}',
                             {image.FisherID},
                            '{image.Boat}',
                             {image.Trip.TripID}   
                           )";
                }

                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Update(LogbookImage image)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update logbook_image set
                                GPSID= '{image.GPS.DeviceID}',
                                DateStart = '{image.Start}',
                                DateEnd = '{image.End}',
                                GearID = '{image.Gear.Code}',
                                FisherID = {image.FisherID},
                                Boat = '{image.Boat}',
                                TripID = {image.Trip.TripID}
                            WHERE ID = '{image.Comment}'";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Delete(string ID)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from logbook_image where ID='{ID}'";
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
        public static void CreateTable()
        {
            using (var conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                string sql = @"CREATE TABLE logbook_image 
                                (
                                ID VarChar NOT NULL PRIMARY KEY,
                                FileName VarChar ,
                                GPSID VarChar,
                                TripID Int,
                                DateStart DateTime,
                                DateEnd DateTime,
                                GearID VarChar, 
                                FisherID Int,
                                Boat VarChar,
                                DateAdded DateTime,
                                CONSTRAINT image_gear FOREIGN KEY (GearID) REFERENCES gear(GearCode),
                                CONSTRAINT image_gps FOREIGN KEY (GPSID) REFERENCES devices(DeviceID),
                                CONSTRAINT image_trip FOREIGN KEY (TripID) REFERENCES trips(TripID)
                                )";
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();



                sql = @"CREATE INDEX GPSIDIndex
                        ON logbook_image(GPSID) WITH DISALLOW NULL";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();

                sql = @"CREATE INDEX GearIndex
                        ON logbook_image(GearID)";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();

                sql = @"CREATE INDEX DateStartIndex
                        ON logbook_image(DateStart)";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();

                sql = @"CREATE INDEX DateEndIndex
                        ON logbook_image(DateStart)";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();

                sql = @"CREATE TABLE logbook_image_ignore
                                (
                                ID VarChar NOT NULL PRIMARY KEY, 
                                FileName VarChar,
                                DateAdded DateTime
                                )";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();


                cmd.Connection.Close();
                conn.Close();
            }
        }
    }
}
