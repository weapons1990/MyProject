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

namespace Balance.Model.Schedule
{
    public class WorkScheduleRule
    {

        public static void AddRelationshipToTzbalance(DataRow tzRow)
        {
            SingleBasicData singleBasicData=SingleBasicData.Creat();
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            string mySql = @"select A.OrganizationID,A.WorkingTeam,A.ShiftDate
                            from system_ShiftArrangement A
                            where A.UpdateDate=(select MAX(UpdateDate) from system_ShiftArrangement)
                            and A.OrganizationID=@organizationId";
            SqlParameter parameter = new SqlParameter("organizationId", singleBasicData.OrganizationId);
            DataTable ruleTable = dataFactory.Query(mySql, parameter);
            string t_ruleAll = ConfigService.GetConfig("ScheduleRule");
            string regexString = @"{0}:\[.*?\]";
            Regex reg = new Regex(string.Format(regexString,singleBasicData.OrganizationId));
            Match match=reg.Match(t_ruleAll);
            string t_match = match.Value;
            string[] ruleArray=new string[]{};
            if (t_match.Contains('['))
            {
                string t_rule=t_match.Substring(t_match.IndexOf('[')).TrimEnd(']');
                ruleArray = t_rule.Split(',','，');
            }
            foreach (DataRow dr in ruleTable.Rows)
            {
                string workingTeam = dr["WorkingTeam"].ToString().Trim();
                DateTime firstworkingDate=DateTime.Parse(dr["ShiftDate"].ToString().Trim());
                string shif = AotuCalculate(ruleArray, firstworkingDate, DateTime.Parse(singleBasicData.Date));
                switch (shif)
                {
                    case "甲班":
                        tzRow["FirstWorkingTeam"] = workingTeam;
                        break;
                    case "乙班":
                        tzRow["SecondWorkingTeam"] = workingTeam;
                        break;
                    case "丙班":
                        tzRow["ThirdWorkingTeam"] = workingTeam;
                        break;
                    default:
                        continue;
                        //break;
                }
            }
        }
        private static string AotuCalculate(string[] ruleArray, DateTime firstworkingDate, DateTime calculateDate)
        {
            TimeSpan timeSpan = calculateDate - firstworkingDate;
            int spanDay = timeSpan.Days;
            int myLength = ruleArray.Length;
            string state = ruleArray[spanDay % myLength];
            return state;
        }
    }
}
