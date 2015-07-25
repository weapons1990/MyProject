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

namespace Balance.Model.MaterialChange
{
    public class DailyMaterialChangeSummation
    {
        public static DataTable GetMaterialChange()
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);

            SingleBasicData singleBasicData = SingleBasicData.Creat();
            SingleTimeService singleTimeService = SingleTimeService.Creat();
            DataTable result = singleBasicData.BalanceTable.Clone();
            //取出1、开始时间或结束时间中至少有一个是今天的，2、取出开始时间小于今天并且结束时间为null的事件,3、取出开始时间小于等于今天并且结束时间大于等于今天的事件
            string sql = @"SELECT C.MaterialValue,C.VariableType,C.VariableId,A.OrganizationID,B.Formula,C.ChangeStartTime,C.ChangeEndTime
                                FROM tz_Material AS A,material_MaterialDetail AS B,material_MaterialChangeLog AS C,system_Organization D
                                WHERE 
                                A.KeyID=B.KeyID
                                AND A.OrganizationID=C.OrganizationID
                                AND B.VariableId=C.VariableId
                                AND C.OrganizationID=D.OrganizationID
								AND A.OrganizationID=D.OrganizationID
                                AND C.VariableType='Cement'
                                AND C.ValueType='Discrete'
                                AND D.LevelCode LIKE (select LevelCode from system_Organization where OrganizationID=@organizationId)+'%'
                                AND (
                                (CONVERT(VARCHAR(10),C.ChangeStartTime,20)=@date OR CONVERT(VARCHAR(10),C.ChangeEndTime,20)=@date) 
                                OR (CONVERT(VARCHAR(10),C.ChangeStartTime,20)<=@date AND C.ChangeEndTime is null)
                                OR (CONVERT(VARCHAR(10),C.ChangeStartTime,20)<=@date AND CONVERT(VARCHAR(10),C.ChangeEndTime,20)>=@date))
                                ORDER BY C.ChangeStartTime";
            SqlParameter[] parameters = { new SqlParameter("date", singleBasicData.Date), 
                                            new SqlParameter("organizationId", singleBasicData.OrganizationId) };
            DataTable infoTable = dataFactory.Query(sql, parameters);
            int n=infoTable.Rows.Count;
            //没有记录则直接返回
            if (n == 0)
                return result;
            //将时间都改为今天的时间
            for (int i = 0; i < n; i++)
            {
                DataRow dr = infoTable.Rows[i];
                //string startTime=datat dr["ChangeStartTime"].ToString()
                if (dr["ChangeStartTime"] is DBNull || dr["ChangeStartTime"].ToString() == "")
                {
                    //开始时间若为空说明本条数据为无效数据，直接扔掉
                    infoTable.Rows.RemoveAt(i);
                }
                else
                {
                    //考虑结束时间为null的情况（此情况开始时间小于等于今天的日期）
                    if (dr["ChangeEndTime"] is DBNull)
                    {
                        dr["ChangeEndTime"] = singleBasicData.Date + " 23:59:59.000";
                    }
                    //考虑开始时间小于今天日期的情况（此情况结束时间要么是今天或第二天的一个时刻要么是null）
                    if (DateTime.Parse(dr["ChangeStartTime"].ToString())<=DateTime.Parse(singleBasicData.Date+" 00:00:00.000"))
                    {
                        dr["ChangeStartTime"] = singleBasicData.Date + " 00:00:00.000";
                    }
                    //考虑到本软件的汇总时间为第二天，所以要去掉第二天的数据（此情况的开始时间必定小于或者等于今天的日期）
                    if (DateTime.Parse(dr["ChangeEndTime"].ToString()) >= DateTime.Parse(singleBasicData.Date + " 23:59:59.000"))
                    {
                        dr["ChangeEndTime"] = singleBasicData.Date + " 23:59:59.000";
                    }
                }
            }
            //if (infoTable.Rows[n - 1]["ChangeEndTime"] is DBNull)
            //{
            //    infoTable.Rows[n - 1]["ChangeEndTime"] = singleBasicData.Date + " 23:59:59.000";
            //}
            //存储所有物料信息
            Dictionary<string, MaterialInfo> myDictionary = new Dictionary<string, MaterialInfo>();
            foreach (DataRow dr in infoTable.Rows)
            {
                string key = dr["MaterialValue"].ToString().Trim() + dr["OrganizationID"].ToString().Trim(); 
                if (myDictionary.Keys.Contains(key))
                {
                    //string key = dr["MaterialValue"].ToString().Trim();
                    //MaterialInfo myMaterialInfo=myDictionary[key];
                    myDictionary[key].timeCriteria.Append("vDate>='");
                    myDictionary[key].timeCriteria.Append(dr["ChangeStartTime"].ToString().Trim());
                    myDictionary[key].timeCriteria.Append("' AND vDate<='");
                    myDictionary[key].timeCriteria.Append(dr["ChangeEndTime"].ToString().Trim());
                    myDictionary[key].timeCriteria.Append("' OR ");
                }
                else
                {
                    MaterialInfo minfo = new MaterialInfo();
                    minfo.name = dr["MaterialValue"].ToString().Trim();//水泥品种
                    minfo.variableId = dr["VariableId"].ToString().Trim(); 
                    minfo.VariableType=dr["VariableType"].ToString().Trim();
                    minfo.organizationId = dr["OrganizationID"].ToString().Trim();
                    minfo.formula = dr["Formula"].ToString().Trim();
                    minfo.timeCriteria = new StringBuilder();
                    minfo.timeCriteria.Append("vDate>='");
                    minfo.timeCriteria.Append(dr["ChangeStartTime"].ToString().Trim());
                    minfo.timeCriteria.Append("' AND vDate<='");
                    minfo.timeCriteria.Append(dr["ChangeEndTime"].ToString().Trim());
                    minfo.timeCriteria.Append("' OR ");
                    myDictionary.Add(key, minfo);
                }
            }

