/*****************************************************************************/
/*    
Source file name (path):  estinc.cs
Program: concep
Version: 4.0
Programmer: tb
Description:  income estimates module of concep
            version 4 adds standardized configuration file for global vars, table names and query content  
 *  *		version 3.5 adds computations for using Series 13 geographies
 *              using _revised names because we've rebenched everything to 2007$.
              THINGS THAT WE FORGET FROM YEAR TO YEAR:
			     parm type definitons
				 all geographies have three parms 1, 2 and 3 are the curve parms 
 *               type 1 = asd
 *               type 2 = nla
 *               type 3 = median
				 type 4 = use base year distribution applied to current hh control
				 type 5 = use sra distribution applied to hh control
				 type 6 = use sra % change applied to base year distribution and current hh control
			     in the income_model_parms tables , parm types 4, 5 and 6 have 0 value for parm
				 
				 the way that we build the next year's income parms includes
				 1:  copy the previous parms 
				 2:  update the medians for parm type 3 from the income_estimates_trends table
				     the trends table gets changed each year as we add a new year's median
					 there is a spreadsheet with the trends that reloads the trends table
					 each year
					 
              
    concep database    
        income_model_parmameters : model asd parm, median and exponent, year = YYYY
        adjustments_income_model : adjustment factors computed from 2010 base year and
                            applied to each estimate year
        popest_mgra : popest data for control totals for hh, MGRAs; year = YYYY
        inc_estimates : household income distribution, CT, SRA, Region; year = YYYY
        income_estimate_mgra: household income distribution, MGRAs; year YYYY
*/
//   Revision History type 1 = asd

//   Date       By   Description
//   ------------------------------------------------------------------
//   04/08/04   tb   added new thread code
//   06/17/04   tb   changes for version 2.5 - estimates with 10 income groups
//   07/16/04   tb   added code for a type 6 parm; 5 was redefined to be use
//                   sra distribution applied to hh control; 6 replaces 5 - use sra %change
//                   applied to ct distribution
//   02/02/05   tb   recode for Version 3.0 SGRAs
//   04/02/12   tb   eliminated nominal $ calculations
//   10/24/12   tb   changes for Version 4
//   ------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Configuration;


namespace estinc
{
    delegate void WriteDelegate(string status);
    public class estinc : System.Windows.Forms.Form
    {
        #region Fields

        public class TableNames
        {
            public string incomeAdj;
            public string incomeEstimates;
            public string incomeEstimatesMGRA;
            public string incomeParms;
            public string popestMGRA;
            public string xref;

        }     //end class TableNames

        public Configuration config;
        public KeyValueConfigurationCollection appSettings;
        public ConnectionStringSettingsCollection connectionStrings;

        public TableNames TN = new TableNames();
        public string networkPath;

        public int NUM_MGRAS;
        public int MAX_CITY;
        public int MAX_CTS_IN_SRA;				/* max number of CTs in any SRA */
        public int NUM_INCOME_GROUPS;	/* number of income groups */
        public int NUM_CTS;				/* number of actual CTs */
        public int NUM_SRA;					/* number of actual SRAs */
        public int NUM_MSA;					/* number of MSAs */
        public int MAX_MGRAS_IN_CTS;		 /* max mgras in any ct */

        private int eyear;
        private int lyear;

        /* sra list */
        public static int[] sra_list = new int[]{1,2,3,4,5,6,10,11,12,13,14,15,16,17,20,21,22,
                                               30,31,32,33,34,35,36,37,38,39,40,41,42,43,50,
                                               51,52,53,54,55,60,61,62,63};
        public static double[,] sra_inc_hh_pct_chg;

        public double[,] sra_inc_hh_pct;
        public static int[] sra_median;
        public int regional_hh_tot;       //regional total households

        public int[] regional_inc_hh;

        public int[,] sra_base_inc_hh;
        public int[,] sra_inc_hh;

        public double[,] sra_inc_hh_pct_diff;
        public double[,] sra_hh_pct_chg;

        public System.Data.SqlClient.SqlConnection sqlConnection;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Label label2;
        public System.Data.SqlClient.SqlCommand sqlCommand;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRunESTINC;
        private System.Windows.Forms.ComboBox txtYear;
        private IContainer components;
        #endregion Fields

        public estinc()
        {
            InitializeComponent();
            string[] years = global::concep.Properties.Settings.Default.estimatesYearComboItems.Split(new char[] { ',' });
            List<object> yearsInt = new List<object>();
            foreach (string year in years)
            {
                yearsInt.Add(Int32.Parse(year));
            }  // end foreach
            txtYear.Items.AddRange(yearsInt.ToArray());

        }  // end estinc()

