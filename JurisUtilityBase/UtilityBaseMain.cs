using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Globalization;
using Gizmox.Controls;
using JDataEngine;
using JurisAuthenticator;
using JurisUtilityBase.Properties;
using JurisSVR;
using Gizmox.Data;

namespace JurisUtilityBase
{
    public partial class UtilityBaseMain : Form
    {
        #region Private  members

        private JurisUtility _jurisUtility;

        #endregion

        #region Public properties

        public string CompanyCode { get; set; }

        public string JurisDbName { get; set; }

        public string JBillsDbName { get; set; }

        public string BT = "*";
        public string OT = "*";
        public string Status = "*";
        public string Lock = "*";
        public string NewDR = "";
        List<ErrorLog> errorList = new List<ErrorLog>();
        #endregion

        #region Constructor

        public UtilityBaseMain()
        {
            InitializeComponent();
            _jurisUtility = new JurisUtility();
        }

        #endregion

        #region Public methods

        public void LoadCompanies()
        {
            var companies = _jurisUtility.Companies.Cast<object>().Cast<Instance>().ToList();
//            listBoxCompanies.SelectedIndexChanged -= listBoxCompanies_SelectedIndexChanged;
            listBoxCompanies.ValueMember = "Code";
            listBoxCompanies.DisplayMember = "Key";
            listBoxCompanies.DataSource = companies;
//            listBoxCompanies.SelectedIndexChanged += listBoxCompanies_SelectedIndexChanged;
            var defaultCompany = companies.FirstOrDefault(c => c.Default == Instance.JurisDefaultCompany.jdcJuris);
            if (companies.Count > 0)
            {
                listBoxCompanies.SelectedItem = defaultCompany ?? companies[0];
            }
        }

        #endregion

        #region MainForm events

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void listBoxCompanies_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_jurisUtility.DbOpen)
            {
                _jurisUtility.CloseDatabase();
            }
            CompanyCode = "Company" + listBoxCompanies.SelectedValue;
            _jurisUtility.SetInstance(CompanyCode);
            JurisDbName = _jurisUtility.Company.DatabaseName;
            JBillsDbName = "JBills" + _jurisUtility.Company.Code;
            _jurisUtility.OpenDatabase();
          


            string TkprIndex2;
            cbStatus.ClearItems();
            string SQLTkpr2 = "select [Status] from (select '*    (All Statuses)' as [Status]  union all select   [Status] + '    ' +  StatusDesc as [Status] from MatterStatusList where [Status] <> 'C') Emp order by [Status]";
            DataSet myRSTkpr2 = _jurisUtility.RecordsetFromSQL(SQLTkpr2);

            if (myRSTkpr2.Tables[0].Rows.Count == 0)
                Status = "";                      
            else
            {
                foreach (DataRow dr in myRSTkpr2.Tables[0].Rows)
                {
                    TkprIndex2 = dr["Status"].ToString();
                    cbStatus.Items.Add(TkprIndex2);
                }
            }

            //set lock items
            cbLock.ClearItems();
            Lock = "";
            cbLock.Items.Add("*    (All Lock Flags)");
            cbLock.Items.Add("0    Allow Time and Expense");
            cbLock.Items.Add("1    Disallow Time, Allow Expense");
            cbLock.Items.Add("2    Allow Time, Disallow Expense");
            cbLock.Items.Add("3    Disallow Time and Expense");

            string PCIndex2;
            cbBT.ClearItems();
            string SQLPC2 = "select empid + case when len(empid)=1 then '     ' when len(empid)=2 then '     ' when len(empid)=3 then '   ' else '  ' end +  empname as employee from employee where empvalidastkpr='Y' order by employee";
            DataSet myRSPC2 = _jurisUtility.RecordsetFromSQL(SQLPC2);

