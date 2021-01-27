using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public static class ShapefileAttributeTableManager
    {

        public static string DataCaption { get; internal set; }
        public static MapInterActionHandler MapInterActionHandler { get; set; }
        
        /// <summary>
        /// creates a datatable that represents the dbf table of shapefile attributes
        /// </summary>
        /// <param name="sf"></param>
        /// <returns></returns>
        public static DataTable SetupAttributeTable(Shapefile sf)
        {
            DataTable dt = new DataTable();
            DataCaption = $"Name of layer: {MapInterActionHandler.MapLayersHandler.CurrentMapLayer.Name}";
            for (int y = 0; y < sf.NumFields; y++)
            {
                Field fld = sf.Field[y];
                string fieldCaption = fld.Name;
                Type t = typeof(int);
                switch (fld.Type)
                {
                    case FieldType.DOUBLE_FIELD:
                        t = typeof(double);
                        break;
                    case FieldType.STRING_FIELD:
                        t = typeof(string);
                        break;
                    case FieldType.INTEGER_FIELD:
                        t = typeof(int);
                        break;
                    case FieldType.DATE_FIELD:
                        t = typeof(DateTime);
                        break;
                    case FieldType.BOOLEAN_FIELD:
                        t = typeof(bool);
                        break;
                }
                dt.Columns.Add(new DataColumn { Caption = fieldCaption, DataType = t, ColumnName = fieldCaption });
            }

            DataRow row;
            if (sf.NumSelected == 0)
            {
                for (int x = 0; x < sf.NumShapes; x++)
                {
                    row = dt.NewRow();
                    for (int z = 0; z < sf.NumFields; z++)
                    {
                        row[z] = sf.CellValue[z, x];
                    }
                    dt.Rows.Add(row);
                }
            }
            else
            {
                for (int x = 0; x < sf.NumShapes; x++)
                {
                    if (sf.ShapeSelected[x])
                    {
                        row = dt.NewRow();
                        for (int z = 0; z < sf.NumFields; z++)
                        {
                            row[z] = sf.CellValue[z, x];
                        }
                        dt.Rows.Add(row);
                    }
                }
            }

            return dt;
        }
    }
}
