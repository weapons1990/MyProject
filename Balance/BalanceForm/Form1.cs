using Balance.Infrastructure.BasicDate;
using Balance.Infrastructure.Configuration;
using Balance.Model;
using Balance.Model.ElectricityQuantity;
using Balance.Model.MaterialChange;
using Balance.Model.MaterialWeight;
using Balance.Model.Monthly;
using Balance.Model.TzBalance;
using Microsoft.Win32;
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

namespace BalanceForm
{
    public partial class Form1 : Form
    {
        bool dailyFlag = false;
        bool monthlyFlag = false;
        string runTime = "03:00";//默认汇总时间为每天3点，具体时间从配置文件中获取 
        public Form1()
        {
            InitializeComponent();
            InitialTray();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RunWhenStartUp();
            string m_config = ConfigService.GetConfig("Display");
            //是否显示按钮和文本控件
            if (m_config == "true")
            {
                button1.Visible = true;
                btnMonth.Visible = true;
                txtDate.Visible = true;
                label1.Visible = true;
                txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                button1.Visible = false;
                btnMonth.Visible = false;
                txtDate.Visible = false;
                label1.Visible = false;
            }
            SingleBasicData singleBasicData = SingleBasicData.Creat();
            singleBasicData.Path = Application.StartupPath + "\\Log.txt";
            runTime= ConfigService.GetConfig("RunTime");
        }
        private void InitialTray()
        {
            //隐藏主窗体  
            this.Hide();

            //托盘图标气泡显示的内容  
            notifyIcon.BalloonTipText = "正在后台运行";
            //托盘图标显示的内容  
            notifyIcon.Text = "能源辅助程序运行中！";
            //注意：下面的路径可以是绝对路径、相对路径。但是需要注意的是：文件必须是一个.ico格式  
            //notifyIcon.Icon = new System.Drawing.Icon(@"C:\Users\Administrator.QH-20140815HAUR\Desktop\备份池\projects\DeliveryDataForms\images\111.ico");  
            //true表示在托盘区可见，false表示在托盘区不可见  
            notifyIcon.Visible = true;
            //气泡显示的时间（单位是毫秒）  
            notifyIcon.ShowBalloonTip(2000);
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);

            ////设置二级菜单  
            //MenuItem setting1 = new MenuItem("二级菜单1");  
            //MenuItem setting2 = new MenuItem("二级菜单2");  
            //MenuItem setting = new MenuItem("一级菜单", new MenuItem[]{setting1,setting2});  

            ////帮助选项，这里只是“有名无实”在菜单上只是显示，单击没有效果，可以参照下面的“退出菜单”实现单击事件  
            //MenuItem help = new MenuItem("帮助");  

            ////关于选项  
            //MenuItem about = new MenuItem("关于");  

