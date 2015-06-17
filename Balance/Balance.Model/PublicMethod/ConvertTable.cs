using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balance.Model.PublicMethod
{
    public class ConvertTable
    {
        /// <summary>
        /// 纵表转横表
        /// </summary>
        /// <param name="source">源表</param>
        /// <returns></returns>
        public static DataTable VerticalToHorizontal(DataTable source, string convertColumn)
        {
            DataTable result = new DataTable();
            foreach (DataRow dr in source.Rows)
            {
                DataColumn column = new DataColumn(dr[0].ToString().Trim(), typeof(decimal));
                result.Columns.Add(column);
            }
            DataRow row = result.NewRow();
            foreach (DataRow dr in source.Rows)
            {               
                row[dr[0].ToString().Trim()] = dr[convertColumn];
            }
            result.Rows.Add(row);
            return result;
        }
    }
}