            string mySql = @"SELECT SUM({0}) AS Value
                                FROM [{1}].[dbo].[HistoryDCSIncrement]
                                WHERE 
                                ({2}) --峰谷平或甲乙丙的时间
                                AND 
                                ({3}) --该物料生产的时间
                                AND 
                                (vDate>='{4}' AND vDate<='{5}')--统计所在天的时间";

            foreach (string myKey in myDictionary.Keys)
            {
                MaterialInfo materialInfo=myDictionary[myKey];
                materialInfo.timeCriteria.Remove(materialInfo.timeCriteria.Length - 4, 4);
                DataRow row = result.NewRow();
                
                string[] arrayFields = { "TotalPeakValleyFlat", "MorePeak", "Peak", "Valley", "MoreValley", "Flat", "First", "Second", "Third" ,
                                   "TotalPeakValleyFlatB", "MorePeakB", "PeakB", "ValleyB", "MoreValleyB", "FlatB", "FirstB", "SecondB", "ThirdB"};
                //初始化为0
                foreach (string item in arrayFields)
                {
                    row[item] = 0;
                }
                row["VariableItemId"] = Guid.NewGuid().ToString();
                row["VariableId"] = materialInfo.name;
                row["VariableName"] = materialInfo.name;//"水泥分品种";
                row["PublicVariableId"] = row["KeyId"] = singleBasicData.KeyId;
                row["OrganizationID"] = materialInfo.organizationId;
                row["ValueType"] = "ChangeLog" + materialInfo.VariableType;

                //******写入数据
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("Peak", singleTimeService.PeakTimeCriterion);
                dictionary.Add("MorePeak", singleTimeService.MorePeakTimeCriterion);
                dictionary.Add("Valley", singleTimeService.ValleyTimeCriterion);
                dictionary.Add("MoreValley", singleTimeService.MoreValleyTimeCriterion);
                dictionary.Add("Flat", singleTimeService.FlatTimeCriterion);

                dictionary.Add("First", singleTimeService.FirstTimeCriterion);
                dictionary.Add("Second", singleTimeService.SecondTimeCriterion);
                dictionary.Add("Third", singleTimeService.ThirdTimeCriterion);
                foreach (string item in dictionary.Keys.ToArray())
                {
                    string timeCriterion = dictionary[item];
                    if (timeCriterion != "1=0")
                    {
                        string test = string.Format(mySql, materialInfo.formula, singleBasicData.AmmeterName, timeCriterion,
                            materialInfo.timeCriteria.ToString(), singleBasicData.Date + " 00:00:00.000", singleBasicData.Date + " 23:59:59.000");
                        DataTable table = dataFactory.Query(string.Format(mySql, materialInfo.formula, singleBasicData.AmmeterName, timeCriterion,
                            materialInfo.timeCriteria.ToString(), singleBasicData.Date + " 00:00:00.000", singleBasicData.Date + " 23:59:59.000"));
                        if (table.Rows.Count == 0)
                            row[item] = 0;
                        else
                            row[item] = table.Rows[0]["Value"] is DBNull?0:table.Rows[0]["Value"];
                    }
                }
                //row["TotalPeakValleyFlat"] = MyToDecimal(row["First"]) + MyToDecimal(row["Second"]) + MyToDecimal(row["Third"]); 
                result.Rows.Add(row);
            }
            foreach (DataRow dr in result.Rows)
            {
                string[] array = { "TotalPeakValleyFlat", "MorePeak", "Peak", "Valley", "MoreValley", "Flat", "First", "Second", "Third" };
                foreach (string field in array)
                {
                    if (field == "TotalPeakValleyFlat")
                    {
                        dr[field] = dr[field + "B"] = MyToDecimal(dr["First"]) + MyToDecimal(dr["Second"]) + MyToDecimal(dr["Third"]);
                    }
                    else
                    {
                        dr[field + "B"] = dr[field];
                    }

                }
            }
            return result;
        }

        /// <summary>
        /// 物料信息
        /// </summary>
        struct MaterialInfo
        {
            public string name;//物料名
            public string variableId;
            public string VariableType;
            public string organizationId;
            public string formula;
            public StringBuilder timeCriteria;
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
