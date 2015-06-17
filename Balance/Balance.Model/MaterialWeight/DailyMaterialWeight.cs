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
    public class DailyMaterialWeight
    {
        public static DataTable GetDailyMaterialWeight()
        {
            SingleBasicData singleBasicData = SingleBasicData.Creat();
            SingleTimeService singleTimeserver = SingleTimeService.Creat();
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            DataTable result = singleBasicData.BalanceTable.Clone();//
            string sql = @"SELECT A.OrganizationID,B.VariableId,(A.Name+B.Name) AS Name,B.TagTableName,B.Formula
                            FROM tz_Material AS A,material_MaterialDetail AS B,system_Organization AS C
                            WHERE A.KeyID=B.KeyID AND
                            A.OrganizationID=C.OrganizationID AND
                            A.Enable='True' AND
                            A.Type=2 AND
                            A.State=0 AND
                            C.LevelCode LIke (select LevelCode from system_Organization where OrganizationID=@organizationId)+'%'";
            SqlParameter parameter = new SqlParameter("organizationId", singleBasicData.OrganizationId);
            //需要保存的balance_Energy的质量信息
            DataTable variableInfo = dataFactory.Query(sql, parameter);
            //StringBuilder sqlBuilder = new StringBuilder();
            //sqlBuilder.Append("SELECT ");
            string mySql = "SELECT SUM({0}) AS Value FROM [{1}].[dbo].[HistoryDCSIncrement] WHERE {2} ";
            foreach (DataRow dr in variableInfo.Rows)
            {
                DataRow row = result.NewRow();
                row["VariableItemId"] = Guid.NewGuid().ToString();//ID
                row["VariableId"] = dr["VariableId"].ToString().Trim();
                row["VariableName"] = dr["Name"].ToString().Trim();
                row["PublicVariableId"] = row["KeyId"] = singleBasicData.KeyId;
                row["OrganizationID"] = dr["OrganizationID"].ToString().Trim();
                row["ValueType"] = "MaterialWeight";
                string[] arrayFields = { "TotalPeakValleyFlat", "MorePeak", "Peak", "Valley", "MoreValley", "Flat", "First", "Second", "Third" };
                foreach (string item in arrayFields)
                    row[item] = 0;
                result.Rows.Add(row);
                //*****************
                if ("1=0" != singleTimeserver.PeakTimeCriterion)//峰期
                {
                    DataTable peak = dataFactory.Query(string.Format(mySql, dr["Formula"].ToString().Trim(),singleBasicData.AmmeterName,singleTimeserver.PeakTimeCriterion));
                    row["Peak"] = peak.Rows.Count == 0 ? 0 : MyToDecimal(peak.Rows[0]["Value"]);
                }
                else
                {
                    row["Peak"] = 0;
                }
                if ("1=0" != singleTimeserver.MorePeakTimeCriterion)//尖峰期
                {
                    DataTable peak = dataFactory.Query(string.Format(mySql, dr["Formula"].ToString().Trim(), singleBasicData.AmmeterName, singleTimeserver.MorePeakTimeCriterion));
                    row["MorePeak"] = peak.Rows.Count == 0 ? 0 : MyToDecimal(peak.Rows[0]["Value"]);
                }
                else
                {
                    row["MorePeak"] = 0;
                }
                if ("1=0" != singleTimeserver.ValleyTimeCriterion)//谷期
                {
                    DataTable peak = dataFactory.Query(string.Format(mySql, dr["Formula"].ToString().Trim(), singleBasicData.AmmeterName, singleTimeserver.ValleyTimeCriterion));
                    row["Valley"] = peak.Rows.Count == 0 ? 0 : MyToDecimal(peak.Rows[0]["Value"]);
                }
                else
                {
                    row["Valley"] = 0;
                }
                if ("1=0" != singleTimeserver.MoreValleyTimeCriterion)//深谷期
                {
                    DataTable peak = dataFactory.Query(string.Format(mySql, dr["Formula"].ToString().Trim(), singleBasicData.AmmeterName, singleTimeserver.MoreValleyTimeCriterion));
                    row["MoreValley"] = peak.Rows.Count == 0 ? 0 : MyToDecimal(peak.Rows[0]["Value"]);
                }
                else
                {
                    row["MoreValley"] = 0;
                }
                if ("1=0" != singleTimeserver.FlatTimeCriterion)//平期
                {
                    DataTable peak = dataFactory.Query(string.Format(mySql, dr["Formula"].ToString().Trim(), singleBasicData.AmmeterName, singleTimeserver.FlatTimeCriterion));
                    row["Flat"] = peak.Rows.Count == 0 ? 0 : MyToDecimal(peak.Rows[0]["Value"]);
                }
                else
                {
                    row["Flat"] = 0;
                }
                if ("1=0" != singleTimeserver.FirstTimeCriterion)//甲班
                {
                    DataTable peak = dataFactory.Query(string.Format(mySql, dr["Formula"].ToString().Trim(), singleBasicData.AmmeterName, singleTimeserver.FirstTimeCriterion));
                    row["First"] = peak.Rows.Count == 0 ? 0 : MyToDecimal(peak.Rows[0]["Value"]);
                }
                else
                {
                    row["First"] = 0;
                }
                if ("1=0" != singleTimeserver.SecondTimeCriterion)//乙班
                {
                    DataTable peak = dataFactory.Query(string.Format(mySql, dr["Formula"].ToString().Trim(), singleBasicData.AmmeterName, singleTimeserver.SecondTimeCriterion));
                    row["Second"] = peak.Rows.Count == 0 ? 0 : MyToDecimal(peak.Rows[0]["Value"]);
                }
                else
                {
                    row["Second"] = 0;
                }
                if ("1=0" != singleTimeserver.ThirdTimeCriterion)//丙班
                {
                    DataTable peak = dataFactory.Query(string.Format(mySql, dr["Formula"].ToString().Trim(), singleBasicData.AmmeterName, singleTimeserver.ThirdTimeCriterion));
                    row["Third"] = peak.Rows.Count == 0 ? 0 : MyToDecimal(peak.Rows[0]["Value"]);
                }
                else
                {
                    row["Third"] = 0;
                }

                //**************
                //string[] arrayFields = { "TotalPeakValleyFlat", "MorePeak", "Peak", "Valley", "MoreValley", "Flat", "First", "Second", "Third" };
                foreach (string field in arrayFields)
                {
                    if (field == "TotalPeakValleyFlat")
                    {
                        row[field] = row[field + "B"] = MyToDecimal(row["First"]) + MyToDecimal(row["Second"]) + MyToDecimal(row["Third"]);
                    }
                    else
                    {
                        row[field + "B"] = row[field];
                    }

                }
                //************

            }
            //将今天的盘库量写到表中
            BalanceMartieralsClass.ProcessMartieralsClass(result);
            return result;
        }

        private static decimal MyToDecimal(object obj)
        {
            if (obj is DBNull)
            {
                obj = 0;
                return Convert.ToDecimal(obj);
            }
            else
            {
                return Convert.ToDecimal(obj);
            }
        }
    }
}
