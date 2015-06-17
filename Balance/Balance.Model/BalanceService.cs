using Balance.Infrastructure.BasicDate;
using Balance.Infrastructure.Configuration;
using Balance.Model.ElectricityQuantity;
using Balance.Model.MaterialChange;
using Balance.Model.MaterialWeight;
using Balance.Model.TzBalance;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balance.Model
{
    public class BalanceService
    {
        public static void SetBalance(DateTime date)
        {
            string[] factorys = ConfigService.GetConfig("FactoryID").Split(',');
            foreach (string factory in factorys)
            {
                string connectionString = ConnectionStringFactory.NXJCConnectionString;
                ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
                SingleBasicData singleBasicData = SingleBasicData.Creat();
                singleBasicData.Init(factory, date.ToString("yyyy-MM-dd"));
                SingleTimeService singleTimeService = SingleTimeService.Creat();
                //每天都重新初始化
                singleTimeService.Init(dataFactory);
                DataTable tzBalance = TzBalanceService.GetDailyTzBalance();
                DataTable electricity = DailyElectricityQuantityService.GetElectricQuantity();
                DataTable materialWeight = DailyMaterialWeight.GetDailyMaterialWeight();
                //将电量产量消耗量合成一表
                electricity.Merge(materialWeight);

                string sql = @"SELECT A.VariableId,B.OrganizationID,(B.Name+A.VariableName) AS Name,A.ValueType,A.ValueFormula
                                    FROM balance_Energy_Template AS A,system_Organization AS B
                                    WHERE 
                                    A.ProductionLineType=B.Type 
                                    AND (A.ValueType='ElectricityConsumption'
                                    OR A.ValueType='CoalConsumption')
                                    AND A.Enabled='True'
                                    AND B.OrganizationID like '{0}%'";
                DataTable template = dataFactory.Query(string.Format(sql, singleBasicData.OrganizationId));
                string[] columns ={"TotalPeakValleyFlat", "MorePeak", "Peak", "Valley", "MoreValley", "Flat", "First", "Second", "Third", "TotalPeakValleyFlatB", "MorePeakB", 
                "PeakB", "ValleyB", "MoreValleyB", "FlatB", "FirstB", "SecondB", "ThirdB"};
                DataTable consumptionTemp = EnergyConsumption.EnergyConsumptionCalculate.CalculateByOrganizationId(electricity, template, "ValueFormula", columns);
                DataTable consumption = singleBasicData.BalanceTable.Clone();
                foreach (DataRow dr in consumptionTemp.Rows)
                {
                    DataRow row = consumption.NewRow();
                    row["VariableItemId"] = Guid.NewGuid().ToString();
                    foreach (DataColumn item in consumptionTemp.Columns)
                    {
                        string name = item.ColumnName;
                        if (name == "ValueFormula")
                            continue;
                        if (name == "Name")
                            row["VariableName"] = dr[name];
                        else
                            row[name] = dr[name];
                        row["PublicVariableId"] = row["KeyId"] = singleBasicData.KeyId;
                    }
                    consumption.Rows.Add(row);
                }              

                electricity.Merge(consumption);
                //获取水泥产量
                DataTable cementTable = DailyMaterialChangeSummation.GetMaterialChange();
                electricity.Merge(cementTable);
                int w = dataFactory.Save("tz_Balance", tzBalance);
                int n=dataFactory.Save("balance_Energy", electricity);
                if (w == -1)
                {
                    StreamWriter sw = File.AppendText(singleBasicData.Path);
                    sw.WriteLine("Error:" + DateTime.Now.ToString() + "tz_Balance表数据写入失败！");
                    sw.Flush();
                    sw.Close();
                }
                if (n == -1)
                {
                    StreamWriter sw = File.AppendText(singleBasicData.Path);
                    sw.WriteLine("Error:" + DateTime.Now.ToString() + "balance_Energy表数据写入失败！");
                    sw.Flush();
                    sw.Close();
                }
            }
        }
    }
}
