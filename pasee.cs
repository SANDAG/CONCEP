/* 
 * Source File: pasee.cs
 * Program: concep
 * Version 4
 * Programmer: tbe
 * Description: PASEE module of concep  
 *              regional demographic characteristics estimates
 *          Version 4 introduces concep.config.exe to store all global constants, queries, table names and file names    
 *  *		version 3.5 adds computations for using Series 13 geographies
 *
 * Database description:
 *    SQL Server Database : concep  
 *       births_ct: births by ct for base year (calender year prior to estimates year)
 *       deaths_ct: deaths by ct for base year (calender year prior to estimates year)
 *       detailed_pop_ct: estimates year pop by age, sex and ethnicity; census tracts; normalized
 *       detailed_pop_tab_ct: estimates year pop by age, sex and ethnicity; census tracts; tabular
 *       detailed_pop_ct_L: base year pop by age, sex and ethnicity; census tracts; normalized
 *       detailed_pop_tab_mgra: estimates year pop by age, sex and ethnicity; MGRAs; tabular
 *       migration_distribution: ethnic and sex distribution of migration by year and ethnicity
 *       migration_rates: regional migration rates by ethnicity and sex
 *       popest_mgra: popest mgra, indexed by estimates_year
 *       pop_mil_dep_pct_2000: 2000 military dependents distribution
 *       pop_mil_pct_2000: 2000 military population distribution
 *       xref_mgra_sr13: series 13 cross reference
*/
//Revision History
 //   Date       By   Description
 //   ------------------------------------------------------------------
 //   07/12/02   tb   started initial coding
 //   04/08/04   tb   added try/catch code for sql calls and multi thread status
 //   04/15/04   tb   added validation code
 //   06/17/04   tb   changes for version 2.5 - eliminating split-tract files for pop change
 //   02/01/05   tb   recode for Version 3.0 SGRAs
 //   06/09/11   tb   recode for Version 3.3 adding detailed CT HH data
 //   ------------------------------------------------------------------
 //******************************************************************************************

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;

namespace pasee
{
    // need this to use the WriteToStatusBox on different thread
    delegate void WriteDelegate(string status);
    /// <summary>
    /// Summary description for pasee.
    /// </summary>
    public class pasee : System.Windows.Forms.Form
    {
       
        public Configuration config;
        public KeyValueConfigurationCollection appSettings;
        public ConnectionStringSettingsCollection connectionStrings;

        public TableNames TN = new TableNames();
        public string networkPath;

        public int NUM_ETH;  // 1 - 8 ; 0 stores all eth
        public int NUM_SEX;	   // 1 - 2 ; 0 stores both sexes
        public int NUM_AGE;   // 0 - 99 and 100+
        public int NUM_CTS;
        public int MAX_ASE;  // 101 * 8 * 2
        public int NUM_SRA;   // allow for 0

        public int MAX_ELEM;
        
        public int MAX_CTS_IN_SRA;
        public int NUM_AGE5;     // "5-year" age groups
        public int MAX_ASE5;     // 20 x 8 x 2 max elements in Pachinko for 5-year groups

        //global vars
        public string pasee_update_0_proc;

        public string ct_stored_proc;
        public string regional_estimates_file;
        public string ct_estimates_file;
        public string sra_estimates_file;

        public int births_total = 0;
        public int deaths_total = 0;
        public int milmig = 0;
        public int nmilmig_total = 0;
        public int num_ct = 0;
        public int num_mil_ct;
        public int popchg = 0;
        public int popest_base = 0;
        public int popest_estimate = 0;
        public int popest_mil_basep_total = 0;
        public int popest_mil_est_total = 0;
        public int totmig = 0;

        public pasee_compute PC = new pasee_compute();						// global arrays
        public pasee_sra PSRA = new pasee_sra();
        public pasee_ct PCT = new pasee_ct();
        public pasee_mgra peg = new pasee_mgra();

        public int[] sra_list = {1,2,3,4,5,6,10,11,12,13,14,15,16,17,20,21,22,30,31,32,33,34,35,
		                        36,37,38,39,40,41,42,43,50,51,52,53,54,55,60,61,62,63};

        public int[,] ct_list;
        public int[] basep;
        public double[, ,] mig_rates;
        public double[,] mig_dist;
        public double[, ,] mil_pct;
        public double[, ,] mil_dep_pct;

        public vals[] row_vals;
        public vals[,] col_vals;
        public vals[] row_age_vals;
        public vals[] col_age_vals;

        public pop_mil[, , ,] pop_mil_ct;
        public pop_mil[, , ,] pop_mil_sra;

        // popest military or special pop structures
        public PopestSpecialPop[] popestCtSpecialPop;
        public PopestSpecialPop[] popestSraSpecialPop;

        //pop_master class
        public pop_master[,] pop;
        public pop_master[, ,] pop_ct;
        public pop_master[, ,] pop_sra;

        public SpecialPopStruct[] specialPops;
        
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.MainMenu mainMenu1;
        public System.Data.SqlClient.SqlCommand sqlCommand;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Label label2;
        public System.Data.SqlClient.SqlConnection sqlConnection;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRunPASEE;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chkMGRAONLY;
        private System.Windows.Forms.ComboBox txtYear;
        private IContainer components;

        public int fyear = 0;
        public int lyear = 0;

        public pasee()
        {
            InitializeComponent();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }  // end if
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.

