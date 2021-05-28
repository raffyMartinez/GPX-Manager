using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities
{
    public class LandingSiteRepository
    {
        public List<LandingSite> LandingSites { get; set; }

        public LandingSiteRepository()
        {
            LandingSites = getLandingSites();
        }

        private List<LandingSite> getLandingSites()
        {
            List<LandingSite> list = new List<LandingSite>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from landing_sites";

                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        list.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            LandingSite ls = new LandingSite();
                            if (double.TryParse(dr["lat"].ToString(), out double lat))
                            {
                                ls.Lat = lat;
                            }
                            if (double.TryParse(dr["lon"].ToString(), out double lon))
                            {
                                ls.Lon = lon;
                            }
                            ls.Name = dr["Name"].ToString();
                            ls.Municipality = dr["Municipality"].ToString();
                            ls.Province = dr["Province"].ToString();
                            ls.ID = int.Parse(dr["ID"].ToString());


                            list.Add(ls);
                        }
                    }
                }
                catch (OleDbException dbex)
                {
                    switch (dbex.ErrorCode)
                    {
                        case -2147217865:
                            CreateTable();
                            break;
                        default:
                            Logger.Log(dbex);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    switch(ex.HResult)
                    {
                        case -2147024809:
                            if(ModifyTable())
                            {
                                return getLandingSites();
                            }
                            break;
                        default:
                            Logger.Log(ex);
                            break;
                    }
                    
                }

                return list;
            }

        }
        private bool AddColumn(string colName, string type, int? length = null)
        {
            string sql = "";
            if (type == "bool")
            {
                sql = $"ALTER TABLE landing_sites ADD COLUMN {colName} BIT DEFAULT 0";
            }
            else
            {
                sql = $"ALTER TABLE landing_sites ADD COLUMN {colName} {type}({length})";
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
        private bool ModifyTable()
        {
            if (AddColumn("Municipality", "VarChar", 100))
            {
                return (AddColumn("Province", "VarChar", 100));
            }
            else
            {
                return false;
            }
        }
        public bool Add(LandingSite ls)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                //var sql = "";

                var sql = $@"Insert into landing_sites(ID, Name, Lat, Lon, Municipality, Province)
                        Values (?, ?, ?, ?, ?, ?)";


                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    update.Parameters.Add("@id", OleDbType.Integer).Value = ls.ID;
                    update.Parameters.Add("@name", OleDbType.VarChar).Value = ls.Name;
                    if (ls.Lat == null)
                    {
                        update.Parameters.Add("@lat", OleDbType.Double).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@lat", OleDbType.Double).Value = ls.Lat;
                    }

                    if (ls.Lon == null)
                    {
                        update.Parameters.Add("@lon", OleDbType.Double).Value = DBNull.Value;
                    }
                    else
                    {
                        update.Parameters.Add("@lon", OleDbType.Double).Value = ls.Lon;
                    }

                    update.Parameters.Add("@mun", OleDbType.VarChar).Value = ls.Municipality;
                    update.Parameters.Add("@prov", OleDbType.VarChar).Value = ls.Province;

                    try
                    {
                        success = update.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException dbex)
                    {
                        switch(dbex.HResult)
                        {
                            case -2147217900:
                                if(ModifyTable())
                                {
                                    return Add(ls);
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            return success;
        }

        public bool Update(LandingSite ls)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    
                    cmd.Parameters.Add("@name", OleDbType.VarChar).Value = ls.Name;
                    cmd.Parameters.Add("@mun", OleDbType.VarChar).Value = ls.Municipality;
                    cmd.Parameters.Add("@prov", OleDbType.VarChar).Value = ls.Province;
                    if (ls.Lat == null)
                    {
                        cmd.Parameters.Add("@lat", OleDbType.Double).Value = DBNull.Value;
                    }
                    else
                    {
                        cmd.Parameters.Add("@lat", OleDbType.Double).Value = ls.Lat;
                    }

                    if (ls.Lon == null)
                    {
                        cmd.Parameters.Add("@lon", OleDbType.Double).Value = DBNull.Value;
                    }
                    else
                    {
                        cmd.Parameters.Add("@lon", OleDbType.Double).Value = ls.Lon;
                    }
                    cmd.Parameters.Add("@id", OleDbType.Integer).Value = ls.ID;

                    //if (ls.Lat != null && ls.Lon != null)
                    //{
                        cmd.CommandText = @"UPDATE landing_sites set
                                       Name =  @name,
                                       Municipality = @mun,
                                       Province = @prov, 
                                       Lat = @lat,
                                       Lon = @lon
                                       WHERE ID = @id";
                    //}
                    //else
                    //{
                    //    cmd.CommandText = @"UPDATE landing_sites set
                    //                   Name =  @name,
                    //                   Municipality = @mun,
                    //                   Province = @prov 
                    //                   WHERE ID = @id";
                    //}


                    try
                    {
                        success = cmd.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException dbex)
                    {
                        Logger.Log(dbex);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
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
                const string sql = "SELECT Max(ID) AS max_record_no FROM landing_sites";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }
        public bool Delete(int id)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from landing_sites where v_unload_id={id}";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    try
                    {
                        success = update.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException)
                    {

                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
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
                string sql = @"CREATE TABLE landing_sites 
                                (
                                ID Int NOT NULL PRIMARY KEY,
                                Name VarChar,
                                Lat Double,
                                Lon Double
                                )";
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;
                cmd.CommandText = sql;

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (OleDbException)
                {
                    //ignore
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }

                cmd.Connection.Close();
                conn.Close();
            }
        }

    }
}
