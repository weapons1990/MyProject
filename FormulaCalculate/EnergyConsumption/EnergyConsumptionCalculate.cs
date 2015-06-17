using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnergyConsumption
{
    public class EnergyConsumptionCalculate
    {
        public static DataTable Calculate(DataTable source, DataTable template, string formulaColumn, string[] calculateColumns)
        {
            //构造目的表
            DataTable result = template.Copy();
            foreach (string item in calculateColumns)
            {
                DataColumn column = new DataColumn(item, source.Columns[item].DataType);
                result.Columns.Add(column);
            }
            //END

            foreach (DataRow dr in result.Rows)
            {
                //公式
                string formula = dr[formulaColumn].ToString().Trim();
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
                    string factor = item.TrimStart('[').TrimEnd(']');
                    DataRow[] rows = source.Select("VariableId='" + factor + "'");
                    if (1 == rows.Count())
                    {
                        m_dictionary.Add(factor, rows[0]);
                    }
                    else if (0 == rows.Count())
                    {
                        m_dictionary.Add(factor, source.NewRow());//没有找到则添加一个空行
                    }
                    else
                    {
                        throw new Exception(item + "对应" + rows.Count() + "条数据！");
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
                            tempFormulaValue = tempFormulaValue.Replace("[" + node + "]", m_dictionary[node][item].ToString());
                        }
                        dr[item] = Convert.ToDecimal(source.Compute(tempFormulaValue, "true"));
                    }
                    catch { dr[item] = 0; };
                }
            }
            return result;
        }
        /// <summary>
        /// 可以一次计算多条产线
        /// </summary>
        /// <param name="source">数据源（必须带有OrganizationID列）</param>
        /// <param name="template">模板表（必须带有OrganizationID列）</param>
        /// <param name="formulaColumn">公式所在字段名称</param>
        /// <param name="calculateColumns"></param>
        /// <returns>返回表的行数和template表一致，在template表后面增加了calculateColumns列</returns>
        public static DataTable CalculateByOrganizationId(DataTable source, DataTable template, string formulaColumn, string[] calculateColumns)
        {
            //构造目的表
            DataTable result = template.Copy();
            foreach (string item in calculateColumns)
            {
                DataColumn column = new DataColumn(item, source.Columns[item].DataType);
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
                        DataRow[] rows = source.Select("VariableId='" + factor + "' AND OrganizationID='" + dr["OrganizationID"].ToString().Trim() + "'");
                        if (1 == rows.Count())
                        {
                            m_dictionary.Add(factor, rows[0]);
                        }
                        else if (0 == rows.Count())
                        {
                            m_dictionary.Add(factor, source.NewRow());//没有找到则添加一个空行
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
                            string value=m_dictionary[node][item].ToString()==""?"0":m_dictionary[node][item].ToString();
                            tempFormulaValue = tempFormulaValue.Replace("[" + node + "]",value );
                        }
                        if (tempFormulaValue.Contains('/')) //判断是不是有分子
                        {
                            int index = tempFormulaValue.IndexOf('/');
                            string denominatorFormula = tempFormulaValue.Substring(index+1);
                            if (Convert.ToDecimal(source.Compute(denominatorFormula, "true")) == 0)
                            {
                                dr[item] = 0;
                            }
                            else
                            {
                                dr[item] = Convert.ToDecimal(source.Compute(tempFormulaValue, "true"));
                            }

                        }
                        else
                        {
                            dr[item] = Convert.ToDecimal(source.Compute(tempFormulaValue, "true"));
                        }

                    }
                    catch { dr[item] = 0; };
                }
            }
            return result;
        }
    }
}
