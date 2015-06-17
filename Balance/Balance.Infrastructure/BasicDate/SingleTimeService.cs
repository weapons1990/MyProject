using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balance.Infrastructure.BasicDate
{
    /// <summary>
    /// 峰谷平甲乙丙时间维护类（单例模式）
    /// </summary>
    public class SingleTimeService
    {
        //********字段**********
        //单例对象
        private static SingleTimeService singleTimeServer;
        private static readonly object syncObject = new object();//为多线程准备
        private string peakTimeCriterion;
        private string morePeakTimeCriterion;
        private string valleyTimeCriterion;
        private string moreValleyTimeCriterion;
        private string flatTimeCriterion;
        private string firstTimeCriterion;
        private string secondTimeCriterion;
        private string thirdTimeCriterion;
        //***********end****************

        //************属性***********
        /// <summary>
        /// 峰期时间
        /// </summary>
        public string PeakTimeCriterion
        {
            get { return peakTimeCriterion; }
        }
        /// <summary>
        /// 尖峰时间
        /// </summary>
        public string MorePeakTimeCriterion
        {
            get { return morePeakTimeCriterion; }
        }
        /// <summary>
        /// 谷期时间
        /// </summary>
        public string ValleyTimeCriterion
        {
            get { return valleyTimeCriterion; }
        }
        /// <summary>
        /// 深谷时间
        /// </summary>
        public string MoreValleyTimeCriterion
        {
            get { return moreValleyTimeCriterion; }
        }
        /// <summary>
        /// 平期时间
        /// </summary>
        public string FlatTimeCriterion
        {
            get { return flatTimeCriterion; }
        }
        /// <summary>
        /// 甲班时间
        /// </summary>
        public string FirstTimeCriterion
        {
            get { return firstTimeCriterion; }
        }
        /// <summary>
        /// 乙班时间
        /// </summary>
        public string SecondTimeCriterion
        {
            get { return secondTimeCriterion; }
        }
        /// <summary>
        /// 丙班时间
        /// </summary>
        public string ThirdTimeCriterion
        {
            get { return thirdTimeCriterion; }
        }
        private SingleBasicData singleBasicDate = SingleBasicData.Creat();//
        //私有构造方法
        private SingleTimeService()
        {
        }
        /// <summary>
        /// 产生单例对象
        /// </summary>
        public static SingleTimeService Creat()
        {
            if (singleTimeServer == null)
            {
                lock (syncObject)//使得多线程使用成为可能
                {
                    if (singleTimeServer == null)
                    {
                        singleTimeServer = new SingleTimeService();
                    }
                }
            }
            return singleTimeServer;
        }
        /// <summary>
        /// 初始化时间规则
        /// </summary>
        /// <param name="factoryOrganizationId">分厂级别组织机构ID</param>
        public void Init(ISqlServerDataFactory dataFactory)
        {
            InitPVFTimeCriterion(dataFactory);
            InitFSTTimeCriterion(dataFactory);
        }
        /// <summary>
        /// 设置峰谷平时间条件属性
        /// </summary>
        /// <param name="factoryOrganizationId"></param>
        /// <param name="dataFactory"></param>
        private void InitPVFTimeCriterion(ISqlServerDataFactory dataFactory)
        {
            //****如果没有该期段则初始化为“1=0”
            peakTimeCriterion =morePeakTimeCriterion=valleyTimeCriterion=moreValleyTimeCriterion=flatTimeCriterion= "1=0";
            //********

            string sqlStr = @"SELECT b.StartTime,b.EndTime,b.Type
                                FROM system_PVF AS a,system_PVF_Detail as b
                                WHERE a.KeyID=b.KeyID AND
                                a.OrganizationID=@factoryOrganizationId  AND
                                a.Flag='True'";
            SqlParameter parameter = new SqlParameter("factoryOrganizationId", singleBasicDate.OrganizationId);
            //峰谷平源数据
            DataTable pvfTable = dataFactory.Query(sqlStr, parameter);
            if (pvfTable.Rows.Count == 0)
            {
                StreamWriter sw = File.AppendText(singleBasicDate.Path);
                sw.WriteLine("没有定义峰谷平时间段！");
                sw.Flush();
                sw.Close();
            }
            IList<string> timeList = new List<string>();
            foreach (DataRow dr in pvfTable.Rows)
            {
                string mType = dr["Type"].ToString().Trim();
                if (timeList.Contains(mType))
                    continue;//如果已有该类型则不添加
                else
                    timeList.Add(mType);
            }
            foreach (string item in timeList)
            {
                string date = singleBasicDate.Date;
                DataRow[] rows = pvfTable.Select("Type='" + item + "'");
                switch (item)
                {
                    case "峰期":
                        peakTimeCriterion = ConvertToSqlStr(rows, date);
                        break;
                    case "尖峰期":
                        morePeakTimeCriterion = ConvertToSqlStr(rows, date);
                        break;
                    case "谷期":
                        valleyTimeCriterion = ConvertToSqlStr(rows, date);
                        break;
                    case "深谷期":
                        moreValleyTimeCriterion = ConvertToSqlStr(rows, date);
                        break;
                    case "平期":
                        flatTimeCriterion = ConvertToSqlStr(rows, date);
                        break;
                    default:
                        StreamWriter sw = File.AppendText(singleBasicDate.Path);
                            sw.WriteLine("请检查数据库中峰谷平填写名称！");
                            sw.Flush();
                            sw.Close();
                            break;
                }
            }
        }
        /// <summary>
        /// 设置早中晚班时间查询条件
        /// </summary>
        /// <param name="dataFactory"></param>
        private void InitFSTTimeCriterion(ISqlServerDataFactory dataFactory)
        {
            //****如果没有该期段则初始化为“1=0”
            firstTimeCriterion = secondTimeCriterion = thirdTimeCriterion = "1=0";
            //****end***********

            string sqlStr = @"SELECT StartTime,EndTime,Shifts
                                FROM system_ShiftDescription
                                WHERE OrganizationID=@organizationId";
            SqlParameter parameter = new SqlParameter("organizationId", singleBasicDate.OrganizationId);
            DataTable fstTable = dataFactory.Query(sqlStr, parameter);
            if (fstTable.Rows.Count == 0)
            {
                StreamWriter sw = File.AppendText(singleBasicDate.Path);
                sw.WriteLine("没有甲乙丙班时间段！");
                sw.Flush();
                sw.Close();
            }

            IList<string> shiftsList = new List<string>();
            foreach (DataRow dr in fstTable.Rows)
            {
                string shift = dr["Shifts"].ToString().Trim();
                if (shiftsList.Contains(shift))
                    continue;//如果已有该类型则不添加
                else
                    shiftsList.Add(shift);
            }
            foreach (string item in shiftsList)
            {
                string date = singleBasicDate.Date;
                DataRow[] rows = fstTable.Select("Shifts='" + item + "'");
                switch (item)
                {
                    case "甲班":
                        firstTimeCriterion = ConvertToSqlStr(rows, date);
                        break;
                    case "乙班":
                        secondTimeCriterion = ConvertToSqlStr(rows, date);
                        break;
                    case "丙班":
                        thirdTimeCriterion = ConvertToSqlStr(rows, date);
                        break;
                    default:
                        throw new Exception("数据库中甲乙丙班名称填写有误！");
                }
            }

        }
        /// <summary>
        /// 根据时间段转化为相应的时间查找条件
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private string ConvertToSqlStr(DataRow[] rows, string date)
        {
            StringBuilder timeBuilder = new StringBuilder();
            int n = rows.Count();
            //SqlParameter paramater = new SqlParameter("variableName", variableName);
            foreach (DataRow dr in rows)
            {
                string endTime;
                if ("24:00" == dr["EndTime"].ToString().Trim())
                {
                    endTime = "23:59:59";
                }
                else
                {
                    endTime = dr["EndTime"].ToString().Trim() + ":00";
                }
                timeBuilder.Append("vDate>=");
                //timeBuilder.Append("#");
                timeBuilder.Append("'");
                timeBuilder.Append(date + " ");
                timeBuilder.Append(dr["StartTime"].ToString().Trim() + ":00");
                //timeBuilder.Append("#");
                timeBuilder.Append("'");
                timeBuilder.Append(" AND ");
                timeBuilder.Append("vDate<=");
                //timeBuilder.Append("#");
                timeBuilder.Append("'");
                timeBuilder.Append(date + " ");
                timeBuilder.Append(endTime);
                //timeBuilder.Append("#");
                timeBuilder.Append("'");
                timeBuilder.Append(" OR ");
            }
            int m_long = timeBuilder.ToString().Length;
            string timeCriterion = timeBuilder.ToString().Substring(0, m_long - 4);
            return timeCriterion;
        }
    }
}
