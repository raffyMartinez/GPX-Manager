using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
namespace GPXManager.entities
{
    public class GPSRepository
    {
        private List<GPS> gpsList = new List<GPS>();
        public List<GPS> GPSes { get; set; }

        public GPSRepository()
        {
            //UpdateTable();
            GPSes = getGPSes();
            if(gpsList.Count>0)
            {
                GPSes = gpsList;
            }
        }

        private List<GPS> getGPSes()
        {
            List<GPS> listGPS = new List<GPS>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from devices where DeviceType=1";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        listGPS.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            GPS gps = new GPS();
                            gps.DeviceID = dr["DeviceID"].ToString();
                            gps.Code = dr["Code"].ToString();
                            gps.DeviceName = dr["DeviceName"].ToString();
                            gps.Brand = dr["Brand"].ToString();
                            gps.Model = dr["Model"].ToString();
                            gps.Folder = dr["Folder"].ToString();
                            gps.DeviceType = (DeviceType)Enum.Parse(typeof(DeviceType), dr["DeviceType"].ToString());
                            //gps.PNPDeviceID = dr["PNPDeviceID"].ToString();
                            //gps.VolumeName = dr["VolumeName"].ToString();
                            listGPS.Add(gps);
                        }
                    }
                }
                catch(OleDbException dbex)
                {
                    switch(dbex.ErrorCode)
                    {
                        case -2147217904:
                            //ModifyGPSTable();
                            if(AddColumn("DeviceType","Int") && UpdateAllDevicesToGPS())
                            {
                                return getGPSes();
                            }
                        //No value given for one or more required parameters.
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);

                }
                
            }
            return listGPS;
        }
        private bool UpdateAllDevicesToGPS()
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update devices set DeviceType = 1";

                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }
        public bool Add(GPS gps)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                //var sql = $@"Insert into devices(Code,DeviceName,Brand,Model,DeviceID,Folder,DateAdded,PNPDeviceID,VolumeName)
                  var sql = $@"Insert into devices(Code,DeviceName,Brand,Model,DeviceID,Folder,DateAdded,isPhone)
                           Values (
                            '{gps.Code}',
                            '{gps.DeviceName}', 
                            '{gps.Brand}',
                            '{gps.Model}',
                            '{gps.DeviceID}',
                            '{gps.Folder}',
                            '{DateTime.Now.ToString("dd-MMMM-yyyyy HH:mm:ss")}',
                            false    
                           )";

                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Update(GPS gps)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update devices set
                                DeviceName= '{gps.DeviceName}',
                                Brand = '{gps.Brand}',
                                Model = '{gps.Model}',
                                Folder = '{gps.Folder}'
                            WHERE Code = '{gps.Code}'";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        //private bool UpdateTable()
        //{
        //    var updated = false;
        //    var columns = new List<string>();
        //    using (var con = new OleDbConnection(Global.ConnectionString))
        //    {
        //        con.Open();
        //        using (var cmd = new OleDbCommand("select * from devices", con))
        //            using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
        //            {
        //                var table = reader.GetSchemaTable();
        //                var nameColIndex = table.Columns["ColumnName"].Ordinal;
        //                foreach (DataRow row in table.Rows)
        //                {
        //                    columns.Add(row.ItemArray[nameColIndex].ToString());
        //                }
        //            }
        //    }

        //    if(!columns.Contains("isPhone"))
        //    {
        //       updated =  AddColumn("isPhone", "bool");

        //    }
        //    return updated;
        //}

        private bool AddColumn(string colName, string type, int? length = null)
        {
            string sql = "";
            if (type == "bool")
            {
                 sql = $"ALTER TABLE devices ADD COLUMN {colName} BIT DEFAULT 0";
            }
            else
            {
                if (length == null)
                {
                    sql = $"ALTER TABLE devices ADD COLUMN {colName} {type}";
                }
                else
                {
                    sql = $"ALTER TABLE devices ADD COLUMN {colName} {type}({length})";
                }
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
                catch(InvalidOperationException)
                {
                    return false;
                }
                catch(Exception ex)
                {
                    Logger.Log(ex);
                    return false;
                }
                myCommand.Connection.Close();
                return true;
            }
        }
        public bool ClearTable()
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from devices";
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
        public bool Delete(string code)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from devices where GPSCode='{code}'";
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