            //退出菜单项  
            MenuItem exit = new MenuItem("退出");
            exit.Click += new EventHandler(exit_Click);
            ////关联托盘控件  
            //注释的这一行与下一行的区别就是参数不同，setting这个参数是为了实现二级菜单  
            //MenuItem[] childen = new MenuItem[] { setting, help, about, exit };  
            MenuItem[] childen = new MenuItem[] { exit };
            notifyIcon.ContextMenu = new ContextMenu(childen);
            //窗体关闭时触发  
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        }
        /// <summary>  
        /// 窗体关闭的单击事件  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            //通过这里可以看出，这里的关闭其实不是真正意义上的“关闭”，而是将窗体隐藏，实现一个“伪关闭”  
            this.Hide();
        }
        /// <summary>  
        /// 鼠标单击  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //鼠标左键单击  
            if (e.Button == MouseButtons.Left)
            {
                //如果窗体是可见的，那么鼠标左击托盘区图标后，窗体为不可见  
                if (this.Visible == true)
                {
                    this.Visible = false;
                }
                else
                {
                    this.Visible = true;
                    this.Activate();
                }
            }
        }
        private void exit_Click(object sender, EventArgs e)
        {
            //退出程序  
            System.Environment.Exit(0);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime date = DateTime.Parse(txtDate.Text.ToString().Trim());
                BalanceService.SetBalance(date);
            }
            catch (Exception msg)
            {
                SingleBasicData singleBasicData = SingleBasicData.Creat();
                StreamWriter sw = File.AppendText(singleBasicData.Path);
                sw.WriteLine("Error:" + DateTime.Now.ToString() + "," + msg.Message + msg.StackTrace);
                sw.Flush();
                sw.Close();
            }
        }
        /// <summary>
        /// 设置开机启动
        /// </summary>
        private void RunWhenStartUp()
        {
            string startupPath = Application.ExecutablePath;
            RegistryKey local = Registry.LocalMachine;
            RegistryKey run = local.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            run.SetValue("BALANCE", startupPath);
            local.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //日数据插入
            try
            {
                DateTime time = DateTime.Parse(DateTime.Now.ToString("HH:mm"));               
                DateTime t1 = DateTime.Parse(runTime);
                DateTime t2 = t1.AddMinutes(5); //DateTime.Parse("00:15");

                if (DateTime.Compare(time, t1) > 0 && DateTime.Compare(time, t2) < 0 && false == dailyFlag)
                {
                    dailyFlag = true;
                    DateTime dateTime = DateTime.Now.AddDays(-1);
                    BalanceService.SetBalance(dateTime);
                }
                if (DateTime.Compare(time, t2) > 0)
                {
                    dailyFlag = false;
                }
            }
            catch (Exception msg)
            {
                SingleBasicData singleBasicData = SingleBasicData.Creat();
                StreamWriter sw = File.AppendText(singleBasicData.Path);
                sw.WriteLine("Error:" + DateTime.Now.ToString() + "," +msg.Message+ msg.StackTrace);
                sw.Flush();
                sw.Close();
            }
            //月数据插入
            try
            {
                DateTime time = DateTime.Parse(DateTime.Now.ToString("HH:mm"));
                int day = time.Day;
                //与插入日数据的时间岔开
                DateTime t1 = DateTime.Parse(runTime).AddMinutes(20);
                DateTime t2 = t1.AddMinutes(10);//DateTime.Parse("00:25");
                //从配置文件中读取每月统计日期
                int myDay = Int16.Parse(ConfigService.GetConfig("StatisticalDay"));

                if (myDay == day && DateTime.Compare(time, t1) > 0 && DateTime.Compare(time, t2) < 0 && false == monthlyFlag)
                {
                    monthlyFlag = true;
                    DateTime dateTime = DateTime.Now;
                    MonthlyBalanceService.SetBalance(dateTime);
                }
                if (DateTime.Compare(time, t2) > 0)
                {
                    monthlyFlag = false;
                }
            }
            catch (Exception msg)
            {
                SingleBasicData singleBasicData = SingleBasicData.Creat();
                //singleBasicData.
                StreamWriter sw = File.AppendText(singleBasicData.Path);
                sw.WriteLine("Error:" + DateTime.Now.ToString() + "," + msg.Message + msg.StackTrace);
                sw.Flush();
                sw.Close();
            }
        }

        private void btnMonth_Click(object sender, EventArgs e)
        {
           // DateTime date = DateTime.Parse("2015-02-01");
            /*
             * test
            SingleBasicData singleBasicData = SingleBasicData.Creat();
            singleBasicData.MonthlyKeyId = Guid.NewGuid().ToString();
            MonthlyService.GetElectricityQuantityMaterialWeight(date);
             * 
             */           
            try
            {
                DateTime date = DateTime.Parse(txtDate.Text.ToString().Trim());
                //date.AddDays(1);
                MonthlyBalanceService.SetBalance(date);
            }
            catch (Exception msg)
            {
                SingleBasicData singleBasicData = SingleBasicData.Creat();
                StreamWriter sw = File.AppendText(singleBasicData.Path);
                sw.WriteLine("Error:" + DateTime.Now.ToString() + ","  +msg.Message+ msg.StackTrace);
                sw.Flush();
                sw.Close();
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            SingleBasicData singleBasicData = SingleBasicData.Creat();
            DateTime date = new DateTime(2015, 02, 02);
            singleBasicData.Init("zc_nxjc_byc_byf","2015-02-02");
            SingleTimeService singleTimeService = SingleTimeService.Creat();
            singleTimeService.Init(dataFactory);
            DataTable table = DailyMaterialChangeSummation.GetMaterialChange();
        }
    }
}
