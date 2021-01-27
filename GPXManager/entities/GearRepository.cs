
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities
{
    public class GearRepository
    {
        public List<Gear> Gears { get; set; }

        public GearRepository()
        {
            Gears = getGears();
        }

        private List<Gear> getGears()
        {
            List<Gear> listGears = new List<Gear>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from gear";

                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        listGears.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            Gear g = new Gear();
                            g.Name = dr["GearName"].ToString();
                            g.BaseGearCode = dr["GenericCode"].ToString();
                            g.IsGeneric = (bool)dr["IsGeneric"];
                            g.Code = dr["GearCode"].ToString();
                            listGears.Add(g);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                return listGears;
            }
        }

        public bool Add(Gear g)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Insert into gear (GearName,GearCode,GenericCode,IsGeneric)
                           Values
                           ('{g.Name}','{g.Code}', '{g.BaseGearCode}', {g.IsGeneric})";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Update(Gear g)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update gear set
                                GearName = '{g.Name}',
                                GenericCode = '{g.BaseGearCode}',
                                IsGeneric = {g.IsGeneric}
                            WHERE GearCode = '{g.Code}'";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
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
                var sql = $"Delete * from gear where GearCode='{code}'";
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