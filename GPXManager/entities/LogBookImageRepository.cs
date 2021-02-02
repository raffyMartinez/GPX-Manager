﻿using System;
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
                            item.GPS = Entities.GPSViewModel.GetGPS(dr["GPSID"].ToString());
                            item.Start = (DateTime)dr["DateStart"];
                            item.End = (DateTime)dr["DateEnd"];
                            item.Gear = Entities.GearViewModel.GetGear(dr["GearID"].ToString());
                        }
                    }
                }
                catch (OleDbException dbex)
                {
                    if (dbex.ErrorCode==-2147217865)
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
                var sql = $@"Insert into logbook_image(FileName, GPSID,DateStart,DateEnd,GearID,DateAdded)
                           Values (
                            '{image.GPS.DeviceID}',
                            '{image.Start}', 
                            '{image.End}',
                            '{image.Gear.Code}',
                            '{DateTime.Now.ToString("dd-MMMM-yyyyy HH:mm:ss")}'
                           )";

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
                                GearID = '{image.Gear.Code}'
                            WHERE FileName = '{image.FileName}'";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Delete(string fileName)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from logbook_image where FileName='{fileName}'";
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
                                FileName VarChar NOT NULL PRIMARY KEY,
                                GPSID VarChar,
                                DateStart DateTime NOT NULL,
                                DateEnd DateTime,
                                GearID VarChar, 
                                DateAdded DateTime Not Null,
                                CONSTRAINT image_gear FOREIGN KEY (GearID) REFERENCES gear(GearCode),
                                CONSTRAINT image_gps FOREIGN KEY (GPSID) REFERENCES devices(DeviceID)
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


                cmd.Connection.Close();                    
                conn.Close();
            }
        }
    }
}