        public void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnRunPASEE = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.sqlCommand = new System.Data.SqlClient.SqlCommand();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.sqlConnection = new System.Data.SqlClient.SqlConnection();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.chkMGRAONLY = new System.Windows.Forms.CheckBox();
            this.txtYear = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(109, 152);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(96, 58);
            this.btnExit.TabIndex = 26;
            this.btnExit.Text = "Return";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnRunPASEE
            // 
            this.btnRunPASEE.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRunPASEE.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunPASEE.Location = new System.Drawing.Point(16, 152);
            this.btnRunPASEE.Name = "btnRunPASEE";
            this.btnRunPASEE.Size = new System.Drawing.Size(96, 58);
            this.btnRunPASEE.TabIndex = 30;
            this.btnRunPASEE.Text = "Run ";
            this.btnRunPASEE.UseVisualStyleBackColor = false;
            this.btnRunPASEE.Click += new System.EventHandler(this.btnRunPASEE_Click);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(16, 336);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 16);
            this.label3.TabIndex = 28;
            this.label3.Text = "Status";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(16, 216);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(622, 112);
            this.txtStatus.TabIndex = 27;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(88, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(152, 32);
            this.label2.TabIndex = 25;
            this.label2.Text = "Estimates Year";
            // 
            // sqlConnection
            // 
            this.sqlConnection.ConnectionString = "Data Source=PILA\\SDGINTDB;Initial Catalog=concep_test;Persist Security Info=True;" +
    "User ID=concep_app;Password=c0nc3p_@pp";
            this.sqlConnection.FireInfoMessageEventOnUserErrors = false;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 32);
            this.label1.TabIndex = 31;
            this.label1.Text = "PASEE";
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(168, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(470, 24);
            this.label4.TabIndex = 32;
            this.label4.Text = "Demographic Characteristics Estimates Model";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(24, 16);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(112, 40);
            this.panel1.TabIndex = 33;
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Location = new System.Drawing.Point(144, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(516, 24);
            this.label5.TabIndex = 34;
            this.label5.Text = "PROGRAM FOR AGE, SEX && ETHNICITY ESTIMATES";
            // 
            // chkMGRAONLY
            // 
            this.chkMGRAONLY.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkMGRAONLY.Location = new System.Drawing.Point(16, 112);
            this.chkMGRAONLY.Name = "chkMGRAONLY";
            this.chkMGRAONLY.Size = new System.Drawing.Size(171, 24);
            this.chkMGRAONLY.TabIndex = 35;
            this.chkMGRAONLY.Text = "Do MGRAs Only";
            // 
            // txtYear
            // 
            this.txtYear.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtYear.Items.AddRange(new object[] {
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
            this.txtYear.Location = new System.Drawing.Point(16, 80);
            this.txtYear.Name = "txtYear";
            this.txtYear.Size = new System.Drawing.Size(64, 31);
            this.txtYear.TabIndex = 37;
            // 
            // pasee
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(664, 385);
            this.Controls.Add(this.txtYear);
            this.Controls.Add(this.chkMGRAONLY);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnRunPASEE);
            this.Controls.Add(this.panel1);
            this.Menu = this.mainMenu1;
            this.Name = "pasee";
            this.Text = "CONCEP Version 4 - PASEE";
            this.Load += new System.EventHandler(this.pasee_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region Run button event handler

        /// <summary>
        /// method invoker for run button - starts another thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRunPASEE_Click(object sender, System.EventArgs e)
        {
            // build the table names from runtime args
            processParams(txtYear.SelectedItem.ToString(), ref fyear, ref lyear);
            ct_list = new int[NUM_CTS, 2];

            mig_rates = new double[NUM_ETH, NUM_SEX, NUM_AGE];
            mig_dist = new double[NUM_ETH, NUM_SEX];
            mil_pct = new double[NUM_ETH, NUM_SEX, NUM_AGE];
            mil_dep_pct = new double[NUM_ETH, NUM_SEX, NUM_AGE];

            row_vals = new vals[NUM_SRA + 1];
            col_vals = new vals[NUM_ETH, NUM_SEX];
            row_age_vals = new vals[NUM_SRA + 1];
            col_age_vals = new vals[NUM_AGE];

            pop_mil_ct = new pop_mil[NUM_CTS + 1, NUM_ETH, NUM_SEX, NUM_AGE];
            pop_mil_sra = new pop_mil[NUM_SRA + 1, NUM_ETH, NUM_SEX, NUM_AGE];

            // popest military or special pop structures
            popestCtSpecialPop = new PopestSpecialPop[NUM_CTS];
            popestSraSpecialPop = new PopestSpecialPop[NUM_SRA + 1];

            //pop_master class
            pop = new pop_master[NUM_ETH, NUM_SEX];
            pop_ct = new pop_master[NUM_CTS + 1, NUM_ETH, NUM_SEX];
            pop_sra = new pop_master[NUM_SRA + 1, NUM_ETH, NUM_SEX];

            pasee_update_0_proc = config.AppSettings.Settings["spPaseeUpdate0"].Value;

            ct_stored_proc = config.AppSettings.Settings["spPopulateCTTab"].Value;
            regional_estimates_file = networkPath + "reg_est_out_" + fyear;
            ct_estimates_file = networkPath + "ct_est_out_" + fyear;
            sra_estimates_file = networkPath + "sra_est_out_" + fyear;

            fyear = int.Parse(txtYear.SelectedItem.ToString());
            lyear = fyear - 1;
            basep = new int[NUM_AGE];
            // build the table names from runtime args

            MethodInvoker mi = new MethodInvoker(beginPASEEWork);
            mi.BeginInvoke(null, null);
        } // end btnRunPasee_Click()

        //*******************************************************************************************

        private void beginPASEEWork()
        {
            pasee P = new pasee();
            //initialize the SQL command object
            WriteToStatusBox("Initializing Connection");
            sqlCommand = new SqlCommand();
            //initialize the connection
            sqlCommand.Connection = sqlConnection;
            WriteToStatusBox("Building Table Names");
            WriteToStatusBox("EXTRACTING CT LIST");

            // extract ct list
            ExtractCTList(ct_list);

            if (!chkMGRAONLY.Checked)
            {
                //initialize the pop_master class structures
                WriteToStatusBox("INITIALIZING POP MASTER STRUCTURES");

                ExtractMILCTList();
                InitializePopMasters();
                WriteToStatusBox("FILLING POPEST ARRAYS");

                // extract popest data
                ExtractPOPEST();

                WriteToStatusBox("FILLING MIGRATION PARMS");

                // extract migration parms
                ExtractRegMigDist();

                WriteToStatusBox("EXTRACTING BIRTHS AND DEATHS");

                // births and deaths
                ExtractDeaths();
                ExtractBirths();

                WriteToStatusBox("EXTRACTING MILITARY POP CHARACTERISTICS");

                /* extract military pop */
                ExtractMil();

                WriteToStatusBox("EXTRACTING BASE-YEAR POP");

                /* base year population data  - returns base year pop adjusted for deaths and military*/
                ExtractPop();

                WriteToStatusBox("PERFORMING POP ADJUSTMENTS");

                // adjust base pop of SRAs, CTs for births, deaths
                PC.AdjustPop(this);
                WriteToStatusBox("SURVIVING POP");
                
                // survive regional totals of pop for each eth, sex
                PC.SurvivePop(this, lyear);

                WriteToStatusBox("SPECIAL POP ESTIMATE");

                PC.SpecialEstimateMain(this);

                WriteToStatusBox("DERIVING NET MIG");

                PC.NetMig(this);

                WriteToStatusBox("POP ESTIMATE MAIN");

                PC.PopEstimate(this);
                DebugPrint1(1, this);

                WriteToStatusBox("Computing SRA Survived Population");
                
                // get SRA totals of survived pop by single year of age
                PC.SurvivePopSRA(this, lyear);

                WriteToStatusBox("SRA Estimates");

                PC.PopEstimateSRA(this);
                DebugPrint1(2, this);

                WriteToStatusBox("CT Estimates");

                PC.PopEstimateCt(this, lyear);
                DebugPrint1(3, this);

                WriteTable(this);
            }     // end if chkmgraONLY.Checked

            WriteToStatusBox("Performing mgra13 Estimates");

            peg.PaseeMGRAMain(this);

            WriteToStatusBox("COMPLETED ESTIMATES");
            MessageBox.Show("COMPLETED ESTIMATES - SELECT OK TO CLOSE PASEE AND RESET ARRAYS");

        }  // end procedure BeginPASEEWork

        //*************************************************************************************

        /* processParams() */
        /// Build the table names from runtime parms

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------

        public void processParams(string year,ref int fyear, ref int lyear)
        {
            fyear = int.Parse(year);
            lyear = fyear - 1;

            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                appSettings = config.AppSettings.Settings;
                connectionStrings = config.ConnectionStrings.ConnectionStrings;

                networkPath = String.Format(appSettings["networkPath"].Value);
                MAX_ASE= int.Parse(appSettings["MAX_ASE"].Value);
                MAX_ASE5 = int.Parse(appSettings["MAX_ASE5"].Value);
                MAX_CTS_IN_SRA= int.Parse(appSettings["MAX_CTS_IN_SRA"].Value);
                MAX_ELEM = int.Parse(appSettings["MAX_ELEM"].Value);
                NUM_AGE= int.Parse(appSettings["NUM_AGE"].Value); 
                NUM_AGE5 = int.Parse(appSettings["NUM_AGE5"].Value);
                NUM_CTS = int.Parse(appSettings["NUM_CTS"].Value);
                NUM_ETH= int.Parse(appSettings["NUM_ETH"].Value);
                NUM_SEX = int.Parse(appSettings["NUM_SEX"].Value);
                NUM_SRA = int.Parse(appSettings["NUM_SRA"].Value);
               
                sqlConnection.ConnectionString = connectionStrings["ConcepDBConnectionString"].ConnectionString;
                this.sqlCommand.Connection = this.sqlConnection;

                TN.age5Lookup = String.Format(appSettings["age5Lookup"].Value);
                TN.birthsCT = String.Format(appSettings["birthsCT"].Value);
                TN.deathsCT = String.Format(appSettings["deathsCT"].Value);
                TN.migrationRates = String.Format(appSettings["migrationRates"].Value);
                TN.migrationDistribution = String.Format(appSettings["migrationDistribution"].Value);
                TN.mil_dep_pct = String.Format(appSettings["mil_dep_pct"].Value);
                TN.mil_pct = String.Format(appSettings["mil_pct"].Value);
                TN.paseeUpdateCT = String.Format(appSettings["paseeUpdateCT"].Value);
                TN.popEstimatesCT = String.Format(appSettings["popEstimatesCT"].Value);
                TN.popEstimatesTabCT = String.Format(appSettings["popEstimatesTabCT"].Value);
                TN.popEstimatesTabMGRA = String.Format(appSettings["popEstimatesTabMGRA"].Value);
                
                TN.popestMGRA = String.Format(appSettings["popestMGRA"].Value);
                TN.specialPopTracts = String.Format(appSettings["specialPopTracts"].Value);
                TN.xref = String.Format(appSettings["xref"].Value);

            }  // end try

            catch (ConfigurationErrorsException c)
            {
                throw c;
            }

        }  // end procedure processParams()

        #endregion

        //***********************************************************************************

        #region Miscellaneous utilities
        // procedures included in this region

        // DoMilBasepTotals - Compute aggregates for military population structures - base year
        // DoMilEstTotals - Compute aggregates for military population structures - estimates year  
        // DoStructTotals - Derive various structure totals 
        // DoStructTotals (overloaded)

        // GetCtIndex - Determine index of Ct  
        // GetSRAIndex - Determine index of SRA   
        // InitializePopMasters - Instantiate the principal pasee classes

        // processParams - Build the table names from runtime selections
        // WriteTable - write the new table to ascii and bulk load
        // WriteToStatusBox - Display the current processing status to the form

        //*****************************************************************************

        /* DoMilBasepTotals()*/

        /// Compute aggregates for military population structures base year

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void DoMilBasepTotals(int which, pop_mil[, , ,] a, int g, int last)
        {
            int i, j, k;
            try
            {

                for (i = 1; i < NUM_ETH; ++i)
                {
                    for (j = 1; j < NUM_SEX; ++j)
                    {
                        for (k = 0; k < NUM_AGE; ++k)
                        {
                            a[g, 0, j, k].b_mil_gen += a[g, i, j, k].b_mil_gen;
                            a[g, i, 0, k].b_mil_gen += a[g, i, j, k].b_mil_gen;
                            a[g, 0, 0, k].b_mil_gen += a[g, i, j, k].b_mil_gen;

                            a[last, i, j, k].b_mil_gen += a[g, i, j, k].b_mil_gen;     /* regional total */
                            a[last, 0, j, k].b_mil_gen += a[g, i, j, k].b_mil_gen;     /* regional total */
                            a[last, i, 0, k].b_mil_gen += a[g, i, j, k].b_mil_gen;     /* regional total */
                            a[last, 0, 0, k].b_mil_gen += a[g, i, j, k].b_mil_gen;     /* regional total */

                            a[g, 0, j, k].b_mil_bases += a[g, i, j, k].b_mil_bases;
                            a[g, i, 0, k].b_mil_bases += a[g, i, j, k].b_mil_bases;
                            a[g, 0, 0, k].b_mil_bases += a[g, i, j, k].b_mil_bases;

                            a[last, i, j, k].b_mil_bases += a[g, i, j, k].b_mil_bases;   /* regional total */
                            a[last, i, 0, k].b_mil_bases += a[g, i, j, k].b_mil_bases;   /* regional total */
                            a[last, 0, j, k].b_mil_bases += a[g, i, j, k].b_mil_bases;   /* regional total */
                            a[last, 0, 0, k].b_mil_bases += a[g, i, j, k].b_mil_bases;   /* regional total */

                            if (which == 2)
                            {
                                pop_sra[g, 0, j].b_mil_totals += a[g, i, j, k].b_mil_gen +
                                    a[g, i, j, k].b_mil_bases;
                                pop_sra[g, i, 0].b_mil_totals += a[g, i, j, k].b_mil_gen +
                                    a[g, i, j, k].b_mil_bases;
                                pop_sra[g, 0, 0].b_mil_totals += a[g, i, j, k].b_mil_gen +
                                    a[g, i, j, k].b_mil_bases;
                                pop_sra[g, i, j].b_mil_totals += a[g, i, j, k].b_mil_gen +
                                    a[g, i, j, k].b_mil_bases;
                            }  // end if
                        }  // end for k
                    }  // end for j
                }  // end for i
            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
        }  // end procedure DOMilBasepTotals()

        //**************************************************************************

        //DoMilEStTotals()

        // compute aggregates for military population structures agrument is one element of
        //  a pop_mil structure either ct or sra

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void DoMilEstTotals(byte which, pop_mil[, , ,] a, int g, int last)
        {
            int i, j, k;

            for (i = 1; i < NUM_ETH; ++i)
            {
                for (j = 1; j < NUM_SEX; ++j)
                {
                    for (k = 0; k < NUM_AGE; ++k)
                    {
                        a[g, 0, j, k].e_mil_gen += a[g, i, j, k].e_mil_gen;
                        a[g, i, 0, k].e_mil_gen += a[g, i, j, k].e_mil_gen;
                        a[g, 0, 0, k].e_mil_gen += a[g, i, j, k].e_mil_gen;
                        a[last, i, j, k].e_mil_gen += a[g, i, j, k].e_mil_gen;     /* regional total */
                        a[last, 0, j, k].e_mil_gen += a[g, i, j, k].e_mil_gen;     /* regional total */
                        a[last, i, 0, k].e_mil_gen += a[g, i, j, k].e_mil_gen;     /* regional total */
                        a[last, 0, 0, k].e_mil_gen += a[g, i, j, k].e_mil_gen;     /* regional total */

                        a[g, 0, j, k].e_mil_bases += a[g, i, j, k].e_mil_bases;
                        a[g, i, 0, k].e_mil_bases += a[g, i, j, k].e_mil_bases;
                        a[g, 0, 0, k].e_mil_bases += a[g, i, j, k].e_mil_bases;
                        a[last, i, j, k].e_mil_bases += a[g, i, j, k].e_mil_bases;   /* regional total */
                        a[last, i, 0, k].e_mil_bases += a[g, i, j, k].e_mil_bases;   /* regional total */
                        a[last, 0, j, k].e_mil_bases += a[g, i, j, k].e_mil_bases;   /* regional total */
                        a[last, 0, 0, k].e_mil_bases += a[g, i, j, k].e_mil_bases;   /* regional total */

                        a[g, 0, j, k].est += a[g, i, j, k].est;
                        a[g, i, 0, k].est += a[g, i, j, k].est;
                        a[g, 0, 0, k].est += a[g, i, j, k].est;
                        a[last, i, j, k].est += a[g, i, j, k].est;   /* regional total */
                        a[last, 0, j, k].est += a[g, i, j, k].est;   /* regional total */
                        a[last, i, 0, k].est += a[g, i, j, k].est;   /* regional total */
                        a[last, 0, 0, k].est += a[g, i, j, k].est;   /* regional total */

                        if (which == 1)     /* is this the ct switch */
                        {
                            pop[i, j].e_mil_totals += a[g, i, j, k].est;

                            pop[0, 0].e_mil_totals += a[g, i, j, k].est;
                            pop[i, 0].e_mil_totals += a[g, i, j, k].est;
                            pop[0, j].e_mil_totals += a[g, i, j, k].est;

                            pop_ct[g, i, j].e_mil_totals += a[g, i, j, k].est;
                            pop_ct[g, 0, 0].e_mil_totals += a[g, i, j, k].est;
                            pop_ct[g, i, 0].e_mil_totals += a[g, i, j, k].est;
                            pop_ct[g, 0, j].e_mil_totals += a[g, i, j, k].est;
                        }  // end if

                        else     /* otherwise, do the sra's */
                        {
                            pop_sra[g, i, j].e_mil_totals += a[g, i, j, k].est;

                            pop_sra[g, 0, 0].e_mil_totals += a[g, i, j, k].est;
                            pop_sra[g, i, 0].e_mil_totals += a[g, i, j, k].est;
                            pop_sra[g, 0, j].e_mil_totals += a[g, i, j, k].est;
                        }  // end else
                    }  // end for k
                }  // end for j
            }  // end for i
        }  // end procedure DoMilEstTotals()

        // ********************************************************************************************

        /// Compute structure totals

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void DoStructTotals(pop_master[, ,] a, int h, int sw)
        {
            int i, j, k;
            for (i = 1; i < NUM_ETH; ++i)
            {
                for (j = 1; j < NUM_SEX; ++j)
                {
                    for (k = 0; k < NUM_AGE; ++k)
                    {
                        switch (sw)
                        {
                            case 1:   /* base totals */
                                a[h, 0, 0].basep[k] += a[h, i, j].basep[k];
                                a[h, 0, j].basep[k] += a[h, i, j].basep[k];
                                a[h, i, 0].basep[k] += a[h, i, j].basep[k];
                                a[h, 0, 0].basep_totals += a[h, i, j].basep[k];
                                a[h, 0, j].basep_totals += a[h, i, j].basep[k];
                                a[h, i, 0].basep_totals += a[h, i, j].basep[k];
                                a[h, i, j].basep_totals += a[h, i, j].basep[k];
                                break;

                            case 2:   /* adjusted pop totals */
                                a[h, 0, 0].basep_adj[k] += a[h, i, j].basep_adj[k];
                                a[h, 0, j].basep_adj[k] += a[h, i, j].basep_adj[k];
                                a[h, i, 0].basep_adj[k] += a[h, i, j].basep_adj[k];
                                a[h, 0, 0].basep_adj_totals += a[h, i, j].basep_adj[k];
                                a[h, 0, j].basep_adj_totals += a[h, i, j].basep_adj[k];
                                a[h, i, 0].basep_adj_totals += a[h, i, j].basep_adj[k];
                                a[h, i, j].basep_adj_totals += a[h, i, j].basep_adj[k];
                                break;

                            case 3:   // nmil est totals 
                                a[h, 0, 0].e_nmil[k] += a[h, i, j].e_nmil[k];
                                a[h, 0, j].e_nmil[k] += a[h, i, j].e_nmil[k];
                                a[h, i, 0].e_nmil[k] += a[h, i, j].e_nmil[k];
                                a[h, 0, 0].e_nmil_totals += a[h, i, j].e_nmil[k];
                                a[h, 0, j].e_nmil_totals += a[h, i, j].e_nmil[k];
                                a[h, i, 0].e_nmil_totals += a[h, i, j].e_nmil[k];
                                a[h, i, j].e_nmil_totals += a[h, i, j].e_nmil[k];
                                break;

                            case 4:   // estimate totals 
                                a[h, 0, 0].est[k] += a[h, i, j].est[k];
                                a[h, 0, j].est[k] += a[h, i, j].est[k];
                                a[h, i, 0].est[k] += a[h, i, j].est[k];
                                a[h, 0, 0].est_totals += a[h, i, j].est[k];
                                a[h, 0, j].est_totals += a[h, i, j].est[k];
                                a[h, i, 0].est_totals += a[h, i, j].est[k];
                                a[h, i, j].est_totals += a[h, i, j].est[k];
                                break;

                            case 5:   // survived pop totals 
                                a[h, 0, 0].surv[k] += a[h, i, j].surv[k];
                                a[h, 0, j].surv[k] += a[h, i, j].surv[k];
                                a[h, i, 0].surv[k] += a[h, i, j].surv[k];
                                a[h, 0, 0].surv_totals += a[h, i, j].surv[k];
                                a[h, 0, j].surv_totals += a[h, i, j].surv[k];
                                a[h, i, 0].surv_totals += a[h, i, j].surv[k];
                                a[h, i, j].surv_totals += a[h, i, j].surv[k];
                                break;

                            case 6:   // netmig totals 
                                a[h, 0, 0].netmig[k] += a[h, i, j].netmig[k];
                                a[h, 0, j].netmig[k] += a[h, i, j].netmig[k];
                                a[h, i, 0].netmig[k] += a[h, i, j].netmig[k];
                                a[h, 0, 0].netmig_tot += a[h, i, j].netmig[k];
                                a[h, 0, j].netmig_tot += a[h, i, j].netmig[k];
                                a[h, i, 0].netmig_tot += a[h, i, j].netmig[k];
                                a[h, i, j].netmig_tot += a[h, i, j].netmig[k];
                                break;
                        }  //end switch
                    }  // end for k
                }  // end for j
            }  // end for i
        }  // end procedure DoStructTotals (0 overloaded

        //*************************************************************************

        // procedure DoStructTotals()
        // Compute structure totals 

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void DoStructTotals(pop_master[,] a, int sw)
        {
            int i, j, k;

            for (i = 1; i < NUM_ETH; ++i)
            {
                for (j = 1; j < NUM_SEX; ++j)
                {
                    for (k = 0; k < NUM_AGE; ++k)
                    {
                        switch (sw)
                        {
                            case 1:   /* base totals */
                                a[0, 0].basep[k] += a[i, j].basep[k];
                                a[0, j].basep[k] += a[i, j].basep[k];
                                a[i, 0].basep[k] += a[i, j].basep[k];
                                a[0, 0].basep_totals += a[i, j].basep[k];
                                a[0, j].basep_totals += a[i, j].basep[k];
                                a[i, 0].basep_totals += a[i, j].basep[k];
                                a[i, j].basep_totals += a[i, j].basep[k];
                                break;

                            case 2:   /* adjusted pop totals */
                                a[0, 0].basep_adj[k] += a[i, j].basep_adj[k];
                                a[0, j].basep_adj[k] += a[i, j].basep_adj[k];
                                a[i, 0].basep_adj[k] += a[i, j].basep_adj[k];
                                a[0, 0].basep_adj_totals += a[i, j].basep_adj[k];
                                a[0, j].basep_adj_totals += a[i, j].basep_adj[k];
                                a[i, 0].basep_adj_totals += a[i, j].basep_adj[k];
                                a[i, j].basep_adj_totals += a[i, j].basep_adj[k];
                                break;

                            case 3:   // nmil est totals 
                                a[0, 0].e_nmil[k] += a[i, j].e_nmil[k];
                                a[0, j].e_nmil[k] += a[i, j].e_nmil[k];
                                a[i, 0].e_nmil[k] += a[i, j].e_nmil[k];
                                a[0, 0].e_nmil_totals += a[i, j].e_nmil[k];
                                a[0, j].e_nmil_totals += a[i, j].e_nmil[k];
                                a[i, 0].e_nmil_totals += a[i, j].e_nmil[k];
                                a[i, j].e_nmil_totals += a[i, j].e_nmil[k];
                                break;

                            case 4:   // estimate totals 
                                a[0, 0].est[k] += a[i, j].est[k];
                                a[0, j].est[k] += a[i, j].est[k];
                                a[i, 0].est[k] += a[i, j].est[k];
                                a[0, 0].est_totals += a[i, j].est[k];
                                a[0, j].est_totals += a[i, j].est[k];
                                a[i, 0].est_totals += a[i, j].est[k];
                                a[i, j].est_totals += a[i, j].est[k];
                                break;

                            case 5:   // survived pop totals 
                                a[0, 0].surv[k] += a[i, j].surv[k];
                                a[0, j].surv[k] += a[i, j].surv[k];
                                a[i, 0].surv[k] += a[i, j].surv[k];
                                a[0, 0].surv_totals += a[i, j].surv[k];
                                a[0, j].surv_totals += a[i, j].surv[k];
                                a[i, 0].surv_totals += a[i, j].surv[k];
                                a[i, j].surv_totals += a[i, j].surv[k];
                                break;

                            case 6:   // netmig totals 
                                a[0, 0].netmig[k] += a[i, j].netmig[k];
                                a[0, j].netmig[k] += a[i, j].netmig[k];
                                a[i, 0].netmig[k] += a[i, j].netmig[k];
                                a[0, 0].netmig_tot += a[i, j].netmig[k];
                                a[0, j].netmig_tot += a[i, j].netmig[k];
                                a[i, 0].netmig_tot += a[i, j].netmig[k];
                                a[i, j].netmig_tot += a[i, j].netmig[k];
                                break;
                        }   // end switch
                    }  // end for k
                }  // end for j
            }  // end for i
        }  // end procedure doStructTotals

        //********************************************************************************************

        /* GetCtIndex() */
        // Determine the index inthe ct_list array of this ct_id

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public int GetCtIndex(int ct_id)
        {
            int i;
            int ret;
            //-------------------------------------------------------------------------
            ret = 999;
            for (i = 0; i < NUM_CTS; ++i)
            {
                if (ct_id == ct_list[i, 0])
                {
                    ret = i;
                    break;
                }     /* end if */
            }     /* end for */

            return (ret);

        }     // end GetCtIndex()

        //*****************************************************************************

        /* GetSRAIndex() */
        /// Locate sra in list and return index

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public int GetSRAIndex(int sra_id)
        {
            int i, ret;
            //-------------------------------------------------------------------------
            ret = 999;
            for (i = 0; i < NUM_SRA; ++i)
            {
                if (sra_id == sra_list[i])
                {
                    ret = i;
                    break;
                }     /* end if */
            }     /* end for */

            return (ret);

        }     // end GetSRAIndex()

        //************************************************************************************************

        /* InitializePopMasters() */

        /// Instantiate a bunch of structures

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void InitializePopMasters()
        {
            int i, j, k, l;
            for (j = 0; j < NUM_ETH; ++j)
            {
                for (k = 0; k < NUM_SEX; ++k)
                {
                    pop[j, k] = new pop_master();
                    pop[j, k].basep = new int[NUM_AGE];
                    pop[j, k].basep_adj = new int[NUM_AGE];
                    for (i = 0; i < NUM_CTS; ++i)
                    {
                        pop_ct[i, j, k] = new pop_master();
                        pop_ct[i, j, k].basep = new int[NUM_AGE];
                        pop_ct[i, j, k].basep_adj = new int[NUM_AGE];
                    }// end for i

                    for (i = 0; i < NUM_SRA + 1; ++i)
                    {
                        pop_sra[i, j, k] = new pop_master();
                        pop_sra[i, j, k].basep = new int[NUM_AGE];
                        pop_sra[i, j, k].basep_adj = new int[NUM_AGE];
                        for (l = 0; l < NUM_AGE; ++l)
                        {
                            pop_mil_sra[i, j, k, l] = new pop_mil();
                        }  // end for l
                    }  // end for i

                    for (i = 0; i < NUM_CTS + 1; ++i)
                    {
                        for (l = 0; l < NUM_AGE; ++l)
                        {
                            pop_mil_ct[i, j, k, l] = new pop_mil();

                        }  // end for l
                    }  // end for i
                }  // end for k
            }  // end for j

            for (i = 0; i < NUM_CTS; ++i)
            {
                popestCtSpecialPop[i] = new PopestSpecialPop();
                if (i < NUM_SRA)
                    popestSraSpecialPop[i] = new PopestSpecialPop();
            }  // end for i

        }  // end procedure InitializePOPMasters()

        //*****************************************************************************************

        /* procedure WriteTable() */

        /// output the new table to ASCII and bulk load


        // Revision History
        //	STR            Date        By   Description
        //	-------------------------------------------------------------------------
        //					01/25/95    tb   initial coding
        //	-------------------------------------------------------------------------
        //
        //  ---------------------------------------------------------------------------*/

        public void WriteTable(pasee P)
        {
            int g, i, j, k, pindex;
            int[] temp_ct = new int[2728];
          
            string str;
            /*-------------------------------------------------------------------------*/

            WriteToStatusBox("WRITING NEW CT TABLE");
           
            /* check the existance of the output table */

            sqlCommand.CommandTimeout = 360;
            
            WriteToStatusBox("DELETING PREVIOUS RUNS FROM DATABASE TABLE");
            sqlCommand.CommandText = string.Format(appSettings["deleteFrom"].Value, TN.popEstimatesCT, fyear);
           
            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

            sqlCommand.CommandText = string.Format(appSettings["truncate"].Value, TN.paseeUpdateCT);
           
            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }
            
            string filePath = networkPath + "est_out";
            /* open a temp ascii file for output */
            // open output file

            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(filePath, false);
            foutw.AutoFlush = true;

            /* for large files, the bulk loading sequence below is faster than SQL inserts */
            /* write the temp output file  for bulk copy*/
            for (g = 0; g < num_ct; ++g)
            {
                WriteToStatusBox("   Processing ct " + (g + 1).ToString());
                /* build a temp array for Pachinko */
                pindex = 0;
                for (i = 1; i < NUM_ETH; ++i)
                {
                    for (j = 1; j < NUM_SEX; ++j)
                    {
                        for (k = 0; k < NUM_AGE; ++k)
                        {
                            temp_ct[pindex++] = P.pop_ct[g, i, j].est[k];
                        }  // end for k
                    }  // end for j
                }  // end for i

                int ret = CU.cUtil.PachinkoNoMaster(pop_ct[g, 0, 0].popest_est_totals, temp_ct, NUM_AGE * NUM_SEX * NUM_ETH);
                if (ret >= 40000)
                {
                    MessageBox.Show("Pachinko did not resolve difference in 40000 iterations");
                }     /* end if */

                /* restore temp array to original struct */
                pindex = 0;
                for (i = 1; i < NUM_ETH; ++i)
                {
                    for (j = 1; j < NUM_SEX; ++j)
                    {
                        for (k = 0; k < NUM_AGE; ++k)
                        {
                            P.pop_ct[g, i, j].est[k] = temp_ct[pindex++];
                        }  // end for k
                    }  // end for j
                }  // end for i

                for (i = 1; i < NUM_ETH; ++i)
                {
                    for (j = 1; j < NUM_SEX; ++j)
                    {
                        for (k = 0; k < NUM_AGE; ++k)
                        {
                            str = ct_list[g, 1] + "," + ct_list[g, 0] + "," + i + "," + j + "," + k + "," + P.pop_ct[g, i, j].est[k];
                            try
                            {
                                foutw.WriteLine(str);
                            }
                            catch (IOException exc)      //exceptions here
                            {
                                MessageBox.Show(exc.Message + " File Write Error");
                                return;
                            }
                        }  // end for k
                    }  // end for j
                }  // end for i

            } // end for g

            foutw.Close();
            WriteToStatusBox("BULK LOADING ESTIMATES CT TABLE");

            sqlCommand.CommandTimeout = 180;
            sqlCommand.CommandText = string.Format(appSettings["bulkInsert"].Value, TN.paseeUpdateCT,filePath);
            //sqlCommand.CommandText = "bulk insert pasee_update_ct" + " from '" + filePath + "' " + " with (fieldterminator = ',', firstrow = 1)";
            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }
            sqlCommand.CommandText = string.Format(appSettings["insertInto"].Value, TN.popEstimatesCT, " SELECT " + fyear + ", sra, ct10, ethnicity, sex, age, pop FROM " + TN.paseeUpdateCT);
           
            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                throw exc;
            }
            finally
            {
                sqlConnection.Close();
            }


            // populate the tabular format table - execute stored procedure
            WriteToStatusBox("BUILDING ESTIMATES CT TABULAR TABLE");
            this.sqlCommand.CommandText = "execute " + ct_stored_proc + " '" + TN.popEstimatesTabCT + "', '" + TN.popEstimatesCT + "', 'xref_mgra_sr13', " + fyear;

            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

            sqlCommand.CommandText = "EXECUTE " + pasee_update_0_proc + " " + fyear + ", '" + TN.popEstimatesTabCT + "', 'ct10'";
            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }
        }  // end procedure WriteTable()

