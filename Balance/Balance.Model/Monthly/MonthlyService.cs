using Balance.Infrastructure.BasicDate;
using Balance.Infrastructure.Configuration;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Balance.Model.Monthly
{
    public class MonthlyService
    {
        /// <summary>
        /// 获得月电量（包括均摊电量）产量
        /// </summary>
        /// <param name="date">要计算月的DateTime</param>
        /// <returns></returns>
        public static DataTable GetElectricityQuantityMaterialWeight()
        {

            SingleBasicData singleBasicData = SingleBasicData.Creat();
            DateTime date = singleBasicData.MonthlyDate;
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            DataTable result = singleBasicData.BalanceTable.Clone();
            string sql = @"select h.VariableName,g.* from(
	                            SELECT VariableId, B.OrganizationID,B.ValueType,SUM(TotalPeakValleyFlat) AS TotalPeakValleyFlat, 
	                            SUM(MorePeak) AS MorePeak, SUM(Peak) AS Peak, SUM(Valley) AS Valley, SUM(MoreValley) AS MoreValley, SUM(Flat) AS Flat, 
	                            SUM(First) AS First,SUM(Second) AS Second, SUM(Third) AS Third, SUM(TotalPeakValleyFlatB) AS TotalPeakValleyFlatB,  
	                            SUM(MorePeakB) AS MorePeakB,SUM(PeakB) AS PeakB, SUM(ValleyB) AS ValleyB, SUM(MoreValleyB) AS MoreValleyB, SUM(FlatB) AS FlatB, 
	                            SUM(FirstB) AS FirstB,SUM(SecondB) AS SecondB, SUM(ThirdB) AS ThirdB
	                            FROM tz_Balance AS A,balance_Energy AS B
	                            WHERE A.BalanceId=B.KeyId
	                            AND B.ValueType<>'ElectricityConsumption'
	                            AND (A.TimeStamp>=@monthStart AND A.TimeStamp<@monthEnd)
	                            GROUP BY VariableId,B.OrganizationID,B.ValueType 
	                            )as g                   /*主表没有名字，用辅表来填充名字*/
                        left join
	                            (SELECT E.VariableId,E.VariableName,E.OrganizationID
	                            FROM (
	                            select d.VariableId,d.OrganizationID,max(c.TimeStamp) as TimeStamp
	                            from tz_Balance c,balance_Energy d
	                            where c.BalanceId=d.KeyID
	                            group by d.VariableId,d.OrganizationID
	                            ) AS F                     /*此表最近一条不重复的VariableId，OrganizationID*/
	                            LEFT JOIN
	                            (select distinct b.VariableId,b.VariableName,b.OrganizationID,a.TimeStamp
	                            from tz_Balance a,balance_Energy b
	                            where a.BalanceId=b.KeyID
	                            ) AS E
	                            ON ( F.OrganizationID=E.OrganizationID
	                            AND F.VariableId=E.VariableId
	                            AND F.TimeStamp=E.TimeStamp)
	                            )as h                       /*此表获得了所有的名字*/
                        on (g.OrganizationID=h.OrganizationID and (g.VariableId=h.VariableId or g.VariableId=h.VariableId+'_ElectricityQuantity'))";//包含上一个月的统计日期的数据 不包含这个月的统计日期的数据
            int monthDay = DateTime.DaysInMonth(date.Year, date.Month);
            int statisticalDay = Int16.Parse(ConfigService.GetConfig("StatisticalDay"));
            SqlParameter[] parameters = { new SqlParameter("monthStart", date.AddMonths(-1).ToString("yyyy-MM") + "-" + statisticalDay.ToString("D2")), new SqlParameter("monthEnd", date.ToString("yyyy-MM") + "-" + statisticalDay.ToString("D2")) };
            DataTable source = dataFactory.Query(sql, parameters);
            //将数据放到result表中（result表结构和balance_Energy表的结构一致）
            foreach (DataRow dr in source.Rows)
            {
                DataRow row = result.NewRow();
                row["VariableItemId"] = Guid.NewGuid().ToString();
                row["PublicVariableId"] = row["KeyId"] = singleBasicData.MonthlyKeyId;
                foreach (DataColumn dc in source.Columns)
                {
                    string dcName = dc.ColumnName;
                    row[dcName] = dr[dcName];
                }
                result.Rows.Add(row);
            }
            //*****计算月分摊电量
            if ("month" == ConfigService.GetConfig("ShareElectricityCycle"))
            {
                ShareElectricityQuantity.ShareElectricityQuantityMonthly.ToShare(source, result, singleBasicData.MonthlyKeyId);
            }
            #region
            //            string sqlShare = @"SELECT A.VariableId,A.OrganizationID,A.VariableName,A.ValueType,A.ValueFormula
            //                                    FROM balance_Energy_ShareTemplate AS A
            //                                    WHERE A.Enabled='True'";
            //            DataTable template = dataFactory.Query(sqlShare);
            //            string[] arrayFields = { "TotalPeakValleyFlat", "MorePeak", "Peak", "Valley", "MoreValley", "Flat", "First", "Second", "Third" ,
            //                                   "TotalPeakValleyFlatB", "MorePeakB", "PeakB", "ValleyB", "MoreValleyB", "FlatB", "FirstB", "SecondB", "ThirdB"};
            //            //平衡
            //            string[] arrayFieldsB = { "TotalPeakValleyFlatB", "MorePeakB", "PeakB", "ValleyB", "MoreValleyB", "FlatB", "FirstB", "SecondB", "ThirdB" };
            //            DataTable share = CalculateByOrganizationId(source, template, "ValueFormula", arrayFields);
            //            //END

            //            string sqlLine = @"SELECT A.OrganizationID,(B.VariableId+'_ElectricityQuantity') AS VariableId
            //                                FROM tz_Formula AS A,formula_FormulaDetail AS B
            //                                WHERE A.KeyID=B.KeyID
            //                                AND B.LevelType='ProductionLine'";
            //            //找出产线级别的数据
            //            DataTable productLine = dataFactory.Query(sqlLine);
            //            foreach (DataRow dr in productLine.Rows)
            //            {
            //                foreach (DataRow row in source.Rows)
            //                {
            //                    //只有等于产线级别的数据才做处理
            //                    if (dr["VariableId"].ToString().Trim() == row["VariableId"].ToString().Trim() && dr["OrganizationID"].ToString().Trim() == row["OrganizationID"].ToString().Trim())
            //                    {
            //                        //找出分摊的
            //                        DataRow[] myRows = share.Select("OrganizationID='" + dr["OrganizationID"].ToString().Trim() + "'");
            //                        foreach (DataRow shareRow in myRows)
            //                        {
            //                            //循环列（带B的）
            //                            foreach (string item in arrayFieldsB)
            //                            {
            //                                //将分摊的电量加到产线上
            //                                row[item] =MyToDecimal(row[item])+MyToDecimal(shareRow[item]);
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            //将数据放到result表中（result表结构和balance_Energy表的结构一致）
            //            foreach (DataRow dr in source.Rows)
            //            {
            //                DataRow row = result.NewRow();
            //                row["VariableItemId"] = Guid.NewGuid().ToString();
            //                row["PublicVariableId"] = row["KeyId"] = singleBasicData.MonthlyKeyId;
            //                foreach (DataColumn dc in source.Columns)
            //                {
            //                    string dcName = dc.ColumnName;
            //                    row[dcName] = dr[dcName];
            //                }
            //                result.Rows.Add(row);
            //            }
            //            //将每天产线分摊后的电量作为一个工序插到result表中
            //            foreach (DataRow dr in share.Rows)
            //            {
            //                DataRow row = result.NewRow();
            //                row["VariableItemId"] = Guid.NewGuid().ToString();
            //                row["PublicVariableId"] = row["KeyId"] = singleBasicData.MonthlyKeyId;
            //                row["VariableId"] = dr["VariableId"];
            //                row["VariableName"] = dr["VariableName"];
            //                row["OrganizationID"] = dr["OrganizationID"];
            //                row["ValueType"] = dr["ValueType"];
            //                foreach (string item in arrayFields)
            //                {
            //                    row[item] = dr[item];
            //                }
            //                result.Rows.Add(row);
            //            }
            #endregion
            return result;
        }
        /// <summary>
        /// 计算均摊电量
        /// </summary>
        /// <param name="source"></param>
        /// <param name="template"></param>
        /// <param name="formulaColumn"></param>
        /// <param name="calculateColumns"></param>
        /// <returns></returns>
        private static DataTable CalculateByOrganizationId(DataTable source, DataTable template, string formulaColumn, string[] calculateColumns)
        {

            //构造目的表
            DataTable dataSource = source.Copy();
            foreach (DataRow dr in dataSource.Rows)
            {
                dr["VariableId"] = dr["OrganizationID"].ToString().Trim() + dr["VariableId"].ToString().Trim();
            }
            DataTable result = template.Copy();
            foreach (string item in calculateColumns)
            {
                DataColumn column = new DataColumn(item, dataSource.Columns[item].DataType);
                result.Columns.Add(column);
            }
            //END

            foreach (DataRow dr in result.Rows)
            {
                //公式
                string formula = dr[formulaColumn].ToString().Trim();
                if (formula == "")
                    continue;
                //拆分公式 variableList中存储各个因式
                IEnumerable<string> factorList = Regex.Split(formula, @"[+\-*/()]+")
                                                .Except((IEnumerable<string>)new string[] { "" })
                                                .Select(p => p = Regex.Replace(p, @"^([0-9]+)([\.]([0-9]+))?$", ""))
                                                .Except((IEnumerable<string>)new string[] { "" });
                //m_dictionary存储各个因式及其对应的行记录
                IDictionary<string, DataRow> m_dictionary = new Dictionary<string, DataRow>();
                foreach (string item in factorList)
                {
                    //因式(factor)
                    if (m_dictionary.Keys.Contains(item))
                        continue;
                    else
                    {
                        string factor = item.TrimStart('[').TrimEnd(']');
                        DataRow[] rows = dataSource.Select("VariableId='" + factor + "'");
                        if (1 == rows.Count())
                        {
                            m_dictionary.Add(factor, rows[0]);
                        }
                        else if (0 == rows.Count())
                        {
                            m_dictionary.Add(factor, dataSource.NewRow());//没有找到则添加一个空行
                        }
                        else
                        {
                            throw new Exception(item + "对应" + rows.Count() + "条数据！");
                        }
                    }
                }

                ///item当前要计算的列名
                foreach (string item in calculateColumns)
                {
                    try
                    {
                        string tempFormulaValue = formula;
                        foreach (string node in m_dictionary.Keys.ToArray())
                        {
                            string test = m_dictionary[node][item].ToString();
                            tempFormulaValue = tempFormulaValue.Replace("[" + node + "]", m_dictionary[node][item].ToString());
                        }
                        dr[item] = Convert.ToDecimal(dataSource.Compute(tempFormulaValue, "true"));
                    }
                    catch { dr[item] = 0; };
                }
            }
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
