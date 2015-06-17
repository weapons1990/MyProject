using Balance.Infrastructure.BasicDate;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balance.Model.TzBalance
{
    //TODO:类太大了 不利于维护
    public class ShiftAndWorkingTeamService
    {
        private static int ShiftsNum = 3;//时间班数目
        private static int TeamNum = 4;//人员班数目
        //private static ISqlServerDataFactory _dataFactory;
        private static Dictionary<string, int> myDictionaryToNum;
        private static Dictionary<int, string> mydictionaryToTeam;
        private static string NowDayFormula = "({0}+4-{1})%4";
        private static string SpanFormula = "({0}+{1}*" + ShiftsNum + ")%" + TeamNum;
        private static DataTable calculateTable;
        static ShiftAndWorkingTeamService()
        {
            myDictionaryToNum = new Dictionary<string, int>();
            myDictionaryToNum.Add("甲班", 0);
            myDictionaryToNum.Add("乙班", 1);
            myDictionaryToNum.Add("丙班", 2);
            myDictionaryToNum.Add("A班", 0);
            myDictionaryToNum.Add("B班", 1);
            myDictionaryToNum.Add("C班", 2);
            myDictionaryToNum.Add("D班", 3);
            mydictionaryToTeam = new Dictionary<int, string>();
            mydictionaryToTeam.Add(0, "A班");
            mydictionaryToTeam.Add(1, "B班");
            mydictionaryToTeam.Add(2, "C班");
            mydictionaryToTeam.Add(3, "D班");
            calculateTable = new DataTable();
        }
        /// <summary>
        /// 在tz_balance表中添加班组和班次的对应关系
        /// </summary>
        /// <param name="time">时间</param>
        /// <param name="factoryOrgId">分厂组织机构ID</param>
        /// <param name="connectionString">连接字符串</param>
        public static DataRow AddRelationshipToTzbalance(string time, string factoryOrgId, string connectionString,DataRow row)
        {
            ISqlServerDataFactory _dataFactory = new SqlServerDataFactory(connectionString);
            string mySql = @"select A.ShiftDate,A.Shifts,A.WorkingTeam
                                from shift_WorkingTeamShiftLog A
                                where A.OrganizationID=@OrganizationID
                                and CONVERT(varchar(10),A.ShiftDate,20)=@date";
            SqlParameter[] parameters = { new SqlParameter("OrganizationID", factoryOrgId), new SqlParameter("date", time) };
            DataTable shiftTable = _dataFactory.Query(mySql, parameters);
            //如果本天的记录数不为0
            if (shiftTable.Rows.Count > 0)
            {
                return AddRelationshipWithTodyShifLog(shiftTable,time, factoryOrgId, connectionString, row);
            }
            else//本天的记录数为0
            {
                SingleBasicData singleBasicData = SingleBasicData.Creat();
                //ISqlServerDataFactory _dataFactory = new SqlServerDataFactory(connectionString);
                string sqlStringTzBalance = @"SELECT top(1)
                                            SW.OrganizationID as OrganizationID,
                                            SW.ShiftDate AS ShiftDate,
                                            SW.Shifts AS Shifts,
                                            SW.WorkingTeam AS WorkingTeam
                                            FROM shift_WorkingTeamShiftLog AS SW
                                            WHERE SW.OrganizationID=@factoryOrgId
                                            order by SW.ShiftDate desc";
                SqlParameter lastShiftParameter = new SqlParameter("factoryOrgId", factoryOrgId);
                DataTable lastShiftTable = _dataFactory.Query(sqlStringTzBalance, lastShiftParameter);
                try
                {
                    //读取时间班的数量
                    string sqlStringShiftsNum = @"SELECT COUNT(*) AS TeamNum
                                            FROM system_ShiftDescription AS SW
                                            WHERE SW.OrganizationID=@facOrgId";
                    SqlParameter shiftsNumParamaters = new SqlParameter("facOrgId", factoryOrgId);
                    DataTable shiftsNumTabl = _dataFactory.Query(sqlStringShiftsNum, shiftsNumParamaters);
                    //读取人员班的数量
                    string sqlStringTeamNum = @"SELECT COUNT(*) AS TeamNum
                                            FROM system_WorkingTeam AS SW
                                            WHERE SW.OrganizationID=@facOrgId";
                    SqlParameter teamNumParamaters = new SqlParameter("facOrgId", factoryOrgId);
                    DataTable teamNumTabl = _dataFactory.Query(sqlStringTeamNum, teamNumParamaters);
                    //将默认的班组班次数量改为实际值
                    if (shiftsNumTabl.Rows.Count > 0)
                    {
                        if (Convert.ToInt16(shiftsNumTabl.Rows[0][0]) != 0)
                        {
                            ShiftsNum = Convert.ToInt16(shiftsNumTabl.Rows[0][0]);
                        }
                    }
                    if (teamNumTabl.Rows.Count > 0)
                    {
                        if (Convert.ToInt16(teamNumTabl.Rows[0][0]) != 0)
                        {
                            TeamNum = Convert.ToInt16(teamNumTabl.Rows[0][0]);
                        }
                    }
                }
                catch (Exception msg)
                {
                    StreamWriter sw = File.AppendText(singleBasicData.Path);
                    sw.WriteLine("Error:" + DateTime.Now.ToString() + "," + msg.Message);
                    sw.Flush();
                    sw.Close();
                }
                //如果交接班记录表中一条记录都没有则直接返回
                if (lastShiftTable.Rows.Count == 0)
                {
                    return row;
                }
                else
                {
                    DataRow lastShiftRow = lastShiftTable.Rows[0];
                    DateTime lastShiftTime = (DateTime)lastShiftRow["ShiftDate"];
                    DateTime nowTime = DateTime.Parse(time);
                    TimeSpan span = nowTime - lastShiftTime;
                    int spanDays = span.Days;
                    //最近一天记录甲班上班人员班的号
                    int firstNum = FirstWorkingTeamAnalysis(lastShiftRow["Shifts"].ToString().Trim(), lastShiftRow["WorkingTeam"].ToString().Trim());
                    string mySpanFormula = string.Format(SpanFormula, firstNum, spanDays);
                    //日期为time时甲班对应的人员班的编号
                    int nowFirstNum = Convert.ToInt16(calculateTable.Compute(mySpanFormula, "true"));
                    //甲乙丙班对应的人员班
                    string firstWorkingTeam = mydictionaryToTeam[nowFirstNum];
                    string secondWorkingTeam = mydictionaryToTeam[(nowFirstNum + 1) % 4];
                    string thirdWorkingTeam = mydictionaryToTeam[(nowFirstNum + 2) % 4];
                    row["FirstWorkingTeam"] = firstWorkingTeam;
                    row["SecondWorkingTeam"] = secondWorkingTeam;
                    row["ThirdWorkingTeam"] = thirdWorkingTeam;
                    return row;
                }
            }
        }
        //根据最近的一条交接班记录推测该记录所在天甲班（早班）上班的人员班
        private static int FirstWorkingTeamAnalysis(string shift, string team)
        {
            string myFormula = string.Format(NowDayFormula, myDictionaryToNum[team], myDictionaryToNum[shift]);
            object num = calculateTable.Compute(myFormula, "true");
            return Convert.ToInt16(num);
        }

        private static DataRow AddRelationshipWithTodyShifLog(DataTable shiftTable,string time, string factoryOrgId, string connectionString, DataRow row)
        {
            //存储时间班与人员班的班号键值对
            IDictionary<int, int> myDictionary = new Dictionary<int, int>();
            foreach (DataRow dr in shiftTable.Rows)
            {               
                //人员班
                string t_team=dr["WorkingTeam"].ToString().Trim();
                //时间班
                string t_shift = dr["Shifts"].ToString().Trim();
                // 时间班号/人员班号键值对加到字典中
                myDictionary.Add(myDictionaryToNum[t_shift], myDictionaryToNum[t_team]);
            }
            if (myDictionary.Keys.Count == 3)
            {
                return row;
            }
            else
            {
                //当前时间班的班号
                int sNum = myDictionary.Keys.ToArray()[0];
                //当前时间班对应的人员班的班号
                int sTeamNum = myDictionary[sNum];
                //当前班右侧班的班号
                int rightNum = (sNum + 1) %3;
                //当前班左侧班的班号
                int leftNum = (sNum + 3 - 1) % 3;
                if (!myDictionary.Keys.Contains(rightNum))
                {
                    int teamNum = (rightNum - sNum+sTeamNum+4) % 4;
                    myDictionary.Add(rightNum, teamNum);
                }
                if (!myDictionary.Keys.Contains(leftNum))
                {
                    int teamNum = (leftNum - sNum+sTeamNum+4) % 4;
                    myDictionary.Add(leftNum, teamNum);
                }
            }
            foreach (int item in myDictionary.Keys.ToArray())
            {
                switch (item)
                {
                    case 0:
                        row["FirstWorkingTeam"] = mydictionaryToTeam[myDictionary[item]];
                        break;
                    case 1:
                        row["SecondWorkingTeam"] = mydictionaryToTeam[myDictionary[item]];
                        break;
                    case 2:
                        row["ThirdWorkingTeam"] = mydictionaryToTeam[myDictionary[item]];
                        break;
                }
            }
            return row;
        }
    }
}
