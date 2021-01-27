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
                            aoi.ID = int.Parse(dr["RowID"].ToString());
                            aoi.Visibility = true;
                            thisList.Add(aoi);
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
                var sql = $@"Insert into aoi(UpperLeftX, UpperLeftY, LowerRightX, LowerRightY, AOIName, RowID)
                           Values (
                               {aoi.UpperLeftX},
                               {aoi.UpperLeftY},
                               {aoi.LowerRightX},
                               {aoi.LowerRightY},
                               '{aoi.Name}',
                               {aoi.ID}
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
                                AOIName = '{aoi.Name}'
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
