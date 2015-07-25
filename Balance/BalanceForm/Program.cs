using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BalanceForm
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            //只允许启动一个实例
            string m_MainModuleName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
            string m_FilePathByName = System.IO.Path.GetFileNameWithoutExtension(m_MainModuleName);
            System.Diagnostics.Process[] m_CurrentProcesses = System.Diagnostics.Process.GetProcessesByName(m_FilePathByName);
            if (m_CurrentProcesses.Length > 1)
            {
                MessageBox.Show("已有一个正在运行的程序!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }
    }
}
