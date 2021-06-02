using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities.mapping
{
    public class AOIRepository
    {
        public List<AOI> AOIs { get; set; }

        public AOIRepository()
        {
            AOIs = getAOIs();
        }

        private List<AOI> getAOIs()
        {
            var thisList = new List<AOI>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from aoi";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        thisList.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            AOI aoi = new AOI();
                            aoi.UpperLeftX = double.Parse(dr["UpperLeftX"].ToString());
                            aoi.UpperLeftY = double.Parse(dr["UpperLeftY"].ToString());
                            aoi.LowerRightX = double.Parse(dr["LowerRightX"].ToString());
                            aoi.LowerRightY = double.Parse(dr["LowerRightY"].ToString());
                            aoi.Name = dr["AOIName"].ToString();
                            aoi.GridFileName = dr["GridFileName"].ToString();
                            aoi.ID = int.Parse(dr["RowID"].ToString());
                            aoi.Visibility = true;
                            thisList.Add(aoi);
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
                            if (AddField(ex.Message.Split(' ', '\'')[2]))
                            {
                                return getAOIs();
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
            switch (name)
            {
                case "GridFileName":
                    return AddColumn(name, "VarChar", 255);
            }
            return false;
        }

        private bool AddColumn(string colName, string type, int? length = null)
        {
            string sql = "";
            if (type == "bool")
            {
                sql = $"ALTER TABLE aoi ADD COLUMN {colName} BIT DEFAULT 0";
            }
            else if (type == "VarChar")
            {
                sql = $"ALTER TABLE aoi ADD COLUMN {colName} {type}({length})";
            }
            else
            {
                sql = $"ALTER TABLE aoi ADD COLUMN {colName} {type}";
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

        public int MaxRecordNumber()
        {
            int max_rec_no = 0;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT Max(RowID) AS max_id FROM aoi";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }
        public bool Add(AOI aoi)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Insert into aoi(UpperLeftX, UpperLeftY, LowerRightX, LowerRightY, AOIName, RowID, GridFileName)
                           Values (
                               {aoi.UpperLeftX},
                               {aoi.UpperLeftY},
                               {aoi.LowerRightX},
                               {aoi.LowerRightY},
                               '{aoi.Name}',
                               {aoi.ID},
                               '{aoi.GridFileName}'
                           )";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Update(AOI aoi)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update aoi set
                                UpperLeftX= {aoi.UpperLeftX},
                                UpperLeftY = {aoi.UpperLeftY},
                                LowerRightX = {aoi.LowerRightX},
                                LowerRightY = {aoi.LowerRightY},
                                AOIName = '{aoi.Name}',
                                GridFileName = '{aoi.GridFileName}'
                            WHERE RowID = {aoi.ID}";
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
                var sql = $"Delete * from aoi";
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
        public bool Delete(int id)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from aoi where RowID={id}";
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
