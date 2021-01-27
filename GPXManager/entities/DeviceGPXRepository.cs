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
                            thisList.Add(gpx);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);

                }
            }
             return thisList;
        }

        public bool Add(DeviceGPX gpx)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                
                conn.Open();
                var sql = $@"Insert into device_gpx (DeviceID,FileName,gpx_xml,RowID,md5,DateAdded,DateModified,gpx_type,time_range_start,time_range_end)
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
                                    '{gpx.TimeRangeEnd}'
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
                                time_range_end = '{gpx.TimeRangeEnd}'
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