        //*****************************************************************************

        /* WriteToStatusBox() */

        // Display the current processing status to the form

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void WriteToStatusBox(string status)
        {
            /* If we are running this method from primary thread, no marshalling is
              * needed. */
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

        #endregion

        #region PASEE data extractions

        /// procedures included in this region
        /// ExtractCTList - Populate Census Tract/SRA lookup table and mil_ct table
        /// ExtractMil - Extract military population distribution
        /// ExtractMilCTList = Extract the military CT list
        /// ExtractRegMigDist - Extract regional migration distribution
        /// ExtractPop - Extract base-year population
        /// ExtractPOPEST - Extract POPEST base and estimates year data
        /// ExtractBirths - Extract births
        /// ExtractDeaths - Extract deaths 
        //--------------------------------------------------------------------------------------

        /* ExtractCTList() */

        /// Populate Census Tract/SRA lookup table and mil_ct table

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void ExtractCTList(int[,] ct_list)
        {
            System.Data.SqlClient.SqlDataReader rdr;

            num_ct = 0;
            WriteToStatusBox("EXTRACTING CENSUS TRACT LIST");
            sqlCommand.CommandText = String.Format(appSettings["selectPASEE1"].Value, TN.xref);
            try
            {
                sqlConnection.Open();

                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    ct_list[num_ct, 1] = (int)rdr.GetByte(0);
                    ct_list[num_ct++, 0] = rdr.GetInt32(1);
                }  // end while
                rdr.Close();

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }
        }  // end procedure ExtractCTList()

