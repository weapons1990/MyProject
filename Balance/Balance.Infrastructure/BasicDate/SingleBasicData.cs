using Balance.Infrastructure.Configuration;
using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balance.Infrastructure.BasicDate
{
    public class SingleBasicData
    {
        //私有构造方法
        private SingleBasicData() { }      

        //********字段*******
        private static SingleBasicData singleBasicData;
        private static readonly object syncObject = new object();//为多线程准备
        private string date;
        private DateTime monthDate;
        private string organizationId;
        private string ammeterName;
        private string keyId;
        private string monthKeyId;
        private static DataTable balanceTable;
        private static  DataTable tzBalance;
        private static string path;
        //*******************

     //*******属性*********       
        /// <summary>
        /// 要插入数据的日期
        /// </summary>
        public string Date
        {
            get { return date; }
        }
        public DateTime MonthlyDate
        {
            get { return monthDate; }
        }
        /// <summary>
        /// 当前分厂的OrganizationID
        /// </summary>
        public string OrganizationId
        {
            get { return organizationId; }
        }
        /// <summary>
        /// 电表数据库名
        /// </summary>
        public string AmmeterName
        {
            get { return ammeterName; }
        }
        public string KeyId
        {
            get { return keyId; }
        }
        /// <summary>
        /// 插入月数据的时候需要首先初始化
        /// </summary>
        public string MonthlyKeyId
        {
            get { return monthKeyId; }
            //set { monthKeyId = value; }
        }
        /// <summary>
        /// balance_Energy表结构
        /// </summary>
        public DataTable BalanceTable
        {
            get { return balanceTable; }
        }
        /// <summary>
        /// tz_Balance表结构
        /// </summary>
        public DataTable TzBalance
        {
            get { return tzBalance; }
        }
        /// <summary>
        /// Log.txt文件路径
        /// </summary>
        public string Path
        {
            get { return path; }
            set { path = value; }
        }
     //*******************

        //当前分厂数据库名字
        //产生实例
        public static SingleBasicData Creat()
        {
            if (singleBasicData == null)
            {
                lock (syncObject)//使得多线程使用成为可能
                {
                    if (singleBasicData == null)
                    {
                        singleBasicData=new SingleBasicData();
                        InitOnlyOnce();
                    }
                }
            }
            return singleBasicData;
        }
        /// <summary>
        ///设置属性值
        /// </summary>
        /// <param name="organizationId">分厂级别的组织机构ID</param>
        /// <param name="date"></param>
        public void Init(string organizationId, string date)
        {
            this.organizationId = organizationId;
            this.date = date;
            this.ammeterName=ConnectionStringFactory.GetAmmeterDatabaseName(organizationId);
            this.keyId = Guid.NewGuid().ToString();
        }
        /// <summary>
        /// 初始化统计月数据时使用到的量
        /// </summary>
        /// <param name="date"></param>
        public void InitMonthlyData(DateTime date)
        {
            monthKeyId = Guid.NewGuid().ToString();
            monthDate = date;
        }
        /// <summary>
        /// 设置只需初始化一次的属性
        /// </summary>
        private static void InitOnlyOnce()
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            string sqlTz = "SELECT * FROM tz_Balance WHERE 1=0";
            string sqlBalance = "SELECT * FROM balance_Energy WHERE 1=0";
            tzBalance = dataFactory.Query(sqlTz);
            balanceTable = dataFactory.Query(sqlBalance);
            
        }
    }
}