            if (myRSPC2.Tables[0].Rows.Count == 0)
                BT = "";
            else
            {
                cbBT.Items.Add("*    (All Timekeepers)");
                foreach (DataRow dr in myRSPC2.Tables[0].Rows)
                {
                    PCIndex2 = dr["employee"].ToString();
                    cbBT.Items.Add(PCIndex2);
                }
            }



            string TkprIndex;
            cbOT.ClearItems();
            string SQLTkpr = "select empid + case when len(empid)=1 then '     ' when len(empid)=2 then '     ' when len(empid)=3 then '   ' else '  ' end +  empname as employee from employee where empvalidastkpr='Y' order by employee";
            DataSet myRSTkpr = _jurisUtility.RecordsetFromSQL(SQLTkpr);

            if (myRSTkpr.Tables[0].Rows.Count == 0)
                OT = "";
            else
            {
                cbOT.Items.Add("*    (All Timekeepers)");
                foreach (DataRow dr in myRSTkpr.Tables[0].Rows)
                { 
                    TkprIndex = dr["employee"].ToString();
                        cbOT.Items.Add(TkprIndex);
                    
                }

            }

            cbStatus.SelectedIndex =0;
            cbLock.SelectedIndex = 0;
            cbBT.SelectedIndex = 0;
            cbOT.SelectedIndex = 0;
            BT = "*";
            OT = "*";
            Status = "*";
            Lock = "*";
            dtOpen.Value = DateTime.Now;
        }



        #endregion

        #region Private methods

        private void DoDaFix()
        {


            //get list of all matters that match
            string where = getWhereClause();
            List<Matter> matList = new List<Matter>();
            Matter mat = null;
            string sql = "";
            if (string.IsNullOrEmpty(where))
                sql = "select distinct dbo.jfn_FormatClientCode(clicode) as ClientCode, clireportingname as ClientName,  dbo.jfn_FormatMatterCode(MatCode) as MatterCode,  matreportingname as MatterName, matsysnbr from matter inner join client on clisysnbr = matclinbr where MatStatusFlag <> 'C'";
            else
                sql = "select distinct dbo.jfn_FormatClientCode(clicode) as ClientCode, clireportingname as ClientName, dbo.jfn_FormatMatterCode(MatCode) as MatterCode,  matreportingname as MatterName, matsysnbr from matter inner join billto on billtosysnbr = matbillto inner join MatOrigAtty on MOrigMat = matsysnbr inner join client on clisysnbr = matclinbr where MatStatusFlag <> 'C' and " + where;
            DataSet matsys = _jurisUtility.RecordsetFromSQL(sql);

            if (matsys.Tables[0].Rows.Count == 0)
                MessageBox.Show("No matters matched that criteria", "No changes", MessageBoxButtons.OK, MessageBoxIcon.None);
            else
            {
                foreach (DataRow dr in matsys.Tables[0].Rows)
                {
                    mat = new Matter();
                    mat.clicode = dr[0].ToString();
                    mat.matcode = dr[2].ToString();
                    mat.matName = dr[3].ToString();
                    mat.matsys = Convert.ToInt32(dr[4].ToString());
                    mat.getsProcessed = true;
                    matList.Add(mat);
                }

                DialogResult dd = MessageBox.Show("Would you like to see a list of matters that will be included?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dd == DialogResult.Yes)
                {
                    ReportDisplay rd = new ReportDisplay(matsys);
                    rd.ShowDialog();
                }

                DialogResult dd1 = MessageBox.Show("Do you want to close all of the listed matters?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dd1 == DialogResult.Yes)
                {
                    int total = matList.Count;
                    int count = 1;
                    //check each matter for balances

                    foreach (Matter mm in matList)
                    {
                        mm.getsProcessed = checkForBalances(mm.matsys);
                        Cursor.Current = Cursors.WaitCursor;
                        Application.DoEvents();
                        toolStripStatusLabel.Text = "Checking Matters for Balances....";
                        statusStrip.Refresh();
                        UpdateStatus("Checking Matters for Balances....", count, total);
                    }
                    //reset to process and close matters
                    Cursor.Current = Cursors.WaitCursor;
                    Application.DoEvents();
                    toolStripStatusLabel.Text = "Closing Matters....";
                    statusStrip.Refresh();
                    UpdateStatus("Closing Matters....", 0, total);

                    count = 1;
                    foreach (Matter mm in matList)
                    {
                        if (mm.getsProcessed)
                        {
                            closeMatter(mm.matsys);
                            Cursor.Current = Cursors.WaitCursor;
                            Application.DoEvents();
                            toolStripStatusLabel.Text = "Closing Matters....";
                            statusStrip.Refresh();
                            UpdateStatus("Closing Matters....", count, total);
                        }


                    }
                    Cursor.Current = Cursors.WaitCursor;
                    Application.DoEvents();
                    toolStripStatusLabel.Text = "Finished!";
                    statusStrip.Refresh();
                    UpdateStatus("Finished", total, total);

                    if (errorList.Count == 0)
                        MessageBox.Show("The process is complete and there were no errors.", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.None);
                    else
                    {
                        DialogResult ff = MessageBox.Show("The process is complete but there were errors." + "\r\n" + "Would you like to see the Error Log?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (ff == DialogResult.Yes)
                        {
                            ErrorDisplay rd = new ErrorDisplay(errorList);
                            rd.showErrors();
                            rd.Show();
                        }
                    }
                }


            }

        }


        private string getWhereClause()
        {
            //if star...simply exclude from where because its all results anyway
            string where = "";
            if (!Status.Equals("*"))
                where = " MatStatusFlag = '" + Status + "' ";
            if (!Lock.Equals("*"))
            {
                if (string.IsNullOrEmpty(where))
                    where =  " MatLockFlag = " + Lock + " ";
                else
                    where = where + " and MatLockFlag = " + Lock + " ";
            }

            if (checkBoxBT.Checked)
            {
                if (!BT.Equals("*"))
                {
                    if (string.IsNullOrEmpty(where))
                        where = " BillToBillingAtty in (select empsysnbr from employee where empid = '" + BT + "') ";
                    else
                        where = where + " and BillToBillingAtty in (select empsysnbr from employee where empid = '" + BT + "') ";
                }
            }

            if (checkBoxOT.Checked)
            {
                if (!OT.Equals("*"))
                {
                    if (string.IsNullOrEmpty(where))
                        where = " MOrigAtty in (select empsysnbr from employee where empid = '" + OT + "') ";
                    else
                        where = where + " and MOrigAtty in (select empsysnbr from employee where empid = '" + OT + "') ";
                }
            }
            return where;
        }

        private bool checkForBalances(int matsysnbr) //true means some balance exists
        {
            string sql = "";
            sql =
                "  select dbo.jfn_FormatClientCode(clicode) as clicode, dbo.jfn_FormatMatterCode(MatCode) as matcode, cast(sum(ppd) as money) as ppd, cast(sum(UT) as money) as UT, cast(sum(UE) as money) as UE, cast(sum(AR) as money) as AR, cast(sum(Trust) as money) as Trust " +
                "from( " +
                "select matsysnbr as matsys, MatPPDBalance as ppd, 0 as UT, 0 as UE, 0 as AR, 0 as Trust " +
                  " from matter " +
                  " where MatPPDBalance <> 0 and matsysnbr = " + matsysnbr +
                  " union all " +
                   " select utmatter as matsys, 0 as ppd, sum(utamount) as UT, 0 as UE, 0 as AR, 0 as Trust " +
                  " from unbilledtime where utmatter = " + matsysnbr +
                  " group by utmatter " +
                  " having sum(utamount) <> 0 " +
                  " union all " +
                  " select uematter as matsys, 0 as ppd, 0 as UT, sum(ueamount) as UE, 0 as AR, 0 as Trust " +
                 " from unbilledexpense where uematter = " + matsysnbr +
                  "  group by uematter " +
                " having sum(ueamount) <> 0 " +
                 " union all " +
                "  select armmatter as matsys, 0 as ppd, 0 as UT, 0 as UE, sum(ARMBalDue) as AR, 0 as Trust " +
                "  from armatalloc where armmatter = " + matsysnbr +
                "  group by armmatter " +
                "  having sum(ARMBalDue) <> 0 " +
                "  union all " +
                "  select tamatter as matsys, 0 as ppd, 0 as UT, 0 as UE, 0 as AR, sum(TABalance) as Trust " +
                 " from trustaccount where tamatter = " + matsysnbr +
                "  group by tamatter " +
                "  having sum(TABalance) <> 0) hhg " +
                " inner join matter on hhg.matsys = matsysnbr " +
                " inner join client on clisysnbr = matclinbr " +
                "  group by dbo.jfn_FormatClientCode(clicode), dbo.jfn_FormatMatterCode(MatCode) " +
                "  having sum(ppd) <> 0 or sum(UT)  <> 0 or sum(UE)  <> 0 or sum(AR)  <> 0 or sum(Trust) <> 0";

            DataSet ds = new DataSet();
            ds = _jurisUtility.RecordsetFromSQL(sql);
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return true;
            else
            {
                ErrorLog er = new ErrorLog();
                er.client = ds.Tables[0].Rows[0][0].ToString();
                er.matter = ds.Tables[0].Rows[0][1].ToString();
                er.message = "Cannot close matter " + er.client + "/" + er.matter + " because balance(s) exist. See below for more detail: \r\n" +
                    "Prepaid Balance: " + ds.Tables[0].Rows[0][2].ToString() + "\r\n" +
                    "Unbilled Time Balance: " + ds.Tables[0].Rows[0][3].ToString() + "\r\n" +
                    "Unbilled Expense Balance: " + ds.Tables[0].Rows[0][4].ToString() + "\r\n" +
                    "A/R Balance: " + ds.Tables[0].Rows[0][5].ToString() + "\r\n" +
                    "Trust Balance: " + ds.Tables[0].Rows[0][6].ToString() + "\r\n" + "\r\n";
                errorList.Add(er);
                return false;
            }
        }


        private void closeMatter(int matsys)
        {
            string sql = "";
            string dates = "";
            if (checkBoxSetDate.Checked)
                dates = ", MatDateLastWork = '" + NewDR + "', MatDateLastExp = '" + NewDR + "', MatDateLastBill = '" + NewDR + "', MatDateLastPaymt = '" + NewDR + "' ";
            sql = "update matter set MatStatusFlag = 'C', MatLockFlag = 3, MatDateClosed = getdate() " + dates + " where matsysnbr = " + matsys.ToString();
            _jurisUtility.ExecuteNonQuery(0, sql);

        }

        private bool VerifyFirmName()
        {
            //    Dim SQL     As String
            //    Dim rsDB    As ADODB.Recordset
            //
            //    SQL = "SELECT CASE WHEN SpTxtValue LIKE '%firm name%' THEN 'Y' ELSE 'N' END AS Firm FROM SysParam WHERE SpName = 'FirmName'"
            //    Cmd.CommandText = SQL
            //    Set rsDB = Cmd.Execute
            //
            //    If rsDB!Firm = "Y" Then
            return true;
            //    Else
            //        VerifyFirmName = False
            //    End If

        }



        private static bool IsDate(String date)
        {
            try
            {
                DateTime dt = DateTime.Parse(date);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static bool IsNumeric(object Expression)
        {
            double retNum;

            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
            return isNum; 
        }

        private void WriteLog(string comment)
        {
            string sql = "Insert Into UtilityLog(ULTimeStamp,ULWkStaUser,ULComment) Values(convert(datetime,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'),cast('" +  GetComputerAndUser() + "' as varchar(100))," + "cast('" + comment.ToString() + "' as varchar(8000)))";
            _jurisUtility.ExecuteNonQueryCommand(0, sql);
        }

        private string GetComputerAndUser()
        {
            var computerName = Environment.MachineName;
            var windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var userName = (windowsIdentity != null) ? windowsIdentity.Name : "Unknown";
            return computerName + "/" + userName;
        }

        /// <summary>
        /// Update status bar (text to display and step number of total completed)
        /// </summary>
        /// <param name="status">status text to display</param>
        /// <param name="step">steps completed</param>
        /// <param name="steps">total steps to be done</param>
        private void UpdateStatus(string status, long step, long steps)
        {
            labelCurrentStatus.Text = status;

            if (steps == 0)
            {
                progressBar.Value = 0;
                labelPercentComplete.Text = string.Empty;
            }
            else
            {
                double pctLong = Math.Round(((double)step/steps)*100.0);
                int percentage = (int)Math.Round(pctLong, 0);
                if ((percentage < 0) || (percentage > 100))
                {
                    progressBar.Value = 0;
                    labelPercentComplete.Text = string.Empty;
                }
                else
                {
                    progressBar.Value = percentage;
                    labelPercentComplete.Text = string.Format("{0} percent complete", percentage);
                }
            }
        }
        private void DeleteLog()
        {
            string AppDir = Path.GetDirectoryName(Application.ExecutablePath);
            string filePathName = Path.Combine(AppDir, "VoucherImportLog.txt");
            if (File.Exists(filePathName + ".ark5"))
            {
                File.Delete(filePathName + ".ark5");
            }
            if (File.Exists(filePathName + ".ark4"))
            {
                File.Copy(filePathName + ".ark4", filePathName + ".ark5");
                File.Delete(filePathName + ".ark4");
            }
            if (File.Exists(filePathName + ".ark3"))
            {
                File.Copy(filePathName + ".ark3", filePathName + ".ark4");
                File.Delete(filePathName + ".ark3");
            }
            if (File.Exists(filePathName + ".ark2"))
            {
                File.Copy(filePathName + ".ark2", filePathName + ".ark3");
                File.Delete(filePathName + ".ark2");
            }
            if (File.Exists(filePathName + ".ark1"))
            {
                File.Copy(filePathName + ".ark1", filePathName + ".ark2");
                File.Delete(filePathName + ".ark1");
            }
            if (File.Exists(filePathName ))
            {
                File.Copy(filePathName, filePathName + ".ark1");
                File.Delete(filePathName);
            }

        }

            

        private void LogFile(string LogLine)
        {
            string AppDir = Path.GetDirectoryName(Application.ExecutablePath);
            string filePathName = Path.Combine(AppDir, "VoucherImportLog.txt");
            using (StreamWriter sw = File.AppendText(filePathName))
            {
                sw.WriteLine(LogLine);
            }	
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            DoDaFix();
        }

        private void btExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }




        private void cbBT_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbStatus.SelectedIndex > 0)
                Status = this.cbStatus.GetItemText(this.cbStatus.SelectedItem).Split(' ') [0];
        }

        private void cbClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbLock.SelectedIndex > 0)
                Lock = this.cbLock.GetItemText(this.cbLock.SelectedItem).Split(' ')[0];
        }

        private void cbPC_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbBT.SelectedIndex > 0)
                BT = this.cbBT.GetItemText(this.cbBT.SelectedItem).Split(' ')[0];
        }
        private void cbExcBT_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbOT.SelectedIndex > 0)
                OT = this.cbOT.GetItemText(this.cbOT.SelectedItem).Split(' ')[0];
        }



        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
                dtOpen.Visible = checkBoxSetDate.Checked;
            NewDR = dtOpen.Value.Date.ToString("MM/dd/yyyy");

        }



        private void checkBoxBT_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxOT_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void dtOpen_ValueChanged(object sender, EventArgs e)
        {
            NewDR = dtOpen.Value.Date.ToString("MM/dd/yyyy");
        }
    }
}
