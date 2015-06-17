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
    /// 日辅助电量均摊
    /// </summary>
    public class ShareElectricityQuantityDaily
    {
        public static void ToShare(DataTable result, string dailyKeyId)
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
            DataTable share = ShareCalculateService.CalculateByOrganizationId(result, template, "ValueFormula", arrayFields);
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
            //将数据放到result表中（result表结构和balance_Energy表的结构一致）
            //foreach (DataRow dr in source.Rows)
            //{
            //    DataRow row = result.NewRow();
            //    row["VariableItemId"] = Guid.NewGuid().ToString();
            //    row["PublicVariableId"] = row["KeyId"] = monthKeyId;// singleBasicData.MonthlyKeyId;
            //    foreach (DataColumn dc in source.Columns)
            //    {
            //        string dcName = dc.ColumnName;
            //        row[dcName] = dr[dcName];
            //    }
            //    result.Rows.Add(row);
            //}
            //将每条产线分摊后的电量作为一个工序插到result表中
            foreach (DataRow dr in share.Rows)
            {
                DataRow row = result.NewRow();
                row["VariableItemId"] = Guid.NewGuid().ToString();
                row["PublicVariableId"] = row["KeyId"] = dailyKeyId;//singleBasicData.MonthlyKeyId;
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