        //************************************************************************

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }  // end if
            }  // end if
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.sqlConnection = new System.Data.SqlClient.SqlConnection();
            this.label4 = new System.Windows.Forms.Label();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.sqlCommand = new System.Data.SqlClient.SqlCommand();
            this.btnExit = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.btnRunESTINC = new System.Windows.Forms.Button();
            this.txtYear = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 

            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(160, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(296, 24);
            this.label4.TabIndex = 40;
            this.label4.Text = "Income Distribution Estimates";
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(8, 280);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 24);
            this.label3.TabIndex = 37;
            this.label3.Text = "Status";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Book Antiqua", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(8, 184);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(448, 88);
            this.txtStatus.TabIndex = 36;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(112, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(168, 32);
            this.label2.TabIndex = 34;
            this.label2.Text = "Estimates Year";
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(100, 120);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(96, 58);
            this.btnExit.TabIndex = 35;
            this.btnExit.Text = "Return";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(8, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(128, 40);
            this.panel1.TabIndex = 41;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Book Antiqua", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 32);
            this.label1.TabIndex = 31;
            this.label1.Text = "ESTINC";
            // 
            // btnRunESTINC
            // 
            this.btnRunESTINC.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRunESTINC.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunESTINC.Location = new System.Drawing.Point(8, 120);
            this.btnRunESTINC.Name = "btnRunESTINC";
            this.btnRunESTINC.Size = new System.Drawing.Size(96, 58);
            this.btnRunESTINC.TabIndex = 39;
            this.btnRunESTINC.Text = "Run ";
            this.btnRunESTINC.UseVisualStyleBackColor = false;
            this.btnRunESTINC.Click += new System.EventHandler(this.btnRunESTINC_Click);
            // 
            // txtYear
            // 
            this.txtYear.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtYear.Items.AddRange(new object[] {
            "2010",
            "2011",
            "2012",
            "2013",
            "2014",
            "2015",
            "2016",
            "2017",
            "2018",
            "2019",
            "2020"});
            this.txtYear.Location = new System.Drawing.Point(16, 72);
            this.txtYear.Name = "txtYear";
            this.txtYear.Size = new System.Drawing.Size(88, 31);
            this.txtYear.TabIndex = 45;
            // 
            // estinc
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(629, 313);
            this.Controls.Add(this.txtYear);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnRunESTINC);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Menu = this.mainMenu1;
            this.Name = "estinc";
            this.Text = "CONCEP Version 4 - ESTINC";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region ESTINC Run Button processing

        // btnRunESTINC_Click()
        /// method invoker for run button - starts another thread
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   04/08/04   tb   added new thread code
        //   ------------------------------------------------------------------
        private void btnRunESTINC_Click(object sender, System.EventArgs e)
        {
            eyear = Int32.Parse(txtYear.SelectedItem.ToString());
            lyear = eyear - 1;
            processParams();
            sra_inc_hh_pct_chg = new double[NUM_SRA, NUM_INCOME_GROUPS];
            sra_inc_hh_pct = new double[NUM_SRA, NUM_INCOME_GROUPS];
            sra_median = new int[NUM_SRA];
            regional_inc_hh = new int[NUM_INCOME_GROUPS];

            sra_base_inc_hh = new int[NUM_SRA, NUM_INCOME_GROUPS];
            sra_inc_hh = new int[NUM_SRA, NUM_INCOME_GROUPS];

            sra_inc_hh_pct_diff = new double[NUM_SRA, NUM_INCOME_GROUPS];
            sra_hh_pct_chg = new double[NUM_SRA, NUM_INCOME_GROUPS];

            MethodInvoker mi = new MethodInvoker(beginESTINCWork);
            mi.BeginInvoke(null, null);

        }  // end btnRunESTINC_CLICK()

        //*****************************************************************************

        // beginESTINCWork()

        /// method invoker for run button - starts another thread
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   04/08/04   tb   added new thread code
        //   ------------------------------------------------------------------
        private void beginESTINCWork()
        {
            //initialize the SQL command object
            sqlCommand = new System.Data.SqlClient.SqlCommand();
            //initialize the connection
            sqlCommand.Connection = sqlConnection;

            DoRegionalIncome(eyear);
            DoSRAIncome(eyear);
            DoCTIncome(eyear);
            DoMGRAIncome(eyear);
            WriteToStatusBox("Completed ESTINC processing");
        }  // end beginESTINCWork()

        //*****************************************************************************
        /* method processParams() */

        /// Method to process input from the form and build table names.

        /* Revision History
        * 
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private void processParams()
        {
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                appSettings = config.AppSettings.Settings;
                connectionStrings = config.ConnectionStrings.ConnectionStrings;

                networkPath = String.Format(appSettings["networkPath"].Value);
                MAX_CTS_IN_SRA = int.Parse(appSettings["MAX_CTS_IN_SRA"].Value);
                MAX_MGRAS_IN_CTS = int.Parse(appSettings["MAX_MGRAS_IN_CTS"].Value);
                NUM_MSA = int.Parse(appSettings["NUM_MSA"].Value);
                NUM_SRA = int.Parse(appSettings["NUM_SRA"].Value);
                NUM_INCOME_GROUPS = int.Parse(appSettings["NUM_INCOME_GROUPS"].Value);
                NUM_CTS = int.Parse(appSettings["NUM_CTS"].Value);
                NUM_MGRAS = int.Parse(appSettings["NUM_MGRAS"].Value);
                MAX_CITY = int.Parse(appSettings["MAX_CITIES"].Value);

                sqlConnection.ConnectionString = connectionStrings["ConcepDBConnectionString"].ConnectionString;
                this.sqlCommand.Connection = this.sqlConnection;

                TN.incomeAdj = String.Format(appSettings["incomeAdj"].Value);
                TN.incomeEstimates = String.Format(appSettings["incomeEstimates"].Value);
                TN.incomeEstimatesMGRA = String.Format(appSettings["incomeEstimatesMGRA"].Value);
                TN.incomeParms = String.Format(appSettings["incomeParms"].Value);
                TN.popestMGRA = String.Format(appSettings["popestMGRA"].Value);
                TN.xref = String.Format(appSettings["xref"].Value);

            }  // end try

            catch (ConfigurationErrorsException c)
            {
                throw c;
            }


        }  // end procedure processParams()

        /*****************************************************************************/

        #endregion

        #region ESTINC processing

        // procedures
        //  DoCTIncome() - Run the CT income estimates
        //  DoMGRAIncome() - Run the mgra income distributions
        //	DoRegionalIncome() - Run the regional income estimates
        //  DoSRAIncome() - Run the SRA income estimates

        // -------------------------------------------------------------------------
        /* DoCTIncome() */

        /* perform ct income computations the cts are processed in SRA order and control to sra distributions */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   12/03/03   tb   added code to exclude 6300 amd 6400 from sra 2 controlling
        //   07/16/04   tb   changed computations for type 5 to apply sra distribution to
        //                   hh totals and added a type 6 to do what 5 used to do - apply
        //                   sra rate of change to ct base year distribution
        //   ------------------------------------------------------------------
        public void DoCTIncome(int eyear)
        {
            int i, nct, k, j;
            int old_median, regional_median;
            int ct_tot;

            int[] ct_hh_tot = new int[MAX_CTS_IN_SRA];
            int[] ct_list = new int[MAX_CTS_IN_SRA];
            int[] ct_median = new int[MAX_CTS_IN_SRA];
            int[] ct_method = new int[MAX_CTS_IN_SRA];

            int[] inter_tot = new int[NUM_INCOME_GROUPS];
            int[,] ct_inc_hh = new int[MAX_CTS_IN_SRA, NUM_INCOME_GROUPS];
            int[,] ct_base_inc_hh = new int[MAX_CTS_IN_SRA, NUM_INCOME_GROUPS];

            double[] ct_asd = new double[MAX_CTS_IN_SRA];
            double[] ct_iexp = new double[MAX_CTS_IN_SRA];
            double[] diss = new double[MAX_CTS_IN_SRA];
            double[,] ct_base_inc_hh_pct = new double[MAX_CTS_IN_SRA, NUM_INCOME_GROUPS];
            double[,] ct_inc_hh_pct = new double[MAX_CTS_IN_SRA, NUM_INCOME_GROUPS];
            double[,] ct_inc_hh_pct_diff = new double[MAX_CTS_IN_SRA, NUM_INCOME_GROUPS];

            int[] tempi = new int[NUM_INCOME_GROUPS];
            double[] tempf = new double[NUM_INCOME_GROUPS];
            int[] tempj = new int[NUM_INCOME_GROUPS];
            string str;

            FileStream ctfile, cta;
            bool use_alts;
            int[,] pass_row = new int[MAX_CTS_IN_SRA, NUM_INCOME_GROUPS];
            int[] pass_row_tot = new int[MAX_CTS_IN_SRA];

            WriteToStatusBox("PROCESSING CT INCOME ESTIMATES");

            // open output file
            try
            {
                ctfile = new FileStream(networkPath + String.Format(appSettings["ct_out"].Value), FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            StreamWriter ct_out = new StreamWriter(ctfile);

            // open ascii file
            try
            {
                cta = new FileStream(networkPath + String.Format(appSettings["ct_ascii"].Value), FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening ASCII File");
                return;
            }

            //assign a wrapper for writing strings to ascii
            StreamWriter ct_a = new StreamWriter(cta);

            for (i = 0; i < NUM_SRA; ++i)
            {
                use_alts = false;
                // reset main arrays
                Array.Clear(ct_hh_tot, 0, ct_hh_tot.Length);
                Array.Clear(ct_median, 0, ct_median.Length);
                Array.Clear(ct_asd, 0, ct_asd.Length);
                Array.Clear(ct_iexp, 0, ct_iexp.Length);
                Array.Clear(ct_inc_hh, 0, ct_inc_hh.Length);
                Array.Clear(ct_inc_hh_pct, 0, ct_inc_hh_pct.Length);
                Array.Clear(ct_list, 0, ct_list.Length);
                Array.Clear(ct_method, 0, ct_method.Length);

                Array.Clear(pass_row, 0, pass_row.Length);
                Array.Clear(pass_row_tot, 0, pass_row_tot.Length);

                nct = 0;
                /* extract ct control households from popest */
                /* this fills the ct_list for this sra and sets value for nct */
                GetCTControls(i, ref nct, ct_list, ct_hh_tot);

                // extract income model parms
                GetCTParms(i, eyear, ct_list, nct, ct_median, ct_asd, ct_iexp, ct_method);

                // extract base_year distributions
                GetCTBase(i, ct_list, nct, ct_base_inc_hh_pct, ct_base_inc_hh);
                GetAdj(1, ct_list, nct, ct_inc_hh_pct_diff);

                /* perform the calculations */
                for (j = 0; j < nct; ++j)
                {
                    if (ct_median[j] > 0)
                    {
                        // move this row to the temp for DoStats call
                        for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                        {
                            tempf[k] = 0;
                        }  // end for k
                        DoStats(ct_list[j], ct_iexp[j], ct_asd[j], ct_median[j], tempf);
                        // move this row from the temp after DoStats call
                        for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                        {
                            ct_inc_hh_pct[j, k] = tempf[k];
                        }  // end for k
                    }  // end if
                    else if (ct_method[j] == 4)     /* special case - use base year distribution */
                    {
                        use_alts = true;
                        for (k = 0; k < NUM_INCOME_GROUPS; k++)
                        {
                            ct_inc_hh_pct[j, k] = ct_base_inc_hh_pct[j, k];
                        }  // end for k
                    }  // end else if
                    else if (ct_method[j] == 6)     /* special case - use chg in SRA cats - applied to base year dist */
                    {
                        use_alts = true;
                        for (k = 0; k < NUM_INCOME_GROUPS; k++)
                        {
                            ct_inc_hh_pct[j, k] = (int)(double)(ct_base_inc_hh_pct[j, k] * sra_inc_hh_pct_chg[i, k]);
                        }  // end for k
                    }  // end else if
                    else if (ct_method[j] == 5)     /* special case - apply sra distribution to ct hh */
                    {
                        use_alts = true;
                        for (k = 0; k < NUM_INCOME_GROUPS; k++)
                        {
                            ct_inc_hh_pct[j, k] = sra_inc_hh_pct[i, k];
                        }  // end for k
                    }  // end else if
                    if (!use_alts)
                    {
                        /* apply adjustments */
                        for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                        {
                            ct_inc_hh_pct[j, k] += ct_inc_hh_pct_diff[j, k];
                            if (ct_inc_hh_pct[j, k] < 0)
                            {
                                ct_inc_hh_pct[j, k] = 0;
                            }  // end if
                        }  // end for k
                    }  // end if

                    /* apply the rates to total hh control */
                    for (k = 0; k < NUM_INCOME_GROUPS; k++)
                    {
                        ct_inc_hh[j, k] = (int)(ct_inc_hh_pct[j, k] * (double)ct_hh_tot[j]);
                    }  // end for k
                    diss[j] = 0;
                    for (k = 0; k < NUM_INCOME_GROUPS; k++)
                    {
                        diss[j] += System.Math.Abs(ct_inc_hh_pct[j, k] * 100 - ct_base_inc_hh_pct[j, k] * 100) / 2;
                    }  // end for k
                }  // end for j

                /* call update to normalize in two directions.  Row totals are ct hh_tot;col tots are sra income distribution */
                // there is an exception here for SRA #2 ct 6300 and 6400 only has 5 households and we
                // dont' want to disturb the distribution with the controlling - so we'll
                // subtract it from the controlling routines - have to adjust the col totals also
                // 6400 is only effected through 2002 - in 2003 it gets new hh and needs to be controlled
                // add a flag for 6300 and 6400 tied to year

                for (j = 0; j < NUM_INCOME_GROUPS; ++j)
                {
                    tempj[j] = sra_inc_hh[i, j];
                }  // end for j

                CU.cUtil.update(nct, NUM_INCOME_GROUPS, ct_inc_hh, ct_hh_tot, tempj);

                /* recompute medians and print the income distributions */
                for (j = 0; j < nct; ++j)
                {
                    old_median = ct_median[j];
                    // move this row to the temp for Median call
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    {
                        tempi[k] = ct_inc_hh[j, k];
                    }  // end for k
                    ct_median[j] = MedianIncome(tempi);

                    if (old_median == 0)
                    {
                        old_median = ct_median[j];
                    }  // end if
                    PrintDist(ct_out, ct_a, eyear, j, ct_list[j], ct_asd[j], ct_iexp[j], ct_hh_tot[j],
                        ct_median[j], old_median, ct_inc_hh, ct_inc_hh_pct, ct_base_inc_hh_pct,
                        diss[j]);
                }  // end for j

                WriteTable1(ct_list, 11, ct_inc_hh, ct_median, nct);

                Array.Clear(inter_tot, 0, inter_tot.Length);
                /* output stacked ct x inc table */
                ct_out.Write("      CT     <15   15-29   30-44   45-59   60-74   75-99 100-124 125-150 150-200    200+   Popest   Rowsum   Diff\n");
                ct_out.Write("-----------------------------------------------------------------------------------------------------------------\n");

                for (j = 0; j < nct; ++j)
                {
                    str = string.Format("{0,8:d}", ct_list[j]);
                    ct_tot = 0;     /* reset row total */
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    {
                        str += string.Format("{0,8:d}", ct_inc_hh[j, k]);
                        inter_tot[k] += ct_inc_hh[j, k];   /* compute continuous col tot */
                        ct_tot += ct_inc_hh[j, k];         /* compute row total */
                    }  // end for k
                    str += string.Format("{0,8:d},{1,8:d},{2,8:d}", ct_hh_tot[j], ct_tot, ct_hh_tot[j] - ct_tot);
                    ct_out.WriteLine(str);
                    ct_out.Flush();
                }     /* end for j */

                str = "Sum Tot ";
                for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    str += string.Format("{0,8:d}", inter_tot[k]);
                ct_out.WriteLine(str);

                str = "SRA Tot ";
                for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    str += string.Format("{0,8:d}", sra_inc_hh[i, k]);
                ct_out.WriteLine(str);
                ct_out.WriteLine();
                ct_out.Flush();

                /* recompute the sra totals and median from cts */
                for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    sra_inc_hh[i, k] = 0;

                for (j = 0; j < nct; ++j)
                {
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    {
                        sra_inc_hh[i, k] += ct_inc_hh[j, k];
                    }  // end for k
                }  // end for j

                for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                {
                    tempi[k] = sra_inc_hh[i, k];
                }  // end for k
                sra_median[i] = MedianIncome(tempi);

            }  // end for i

            /* write the sra data */
            WriteTable1(sra_list, 2, sra_inc_hh, sra_median, NUM_SRA);

            /* recompute the regional stuff */
            Array.Clear(regional_inc_hh, 0, regional_inc_hh.Length);
            for (i = 0; i < NUM_SRA; ++i)
            {
                for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                {
                    regional_inc_hh[k] += sra_inc_hh[i, k];
                }  // end for k
            }  // end for i
            regional_median = MedianIncome(regional_inc_hh);

            ct_out.Close();
            ct_a.Close();
        } // end DoCTIncome()

        //***************************************************************************************

        // DoMGRAIncome()

        /* perform mgra income computations */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void DoMGRAIncome(int eyear)
        {
            int[] ct_list = new int[NUM_CTS];
            int[] inc = new int[NUM_INCOME_GROUPS];
            int[,] master_inc = new int[NUM_CTS, NUM_INCOME_GROUPS];
            int[,] mgra_income = new int[MAX_MGRAS_IN_CTS, NUM_INCOME_GROUPS];
            int[] ctIncomeDist = new int[NUM_INCOME_GROUPS];
            int[] currMGRAIncDist = new int[NUM_INCOME_GROUPS];
            int[,] hhBymgras = new int[NUM_CTS, MAX_MGRAS_IN_CTS];
            int[,] mgra_ids = new int[NUM_CTS, MAX_MGRAS_IN_CTS];      /* mgra ids for mgras */
            int[] num_mgras_by_ct = new int[NUM_CTS];
            int nct = 0, ct, i, j, k;
            int[,] prevMGRAIncDist = new int[NUM_MGRAS, NUM_INCOME_GROUPS];
            int prevHhSum;
            int currentMGRA;

            int[,] passer = null;
            FileStream mgraascii;

            SqlDataReader rdr;
            try
            {
                mgraascii = new FileStream(networkPath + String.Format(appSettings["mgra_ascii"].Value), FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            //assign a wrapper for writing strings to ascii
            StreamWriter mgra_ascii = new StreamWriter(mgraascii);

            // fill ct list from popest
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC13"].Value, TN.popestMGRA, eyear);

            try
            {
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                int ii = 0;
                while (rdr.Read())
                {
                    ct_list[ii++] = rdr.GetInt32(0);
                }  // end whild
                rdr.Close();
                nct = ii;
            }  // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

            // get the previous year's est geo inc distribution
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC8"].Value, TN.incomeEstimatesMGRA, lyear);
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                int ii = 0;
                while (rdr.Read())
                {
                    for (int incs = 0; incs < NUM_INCOME_GROUPS; incs++)
                    {
                        prevMGRAIncDist[ii, incs] = rdr.GetInt32(incs + 1);
                    }  // end for incs
                    ii++;
                }  // end while
                rdr.Close();
            }  // end try
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                sqlConnection.Close();
            }

            /* get the detailed characteristics from the CT base */
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC9"].Value, TN.incomeEstimates, eyear);

            try
            {
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    ct = GetIndex(rdr.GetInt32(0), ct_list);
                    for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                        inc[i] = rdr.GetInt32(i + 1);
                    if (ct != 999)
                    {
                        for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                        {
                            master_inc[ct, i] = inc[i];
                        }  // end for i
                    }  // end if
                }  // while
                rdr.Close();
            } // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC14"].Value, TN.popestMGRA, TN.xref, eyear);

            try
            {
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    ct = GetIndex(rdr.GetInt32(0), ct_list);
                    if (ct != 999)
                    {
                        mgra_ids[ct, num_mgras_by_ct[ct]] = rdr.GetInt32(1);
                        hhBymgras[ct, num_mgras_by_ct[ct]++] = rdr.GetInt32(2);
                    }  // end if
                }  // end while
                rdr.Close();
            }  // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

            // do the distributions. Iterate through each CT
            for (i = 0; i < NUM_CTS; i++)
            {
                // store the inc dist for this CT before passing to Pachinko
                for (j = 0; j < NUM_INCOME_GROUPS; j++)
                {
                    ctIncomeDist[j] = master_inc[i, j];
                }  // end for
                WriteToStatusBox("Processing CT # " + (i + 1) + " ID = " + ct_list[i]);

                // distribute the detailed charasteristic to the master vector
                if (num_mgras_by_ct[i] > 0)
                {
                    passer = new int[num_mgras_by_ct[i], NUM_INCOME_GROUPS];

                    for (j = 0; j < num_mgras_by_ct[i]; j++) // iterate through the est geos in this CT
                    {
                        currentMGRA = mgra_ids[i, j] - 1;
                        prevHhSum = 0;

                        for (k = 0; k < NUM_INCOME_GROUPS; k++)  // zero out the income groups
                        {
                            // get the sum of HH by est geo from prev year
                            prevHhSum += prevMGRAIncDist[currentMGRA, k];
                            mgra_income[j, k] = prevMGRAIncDist[currentMGRA, k];
                            currMGRAIncDist[k] = mgra_income[j, k];
                        }  // end for k
                        int newSum = 0;

                        // check if new popest HH total = 0. If so, don't run Pachinko; just clear the array.
                        if (hhBymgras[i, j] == 0)
                        {
                            currMGRAIncDist = new int[NUM_INCOME_GROUPS];
                        }  // end if
                        // get rid of negative change
                        else if (hhBymgras[i, j] - prevHhSum < 0)
                        {
                            for (int ii = 0; ii < currMGRAIncDist.Length; ii++)
                            {
                                currMGRAIncDist[ii] = (int)((double)hhBymgras[i, j] / (double)prevHhSum * currMGRAIncDist[ii] + 0.5);
                                newSum += currMGRAIncDist[ii];
                            }  // end for ii
                            // subtract from scaled totals
                            if (newSum > hhBymgras[i, j])
                            {
                                while (newSum != hhBymgras[i, j])
                                {
                                    currMGRAIncDist[findMaxIndex(currMGRAIncDist)]--;
                                    newSum--;
                                } // end while
                            }
                            else
                            {
                                // use Pachinko to add the remainder
                                int ret = CU.cUtil.PachinkoWithMasterNoDecrement(hhBymgras[i, j] - newSum, ctIncomeDist, currMGRAIncDist, NUM_INCOME_GROUPS);
                                if (ret >= 140000)
                                {
                                    MessageBox.Show("Pachinko did not resolve difference in 140000 iterations for CT " + i.ToString() + " MGRA " + j.ToString());
                                }  // end if
                            }  // end else
                        }  // end else if
                        else if (hhBymgras[i, j] - prevHhSum > 0)
                        {
                            /* goal here: apply previous year's MGRA inc dist to mgra_inc_row,
                             * then compute difference in HHs of current year to prev year. If positive,
                             * this is the number of HHs that Pachinko should distribute. target for
                             * Pachinko is difference of current - prev HH sum 
                             */
                            int ret = CU.cUtil.PachinkoWithMasterNoDecrement(hhBymgras[i, j] - prevHhSum, ctIncomeDist, currMGRAIncDist, NUM_INCOME_GROUPS);
                            if (ret >= 140000)
                            {
                                MessageBox.Show("Pachinko did not resolve difference in 140000 iterations for CT " + i.ToString() + " MGRA " + j.ToString());
                            }  // end if
                        }  // end else if
                        for (k = 0; k < NUM_INCOME_GROUPS; k++)
                        {
                            mgra_income[j, k] = currMGRAIncDist[k];
                            passer[j, k] = currMGRAIncDist[k];
                        }  // end for k
                    }  // end for j
                }  // end if

                int[] rowTotal = new int[num_mgras_by_ct[i]];

                for (int p = 0; p < num_mgras_by_ct[i]; p++)
                {
                    rowTotal[p] = hhBymgras[i, p];
                }  // end if

                // do the two-dimensional controlling. row totals are the MGRA HH; col totals CT inc dist
                CU.cUtil.update(num_mgras_by_ct[i], NUM_INCOME_GROUPS, passer, rowTotal, ctIncomeDist);

                // write the output to the database
                //WriteMGRAIncome(mgra_ascii, i, mgra_ids, mgra_income, num_mgras_by_ct);
                WriteMGRAIncome(mgra_ascii, i, mgra_ids, passer, num_mgras_by_ct);
            }  // end for i
            mgra_ascii.Close();
            BulkLoadMGRATable();

        }  // end DoMGRAIncome

        //****************************************************************************************************

        // findMaxIndex()

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------


        private int findMaxIndex(int[] vals)
        {
            int maxIndex = 0;
            for (int i = 0; i < vals.Length; i++)
            {
                if (vals[i] > vals[maxIndex])
                {
                    maxIndex = i;
                }  // end if
            }  // end for i
            return maxIndex;
        }  // end findMaxIndex

        //******************************************************************************************************

        // DoRegionalIncome()

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void DoRegionalIncome(int eyear)
        {
            int i, k;
            int regional_median = 0;
            int old_median;

            double regional_iexp = 0f;
            double regional_asd = 0f;

            double[] regional_base_inc_hh_pct = new double[NUM_INCOME_GROUPS];
            double[] regional_inc_hh_pct_diff = new double[NUM_INCOME_GROUPS];
            double[] regional_inc_hh_pct = new double[NUM_INCOME_GROUPS];

            FileStream regfile;

            TruncateOutputTable();
            // open output file
            try
            {
                regfile = new FileStream(networkPath + String.Format(appSettings["reg_out"].Value), FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            //assign a wrapper for writing strings to ascii
            StreamWriter reg_out = new StreamWriter(regfile);

            /* extract regional control households from popest */
            GetRegionalParms(eyear, ref regional_hh_tot, ref regional_median, ref regional_asd, ref regional_iexp);

            /* extract base year distribution */
            GetRegionalBase(regional_base_inc_hh_pct);

            /* get base year adjustments if applicable */
            GetRegionalAdj(regional_inc_hh_pct_diff);

            DoStats(999, regional_iexp, regional_asd, regional_median, regional_inc_hh_pct);

            /* apply adjustments */
            for (k = 0; k < NUM_INCOME_GROUPS; ++k)
            {
                regional_inc_hh_pct[k] += regional_inc_hh_pct_diff[k];
                if (regional_inc_hh_pct[k] < 0)
                    regional_inc_hh_pct[k] = 0;
            }     /* end for k */

            old_median = regional_median;

            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                regional_inc_hh[i] = (int)(regional_inc_hh_pct[i] * (double)regional_hh_tot);

            /* Pachinko for +1 rounding */
            int ret = CU.cUtil.PachinkoNoMaster(regional_hh_tot, regional_inc_hh, NUM_INCOME_GROUPS);
            if (ret >= 40000)
            {
                MessageBox.Show("Pachinko did not resolve difference in 40000 iterations\n");
            }  // end if

            /* recompute median */
            regional_median = MedianIncome(regional_inc_hh);

            PrintDist(reg_out, eyear, 999, regional_asd, regional_iexp, regional_hh_tot,
                        regional_median, old_median, regional_inc_hh, regional_inc_hh_pct,
                        regional_base_inc_hh_pct, 0f);
            WriteTable2(4, regional_inc_hh, regional_median);
            reg_out.Close();

        }  // end DoRegionalIncome()

        //********************************************************************************************

        // DoSRAIncome()

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        /* perform SRA income computations */
        public void DoSRAIncome(int eyear)
        {
            int i, j, k;
            int old_median;

            int[] sra_median = new int[NUM_SRA];
            int[] sra_hh_tot = new int[NUM_SRA];

            double[] diss = new double[NUM_SRA];
            double[] sra_asd = new double[NUM_SRA];
            //double [,] sra_inc_hh_pct = new double[NUM_SRA,NUM_INCOME_GROUPS];
            double[] sra_iexp = new double[NUM_SRA];
            double[,] sra_base_inc_hh_pct = new double[NUM_SRA, NUM_INCOME_GROUPS];

            int[] tempi = new int[NUM_INCOME_GROUPS];
            double[] tempf = new double[NUM_INCOME_GROUPS];
            string str;
            FileStream srafile, sraa;
            bool use_alts;
            /*--------------------------------------------------------------------------*/

            // open output file
            try
            {
                srafile = new FileStream(networkPath + String.Format(appSettings["sra_out"].Value), FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            try
            {
                sraa = new FileStream(networkPath + String.Format(appSettings["sra_ascii"].Value), FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            //assign a wrapper for writing strings to ascii
            StreamWriter sra_out = new StreamWriter(srafile);
            StreamWriter sr_aa = new StreamWriter(sraa);

            Array.Clear(sra_hh_tot, 0, sra_hh_tot.Length);
            Array.Clear(sra_median, 0, sra_median.Length);
            Array.Clear(sra_asd, 0, sra_asd.Length);
            Array.Clear(sra_iexp, 0, sra_iexp.Length);
            Array.Clear(sra_inc_hh, 0, sra_inc_hh.Length);
            Array.Clear(sra_inc_hh_pct, 0, sra_inc_hh_pct.Length);

            /* extract sra control households from popest */
            GetSRAControls(sra_hh_tot);

            /* extract income model parms */
            GetSRAParms(eyear, sra_median, sra_asd, sra_iexp);

            /* extract prev year distribution and compute % change for use with
                ct computations */
            GetSRABase(sra_base_inc_hh_pct, sra_base_inc_hh);

            GetAdj(2, sra_list, NUM_SRA, sra_inc_hh_pct_diff);

            /* computations */
            for (i = 0; i < NUM_SRA; ++i)
            {
                use_alts = false;
                if (sra_median[i] > 0)     /* is this an SRA that uses the curve */
                {
                    // move this row to the temp for DoStats call
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                        tempf[k] = 0;
                    DoStats(sra_list[i], sra_iexp[i], sra_asd[i], sra_median[i], tempf);
                    // move this row from the temp after DoStats call
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                        sra_inc_hh_pct[i, k] = tempf[k];
                }     /* end if */

                else     /* otherwise use the base year distribution */
                {
                    use_alts = true;
                    for (j = 0; j < NUM_INCOME_GROUPS; ++j)
                        sra_inc_hh_pct[i, j] = sra_base_inc_hh_pct[i, j];
                }     /* end else */

                /* apply adjustments */
                if (!use_alts)
                {
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    {
                        sra_inc_hh_pct[i, k] += sra_inc_hh_pct_diff[i, k];
                        if (sra_inc_hh_pct[i, k] < 0)
                            sra_inc_hh_pct[i, k] = 0;
                    }     /* end for k */
                }   // end if

                /* apply distribution to control number of households */
                for (j = 0; j < NUM_INCOME_GROUPS; ++j)
                {
                    sra_inc_hh[i, j] = (int)(sra_inc_hh_pct[i, j] * (double)sra_hh_tot[i]);
                    if (sra_base_inc_hh_pct[i, j] > 0)
                        sra_inc_hh_pct_chg[i, j] = sra_inc_hh_pct[i, j] / sra_base_inc_hh_pct[i, j];
                    else
                        sra_inc_hh_pct_chg[i, j] = 1f;
                }     /* end for j */

                diss[i] = 0;     /* init index of dissimilarity */
                for (j = 0; j < NUM_INCOME_GROUPS; ++j)
                    diss[i] += System.Math.Abs(sra_inc_hh_pct[i, j] * 100 - sra_base_inc_hh_pct[i, j] * 100) / 2;

            }     /* end for i */

            /* call update to normalize in two directions.  Row totals are SRA hh_tot; col tots are regional income distribution */
            CU.cUtil.update(NUM_SRA, NUM_INCOME_GROUPS, sra_inc_hh, sra_hh_tot, regional_inc_hh);

            /* recompute medians and print the income distributions */
            for (i = 0; i < NUM_SRA; ++i)
            {
                old_median = sra_median[i];
                for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    tempi[k] = sra_inc_hh[i, k];
                sra_median[i] = MedianIncome(tempi);

                PrintDist(sra_out, sr_aa, eyear, i, sra_list[i], sra_asd[i], sra_iexp[i], sra_hh_tot[i],
                        sra_median[i], old_median, sra_inc_hh, sra_inc_hh_pct, sra_base_inc_hh_pct, diss[i]);

            }     /* end for i */

            /* output stacked sra x income table */
            for (i = 0; i < NUM_SRA; ++i)
            {
                str = string.Format("{0,8:d}", sra_list[i]);
                for (j = 0; j < NUM_INCOME_GROUPS; ++j)
                    str += string.Format("{0,8:d}", sra_inc_hh[i, j]);
                str += string.Format("{0,8:d}", sra_hh_tot[i]);
                sra_out.WriteLine(str);
                sra_out.Flush();
            }     /* end for i */
            str = "  Total";
            for (j = 0; j < NUM_INCOME_GROUPS; ++j)
                str += string.Format("{0,8:d}", regional_inc_hh[j]);
            sra_out.WriteLine(str);
            sra_out.Flush();
            sra_out.Close();
            sr_aa.Close();
        }     /* end DoSRAIncome()*/

        //************************************************************************************************
        #endregion

        #region Miscellaneous Utilities

        // procedures
        //  WriteToStatusBox() - Display the current processing steps
        //  DoStats() - Distribution computations
        //  erff() - Error function for distribution computation
        //  GetIndex() - Return the index of the id in the supplied list
        //  LoadMGRAASCII() - bulk load the MGRA ASCII data
        //  MedianIncome() - Compute median income
        //	processParams() - Build table names and other parms from runtime arguments
        //  PrintDist() - formatted output 
        //  TruncateOutputTable() - purge the target output
        //  WriteMGRAASCII() - write the mgra estimates to ascii for bulk loading
        //  WriteTable1() - write to the ct output table
        //  WriteTable2() - regional data to output table

        //---------------------------------------------------------------------------------

        /* WriteToStatusBox() */

        /// Display the current processing status to the form

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void WriteToStatusBox(string status)
        {
            /* If we are running this method from primary thread, no marshalling is needed. */
            if (!txtStatus.InvokeRequired)
            {
                // Append to the string (whose last character is already newLine)
                txtStatus.Text += status + Environment.NewLine;
                // Move the caret to the end of the string, and then scroll to it.
                txtStatus.Select(txtStatus.Text.Length, 0);
                txtStatus.ScrollToCaret();
                Refresh();
            }  // end if
            // Invoked from another thread.  Show progress asynchronously.
            else
            {
                WriteDelegate write = new WriteDelegate(WriteToStatusBox);
                Invoke(write, new object[] { status });
            }  // end else
        }     //end WriteToStatusBox

        //*****************************************************************************

        /* DoStats() */

        // Revision History
        // Date			By	Description
        // ----------------------------------------------------------------------------
        // 07/11/02     tb  started this version
        // ----------------------------------------------------------------------------

        public void DoStats(int id, double iexp, double asd, int median, double[] hh_pct_est)
        {
            int[] bounds = new int[] { 15000, 30000, 45000, 60000, 75000, 100000, 125000, 150000, 200000, 350000 };
            int i;

            double log_med;                 /* log median */
            double[] log_income = new double[NUM_INCOME_GROUPS];

            double[] adj = new double[NUM_INCOME_GROUPS];
            double[,] p = new double[2, NUM_INCOME_GROUPS];
            double[] z_est = new double[NUM_INCOME_GROUPS];          /* estimated z values */
            double[] z1 = new double[NUM_INCOME_GROUPS];             /* difference between bound and median */

            double sqrt2;                   /* square root of 2 */

            /*--------------------------------------------------------------------------*/

            /* get log of income bounds */
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                log_income[i] = System.Math.Log((double)bounds[i]);

            sqrt2 = (System.Math.Sqrt((double)2));

            /* log median */
            if (median > 0)
                log_med = System.Math.Log((double)median);
            else
                log_med = 1;

            Array.Clear(p, 0, p.Length);
            Array.Clear(adj, 0, adj.Length);
            Array.Clear(z_est, 0, z_est.Length);
            Array.Clear(z1, 0, z1.Length);

            for (i = 0; i < NUM_INCOME_GROUPS - 1; ++i)
            {
                z1[i] = log_income[i] - log_med;
                adj[i] = asd * (System.Math.Pow(log_income[i], iexp));
                z_est[i] = z1[i] * adj[i];
                p[0, i] = (1 + erff(z_est[i] / sqrt2)) / 2;
                if (i > 0)
                    p[1, i] = p[0, i] - p[0, i - 1];
            }     /* end for i */

            p[1, 0] = p[0, 0];
            p[1, NUM_INCOME_GROUPS - 1] = 1 - p[0, NUM_INCOME_GROUPS - 2];

            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                hh_pct_est[i] = (double)p[1, i];

        }     /* end DoStats()*/

        //*****************************************************************************

        /* procedure erff() */

        /* error function */

        // Revision History
        // Date			By	Description
        // ----------------------------------------------------------------------------
        // 07/11/02     tb  started this version
        // ----------------------------------------------------------------------------

        public double erff(double y)
        {
            int i;
            int isw;
            int iskip;
            double x, res, xsq, xnum, xden, xi;

            /*     COEFFICIENTS FOR 0.0 <= Y < .477 */
            double[] p = new double[] {113.8641541510502, 377.4852376853020,
								3209.377589138469, .1857777061846032,
								3.181123743870566};

            double[] q = new double[] {244.0246379344442, 1282.616526077372,
								2844.236833439171, 23.60129095234412};

            double[] p1 = new double[] {8.883149794388376, 66.11919063714163,
								298.6351381974001, 881.9522212417691,
								1712.047612634071, 2051.078377826071,
								1230.339354797997, 2.153115354744038E-8,
								.5641884969886701};
            double[] q1 = new double[] {117.6939508913125, 537.1811018620099,
								1621.389574566690, 3290.799235733460,
								4362.619090143247, 3439.367674143722,
								1230.339354803749, 15.74492611070983};

            /*     COEFFICIENTS FOR 4.0 < Y */

            double[] p2 = new double[] {-3.603448999498044E-01, -1.257817261112292E-01,
									-1.608378514874228E-02, -6.587491615298378E-04,
									-1.631538713730210E-02, -3.053266349612323E-01};
            double[] q2 = new double[] {1.872952849923460,    5.279051029514284E-01,
									6.051834131244132E-02,  2.335204976268692E-03,
									2.568520192289822};
            double xmin = 1.0E-10;
            double xlarge = 6.375;
            double sqrpi = .5641895835477563;

            /*-------------------------------------------------------------------------*/
            iskip = 0;
            x = y;
            isw = 1;
            xsq = 0;
            if (x < 0.0)
            {
                isw = -1;
                x = -x;
            }     /* end if */

            if (x < 0.477)
            {
                if (x >= xmin)
                {
                    xsq = x * x;
                    xnum = p[3] * xsq + p[4];
                    xden = xsq + q[3];
                    for (i = 0; i < 3; ++i)
                    {
                        xnum = xnum * xsq + p[i];
                        xden = xden * xsq + q[i];
                    }  // end for i

                    res = x * (xnum / xden);
                }     /* end if */

                else
                    res = x * (p[2] / q[2]);
                iskip = 1;
            }     /* end if x */

            else if (x <= 4.0)
            {
                xsq = x * x;

                xnum = p1[7] * x + p1[8];
                xden = x + q1[7];
                for (i = 0; i < 7; ++i)
                {
                    xnum = xnum * x + p1[i];
                    xden = xden * x + q1[i];
                }  // end for i

                res = xnum / xden;

            }     /* end else if */

            else if (x < xlarge)
            {
                xsq = x * x;
                xi = 1.0 / xsq;
                xnum = p2[4] * xi + p2[5];
                xden = xi + q2[4];
                for (i = 0; i < 4; ++i)
                {
                    xnum = xnum * xi + p2[i];
                    xden = xden * xi + q2[i];
                }  // end for i

                res = (sqrpi + xi * (xnum / xden)) / x;
            }     /* end else if */

            else
            {
                res = 1.0;
                iskip = 1;
            }  // end else

            if (iskip == 0)
            {
                res = res * System.Math.Exp(-xsq);
                res = 1.0 - res;
            }  // end if

            if (isw == -1)
                res = -res;

            return ((double)res);
        }  // end erf()

        //*****************************************************************************************


        // GetIndex()

        /* get the index value for the sra or ct  argument */
        // Revision History
        // Date			By	Description
        // ----------------------------------------------------------------------------
        // 07/11/02     tb  started this version
        // ----------------------------------------------------------------------------
        public int GetIndex(int id, int[] list)
        {
            int ret = 999;
            for (int i = 0; i < list.Length; i++)
            {
                if (id == list[i])
                {
                    ret = i;
                    break;
                }  // end if
            }  // end for i
            return ret;
        }  // end GetIndex()

        //*************************************************************************************************

        //BulkLoadMGRATable()

        // Revision History
        // Date			By	Description
        // ----------------------------------------------------------------------------
        // 07/11/02     tb  started this version
        // ----------------------------------------------------------------------------
        public void BulkLoadMGRATable()
        {
            sqlCommand.CommandText = String.Format(appSettings["deleteFrom"].Value, TN.incomeEstimatesMGRA, eyear);

            try
            {
                sqlConnection.Open();
                //these are self documenting

                sqlCommand.ExecuteNonQuery();	   //this is the equivalent of esqlc execute immediate	 

                sqlCommand.CommandText = "SELECT * into #temp FROM " + TN.incomeEstimatesMGRA + " WHERE 1 = 2";
                sqlCommand.ExecuteNonQuery();

                sqlCommand.CommandTimeout = 180;
                sqlCommand.CommandText = "bulk insert #temp from '" + networkPath + String.Format(appSettings["mgra_ascii"].Value) +
                                            " ' with (fieldterminator = ',', firstrow = 1)";
                sqlCommand.ExecuteNonQuery();

                sqlCommand.CommandText = "INSERT INTO " + TN.incomeEstimatesMGRA + " SELECT * from #temp";
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }  // end procedure BulkLoadMGRATable()

        //**************************************************************************

        /* procedure MedianIncome() */
        /* perform median income calculations */

        // Revision History
        // Date			By	Description
        // ----------------------------------------------------------------------------
        // 07/11/02     tb  started this version
        // ----------------------------------------------------------------------------
        public int MedianIncome(int[] data)
        {
            int sx, i;
            int[] bounds = new int[] { 0, 15000, 30000, 45000, 60000, 75000, 100000, 125000, 150000, 200000, 350000 };
            double xp, sx1, med_val, total;

            sx = 0;
            sx1 = 0;
            med_val = 0;
            total = 0;

            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                total += data[i];

            if (total == 0)     /* if the total of the array is zero - return 0 for med*/
                return ((int)med_val);

            xp = (double)(total * 50) / 100;

            for (i = 1; i < 9; ++i)
            {
                sx += data[i - 1];
                sx1 = data[i - 1] - sx + xp;
                if (xp - sx < 0)
                {
                    med_val = sx1 / (double)data[i - 1] * (double)(bounds[i] - bounds[i - 1])
                            + (double)bounds[i - 1];
                    break;
                }  // end if
                else if (xp - sx == 0)
                {
                    med_val = (double)bounds[i];
                    break;
                }  // end else if
            }  // end for i

            return ((int)med_val);

        }  // end MedianIncome()

        //***********************************************************************************************

        /* PrintDist()*/

        /* print distributions */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void PrintDist(StreamWriter filer, StreamWriter ct_a, int eyear, int array_index, int id,
                                double asd, double iexp,
                                int hh_tot, int median, int old_median, int[,] inc_hh,
                                double[,] hh_pct_est,
                                double[,] based, double diss)
        {
            int i;
            string str;
            double diff = 0f;
            double[] new_pct = new double[NUM_INCOME_GROUPS];

            /*--------------------------------------------------------------------------*/

            //WriteToStatusBox("PROCESSING CT INCOME ESTIMATES");

            if (old_median > 0)
                diff = ((double)median / (double)old_median - 1) * 100;

            str = id.ToString() + "," + diss.ToString();
            ct_a.WriteLine(str);
            ct_a.Flush();

            str = "YEAR:" + string.Format("{0:d}", eyear) +
                  " ID:" + string.Format("{0:d}", id) +
                  " ASD:" + string.Format("{0:f3}", asd) +
                      " EXP:" + string.Format("{0:f3}", iexp) +
                  " DISS:" + string.Format("{0:f3}", diss) +
                        " HH:" + string.Format("{0:d}", hh_tot) +
                  " MED:" + string.Format("{0:d}", old_median) +
                  " ADJ MED:" + string.Format("{0:d}", median) +
                  " PCT DIFF:" + string.Format("{0:f3}", diff);
            filer.WriteLine(str);

            str = "BASE: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += " " + string.Format("{0:f5}", based[array_index, i]);
            filer.WriteLine(str);

            str = " PCT: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += " " + string.Format("{0:f5}", hh_pct_est[array_index, i]);
            filer.WriteLine(str);

            str = " REV: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
            {
                if (hh_tot > 0)
                    new_pct[i] = (double)inc_hh[array_index, i] / (double)hh_tot;
                else
                    new_pct[i] = 0;
                str += " " + string.Format("{0:f5}", new_pct[i]);
            }  // end for i
            filer.WriteLine(str);

            str = "hh: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += " " + string.Format("{0:d}", inc_hh[array_index, i]);
            filer.WriteLine(str);
            filer.WriteLine();

            filer.Flush();

        }     /* end PrintDist()*/

        /******************************************************************************/

        /* PrintDist() - overloaded for region all*/

        /* print distributions */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void PrintDist(StreamWriter filer, int eyear, int id, double asd, double iexp, int hh_tot, int median, int old_median, int[] inc_hh,
                              double[] hh_pct_est, double[] based, double diss)
        {
            int i;
            string str;
            double diff = 0f;

            double[] new_pct = new double[NUM_INCOME_GROUPS];

            /*---------------------------------------------------------------------------*/
            if (old_median > 0)
                diff = ((double)median / (double)old_median - 1) * 100;

            str = "YEAR:" + string.Format("{0:d}", eyear) +
                " ID:" + string.Format("{0:d}", id) +
                " ASD:" + string.Format("{0:f3}", asd) +
                " EXP:" + string.Format("{0:f3}", iexp) +
                " DISS:" + string.Format("{0:f3}", diss) +
                " HH:" + string.Format("{0:d}", hh_tot) +
                " MED:" + string.Format("{0:d}", old_median) +
                " ADJ MED:" + string.Format("{0:d}", median) +
                " PCT DIFF:" + string.Format("{0:f3}", diff);
            filer.WriteLine(str);

            str = "BASE: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += " " + string.Format("{0:f5}", based[i]);
            filer.WriteLine(str);

            str = " PCT: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += " " + string.Format("{0:f5}", hh_pct_est[i]);
            filer.WriteLine(str);

            str = " REV: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
            {
                if (hh_tot > 0)
                    new_pct[i] = (double)inc_hh[i] / (double)hh_tot;
                else
                    new_pct[i] = 0;
                str += " " + string.Format("{0:f5}", new_pct[i]);
            }  // end for i
            filer.WriteLine(str);

            str = "hh: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += " " + string.Format("{0:d}", inc_hh[i]);
            filer.WriteLine(str);
            filer.WriteLine();

            filer.Flush();

        }     /* end PrintDist()*/

        /******************************************************************************/

        /* TruncateOutputTable */

        /* purge the output table before reloading */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   11/21/03   tb   started initial coding
        //   ------------------------------------------------------------------

        public void TruncateOutputTable()
        {

            sqlCommand.CommandText = String.Format(appSettings["deleteFrom"].Value, TN.incomeEstimates, eyear);

            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }  // end procedure TruncateOutputTable()

        //*********************************************************************

        // WriteMGRAIncome()

        /* write the mgra data to ascii for bulk loading */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void WriteMGRAIncome(StreamWriter estimates_ascii, int ct_index, int[,] mgra_ids, int[,] data, int[] mgra_index)
        {
            int[] inc = new Int32[NUM_INCOME_GROUPS];
            int i, j, geo, hh;
            string str;
            for (i = 0; i < mgra_index[ct_index]; ++i)
            {
                geo = mgra_ids[ct_index, i];
                str = eyear + "," + geo + ",";
                hh = 0;
                for (j = 0; j < NUM_INCOME_GROUPS; ++j)
                {
                    inc[j] = data[i, j];     /* store the income in local array */
                    hh += inc[j];
                    str += inc[j] + ",";
                }  // end for j
                str += hh;
                estimates_ascii.WriteLine(str);
                estimates_ascii.Flush();
            }  // end for i
        }  //end procedure WriteMGRAIncome()

        //***********************************************************************************

        // WriteTable1()

        /* output estimates to database table */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void WriteTable1(int[] list, byte type, int[,] inc, int[] med, int ndx)
        {
            int i, k, hh;
            string s1;

           
            for (i = 0; i < ndx; ++i)
            { 
                s1 = " values(";
                hh = 0;
                s1 += eyear + "," + type + "," + list[i] + ",";
                for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                {
                    hh += inc[i, k];
                    s1 += inc[i, k].ToString() + ",";
                }
                s1 += hh + "," + med[i] + ")";

                sqlCommand.CommandText = String.Format(appSettings["insertInto"].Value, TN.incomeEstimates, s1);
                try
                {
                    sqlConnection.Open();
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), e.GetType().ToString());
                }
                finally
                {
                    sqlConnection.Close();
                }
            }  // end for i

        }  // endprocedure WriteTable1()

        //****************************************************************************************

        /* WriteTable2*/
        /* output estimates to database table */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void WriteTable2(byte type, int[] inc, int med)
        {
            int k, hh;
            string s1;

            s1 = " values(";
            hh = 0;
            s1 += eyear + "," + type + ",999,";
            for (k = 0; k < NUM_INCOME_GROUPS; ++k)
            {
                hh += inc[k];
                s1 += inc[k].ToString() + ",";
            }
            s1 += hh.ToString() + "," + med + ")";

            sqlCommand.CommandText = String.Format(appSettings["insertInto"].Value, TN.incomeEstimates, s1);

            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }
        }  // end WriteTable2()

        //*****************************************************************************
        #endregion


        // procedures
        //	GetAdj()

        // *******************************************************************************
        /* GetAdj()*/

        /* extract income model distribution adjustments */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void GetAdj(byte type, int[] list, int nlist, double[,] adj)
        {
            int j, k;
            int loc_id, id;
            string tex = "";
            System.Data.SqlClient.SqlDataReader rdr;
            /*--------------------------------------------------------------------------*/

            //WriteToStatusBox("EXTRACTING CT DISTRIBUTION ADJUSTMENTS");

            // open the connection

            if (type == 2)
                tex = " where geo_type = 2";
            else
            {
                tex = " where geo_type = 11 and geo_id in (";

                /* finish sql_buffer with comma-sep list of cts (except last one) */
                for (j = 0; j < nlist - 1; ++j)
                    tex += list[j].ToString() + ",";

                /* add last ct and close query */
                tex += list[nlist - 1].ToString() + ")";
            }   // end else
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC5"].Value, TN.incomeAdj, tex);
            try
            {
                sqlConnection.Open();
                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    id = rdr.GetInt32(1);
                    loc_id = GetIndex(id, list);
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                        adj[loc_id, k] = rdr.GetDouble(k + 2);
                }     // end while
                rdr.Close();

            }   // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }     /* end GetAdj()*/

        //********************************************************************************

        //	GetCTBase()
        //	GetCTControls()
        //	GetCTParms()
        //-------------------------------------------------------------------
        /* GetCTBase()*/

        /* extract ct base year income distributions */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void GetCTBase(int sra_index, int[] list, int nct, double[,] base_array1, int[,] base_array2)
        {
            int i, j;
            int id, loc_id;
            int[] old_inc = new int[NUM_INCOME_GROUPS];
            int old_inc_total = 0;
            string tex = "";
            System.Data.SqlClient.SqlDataReader rdr;

            WriteToStatusBox("EXTRACTING BASE YEAR DISTRIBUTION FOR CTs FOR SRA " + sra_list[sra_index]);

            tex = " and geo_type = 11 and id in (";
            /* finish sql_buffer with comma-sep list of cts (except last one) */
            for (j = 0; j < nct - 1; ++j)
            {
                tex += list[j].ToString() + ",";
            }  // end for j

            /* add last ct and close query */
            tex += list[nct - 1].ToString() + ")";
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC6"].Value, TN.incomeEstimates, lyear, tex);
            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    id = rdr.GetInt32(1);
                    loc_id = GetIndex(id, list);
                    old_inc_total = 0;
                    for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                    {
                        old_inc[i] = rdr.GetInt32(i + 2);
                        base_array2[loc_id, i] = old_inc[i];
                        base_array1[loc_id, i] = 0;
                        old_inc_total += old_inc[i];
                    }     // end for 
                    for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                    {
                        if (old_inc_total > 0)
                            base_array1[loc_id, i] = (double)old_inc[i] / (double)old_inc_total;
                    }  // end for i
                }  // end while
                rdr.Close();

            }  // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }  // end GetBase()

        //********************************************************************************************

        // GetCTControls
        /* extract ct hh control totals */
        public void GetCTControls(int sra_index, ref int nct, int[] ct_list, int[] ct_hh_tot)
        {

            System.Data.SqlClient.SqlDataReader rdr;
            WriteToStatusBox("EXTRACTING POPEST CONTROLS FOR CTs FOR SRA " + sra_list[sra_index]);

            sqlCommand.CommandText = String.Format(appSettings["selectESTINC2"].Value, TN.popestMGRA, TN.xref, eyear, sra_list[sra_index]);

            nct = 0;
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    ct_list[nct] = rdr.GetInt32(0);
                    ct_hh_tot[nct] = rdr.GetInt32(1);
                    ++nct;
                }  // end while
                rdr.Close();

            }  // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }     /* end GetCTControls()

		/******************************************************************************/

        /* GetCTParms()*/

        /* extract ct model parms */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        void GetCTParms(int sra_index, int eyear, int[] list, int nct, int[] ct_median, double[] ct_asd, double[] ct_iexp, int[] ct_method)
        {
            int loc_ct, ct;
            int j, pt;
            double parm;
            string tex = "";
            System.Data.SqlClient.SqlDataReader rdr;
            /*--------------------------------------------------------------------------*/

            WriteToStatusBox("EXTRACTING INCOME MODEL PARMS FOR CTs FOR SRA " + sra_list[sra_index].ToString());

            tex = "and geo_type = 11 and geo_id in (";
            /* finish sql_buffer with comma-sep list of cts (except last one) */
            for (j = 0; j < nct - 1; ++j)
                tex += list[j].ToString() + ",";

            tex += list[nct - 1].ToString() + ")";
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC10"].Value, TN.incomeParms, eyear, tex);
            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    ct = rdr.GetInt32(0);
                    pt = rdr.GetByte(1);
                    parm = rdr.GetDouble(2);
                    loc_ct = GetIndex(ct, list);
                    switch (pt)
                    {
                        case 3:
                            ct_median[loc_ct] = (int)parm;
                            break;
                        case 1:
                            ct_asd[loc_ct] = parm;
                            break;
                        case 2:
                            ct_iexp[loc_ct] = parm;
                            break;
                        //for case 4 and 5 set any median = 0
                        case 4:
                        case 5:
                        case 6:
                            ct_method[loc_ct] = pt;
                            ct_median[loc_ct] = 0;
                            break;
                    }     // end switch
                }     // end while
                rdr.Close();

            }   // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }   // end catch
            finally
            {
                sqlConnection.Close();
            }

        }     /* end GetCTParms()() */

        /******************************************************************************/

        #region Regional extraction methods()
        //	GetRegionalAdj()
        //	GetRegionalBase()
        //	GetRegionalParms()

        /* procedure GetRegionalAdj()*/

        /* extract income model distribution adjustments for region*/

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void GetRegionalAdj(double[] adj)
        {
            int i;
            string tex = "";
            System.Data.SqlClient.SqlDataReader rdr;
            /*--------------------------------------------------------------------------*/

            WriteToStatusBox("EXTRACTING DISTRIBUTION ADJUSTMENTS");
            tex = "where geo_type = 4";
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC5"].Value, TN.incomeAdj, tex);

            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                        adj[i] = rdr.GetDouble(i + 2);
                }     // end while
                rdr.Close();

            }  // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }     /* end GetRegionalAdj*/

        /******************************************************************************/

        /* procedure GetRegionalBase()*/

        /* extract regional base year income distributions */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void GetRegionalBase(double[] base_array)
        {
            int i, k, geo_type;
            int old_inc_total;

            int[] old_inc = new int[NUM_INCOME_GROUPS];

            System.Data.SqlClient.SqlDataReader rdr;
            /*--------------------------------------------------------------------------*/

            WriteToStatusBox("EXTRACTING BASE YEAR INCOME DISTRIBUTIONS FOR REGION");
            geo_type = 4;
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC4"].Value, TN.incomeEstimates, lyear, geo_type);

            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                        old_inc[i] = rdr.GetInt32(i + 2);
                }     // end while
                rdr.Close();

            }  // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

            old_inc_total = 0;     /* init the sum */
            for (k = 0; k < NUM_INCOME_GROUPS; ++k)      /* zero the pct array and build the total */
            {
                old_inc_total += old_inc[k];
                base_array[k] = 0;
            }     /* end for k */

            /* compute the base year pct distribution */
            for (k = 0; k < NUM_INCOME_GROUPS; ++k)
            {
                if (old_inc_total > 0)
                    base_array[k] = (double)old_inc[k] / (double)old_inc_total;
            }     /* end for k */

        }     /* end procedure GetRegionalBase()*/

        //*************************************************************************************

        /* procedure GetRegionalParms()*/

        /* extract regional hh totals from popest and model parms */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void GetRegionalParms(int eyear, ref int reg_hh_tot, ref int reg_median, ref double reg_asd, ref double reg_iexp)
        {

            double parm;
            int parm_type;
            System.Data.SqlClient.SqlDataReader rdr;

            /*---------------------------------------------------------------------------*/
            WriteToStatusBox("EXTRACTING REGIONAL POPEST CONTROLS");
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC1"].Value, TN.popestMGRA, eyear);

            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    reg_hh_tot = rdr.GetInt32(0);
                }     // end while
                rdr.Close();

            }  // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

            sqlCommand.CommandText = String.Format(appSettings["selectESTINC11"].Value, TN.incomeParms, eyear);

            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    parm_type = rdr.GetByte(0);
                    parm = rdr.GetDouble(1);
                    if (parm_type == 3)
                        reg_median = (int)parm;
                    else if (parm_type == 1)
                        reg_asd = parm;
                    else if (parm_type == 2)
                        reg_iexp = parm;
                }     // end while
                rdr.Close();

            }  // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }     /* end GetRegionalParms()*/

        //****************************************************************************************

        #endregion

        #region SRA extraction methods

        //  GetSRABase()
        //	GetSRAControls()
        //	GetSRAParms()

        /******************************************************************************/

        /* procedure GetSRABase()*/

        /* extract sra base year income distributions */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void GetSRABase(double[,] base_array_pct, int[,] base_array)
        {
            int k, sra, nsra;
            int old_inc_total = 0;
            int[] old_inc = new int[NUM_INCOME_GROUPS];
            System.Data.SqlClient.SqlDataReader rdr;

            WriteToStatusBox("EXTRACTING BASE YEAR INCOME DISTRIBUTION FOR SRA");
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC7"].Value, TN.incomeEstimates, lyear);

            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    sra = rdr.GetInt32(0);
                    old_inc_total = 0;
                    nsra = GetIndex(sra, sra_list);
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    {
                        old_inc[k] = rdr.GetInt32(k + 1);
                        old_inc_total += old_inc[k];
                    }     // end for k
                    /* compute the base year distribution */
                    for (k = 0; k < NUM_INCOME_GROUPS; ++k)
                    {
                        if (old_inc_total > 0)
                            base_array_pct[nsra, k] = (double)old_inc[k] / (double)old_inc_total;
                    }     /* end for k */
                }     // end while
                rdr.Close();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }  // end procedure GetSRABase()

        //*********************************************************************************************

        /* extract sra hh control totals */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void GetSRAControls(int[] hh)
        {
            int nsra, sra;

            SqlDataReader rdr;

            WriteToStatusBox("EXTRACTING POPEST CONTROLS FOR SRA");
            sqlCommand.CommandText = String.Format(appSettings["selectESTINC3"].Value, TN.popestMGRA, TN.xref, eyear);

            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    sra = rdr.GetInt32(0);
                    nsra = GetIndex(sra, sra_list);
                    hh[nsra] = rdr.GetInt32(1);
                }     // end while
                rdr.Close();

            }   // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }   // end catch
            finally
            {
                sqlConnection.Close();
            }

        }     /* end procedure GetSRAControls() */

        /******************************************************************************/

        /* procedure GetSRAParms()*/

        /* extract sra model parms */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void GetSRAParms(int eyear, int[] sra_median, double[] sra_asd, double[] sra_iexp)
        {
            int nsra, pt, sra;
            double parm;
            System.Data.SqlClient.SqlDataReader rdr;

            /*---------------------------------------------------------------------------*/

            WriteToStatusBox("EXTRACTING INCOME MODEL PARMS FOR SRA");

            sqlCommand.CommandText = String.Format(appSettings["selectESTINC12"].Value, TN.incomeParms, eyear);

            try
            {
                // open the connection
                sqlConnection.Open();
                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    sra = rdr.GetInt32(0);
                    nsra = GetIndex(sra, sra_list);
                    pt = rdr.GetByte(1);
                    parm = rdr.GetDouble(2);
                    if (pt == 3)
                        sra_median[nsra] = (int)parm;
                    else if (pt == 1)
                        sra_asd[nsra] = parm;
                    else if (pt == 2)
                        sra_iexp[nsra] = parm;
                }     // end while
                rdr.Close();

            }   // end try
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }     /* end procedure GetSRAParms()*/

        /******************************************************************************/

        #endregion

        private void btnExit_Click(object sender, System.EventArgs e)
        {
            Application.Exit();
        }
    }
}   // end namespace