using Balance.Infrastructure.BasicDate;
using Balance.Infrastructure.Configuration;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balance.Model.MaterialWeight
{
    /// <summary>
    /// 班盘库(盘库后的值填到balance表带B的字段中)
    /// </summary>
    public class BalanceMartieralsClass
    {
        public static void ProcessMartieralsClass(DataTable balanceTable)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("甲班", "FirstB");
            dictionary.Add("乙班", "SecondB");
            dictionary.Add("丙班", "ThirdB");
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            string mySql = @"select  B.ShiftDate,A.Class,A.OrganizationID, A.VariableId, A.DataValue
                                from balance_BalanceMartieralsClass A,shift_WorkingTeamShiftLog B
                                where A.WorkingTeamShiftLogID=B.WorkingTeamShiftLogID
								and CONVERT(varchar(10),B.ShiftDate,20)=@date";
            SingleBasicData singleBasicData = SingleBasicData.Creat();
            string date = singleBasicData.Date;
            SqlParameter parameter = new SqlParameter("date", date);
            DataTable sourceTable = dataFactory.Query(mySql, parameter);
            foreach (DataRow dr in sourceTable.Rows)
            {
                //信息列表
                string t_class = dr["Class"].ToString().Trim();
                string t_classField = dictionary[t_class];//班组对应的字段
                string t_organizationId = dr["OrganizationID"].ToString().Trim();
                string t_variableId = dr["VariableId"].ToString().Trim();
                decimal t_value = 0;
                decimal.TryParse(dr["DataValue"].ToString().Trim(), out t_value);
                //
                DataRow[] rows = balanceTable.Select(string.Format("OrganizationID='{0}' and VariableId='{1}'",t_organizationId,t_variableId));
                if (rows.Count() == 0)
                {
                    continue;
                }
                else if (rows.Count() >= 2)
                {
                    throw new Exception("数据错误");
                }
                else if(rows.Count()==1)
                {
                    rows[0][t_classField] = t_value;//将值写到balanceTable中
                    decimal total=Convert.ToDecimal(rows[0]["TotalPeakValleyFlat"]);
                    //各个期所占比例
                    decimal peakRatio = total == 0 ? 0 : Convert.ToDecimal(rows[0]["Peak"]) / total;                //峰期
                    decimal morePeakRatio = total == 0 ? 0 : Convert.ToDecimal(rows[0]["MorePeak"]) / total;        //深峰期
                    decimal valleyRatio = total == 0 ? 0 : Convert.ToDecimal(rows[0]["Valley"]) / total;            //谷期
                    decimal moreValleyRatio = total == 0 ? 0 : Convert.ToDecimal(rows[0]["MoreValley"]) / total;    //深谷
                    decimal flatRatio = total == 0 ? 0 : Convert.ToDecimal(rows[0]["Flat"]) / total;                //平期
                    rows[0]["TotalPeakValleyFlatB"] = Convert.ToDecimal(rows[0]["FirstB"]) + Convert.ToDecimal(rows[0]["SecondB"])
                        + Convert.ToDecimal(rows[0]["ThirdB"]);
                    //峰谷平分摊盘库量
                    rows[0]["PeakB"] = Convert.ToDecimal(rows[0]["TotalPeakValleyFlatB"]) * peakRatio;
                    rows[0]["MorePeakB"] = Convert.ToDecimal(rows[0]["TotalPeakValleyFlatB"]) * morePeakRatio;
                    rows[0]["ValleyB"] = Convert.ToDecimal(rows[0]["TotalPeakValleyFlatB"]) * valleyRatio;
                    rows[0]["MoreValleyB"] = Convert.ToDecimal(rows[0]["TotalPeakValleyFlatB"]) * moreValleyRatio;
                    rows[0]["FlatB"] = Convert.ToDecimal(rows[0]["TotalPeakValleyFlatB"]) * flatRatio;
                }
            }
        }
    }
}
