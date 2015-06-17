using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string Path=Application.StartupPath + "\\Log.txt";
            //连接字符串
            string standard = txtStandardDB.Text.Trim();
            string test = txtTestDB.Text.Trim();
            ISqlServerDataFactory standardFactory = new SqlServerDataFactory(standard);
            ISqlServerDataFactory testFactory = new SqlServerDataFactory(test);
            string standardSQL = @"select   name   from   sysobjects   where   type='U'";
            string testSQL = @"select   name   from   sysobjects   where   type='U'";
            //查出数据库中的所有表
            DataTable standardDB = standardFactory.Query(standardSQL);
            DataTable testDB = testFactory.Query(testSQL);
            //----------------------------
            //写入测试基本数据
            StreamWriter m_sw = File.AppendText(Path);
            m_sw.WriteLine("-------------start---------------");
            m_sw.WriteLine("*****************");
            m_sw.WriteLine("测试开始，测试时间：" + DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"));
            m_sw.WriteLine("标准数据库中表个数为：" + standardDB.Rows.Count);
            m_sw.WriteLine("测试数据库中表个数为：" + testDB.Rows.Count);
            m_sw.WriteLine("*****************");
            m_sw.Flush();
            m_sw.Close();
            //----------------------------
            //遍历数据库中的表
            foreach (DataRow dr in standardDB.Rows)
            {
                string tableName=dr["name"].ToString();
                DataRow[] m_tableName = testDB.Select("name='" + tableName + "'");
                //判断测试数据库中是否有该表
                //若没有则写入文件，跳过此次循环
                //若有则继续
                if (m_tableName.Count() == 0)
                {
                    StreamWriter sw = File.AppendText(Path);
                    sw.WriteLine("测试数据库中缺少表："+tableName);
                    sw.Flush();
                    sw.Close();
                    continue;
                }
                string mySQL = @"sp_MShelpcolumns {0}";
                DataTable standardTable = standardFactory.Query(string.Format(mySQL, tableName));
                DataTable testTable = testFactory.Query(string.Format(mySQL,tableName));
                //字段个数
                int standardColumnCount = standardTable.Rows.Count;
                int testColumnCount = testTable.Rows.Count;
                //判断表字段个数是否相等
                if (standardColumnCount != testColumnCount)
                {
                    StreamWriter sw = File.AppendText(Path);
                    sw.WriteLine("标准数据库和测试数据库中表"+tableName+"中字段个数不相等，分别为："+standardColumnCount+"个、"+testColumnCount+"个。");
                    sw.Flush();
                    sw.Close();
                    continue;
                }
                //遍历列
                for (int i = 0; i < standardColumnCount;i++ )
                {
                    //判断字段名
                    if (standardTable.Rows[i]["col_name"].ToString() != testTable.Rows[i]["col_name"].ToString())
                    {
                        StreamWriter sw = File.AppendText(Path);
                        sw.WriteLine("标准数据库和测试数据库中表" + tableName + "中第" + i + "个字段名不相等，分别为：" + standardTable.Rows[i]["col_name"] + "、" + testTable.Rows[i]["col_name"] + "。");
                        sw.Flush();
                        sw.Close();
                    }
                    //判断数据类型
                    if (standardTable.Rows[i]["col_typename"].ToString() != testTable.Rows[i]["col_typename"].ToString())
                    {
                        StreamWriter sw = File.AppendText(Path);
                        sw.WriteLine("标准数据库和测试数据库中表" + tableName + "中第" + i + "个字段类型不相等，分别为：" + standardTable.Rows[i]["col_typename"] + "、" + testTable.Rows[i]["col_typename"] + "。");
                        sw.Flush();
                        sw.Close();
                    }
                    //判断数据长度
                    if (standardTable.Rows[i]["col_len"].ToString() != testTable.Rows[i]["col_len"].ToString())
                    {
                        StreamWriter sw = File.AppendText(Path);
                        sw.WriteLine("标准数据库和测试数据库中表" + tableName + "中第" + i + "个字段类型长度不相等，分别为：" + standardTable.Rows[i]["col_len"] + "、" + testTable.Rows[i]["col_len"] + "。");
                        sw.Flush();
                        sw.Close();
                    }
                    //判断字段是否可以为空
                    if (standardTable.Rows[i]["col_null"].ToString() != testTable.Rows[i]["col_null"].ToString())
                    {
                        StreamWriter sw = File.AppendText(Path);
                        sw.WriteLine("标准数据库和测试数据库中表" + tableName + "中第" + i + "个字段是否可以为空的设置不相等，分别为：" + standardTable.Rows[i]["col_null"] + "、" + testTable.Rows[i]["col_null"] + "。");
                        sw.Flush();
                        sw.Close();
                    }
                }               
            }
            StreamWriter m_sw1 = File.AppendText(Path);
            m_sw1.WriteLine("-------------end---------------");
            m_sw1.Flush();
            m_sw1.Close();
        }
    }
}
