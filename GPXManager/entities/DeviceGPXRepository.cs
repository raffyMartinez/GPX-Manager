using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities
{
    public class DeviceGPXRepository
    {
        public List<DeviceGPX> DeviceGPXes { get; set; }

        public DeviceGPXRepository()
        {
            DeviceGPXes = getDeviceGPXes();
        }




        public List<DeviceGPX> getDeviceGPXes()
        {
            var thisList = new List<DeviceGPX>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from device_gpx";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        thisList.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            DeviceGPX gpx = new DeviceGPX();
                            gpx.Filename = dr["FileName"].ToString();
                            gpx.GPS = Entities.GPSViewModel.GetGPSEx(dr["DeviceID"].ToString());
                            gpx.RowID = int.Parse(dr["RowID"].ToString());
                            gpx.GPX = dr["gpx_xml"].ToString();
                            gpx.MD5 = dr["md5"].ToString();
                            gpx.GPXType = dr["gpx_type"].ToString(); ;
                            gpx.TimeRangeStart = (DateTime)dr["time_range_start"];
                            gpx.TimeRangeEnd = (DateTime)dr["time_range_end"];
                            if (dr["TimerInterval"] != DBNull.Value)
                            {
                                gpx.TimerInterval = (int)dr["TimerInterval"];
                            }
                            thisList.Add(gpx);
                        }
                    }
                }
                catch (OleDbException dbex)
                {

                }
                catch (Exception ex)
                {
                    switch (ex.HResult)
                    {
                        case -2147024809:
                            var arr = ex.Message.Split(new char[] { ' ', '\'' });
                            string fieldName = arr[2];
                            if (AddField(fieldName))
                            {
                                return getDeviceGPXes();
                            }
                            break;
                        default:
                            Logger.Log(ex);
                            break;
                    }

                }
            }
            return thisList;
        }

        private bool AddField(string name)
        {
            Type t;
            switch (name)
            {
                case "TimerInterval":
                    return AddColumn(name, "Int");
            }
            return false;
        }

        private bool AddColumn(string colName, string type, int? length = null)
        {
            string sql = "";
            if (type == "bool")
            {
                sql = $"ALTER TABLE device_gpx ADD COLUMN {colName} BIT DEFAULT 0";
            }
            else if(type=="VarChar")
            {
                sql = $"ALTER TABLE device_gpx ADD COLUMN {colName} {type}({length})";
            }
            else
            {
                sql = $"ALTER TABLE device_gpx ADD COLUMN {colName} {type}";
            }
            using (var con = new OleDbConnection(Global.ConnectionString))
            {
                con.Open();
                OleDbCommand myCommand = new OleDbCommand();
                myCommand.Connection = con;
                myCommand.CommandText = sql;
                try
                {
                    myCommand.ExecuteNonQuery();
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    return false;
                }
                myCommand.Connection.Close();
                return true;
            }
        }
        public bool Add(DeviceGPX gpx)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {

                conn.Open();
                var sql = $@"Insert into device_gpx (DeviceID,FileName,gpx_xml,RowID,d5,DateAdded,DateModified,gpx_type,time_range_start,time_range_end,TimerInterval)
                           Values (
                                    '{gpx.GPS.DeviceID}',
                                    '{gpx.Filename}', 
                                    '{gpx.GPX}',
                                     {gpx.RowID},
                                    '{gpx.MD5}',
                                    '{DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss")}',
                                    '{DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss")}',
                                    '{gpx.GPXType}',
                                    '{gpx.TimeRangeStart}',
                                    '{gpx.TimeRangeEnd}',
                                     {gpx.TimerInterval}
                                  )";
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
                var sql = $"Delete * from device_gpx";
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
        public bool Update(DeviceGPX gpx)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update device_gpx set
                                DeviceID= '{gpx.GPS.DeviceID}',
                                gpx_xml = '{gpx.GPX}',
                                FileName = '{gpx.Filename}',
                                md5='{gpx.MD5}',
                                DateModified='{DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss")}',
                                gpx_type = '{gpx.GPXType}',
                                time_range_start = '{gpx.TimeRangeStart}',
                                time_range_end = '{gpx.TimeRangeEnd}',
                                TimerInterval = {gpx.TimerInterval}
                            WHERE RowID = {gpx.RowID}";
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
                var sql = $"Delete * from device_gpx where RowID={rowID}";
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

        public int MaxRecordNumber()
        {
            int max_rec_no = 0;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT Max(RowID) AS max_id FROM device_gpx";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }
    }
}
