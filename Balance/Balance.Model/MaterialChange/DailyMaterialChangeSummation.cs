using Balance.Infrastructure.BasicDate;
using Balance.Infrastructure.Configuration;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
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
            string sql = @"SELECT C.MaterialValue,C.VariableId,A.OrganizationID,B.Formula,C.ChangeStartTime,C.ChangeEndTime
                                FROM tz_Material AS A,material_MaterialDetail AS B,material_MaterialChangeLog AS C
                                WHERE 
                                A.KeyID=B.KeyID
                                AND A.OrganizationID=C.OrganizationID
                                AND B.VariableId=C.VariableId
                                AND C.VariableType='Cement'
                                AND C.ValueType='Discrete'
                                AND 
                                (CONVERT(VARCHAR(10),C.ChangeStartTime,20)='{0}' OR CONVERT(VARCHAR(10),C.ChangeEndTime,20)='{1}')
                                AND A.OrganizationID LIKE '{2}%'
                                ORDER BY C.ChangeStartTime";
            DataTable infoTable = dataFactory.Query(string.Format(sql, singleBasicData.Date, singleBasicData.Date,singleBasicData.OrganizationId));
            int n=infoTable.Rows.Count;
            //本天没有生产则直接返回
            if (n == 0)
                return result;
            if (infoTable.Rows[n - 1]["ChangeEndTime"] is DBNull)
            {
                infoTable.Rows[n - 1]["ChangeEndTime"] = singleBasicData.Date + " 23:59:59.000";
            }
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
