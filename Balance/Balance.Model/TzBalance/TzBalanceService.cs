using Balance.Infrastructure.BasicDate;
using Balance.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balance.Model.TzBalance
{
    public class TzBalanceService
    {
        /// <summary>
        /// 维护日tz_Balance
        /// </summary>
        public static DataTable GetDailyTzBalance()
        {
            SingleBasicData singleBasicData = SingleBasicData.Creat();
            DataTable result = singleBasicData.TzBalance.Clone();
            DataRow tzRow = result.NewRow();
            tzRow["BalanceId"] = singleBasicData.KeyId;
            tzRow["BalanceName"] = "自动平衡";
            tzRow["OrganizationID"] = singleBasicData.OrganizationId;
            tzRow["DataTableName"] = "";
            tzRow["StaticsCycle"] = "day";
            tzRow["TimeStamp"] = singleBasicData.Date;
            tzRow["BalanceStatus"] = 1;
            tzRow=ShiftAndWorkingTeamService.AddRelationshipToTzbalance(singleBasicData.Date,singleBasicData.OrganizationId,ConnectionStringFactory.NXJCConnectionString,tzRow);
            result.Rows.Add(tzRow);
            return result;
        }
        /// <summary>
        /// 维护月tz_Balance
        /// </summary>
        public static DataTable GetMonthyTzBalance()
        {
            SingleBasicData singleBasicData = SingleBasicData.Creat();
            DataTable result = singleBasicData.TzBalance.Clone();
            DataRow tzRow = result.NewRow();
            tzRow["BalanceId"] = singleBasicData.MonthlyKeyId;
            tzRow["BalanceName"] = "自动平衡";
            tzRow["OrganizationID"] = singleBasicData.OrganizationId;
            tzRow["DataTableName"] = "";
            tzRow["StaticsCycle"] = "month";
            int statisticalDay = Int16.Parse(ConfigService.GetConfig("StatisticalDay"));
            if (statisticalDay > 15)
            {
                tzRow["TimeStamp"] = singleBasicData.MonthlyDate.ToString("yyyy-MM");
            }
            else
            {
                tzRow["TimeStamp"] = singleBasicData.MonthlyDate.AddMonths(-1).ToString("yyyy-MM");
            }
            tzRow["BalanceStatus"] = 1;
            //tzRow = ShiftAndWorkingTeamService.AddRelationshipToTzbalance(singleBasicData.Date, singleBasicData.OrganizationId, ConnectionStringFactory.NXJCConnectionString, tzRow);
            result.Rows.Add(tzRow);
            return result;
        }
    }
}