        //***************************************************************************************

        /* ExtractMil() */

        // Extract Military population distribution
        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void ExtractMil()
        {
            System.Data.SqlClient.SqlDataReader rdr;
            //----------------------------------------------------------------------
            // open the connection

            WriteToStatusBox("EXTRACTING BASE YEAR MILITARY DISTRIBUTION");

            // build command for base year distribution by age, sex and ethnicity
            sqlCommand.CommandText = string.Format(appSettings["selectAllWhere"].Value, TN.mil_pct, fyear);
            //this.sqlCommand.CommandText = "select ethnicity,sex,age,pct from " + TN.mil_pct;
            try
            {
                sqlConnection.Open();

                rdr = this.sqlCommand.ExecuteReader();

                while (rdr.Read())
                {
                    mil_pct[(int)rdr.GetByte(1), (int)rdr.GetByte(2), (int)rdr.GetByte(3)] = rdr.GetDouble(4); // skip year id
                }  // end while

                rdr.Close();

                WriteToStatusBox("EXTRACTING BASE YEAR MILITARY DEPENDENT DISTRIBUTION");
                // this is 2000 distribution - skip year id
                // build command for base year dependent distribution by age, sex and ethnicity
                sqlCommand.CommandText = string.Format(appSettings["selectAll"].Value, TN.mil_dep_pct);
                //this.sqlCommand.CommandText = "select ethnicity,sex,age,pct from " + TN.mil_dep_pct;
                rdr = this.sqlCommand.ExecuteReader();

                while (rdr.Read())
                {
                    mil_dep_pct[(int)rdr.GetByte(1), (int)rdr.GetByte(2), (int)rdr.GetByte(3)] = rdr.GetDouble(4); // skip year id
                }  // end while
                //close the data reader
                rdr.Close();

            }   // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }   // end catch
            finally
            {
                sqlConnection.Close();
            }

        }     // end ExtractMil()

        //*****************************************************************************

        /* ExtractMILCTList() */

        // Populate  mil_ct table

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void ExtractMILCTList()
        {
            SqlDataReader rdr;
            WriteToStatusBox("EXTRACTING MILITARY (SPECIAL) TRACT LIST  AND PARAMETERS");
            sqlCommand.CommandText = String.Format(appSettings["selectPASEE2"].Value, TN.specialPopTracts);

            try
            {
                sqlConnection.Open();

                specialPops = new SpecialPopStruct[(int)sqlCommand.ExecuteScalar()];
                num_mil_ct = specialPops.Length;

                sqlCommand.CommandText = String.Format(appSettings["selectAll"].Value, TN.specialPopTracts);
                rdr = sqlCommand.ExecuteReader();
                int i = 0;
                while (rdr.Read())
                {
                    specialPops[i] = new SpecialPopStruct();
                    specialPops[i].ct = rdr.GetInt32(0);
                    specialPops[i].basep_code = (int)rdr.GetByte(1);
                    specialPops[i++].sra = (int)rdr.GetByte(2);
                }  // end while
                rdr.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }
        }  // end procedure ExtractMilCTList()

        //*********************************************************************************

        /* ExtractRegMigDist() */

