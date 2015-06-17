using Balance.Infrastructure.Configuration;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balance.Model.ShareElectricityQuantity
{
    /// <summary>
    /// 月辅助电量均摊
    /// </summary>
    public class ShareElectricityQuantityMonthly
    {
        /// <summary>
        /// 辅助电量分摊
        /// </summary>
        /// <param name="source">数据源表</param>
        /// <param name="result">结果表</param>
        /// <param name="monthKeyId">keyId(若为日均摊则为日keyId,若为月均摊则为月keyId)</param>
        public static void ToShare(DataTable source,DataTable result,string monthKeyId)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            //*****计算分摊电量
            string sqlShare = @"SELECT A.VariableId,A.OrganizationID,A.VariableName,A.ValueType,A.ValueFormula
                                    FROM balance_Energy_ShareTemplate AS A
                                    WHERE A.Enabled='True'";
            DataTable template = dataFactory.Query(sqlShare);
            string[] arrayFields = { "TotalPeakValleyFlat", "MorePeak", "Peak", "Valley", "MoreValley", "Flat", "First", "Second", "Third" ,
                                   "TotalPeakValleyFlatB", "MorePeakB", "PeakB", "ValleyB", "MoreValleyB", "FlatB", "FirstB", "SecondB", "ThirdB"};
            //平衡
            string[] arrayFieldsB = { "TotalPeakValleyFlatB", "MorePeakB", "PeakB", "ValleyB", "MoreValleyB", "FlatB", "FirstB", "SecondB", "ThirdB" };
            DataTable share = ShareCalculateService.CalculateByOrganizationId(source, template, "ValueFormula", arrayFields);
            //END

            string sqlLine = @"SELECT A.OrganizationID,(B.VariableId+'_ElectricityQuantity') AS VariableId
                                FROM tz_Formula AS A,formula_FormulaDetail AS B
                                WHERE A.KeyID=B.KeyID
                                AND B.LevelType='ProductionLine'";
            //找出产线级别的数据
            DataTable productLine = dataFactory.Query(sqlLine);
            foreach (DataRow dr in productLine.Rows)
            {
                foreach (DataRow row in result.Rows)
                {
                    //只有等于产线级别的数据才做处理
                    if (dr["VariableId"].ToString().Trim() == row["VariableId"].ToString().Trim() && dr["OrganizationID"].ToString().Trim() == row["OrganizationID"].ToString().Trim())
                    {
                        //找出分摊的
                        DataRow[] myRows = share.Select("OrganizationID='" + dr["OrganizationID"].ToString().Trim() + "'");
                        foreach (DataRow shareRow in myRows)
                        {
                            //循环列（带B的）
                            foreach (string item in arrayFieldsB)
                            {
                                //将分摊的电量加到产线上
                                row[item] = ShareCalculateService.MyToDecimal(row[item]) + ShareCalculateService.MyToDecimal(shareRow[item]);
                            }
                        }
                    }
                }
            }
            foreach (DataRow dr in share.Rows)
            {
                DataRow row = result.NewRow();
                row["VariableItemId"] = Guid.NewGuid().ToString();
                row["PublicVariableId"] = row["KeyId"] = monthKeyId;
                row["VariableId"] = dr["VariableId"];
                row["VariableName"] = dr["VariableName"];
                row["OrganizationID"] = dr["OrganizationID"];
                row["ValueType"] = dr["ValueType"];
                foreach (string item in arrayFields)
                {
                    row[item] = dr[item];
                }
                result.Rows.Add(row);
            }
        }
    }
}
