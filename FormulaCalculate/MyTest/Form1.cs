using SqlServerDataAdapter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //string connectionString = "Data Source=192.168.186.48;Initial Catalog=NXJC;User ID=sa;Password=111";
            string connectionString = "Data Source=CORPHISH;Initial Catalog=NXJC;User ID=sa;Password=cdy";
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            string sqlSource = @"SELECT B.OrganizationID, B.VariableId,B.[First],B.[Second],B.[Third] FROM tz_Balance AS A,balance_Energy AS B
                                    WHERE A.BalanceId=B.KeyId AND
                                    A.TimeStamp='2015-02-11' AND
                                    B.ValueType<>'ElectricityConsumption' 
                                    --AND B.OrganizationID='zc_nxjc_byc_byf_clinker01'";
            string sqlTemplate = @"SELECT A.VariableName AS VariableName,A.ValueFormula AS ValueFormula 
                                    FROM balance_Energy_Template AS A
                                    WHERE A.ValueType='ElectricityConsumption'";
            DataTable source = dataFactory.Query(sqlSource);
            DataTable teplate = dataFactory.Query(sqlTemplate);
            string[] columns={"First","Second"};
           // DataTable result= EnergyConsumption.EnergyConsumptionCalculate.Calculate(source, teplate, "ValueFormula", columns);
            string sqlTemplete02 = @"SELECT A.VariableId,B.OrganizationID,(B.Name+A.VariableName) AS Name,A.ValueType,A.ValueFormula
                                        FROM balance_Energy_Template AS A,system_Organization AS B
                                        WHERE 
                                        A.ProductionLineType=B.Type AND
                                        A.ValueType='ElectricityConsumption' AND
                                        A.Enabled='True'";
            DataTable template02 = dataFactory.Query(sqlTemplete02);
            DataTable result02 = EnergyConsumption.EnergyConsumptionCalculate.CalculateByOrganizationId(source, template02, "ValueFormula", columns);
            string sqlTemplate03 = @"SELECT  SO.Name,SO.LevelCode,DETAIL.* FROM system_Organization AS SO LEFT JOIN (
                                        SELECT A.OrganizationID,A.Name,B.VariableName,D.LevelCode,B.ValueFormula
                                        FROM system_Organization AS A,balance_Energy_Template AS B,tz_Formula AS C,formula_FormulaDetail AS D
                                        WHERE A.Type=B.ProductionLineType
                                        AND A.OrganizationID=C.OrganizationID
                                        AND C.KeyID=D.KeyID
                                        AND D.VariableId+'_ElectricityConsumption'=B.VariableId
                                        AND A.Type<>'余热发电'
                                        AND C.Type=2
                                        AND C.ENABLE='True'
                                        AND C.State=0
                                        ) AS DETAIL
                                        ON SO.OrganizationID=DETAIL.OrganizationID
                                        WHERE SO.LevelCode LIKE 'O03%'
                                        AND ISNULL(SO.Type,'')<>'余热发电'
                                        ORDER BY SO.LevelCode,DETAIL.LevelCode";
            DataTable template03 = dataFactory.Query(sqlTemplate03);
            DataTable result03 = EnergyConsumption.EnergyConsumptionCalculate.CalculateByOrganizationId(source, template03, "ValueFormula", columns);
        }
    }
}