        // Extract regional migration distribution
        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void ExtractRegMigDist()
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int counter = 0;
            //------------------------------------------------------------------------
            WriteToStatusBox("EXTRACTING REGIONAL MIGRATION RATES");
            this.sqlCommand.CommandText = String.Format(appSettings["selectAllWhere"].Value, TN.migrationRates, fyear);
            try
            {
                sqlConnection.Open();

                // build command for base year regional migration rates

                rdr = sqlCommand.ExecuteReader();

                while (rdr.Read())
                {
                    mig_rates[(int)rdr.GetByte(1), (int)rdr.GetByte(2), (int)rdr.GetByte(3)] = rdr.GetDouble(4);
                }  // ens while
                rdr.Close();

                WriteToStatusBox("EXTRACTING REGIONAL MIGRATION DISTRIBUTION");
                // build command for base year regional migration distribution
                this.sqlCommand.CommandText = String.Format(appSettings["selectCountWhere"].Value, TN.migrationDistribution, fyear);
                rdr = this.sqlCommand.ExecuteReader();

                while (rdr.Read())
                    ++counter;
                rdr.Close();
                if (counter == 0)
                {
                    MessageBox.Show("ERROR - NO MIGRATION DISTRIBUTION PARMS FOR THIS ESTIMATES YEAR");

                }  // end if
                else
                {
                    this.sqlCommand.CommandText = String.Format(appSettings["selectAllWhere"].Value, TN.migrationDistribution, fyear);

                    rdr = this.sqlCommand.ExecuteReader();

                    while (rdr.Read())
                    {
                        mig_dist[(int)rdr.GetByte(1), (int)rdr.GetByte(2)] = rdr.GetDouble(3);
                    }  // end while
                    rdr.Close();
                }  // end else

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }
        }  // end procedure ExtractRegMigDist()

        //**********************************************************************************************

        //  ExtractPop()

        // Extract base-year pop

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void ExtractPop()
        {
            int age, ethnic, sex, sra, popin, ct;
            int locct, locsra;
            int i;
            System.Data.SqlClient.SqlDataReader rdr;
            WriteToStatusBox("EXTRACTING BASE YEAR POPULATION");
            sqlCommand.CommandText = String.Format(appSettings["selectPASEE3"].Value, TN.popEstimatesCT, lyear);
            try
            {
                sqlConnection.Open();

                // build command for base year regional migration distribution

                rdr = sqlCommand.ExecuteReader();

                while (rdr.Read())
                {
                    //fill temporaty indexes
                    sra = (int)rdr.GetByte(0);
                    ct = rdr.GetInt32(1);
                    ethnic = (int)rdr.GetByte(2);
                    sex = (int)rdr.GetByte(3);
                    age = (int)rdr.GetByte(4);
                    if (age > 100)
                        age = 100;
                    popin = rdr.GetInt32(5);

                    // add pop to regional total
                    if (pop[ethnic, sex].basep == null)
                        pop[ethnic, sex].basep = new int[NUM_AGE];
                    pop[ethnic, sex].basep[age] += popin;

                    /* process military records */
                    locct = GetCtIndex(ct);
                    if (locct == 999)
                    {
                        MessageBox.Show(" Bad CT number on Extracting Base Year Population for ct = " + ct);
                    }  // end if

                    else
                    {
                        if (pop_ct[locct,ethnic,sex].basep == null)
                            pop_ct[locct,ethnic,sex].basep = new int[NUM_AGE];
                        pop_ct[locct, ethnic, sex].basep[age] += popin;

                        for (i = 0; i < specialPops.Length; ++i)
                        {
                            if (ct == specialPops[i].ct)
                            {
                                pop[ethnic, sex].b_mil[age] += popin;
                                pop[ethnic, 0].b_mil[age] += popin;
                                pop[0, sex].b_mil[age] += popin;
                                pop[0, 0].b_mil[age] += popin;
                                pop[ethnic, sex].b_mil_totals += popin;
                                pop[ethnic, 0].b_mil_totals += popin;
                                pop[0, sex].b_mil_totals += popin;
                                pop[0, 0].b_mil_totals += popin;

                                pop_ct[locct, 0, 0].b_mil_totals += popin;

                                // determine whether or not this is on or off base mil (or special) pop
                                if (specialPops[i].basep_code == 2)
                                    pop_mil_ct[locct, ethnic, sex, age].b_mil_bases += popin;
                                else
                                    pop_mil_ct[locct, ethnic, sex, age].b_mil_gen += popin;
                                break;
                            }  // end if
                        }  // end for i
                    }  // end else

                    locsra = GetSRAIndex(sra);
                    if (pop_sra[locsra, ethnic, sex].basep == null)
                        pop_sra[locsra, ethnic, sex].basep = new int[NUM_AGE];
                    pop_sra[locsra, ethnic, sex].basep[age] += popin;
                    for (i = 0; i < num_mil_ct; ++i)
                    {
                        if (ct == specialPops[i].ct)
                        {
                            if (specialPops[i].basep_code == 2)
                            {
                                pop_mil_sra[locsra, ethnic, sex, age].b_mil_bases += popin;
                                pop_mil_sra[locsra, 0, sex, age].b_mil_bases += popin;
                                pop_mil_sra[locsra, ethnic, 0, age].b_mil_bases += popin;
                                pop_mil_sra[locsra, 0, 0, age].b_mil_bases += popin;
                            }  // end if
                            else
                            {
                                // off base mil or special pop
                                pop_mil_sra[locsra, ethnic, sex, age].b_mil_gen += popin;
                                pop_mil_sra[locsra, 0, sex, age].b_mil_gen += popin;
                                pop_mil_sra[locsra, ethnic, 0, age].b_mil_gen += popin;
                                pop_mil_sra[locsra, 0, 0, age].b_mil_gen += popin;
                            }  // end else
                            // sum to SRA total for special pops
                            pop_sra[locsra, ethnic, sex].b_mil[age] += popin;
                            pop_sra[locsra, 0, sex].b_mil[age] += popin;
                            pop_sra[locsra, ethnic, 0].b_mil[age] += popin;
                            pop_sra[locsra, 0, 0].b_mil[age] += popin;

                            break;
                        }  // end if
                    }  // end for i
                }  // end while
                rdr.Close();

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

            WriteToStatusBox("COMPLETED LOADING BASE YEAR POP DATA");
        }  // end procedure ExtractPop()

        //***************************************************************************************

        /* ExtractPOPEST() */

        /// Extract POPEST base and estimates year data

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void ExtractPOPEST()
        {
            int popin, ct, hhp, gq_mil, sra;
            int i, locct, locsra;
            System.Data.SqlClient.SqlDataReader rdr;

            WriteToStatusBox("EXTRACTING POPEST BASE YEAR");
            sqlCommand.CommandText = String.Format(appSettings["selectPASEE4"].Value, TN.popestMGRA, TN.xref, lyear);
            /* popest base year */
            try
            {
                sqlConnection.Open();

                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    sra = (int)rdr.GetByte(0);
                    ct = rdr.GetInt32(1);
                    popin = rdr.GetInt32(2);
                    hhp = rdr.GetInt32(3);
                    gq_mil = rdr.GetInt32(4);

                    popest_base += popin;
                    locsra = GetSRAIndex(sra);
                    locct = GetCtIndex(ct);     /* get the ct index */
                    if (locct == 999)
                    {
                        MessageBox.Show(" Bad CT number on Extracting POPEST Base Year for ct = " + ct);
                    }  // end if
                    else      /* ct index in range ? */
                    {
                        /* build uniformed military for special bases from group quarters*/
                        for (i = 0; i < num_mil_ct; ++i)
                        {
                            // sum to regional totals for mil and special pop
                            if (ct == specialPops[i].ct)
                            {
                                popest_mil_basep_total += popin;
                                popestCtSpecialPop[locct].baseYearSpecialPop += popin;
                                popestSraSpecialPop[locsra].baseYearSpecialPop += popin;

                                if (specialPops[i].basep_code == 2)     // on base special
                                {
                                    popestCtSpecialPop[locct].b_umil += gq_mil;
                                    popestCtSpecialPop[locct].b_hmil += hhp;

                                    popestSraSpecialPop[locsra].b_umil += gq_mil;
                                    popestSraSpecialPop[locsra].b_hmil += hhp;
                                }  // end if
                                break;
                            }  // end if
                        }  // end for i
                    }  // end else
                }  // end while

                rdr.Close();

                /* popest estimates year */
                WriteToStatusBox("EXTRACTING POPEST ESTIMATE YEAR POP");
                sqlCommand.CommandText = String.Format(appSettings["selectPASEE4"].Value, TN.popestMGRA, TN.xref, fyear);
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    sra = (int)rdr.GetByte(0);
                    ct = rdr.GetInt32(1);
                    popin = rdr.GetInt32(2);
                    hhp = rdr.GetInt32(3);
                    gq_mil = rdr.GetInt32(4);

                    popest_estimate += popin;
                    locct = GetCtIndex(ct);
                    locsra = GetSRAIndex(sra);
                    if (locct == 999)
                    {
                        MessageBox.Show(" Bad CT number on Extracting POPEST Estimates Year Population for ct = " + ct);
                    }  // end if
                    else     /* ct index in range */
                    {
                        /* build uniformed military for special bases from group quarters*/
                        for (i = 0; i < num_mil_ct; ++i)
                        {
                            if (ct == specialPops[i].ct)     /* on-base special */
                            {
                                popestCtSpecialPop[locct].e_mil += popin;
                                popest_mil_est_total += popin;

                                if (specialPops[i].basep_code == 2)
                                {
                                    popestCtSpecialPop[locct].e_umil += gq_mil;
                                    popestCtSpecialPop[locct].e_hmil += hhp;
                                }  // end if
                                pop_ct[locct, 0, 0].popest_mil_est_totals += popin;
                                break;
                            }  // end if
                        }  // end for i
                        pop_ct[locct, 0, 0].est_totals += popin;       /* store ct population estimate */
                        pop_ct[locct, 0, 0].popest_est_totals += popin;
                    }  // end else

                    if (locsra != 999)
                        pop_sra[locsra, 0, 0].est_totals += popin;     /* store sra population estimate */
                }  // end while

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

        }  // end procedure ExtractPopest()

        //**************************************************************************************************

        /* ExtractBirths() */

        /// Extract births

        public void ExtractBirths()
        {

            int ethnic, sex, popin, ct;
            int g, i, j, h, locct;
            System.Data.SqlClient.SqlDataReader rdr;

            WriteToStatusBox("EXTRACTING BIRTHS");
            sqlCommand.CommandText = string.Format(appSettings["selectPASEE7"].Value, TN.birthsCT, lyear);
            //sqlCommand.CommandText = "select ct10,ethnicity,sex,sum(births) from " + TN.birthsCT + " where birth_year = " + lyear + " group by ct10,ethnicity,sex";
            try
            {
                sqlConnection.Open();

                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    ct = (int)rdr.GetInt32(0);
                    ethnic = (int)rdr.GetByte(1);
                    sex = (int)rdr.GetByte(2);
                    popin = rdr.GetInt32(3);

                    locct = GetCtIndex(ct);
                    if (locct != 999)
                        pop_ct[locct, ethnic, sex].births += popin;
                    else
                        MessageBox.Show("BAD CT NUMBER ON BIRTHS RECORD CT = " + ct.ToString());
                }  // end while
                rdr.Close();

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }

            for (i = 1; i < NUM_ETH; ++i)
            {
                for (j = 1; j < NUM_SEX; ++j)
                {
                    /* adjust ct level totals if applicable */
                    for (h = 0; h < NUM_CTS; ++h)
                    {
                        g = GetSRAIndex(ct_list[h, 1]);

                        /* adjust births for infant deaths */
                        //pop_ct[h,i,j].births -= pop_ct[h,i,j].deaths[0];

                        /* aggregate */
                        pop_ct[h, 0, 0].births += pop_ct[h, i, j].births;
                        pop_ct[h, i, 0].births += pop_ct[h, i, j].births;
                        pop_ct[h, 0, j].births += pop_ct[h, i, j].births;

                        /* aggregate to sra */
                        pop_sra[g, i, j].births += pop_ct[h, i, j].births;
                        pop_sra[g, 0, 0].births += pop_ct[h, i, j].births;
                        pop_sra[g, i, 0].births += pop_ct[h, i, j].births;
                        pop_sra[g, 0, j].births += pop_ct[h, i, j].births;

                        /* aggregate to region */
                        pop[i, j].births += pop_ct[h, i, j].births;
                        pop[0, 0].births += pop_ct[h, i, j].births;
                        pop[i, 0].births += pop_ct[h, i, j].births;
                        pop[0, j].births += pop_ct[h, i, j].births;
                    }  // end for h
                }  // end for j
            }  // end for i
            births_total = pop[0, 0].births;
        }  // end procedure ExtractBirths()

        //********************************************************************************

        /* ExtractDeaths() */

        /// Extract deaths

        /// //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   07/15/03   tb   reset calculations for infant deaths
        //   ------------------------------------------------------------------
        public void ExtractDeaths()
        {
            int g, h, i, j, k, locct, ct;
            int ethnic, sex, popin, age;

            System.Data.SqlClient.SqlDataReader rdr;
            //-------------------------------------------------------------------------

            WriteToStatusBox("EXTRACTING DEATHS");
            sqlCommand.CommandText = string.Format(appSettings["selectPASEE8"].Value, TN.deathsCT, lyear);
            //sqlCommand.CommandText = "select ct10,ethnicity,sex,age,deaths from " + TN.deathsCT + " where death_year = " + lyear;
            try
            {
                sqlConnection.Open();

                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    ct = (int)rdr.GetInt32(0);
                    ethnic = (int)rdr.GetByte(1);
                    sex = (int)rdr.GetByte(2);
                    age = (int)rdr.GetByte(3);
                    popin = rdr.GetInt32(4);

                    locct = GetCtIndex(ct);
                    if (locct != 999)
                    {
                        pop_ct[locct, ethnic, sex].deaths[age] += popin;
                    }  // end if
                    else
                        MessageBox.Show("BAD CT NUMBER ON DEATHS RECORD CT = " + ct.ToString());
                }  // end while

                rdr.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }   // end catch
            finally
            {
                sqlConnection.Close();
            }

            for (i = 1; i < NUM_ETH; ++i)
            {
                for (j = 1; j < NUM_SEX; ++j)
                {
                    for (h = 0; h < num_ct; ++h)
                    {
                        g = GetSRAIndex(ct_list[h, 1]);     /* get the sra index */
                        for (k = 0; k < NUM_AGE; ++k)
                        {
                            if (pop_ct[h, i, j].deaths[k] > 0)
                            {
                                pop_ct[h, 0, 0].deaths[k] += pop_ct[h, i, j].deaths[k];
                                pop_ct[h, 0, j].deaths[k] += pop_ct[h, i, j].deaths[k];
                                pop_ct[h, i, 0].deaths[k] += pop_ct[h, i, j].deaths[k];

                                pop_ct[h, i, j].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop_ct[h, 0, 0].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop_ct[h, 0, j].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop_ct[h, i, 0].deaths_totals += pop_ct[h, i, j].deaths[k];

                                /* sum to sra level */
                                pop_sra[g, i, j].deaths[k] += pop_ct[h, i, j].deaths[k];
                                pop_sra[g, 0, 0].deaths[k] += pop_ct[h, i, j].deaths[k];
                                pop_sra[g, 0, j].deaths[k] += pop_ct[h, i, j].deaths[k];
                                pop_sra[g, i, 0].deaths[k] += pop_ct[h, i, j].deaths[k];

                                pop_sra[g, i, j].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop_sra[g, 0, 0].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop_sra[g, 0, j].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop_sra[g, i, 0].deaths_totals += pop_ct[h, i, j].deaths[k];

                                /* sum to region */
                                pop[i, j].deaths[k] += pop_ct[h, i, j].deaths[k];

                                pop[0, 0].deaths[k] += pop_ct[h, i, j].deaths[k];
                                pop[0, j].deaths[k] += pop_ct[h, i, j].deaths[k];
                                pop[i, 0].deaths[k] += pop_ct[h, i, j].deaths[k];

                                pop[i, j].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop[0, 0].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop[0, j].deaths_totals += pop_ct[h, i, j].deaths[k];
                                pop[i, 0].deaths_totals += pop_ct[h, i, j].deaths[k];
                            }  // end if
                        }  // end for k
                    }  // end for h
                }  // end for j
            }  // end for i
            deaths_total = pop[0, 0].deaths_totals;
        }  // end procedure ExtractDeaths()

        //********************************************************************************************

        #endregion

        #region debug print routines
        // procedures included in this region
        // DebugPrint1 - population estimates output


        //*******************************************************************************
        /* DebugPrint1() */
        /// <summary>
        /// Print population estimate to ASCII file for debug
        /// </summary>
        /// <param name="n"><value>switch for output formatting</value></param>
        /// <param name="P"><value>Pasee class</value></param>/>

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/07/03   tb   started initial coding
        //   ------------------------------------------------------------------

        public void DebugPrint1(byte n, pasee P)
        {
            string str;
            int g, i, j, k, dif;
            int[,] temp = new int[NUM_ETH, 2];
            int[] totals = new int[9];
            int chg = 0;
            int[] b1 = new int[9];
            int[] f1 = new int[9];

            int[] diff = new int[9];
            string[] ethnic_id = new string[9] { "", "HISP", "NHW", "NHB", "NHI", "NHA", "NHH", "NHO", "NH2" };
            string[] sex_id = new string[3] { "TOTAL", "MALE", "FEMALE" };
            StreamWriter reg_est_out, sra_est_out, ct_est_out;
            /*--------------------------------------------------------------------------*/
            totals.Initialize();

            /* load migration option label in header */
            switch (n)
            {
                case 1:     /* regional total */
                    try
                    {
                        reg_est_out = new StreamWriter(P.regional_estimates_file);
                    }
                    catch (IOException exc)
                    {
                        Console.WriteLine(exc.Message + " Cannot Open File.");
                        return;
                    }
                    for (i = 0; i < NUM_ETH; ++i)
                    {
                        for (j = 0; j < NUM_SEX; ++j)
                        {
                            str = "";
                            str = " POPULATION ESTIMATE - REGIONAL TOTAL " + "\r\n";

                            str += ethnic_id[i] + " " + sex_id[j] + " ADJ BIRTHS: " + P.pop[i, j].births.ToString();
                            reg_est_out.WriteLine(str);
                            str += "AGE    Base  Deaths BaseMil     Adj    Surv     Mig  P-NMil Pop-Mil   Total Pct Chg";
                            reg_est_out.WriteLine(str);
                            str += "-----------------------------------------------------------------------------------";
                            reg_est_out.WriteLine(str);
                          
                            reg_est_out.Flush();

                            str = "";
                            for (k = 0; k < NUM_AGE; ++k)
                            {
                                str = String.Format("{0,3}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8:F3}",
                                        k, P.pop[i, j].basep[k], P.pop[i, j].deaths[k],
                                        P.pop[i, j].b_mil[k], P.pop[i, j].basep_adj[k],
                                        P.pop[i, j].surv[k], P.pop[i, j].netmig[k], P.pop[i, j].e_nmil[k],
                                        P.pop_mil_ct[num_ct, i, j, k].est, P.pop[i, j].est[k],
                                        ((double)P.pop[i, j].est[k] / (double)P.pop[i, j].basep[k] - 1.0) * 100);
                                reg_est_out.WriteLine(str);
                                reg_est_out.Flush();
                            } /* end for k */

                            str = String.Format("TOT{0,8}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8:F3}",
                                    P.pop[i, j].basep_totals, P.pop[i, j].deaths_totals,
                                    P.pop[i, j].b_mil_totals, P.pop[i, j].basep_adj_totals,
                                    P.pop[i, j].surv_totals, P.pop[i, j].netmig_tot,
                                    P.pop[i, j].e_nmil_totals,
                                    P.pop[i, j].e_mil_totals, P.pop[i, j].est_totals,
                                    ((double)P.pop[i, j].est_totals / (double)P.pop[i, j].basep_totals - 1.0) * 100);
                            reg_est_out.WriteLine(str);
                            reg_est_out.Flush();

                        }     /* end for j */

                    }     /* end for i */
                    reg_est_out.Close();
                    break;     /* end case 1 */

                case 2:     /* sra */
                    try
                    {
                        sra_est_out = new StreamWriter(P.sra_estimates_file);
                    }
                    catch (IOException exc)
                    {
                        Console.WriteLine(exc.Message + " Cannot Open File.");
                        return;
                    }

                    //***************************************************************************************************
                    for (i = 1; i < NUM_ETH; ++i)
                    {
                        for (j = 1; j < NUM_SEX; ++j)
                        {

                            str = "";
                            str = " POPULATION ESTIMATE - SRA 4 " + "\r\n";

                            str += ethnic_id[i] + " " + sex_id[j] + " ADJ BIRTHS: " + P.pop_sra[3, i, j].births.ToString();
                            sra_est_out.WriteLine(str);
                            str += "AGE    Base  Deaths BaseMil     Adj    Surv     Mig  P-NMil Pop-Mil   Total     Chg";
                            sra_est_out.WriteLine(str);
                            str += "-----------------------------------------------------------------------------------";
                            sra_est_out.WriteLine(str);
                            sra_est_out.Write(str);
                            sra_est_out.Flush();

                            str = "";
                            for (k = 0; k < NUM_AGE; ++k)
                            {
                                str = String.Format("{0,3}{1,8}{2,8}{4,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8}",
                                    k, P.pop_sra[4, i, j].basep[k], P.pop_sra[4, i, j].deaths[k],
                                    P.pop_sra[4, i, j].b_mil[k], P.pop_sra[4, i, j].basep_adj[k],
                                    P.pop_sra[4, i, j].surv[k], P.pop_sra[4, i, j].netmig[k], P.pop_sra[4, i, j].e_nmil[k],
                                    P.pop_mil_sra[4, i, j, k].est, P.pop_sra[4, i, j].est[k],
                                    P.pop_sra[4, i, j].est[k] - P.pop_sra[4, i, j].basep[k]);
                                sra_est_out.WriteLine(str);
                                sra_est_out.Flush();
                            } /* end for k */

                            str = String.Format("TOT{0,8}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}",
                                P.pop_sra[4, i, j].basep_totals, P.pop_sra[4, i, j].deaths_totals,
                                P.pop_sra[4, i, j].b_mil_totals, P.pop_sra[4, i, j].basep_adj_totals,
                                P.pop_sra[4, i, j].surv_totals, P.pop_sra[4, i, j].netmig_tot,
                                P.pop_sra[4, i, j].e_nmil_totals,
                                P.pop_sra[4, i, j].e_mil_totals, P.pop_sra[4, i, j].est_totals,
                                P.pop_sra[4, i, j].est_totals - P.pop_sra[4, i, j].basep_totals);
                            sra_est_out.WriteLine(str);
                            sra_est_out.Flush();
                        }
                    }

                    str = "";
                    str = " POPULATION ESTIMATES ALL SRAs ";
                    sra_est_out.WriteLine(str);

                    str += " ADJ BIRTHS: " + P.pop[0, 0].births.ToString();
                    sra_est_out.WriteLine(str);
                    str += "SRA    Base  Deaths BaseMil     Adj    Surv     Mig  P-NMil Pop-Mil   Total     Chg";
                    sra_est_out.WriteLine(str);
                    str += "-----------------------------------------------------------------------------------";
                    sra_est_out.WriteLine(str);
                    sra_est_out.Flush();

                    str = "";
                    for (k = 0; k < NUM_SRA; ++k)
                    {
                        str = String.Format("{0,3}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8}",
                            k, P.pop_sra[k, 0, 0].basep_totals, P.pop_sra[k, 0, 0].deaths_totals,
                            P.pop_sra[k, 0, 0].b_mil_totals, P.pop_sra[k, 0, 0].basep_adj_totals,
                            P.pop_sra[k, 0, 0].surv_totals, P.pop_sra[k, 0, 0].netmig_tot,
                            P.pop_sra[k, 0, 0].e_nmil_totals,
                            P.pop_sra[k, 0, 0].e_mil_totals, P.pop_sra[k, 0, 0].est_totals,
                            P.pop_sra[k, 0, 0].est_totals - P.pop_sra[k, 0, 0].basep_totals);
                        sra_est_out.WriteLine(str);
                        sra_est_out.Flush();
                    } /* end for k */

                    str = String.Format("TOT{0,8}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}",
                        P.pop[0, 0].basep_totals, P.pop[0, 0].deaths_totals,
                        P.pop[0, 0].b_mil_totals, P.pop[0, 0].basep_adj_totals,
                        P.pop[0, 0].surv_totals, P.pop[0, 0].netmig_tot,
                        P.pop[0, 0].e_nmil_totals,
                        P.pop[0, 0].e_mil_totals, P.pop[0, 0].est_totals,
                        P.pop[0, 0].est_totals - P.pop[0, 0].basep_totals);
                    sra_est_out.WriteLine(str);
                    sra_est_out.Flush();

                    str = "";
                    str = " POPULATION ESTIMATES ALL SRAs BLACK - MALES";
                    sra_est_out.WriteLine(str);

                    str += " ADJ BIRTHS: " + P.pop[3, 1].births.ToString();
                    sra_est_out.WriteLine(str);
                    str += "SRA    Base  Deaths BaseMil     Adj    Surv     Mig  P-NMil   Total";
                    sra_est_out.WriteLine(str);
                    str += "-------------------------------------------------------------------";
                    sra_est_out.WriteLine(str);

                    sra_est_out.Flush();

                    str = "";
                    for (k = 0; k < NUM_SRA; ++k)
                    {
                        str = String.Format("{0,3}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}",
                            k, P.pop_sra[k, 3, 1].basep[82], P.pop_sra[k, 3, 1].deaths[82],
                            P.pop_sra[k, 3, 1].b_mil[82], P.pop_sra[k, 3, 1].basep_adj[82],
                            P.pop_sra[k, 3, 1].surv[83], P.pop_sra[k, 3, 1].netmig[83],
                            P.pop_sra[k, 3, 1].e_nmil[83],
                            P.pop_sra[k, 3, 1].est[83]);

                        sra_est_out.WriteLine(str);
                        sra_est_out.Flush();
                    } /* end for k */

                    str = String.Format("TOT{0,8}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}",
                        P.pop[3, 1].basep[83], P.pop[3, 1].deaths[83],
                        P.pop[3, 1].b_mil[83], P.pop[3, 1].basep_adj[83],
                        P.pop[3, 1].surv[83], P.pop[3, 1].netmig[83],
                        P.pop[3, 1].e_nmil[83],
                        P.pop[3, 1].est[83]);

                    sra_est_out.WriteLine(str);
                    sra_est_out.Flush();

                    str = "";
                    str = " POPULATION ESTIMATES ALL SRAs BLACK - FEMALES";
                    sra_est_out.WriteLine(str);

                    str += " ADJ BIRTHS: " + P.pop[3, 1].births.ToString();
                    sra_est_out.WriteLine(str);
                    str += "SRA    Base  Deaths BaseMil     Adj    Surv     Mig  P-NMil   Total     Chg";
                    sra_est_out.WriteLine(str);
                    str += "---------------------------------------------------------------------------";
                    sra_est_out.WriteLine(str);
                    sra_est_out.Flush();

                    str = "";
                    for (k = 0; k < NUM_SRA; ++k)
                    {
                        str = String.Format("{0,3}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}",
                            k, P.pop_sra[k, 3, 2].basep[82], P.pop_sra[k, 3, 2].deaths[82],
                            P.pop_sra[k, 3, 2].b_mil[82], P.pop_sra[k, 3, 2].basep_adj[83],
                            P.pop_sra[k, 3, 2].surv[83], P.pop_sra[k, 3, 2].netmig[83],
                            P.pop_sra[k, 3, 2].e_nmil[83],
                            P.pop_sra[k, 3, 2].est[83]);

                        sra_est_out.WriteLine(str);
                        sra_est_out.Flush();
                    } /* end for k */

                    str = String.Format("TOT{0,8}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}",
                        P.pop[3, 2].basep[83], P.pop[3, 2].deaths[83],
                        P.pop[3, 2].b_mil[83], P.pop[3, 2].basep_adj[83],
                        P.pop[3, 2].surv[83], P.pop[3, 2].netmig[83],
                        P.pop[3, 2].e_nmil[83],
                        P.pop[3, 2].est[83]);

                    sra_est_out.WriteLine(str);
                    sra_est_out.Flush();


                    //***************************************************************************************************

                    str = "";
                    str = "POPULATION ESTIMATES - SRAs";
                    sra_est_out.WriteLine(str);
                    str += "          Hispanic            NonHisp White       NonHisp Black        NonHisp Indian       NonHisp Asian        NonHisp Haw          NonHisp Other        NonHisp Two             Total";
                    sra_est_out.WriteLine(str);
                    str += "SRA   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg     POPEST    DIF";
                    sra_est_out.WriteLine(str);
                    str += "------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------";
                    sra_est_out.WriteLine(str);
                    sra_est_out.Flush();

                    for (g = 0; g < NUM_SRA; ++g)
                    {
                        str = String.Format("{0,3}", sra_list[g]);
                        for (i = 1; i < NUM_ETH; ++i)
                        {
                            if (P.pop_sra[g, i, 0].basep_totals > 0)
                                chg = P.pop_sra[g, i, 0].est_totals - P.pop_sra[g, i, 0].basep_totals;
                            str += String.Format("{0,7}{1,7}{2,7}", P.pop_sra[g, i, 0].basep_totals, P.pop_sra[g, i, 0].est_totals, chg);

                            totals[i - 1] += pop_sra[g, i, 0].est_totals;
                        }     /* end for i */

                        if (pop_sra[g, 0, 0].basep_totals != 0)
                            chg = P.pop_sra[g, 0, 0].est_totals - P.pop_sra[g, 0, 0].basep_totals;
                        else
                            chg = 0;
                        dif = P.pop_sra[g, 0, 0].popest_est_totals - P.pop_sra[g, 0, 0].est_totals;
                        str += String.Format("{0,7}{1,7}{2,7}    {3,7}{4,7}", P.pop_sra[g, 0, 0].basep_totals, P.pop_sra[g, 0, 0].est_totals,
                            chg, P.pop_sra[g, 0, 0].popest_est_totals, dif);

                        sra_est_out.WriteLine(str);
                        sra_est_out.Flush();

                    }     /* end for g */

                    str = "POPULATION ESTIMATES - SRAs";
                    sra_est_out.WriteLine(str);
                    str += "       Base Year Distrib                                                          Fcst Year Distrib                             Change";
                    sra_est_out.WriteLine(str);
                    str += "SRA  Hisp   NHW   NHB   NHI   NHA   NHH   NHO   NH2      Hisp   NHW   NHB   NHI   NHA   NHH   NHO   NH2      Hisp   NHW   NHB   NHI   NHA   NHH   NHO   NH2";
                    sra_est_out.WriteLine(str);
                    str += "-----------------------------------------------------------------------------------------------------------------------------------------------------------";
                    sra_est_out.WriteLine(str);
                    sra_est_out.Flush();

                    for (g = 0; g < NUM_SRA; ++g)
                    {
                        str = String.Format("{0,3}", P.sra_list[g]);
                        for (i = 1; i < NUM_ETH; ++i)
                        {
                            if (P.pop_sra[g, 0, 0].basep_totals != 0)
                                b1[i] = P.pop_sra[g, i, 0].basep_totals;
                            else
                                b1[i] = 0;

                            if (pop_sra[g, 0, 0].est_totals != 0)
                                f1[i] = P.pop_sra[g, i, 0].est_totals;
                            else
                                f1[i] = 0;

                            diff[i] = f1[i] - b1[i];
                        }     /* end for i */

                        for (i = 1; i < NUM_ETH; ++i)
                            str += String.Format("{0,6}", b1[i]);

                        str += "    ";

                        for (i = 1; i < NUM_ETH; ++i)
                            str += String.Format("{0,6}", f1[i]);
                        str += "    ";

                        for (i = 1; i < NUM_ETH; ++i)
                            str += String.Format("{0,6}", diff[i]);
                        sra_est_out.WriteLine(str);

                    }     /* end for g */

                    sra_est_out.Close();

                    break;     /* end case 2 */

                case 3:     /* ct */
                    try
                    {
                        ct_est_out = new StreamWriter(P.ct_estimates_file);
                    }
                    catch (IOException exc)
                    {
                        Console.WriteLine(exc.Message + " Cannot Open File.");
                        return;
                    }

                    //***************************************************************************************************
                    for (g = 0; g < NUM_SRA; ++g)
                    {
                        str = "";
                        str = " CT POPULATION ESTIMATES SRA " + sra_list[g].ToString();
                        ct_est_out.WriteLine(str);

                        str += "CT      Base  Deaths BaseMil   Births     Adj    Surv     Mig  P-NMil Pop-Mil   Total  POPEST";
                        ct_est_out.WriteLine(str);
                        str += "---------------------------------------------------------------------------------------------";
                        ct_est_out.WriteLine(str);
                        ct_est_out.Flush();

                        str = "";
                        for (k = 0; k < NUM_CTS; ++k)
                        {
                            if (ct_list[k, 1] == sra_list[g])
                            {
                                str = String.Format("{0,-5}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8}{11,8}",
                                    ct_list[k, 0], P.pop_ct[k, 0, 0].basep_totals, P.pop_ct[k, 0, 0].deaths_totals,
                                    P.pop_ct[k, 0, 0].b_mil_totals, P.pop_ct[k, 0, 0].births, P.pop_ct[k, 0, 0].basep_adj_totals,
                                    P.pop_ct[k, 0, 0].surv_totals, P.pop_ct[k, 0, 0].netmig_tot,
                                    P.pop_ct[k, 0, 0].e_nmil_totals,
                                    P.pop_ct[k, 0, 0].e_mil_totals, P.pop_ct[k, 0, 0].est_totals,
                                    P.pop_ct[k, 0, 0].popest_est_totals);
                                ct_est_out.WriteLine(str);
                                ct_est_out.Flush();
                            }   // end if
                        } /* end for k */

                        str = String.Format("TOT  {0,8}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8}",
                            P.pop_sra[g, 0, 0].basep_totals, P.pop_sra[g, 0, 0].deaths_totals,
                            P.pop_sra[g, 0, 0].b_mil_totals, P.pop_sra[g, 0, 0].births, P.pop_sra[g, 0, 0].basep_adj_totals,
                            P.pop_sra[g, 0, 0].surv_totals, P.pop_sra[g, 0, 0].netmig_tot,
                            P.pop_sra[g, 0, 0].e_nmil_totals,
                            P.pop_sra[g, 0, 0].e_mil_totals, P.pop_sra[g, 0, 0].est_totals,
                            P.pop_sra[g, 0, 0].popest_est_totals);
                        ct_est_out.WriteLine(str);
                        ct_est_out.Write("\r\n");
                        ct_est_out.Flush();
                    }     // end for g

                    for (i = 1; i < NUM_ETH; ++i)
                    {
                        for (j = 1; j < NUM_SEX; ++j)
                        {

                            str = "";
                            str = " POPULATION ESTIMATE - CT 1 ";

                            str += ethnic_id[i] + " " + sex_id[j] + " ADJ BIRTHS: "
                                + P.pop_ct[0, i, j].births.ToString()
                                + "\r\n";
                            str += "AGE      Base  Deaths BaseMil     Adj    Surv     Mig  P-NMil Pop-Mil   Total     Chg\r\n";
                            str += "-------------------------------------------------------------------------------------\r\n";
                            ct_est_out.Write(str);
                            ct_est_out.Flush();

                            str = "";
                            for (k = 0; k < NUM_AGE; ++k)
                            {
                                str = String.Format("{0,3}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8}",
                                    k, P.pop_ct[0, i, j].basep[k], P.pop_ct[0, i, j].deaths[k],
                                    P.pop_ct[0, i, j].b_mil[k], P.pop_ct[0, i, j].basep_adj[k],
                                    P.pop_ct[0, i, j].surv[k], P.pop_ct[0, i, j].netmig[k], P.pop_ct[0, i, j].e_nmil[k],
                                    P.pop_mil_ct[0, i, j, k].est, P.pop_ct[0, i, j].est[k],
                                    P.pop_ct[0, i, j].est[k] - P.pop_ct[0, i, j].basep[k]);
                                ct_est_out.WriteLine(str);
                                ct_est_out.Flush();
                            } /* end for k */

                            str = String.Format("TOT{0,8}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}",
                                P.pop_ct[0, i, j].basep_totals, P.pop_ct[0, i, j].deaths_totals,
                                P.pop_ct[0, i, j].b_mil_totals, P.pop_ct[0, i, j].basep_adj_totals,
                                P.pop_ct[0, i, j].surv_totals, P.pop_ct[0, i, j].netmig_tot,
                                P.pop_ct[0, i, j].e_nmil_totals,
                                P.pop_ct[0, i, j].e_mil_totals, P.pop_ct[0, i, j].est_totals,
                                P.pop_ct[0, i, j].est_totals - P.pop_ct[0, i, j].basep_totals);
                            ct_est_out.WriteLine(str);
                            ct_est_out.Flush();
                        }
                    }

                    str = "";
                    str = " POPULATION ESTIMATE - CT 1 " + "\r\n";

                    str += ethnic_id[0] + " " + sex_id[0] + " ADJ BIRTHS: "
                        + P.pop_ct[0, 0, 0].births.ToString()
                        + "\r\n";
                    str += "AGE    Base  Deaths BaseMil     Adj    Surv     Mig  P-NMil Pop-Mil   Total     Chg" + "\r\n";
                    str += "-----------------------------------------------------------------------------------" + "\r\n";
                    ct_est_out.Write(str);
                    ct_est_out.Flush();

                    str = "";
                    for (k = 0; k < NUM_AGE; ++k)
                    {
                        str = String.Format("{0,3}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8}",
                            k, P.pop_ct[0, 0, 0].basep[k], P.pop_ct[0, 0, 0].deaths[k],
                            P.pop_ct[0, 0, 0].b_mil[k], P.pop_ct[0, 0, 0].basep_adj[k],
                            P.pop_ct[0, 0, 0].surv[k], P.pop_ct[0, 0, 0].netmig[k], P.pop_ct[0, 0, 0].e_nmil[k],
                            P.pop_mil_ct[0, 0, 0, k].est, P.pop_ct[0, 0, 0].est[k],
                            P.pop_ct[0, 0, 0].est[k] - P.pop_ct[0, 0, 0].basep[k]);
                        ct_est_out.WriteLine(str);
                        ct_est_out.Flush();
                    } /* end for k */

                    str = String.Format("TOT{0,8}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}",
                        P.pop_ct[0, 0, 0].basep_totals, P.pop_ct[0, 0, 0].deaths_totals,
                        P.pop_ct[0, 0, 0].b_mil_totals, P.pop_ct[0, 0, 0].basep_adj_totals,
                        P.pop_ct[0, 0, 0].surv_totals, P.pop_ct[0, 0, 0].netmig_tot,
                        P.pop_ct[0, 0, 0].e_nmil_totals,
                        P.pop_ct[0, 0, 0].e_mil_totals, P.pop_ct[0, 0, 0].est_totals,
                        P.pop_ct[0, 0, 0].est_totals - P.pop_ct[0, 0, 0].basep_totals);
                    ct_est_out.WriteLine(str);
                    ct_est_out.Flush();


                    //***************************************************************************************************

                    str = "";
                    str = "POPULATION ESTIMATES - CTS";
                    ct_est_out.WriteLine(str);
                    str += "            Hispanic            NonHisp White       NonHisp Black        NonHisp Indian       NonHisp Asian        NonHisp Haw          NonHisp Other        NonHisp Two             Total";
                    ct_est_out.WriteLine(str);
                    str += "CT      Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg   Base   Fcst    Chg     POPEST    DIF";
                    ct_est_out.WriteLine(str);
                    str += "--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------";
                    ct_est_out.WriteLine(str);
                    ct_est_out.Flush();

                    for (g = 0; g < NUM_CTS; ++g)
                    {
                        str = String.Format("{0,-5}", ct_list[g, 0]);
                        for (i = 1; i < NUM_ETH; ++i)
                        {
                            if (P.pop_ct[g, i, 0].basep_totals > 0)
                                chg = P.pop_ct[g, i, 0].est_totals - P.pop_ct[g, i, 0].basep_totals;
                            str += String.Format("{0,7}{1,7}{2,7}", P.pop_ct[g, i, 0].basep_totals, P.pop_ct[g, i, 0].est_totals, chg);

                            totals[i - 1] += pop_ct[g, i, 0].est_totals;
                        }     /* end for i */

                        if (pop_ct[g, 0, 0].basep_totals != 0)
                            chg = P.pop_ct[g, 0, 0].est_totals - P.pop_ct[g, 0, 0].basep_totals;
                        else
                            chg = 0;
                        dif = P.pop_ct[g, 0, 0].popest_est_totals - P.pop_ct[g, 0, 0].est_totals;
                        str += String.Format("{0,7}{1,7}{2,7}    {3,7}{4,7}",
                            P.pop_ct[g, 0, 0].basep_totals,
                            P.pop_ct[g, 0, 0].est_totals,
                            chg, P.pop_ct[g, 0, 0].popest_est_totals,
                            dif);

                        ct_est_out.WriteLine(str);
                        ct_est_out.Flush();

                    }     /* end for g */

                    str = "POPULATION ESTIMATES - CTs\r\n";
                    str += "         Base Year Distrib                                                          Fcst Year Distrib                           Change\r\n";
                    str += "CT     Hisp   NHW   NHB   NHI   NHA   NHH   NHO   NH2      Hisp   NHW   NHB   NHI   NHA   NHH   NHO   NH2      Hisp   NHW   NHB   NHI   NHA   NHH   NHO   NH2\r\n";
                    str += "-------------------------------------------------------------------------------------------------------------------------------------------------------------\r\n";
                    ct_est_out.Write(str);
                    ct_est_out.Flush();

                    for (g = 0; g < NUM_CTS; ++g)
                    {
                        str = String.Format("{0,-5}", P.ct_list[g, 0]);
                        for (i = 1; i < NUM_ETH; ++i)
                        {
                            if (P.pop_ct[g, 0, 0].basep_totals != 0)
                                b1[i] = P.pop_ct[g, i, 0].basep_totals;
                            else
                                b1[i] = 0;

                            if (pop_ct[g, 0, 0].est_totals != 0)
                                f1[i] = P.pop_ct[g, i, 0].est_totals;
                            else
                                f1[i] = 0;

                            diff[i] = f1[i] - b1[i];
                        }     /* end for i */

                        for (i = 1; i < NUM_ETH; ++i)
                            str += String.Format("{0,6}", b1[i]);

                        str += "    ";

                        for (i = 1; i < NUM_ETH; ++i)
                            str += String.Format("{0,6}", f1[i]);
                        str += "    ";

                        for (i = 1; i < NUM_ETH; ++i)
                            str += String.Format("{0,6}", diff[i]);
                        ct_est_out.WriteLine(str);

                    }     /* end for g */

                    ct_est_out.Close();

                    break;     /* end case 3 */


            }     /* end switch */


        }     //end DebugPrint1

        //*********************************************************************
        #endregion

        #region Miscellaneous button handlers
        private void pasee_Load(object sender, System.EventArgs e)
        {

        }

        private void ReturnItem_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }


        #endregion

    }
    public class vals
    {
        public int control;
        public int summ;
        public int sumabs;
        public double padj;
        public double nadj;
        public bool adj_flag;
    }

    public class SpecialPopStruct
    {
        public int ct;
        public int basep_code; //3 big bases - gq and hhp handled separately
        public int sra;
    }

    public class PopestSpecialPop
    {
        public int b_hmil;     /* base year mil household pop */
        public int e_hmil;     /* estimated year mil household pop */
        public int baseYearSpecialPop;      /* base total */
        public int e_mil;      /* est total */
        public int b_umil;     /* base uniform */
        public int e_umil;     /* est uniform */
    }

    public class pop_mil
    {
        public int b_mil_gen;     /* base year mil pop except bases */
        public int b_mil_bases;   /* base year mil pop - bases only */
        public int e_mil_gen;      /* estimated year mil pop - except bases */
        public int e_mil_bases;    /* estimated year mil pop - bases only */
        public int est;          /* estimated total */
    }

    public class pop_master
    {
        public int[] basep;
        public int[] basep_adj = new int[101];
        public int[] b_mil = new int[101];
        public int b_mil_totals;
        public int e_mil_totals;
        public int basep_totals;
        public int basep_adj_totals;
        public int births;
        public int[] deaths = new int[101];
        public int deaths_totals;
        public int[] est = new int[101];
        public int est_totals;
        public int[] netmig = new int[101];
        public int netmig_tot;
        public int newtot;
        public int[] e_nmil = new int[101];
        public int e_nmil_totals;
        public int nmilmig;
        public int[] popest_est = new int[101];
        public int popest_est_totals;
        public int popest_mil_est_totals;
        public double regadj;
        public int[] surv = new int[101];
        public int surv_totals;
    }

    public class TableNames
    {
        public string age5Lookup;
        public string birthsCT;
        public string deathsCT;
        public string migrationDistribution;
        public string migrationRates;
        public string mil_pct;
        public string mil_dep_pct;
        public string paseeUpdateCT;
        public string popestMGRA;
        public string popEstimatesCT;
        public string popEstimatesTabCT;
        public string popEstimatesTabMGRA;
        public string specialPopTracts;
        public string xref;

    }     //end class TableNames
    
}
