using Balance.Infrastructure.BasicDate;
using Balance.Infrastructure.Configuration;
using Balance.Model.Monthly;
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
    public class MonthlyBalanceService
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
                singleBasicData.InitMonthlyData(date);
                SingleTimeService singleTimeService = SingleTimeService.Creat();
                singleTimeService.Init(dataFactory);
                DataTable tzBalance = TzBalanceService.GetMonthyTzBalance();
                //电量（包括分摊电量，产线级别的电量带B的字段为加上分摊电量后的值）产量表
                DataTable electricityMaterialWeight = MonthlyService.GetElectricityQuantityMaterialWeight();

                string sql = @"SELECT A.VariableId,B.OrganizationID,(B.Name+A.VariableName) AS Name,A.ValueType,A.ValueFormula
                                    FROM balance_Energy_Template AS A,system_Organization AS B
                                    WHERE 
                                    A.ProductionLineType=B.Type 
                                    AND (A.ValueType='ElectricityConsumption' 
                                    OR A.ValueType='CoalConsumption')
                                    AND A.Enabled='True'
                                    AND B.LevelCode like(select LevelCode from system_Organization where OrganizationID='zc_nxjc_byc_byf')+'%'";
                DataTable template = dataFactory.Query(string.Format(sql, singleBasicData.OrganizationId));
                string[] columns ={"TotalPeakValleyFlat", "MorePeak", "Peak", "Valley", "MoreValley", "Flat", "First", "Second", "Third", "TotalPeakValleyFlatB", "MorePeakB", 
                "PeakB", "ValleyB", "MoreValleyB", "FlatB", "FirstB", "SecondB", "ThirdB"};
                DataTable consumptionTemp = EnergyConsumption.EnergyConsumptionCalculate.CalculateByOrganizationId(electricityMaterialWeight, template, "ValueFormula", columns);
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
                        row["PublicVariableId"] = row["KeyId"] = singleBasicData.MonthlyKeyId;
                    }
                    consumption.Rows.Add(row);
                }
                electricityMaterialWeight.Merge(consumption);
                int w = dataFactory.Save("tz_Balance", tzBalance);
                int n = dataFactory.Save("balance_Energy", electricityMaterialWeight);
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
