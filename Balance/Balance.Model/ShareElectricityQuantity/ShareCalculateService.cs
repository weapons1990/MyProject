using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Balance.Model.ShareElectricityQuantity
{
    public class ShareCalculateService
    {
        /// <summary>
        /// 计算均摊电量
        /// </summary>
        /// <param name="source"></param>
        /// <param name="template"></param>
        /// <param name="formulaColumn"></param>
        /// <param name="calculateColumns"></param>
        /// <returns></returns>
        public static DataTable CalculateByOrganizationId(DataTable source, DataTable template, string formulaColumn, string[] calculateColumns)
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

        public static decimal MyToDecimal(object obj)
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
