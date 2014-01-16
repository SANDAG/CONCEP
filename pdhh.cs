/* 
 * Source File: pdhh.cs
 * Program: concep
 * Version 4
 * Programmer: tbe
 * Description:
 *		This is the detailed HH data component of popest (concep)
 *		Version 4 introduces concep.config.exe to store all global constants, queries, table names and file names
 *		version 3.5 adds computations for using Series 13 geographies
 *		version 3.3 adds computations for CT-level HH detail, including HH by size of HH, HH by presence of children and HH by # workers
 *		           
 *     
 */
/*   Database Description:
 *		SQL Server Database: concep
 *			Tables:
 *			popest_MGRA	: reduced data set popest MGRAs, indexed by estimates_year
 *          controls_popest_city : adjusted dof city controls, indexed by estimates_year
 *          controls_popest_HH_region : regional totals for HHS X category
 *          popest_HHdetail_tab_ct:  tabular output from detailed HH calcs
 *          controls_popest_HH_region
 *          distribution_popest_HH_wo_children_ct
 *          distribution_popest_HH_by_workers_region
 *          error_factors_popest_HHS_ct
 *          overrides_popest_HHSdetail
 *          census2010_ct_kids_hh
 *          detailed_pop_tab_ct
 
*/
//Revision History
 //   Date       By   Description
 //   ------------------------------------------------------------------
 //   06/08/11   tbe  changes for Version 3.3 CT HH detail
 //   
 //   ------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Configuration;
//using CU;

namespace popestDHH
{
    // need this to use the WriteToStatusBox on different thread
    delegate void WriteDelegate(string status);

   

    public class pdhh : System.Windows.Forms.Form
    { 
        public Configuration config;
        public KeyValueConfigurationCollection appSettings;
        public ConnectionStringSettingsCollection connectionStrings;

        public class TableNames
        {
            public string ct10Kids;
            public string detailedPopTabCT;
            public string distributionHHWorkersRegion;
            public string distributionHHWOC;
            public string errorFactorsHHSCT;
            public string overridesHHSDetail;
            public string popestControlsHHRegion;
            public string popestHHDetailMGRA;
            public string popestHHDetailTabCT;
            public string popestMGRA;      //mgra table name     
            public string xref;

        }  // end class

        public TableNames TN = new TableNames();

        public class REG  //regional control for detailed HH data class
        {
            public int hhp;   //regional hhp control
            public int hh;    // regional hh control
            public int[] hhXs;           // regional hh X size controls
            public int[] hhXsAdjusted;   // regional controls adjusted for overrides
            public int[] hhwocAdjusted;
            public int[] hhworkersAdjusted;

            public int[] hhwoc;                   // regional total hh without children 0 = without, 1 = with
            public int[] hhworkers;  // regional total hh with workers by category, 0, 1, 2, 3+
            public double[,] hhworkersp; //distribution of workers categories by hh size
        } // end class

        //****************************************************************************************

        // detailed mgra hh data class

        public class MDHH
        {
            public int mgraid;
            public int[] hhworkers;             // mgra hh by workers rounded
            public int[] hhwoc;                                // mgra hh wo children rounded
            public int[] hhXs;                       // mgra hh x size category rounded
            public double[] hhXsc;                // ct hh x size category computed
            public double[] hhXsp;                // mgra hh x size proportions derived with poisson function
            public double[] hhXspa;               // mgra hh x size poisson proportions adjusted by error factor
            public int hh;                                                  // number of hh in mgra
            public int hhp;                                                 // hhp in mgra
            public int hhpc;                                                // implied hhp from hhxs
            public bool hhis1 = false;                                      // is this mgra hh = 1; used for controlling
            public double hhs;

        }  // end class

        //******************************************************************************************

        public class CTMASTER  //CT-level detailed HH data class
        {
            public int ctid;
            public int hhp;
            public int hhpc;                          // ct hhp reconstituted
            public int hh;                            // ct total hh
            public double hhs;                        // ct hhs
            public bool HHSovr;                          // whether of not this ct uses hhXs overrides
            public bool HHWOCovr;                        // whether or not this ct uses hhwoc overrides
            public bool HHWORKERSovr;                    // whether or not this ct uses hhworkers overrides
            public int[] hhXso;    // hhs overrides for special ct;
            public double[] hhXsop;   // hhs overrides expressed as % of total hh
            public double[] hhXsp;    // ct hh x size proportions derived with poisson function
            public double[] hhXsef;   // ct level hh x size error factors from base data
            public double[] hhXspa;   // ct hh x size poisson proportions adjusted by error factor
            public double[] hhXsc;    // ct hh x size category computed
            public int[] hhXs;           // ct hh x size category rounded
            public int[] hhXs4;             // hh X size summed to 4 categories hhs = 1, hhs = 2, hhs = 3, hhs = 4+

            public double[] hhwocp;     // hh wo children % 4 categories hhs = 1, hhs = 2, hhs = 3, hhs = 4+
            public double[] hhwocc;   // ct hh wo children computed
            public int[] hhwoc;            // ct hh wo children rounded

            public double[] hhworkersp;
            public double[] hhworkersc;     // ct hh by workers computed
            public int[] hhworkers;            // ct hh by workers rounded

            public int num_mgras;
            public int kids;           // number of children 0 - 17
            public int kidsl;          // minimum number of children computed from hhwc * 1
            public int kidsu;          // maximum number of kids computed from hhwc * 3(or some other acceptable number like 2.5)
            public double kids_hh_wkids;  // number of kids/hh with kids from 2000 census

            public MDHH[] m;

        }  // end class

        public int MAX_MGRAS_IN_CITY;  // max number of MGRAs in any city
        public int MAX_CITIES;
        public int NUM_MGRAS;
        public int MAX_POPEST_EXCEPTIONS;
       
        public int NUM_CTS;     // number of census tracts (series 12)
        public int NUM_HHXS;     // number of hh by size categories
        public int NUM_HHWORKERS;      // number of hh by workers categories
        public int MAX_MGRAS_IN_CTS;  //maximum number of mgras in any ct

        public string networkPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label label2;
        private System.Data.SqlClient.SqlCommand sqlCommand1;
        private System.Data.SqlClient.SqlConnection sqlConnection;
        private System.Windows.Forms.MenuItem menuItem4;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnValidate;
        private System.Windows.Forms.CheckBox chkOverrides;
        private System.Windows.Forms.Button btnRunpdhh;
        private System.Windows.Forms.ComboBox txtYear;
        //private IContainer components;
        private int fyear;
        private int lyear;
        private bool useOverrides;
        private CheckBox checkBox1;
        
        public pdhh()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
       

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label3 = new System.Windows.Forms.Label();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.btnExit = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.btnRunpdhh = new System.Windows.Forms.Button();
            this.sqlCommand1 = new System.Data.SqlClient.SqlCommand();
            this.sqlConnection = new System.Data.SqlClient.SqlConnection();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.btnValidate = new System.Windows.Forms.Button();
            this.chkOverrides = new System.Windows.Forms.CheckBox();
            this.txtYear = new System.Windows.Forms.ComboBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(32, 296);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 16);
            this.label3.TabIndex = 12;
            this.label3.Text = "Status";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(24, 208);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(554, 80);
            this.txtStatus.TabIndex = 11;
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(116, 144);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(96, 58);
            this.btnExit.TabIndex = 10;
            this.btnExit.Text = "Return";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(111, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(176, 24);
            this.label2.TabIndex = 9;
            this.label2.Text = "Estimates Year";
            // 
            // btnRunpdhh
            // 
            this.btnRunpdhh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRunpdhh.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunpdhh.Location = new System.Drawing.Point(24, 144);
            this.btnRunpdhh.Name = "btnRunpdhh";
            this.btnRunpdhh.Size = new System.Drawing.Size(96, 58);
            this.btnRunpdhh.TabIndex = 15;
            this.btnRunpdhh.Text = "Run ";
            this.btnRunpdhh.UseVisualStyleBackColor = false;
            this.btnRunpdhh.Click += new System.EventHandler(this.btnRunpdhh_Click);
            // 
            //sqlConnection
            // 
            
            // 
            // menuItem4
            // 
            this.menuItem4.Index = -1;
            this.menuItem4.Text = "";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(24, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(458, 40);
            this.panel1.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Book Antiqua", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Blue;
            this.label1.Location = new System.Drawing.Point(-8, -1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(434, 40);
            this.label1.TabIndex = 0;
            this.label1.Text = "Detailed HH Characteristics";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnValidate
            // 
            this.btnValidate.Location = new System.Drawing.Point(0, 0);
            this.btnValidate.Name = "btnValidate";
            this.btnValidate.Size = new System.Drawing.Size(75, 23);
            this.btnValidate.TabIndex = 0;
            // 
            // chkOverrides
            // 
            this.chkOverrides.ForeColor = System.Drawing.Color.Blue;
            this.chkOverrides.Location = new System.Drawing.Point(130, 64);
            this.chkOverrides.Name = "chkOverrides";
            this.chkOverrides.Size = new System.Drawing.Size(104, 24);
            this.chkOverrides.TabIndex = 0;
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
            this.txtYear.Location = new System.Drawing.Point(24, 64);
            this.txtYear.Name = "txtYear";
            this.txtYear.Size = new System.Drawing.Size(72, 31);
            this.txtYear.TabIndex = 19;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(24, 112);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(93, 17);
            this.checkBox1.TabIndex = 20;
            this.checkBox1.Text = "Use Overrides";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // pdhh
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(590, 336);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.txtYear);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnRunpdhh);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.label2);
            this.Name = "pdhh";
            this.Text = "CONCEP - Detailed HH Characteristics";
            this.Load += new System.EventHandler(this.pdhh_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region pdhh Run button processing

        /*  btnRunpdhh_Click() */

        /// method invoker for run button - starts another thread
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   08/02/11   tb   added new thread code

        //   ------------------------------------------------------------------
        private void btnRunpdhh_Click(object sender, System.EventArgs e)
        {
            //build the table names from runtime args
            processParams(txtYear.SelectedItem.ToString(), ref fyear, ref lyear);
            MethodInvoker mi = new MethodInvoker(doPDHHWork);
            mi.BeginInvoke(null, null);
        } // end method btnRunpdhh_Click()

        //***********************************************************************************************

        /*  beginpdhhwork() */

        /// popestDHH MGRA Main
 
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   08/02/11   tb   initial coding
      
        //   ------------------------------------------------------------------
        private void doPDHHWork()
        {
            CTMASTER[] ct = new CTMASTER[NUM_CTS];      //ct data class
            REG reg = new REG();
            int i, j;

            reg.hhXs = new int[NUM_HHXS];           // regional hh X size controls
            reg.hhXsAdjusted = new int[NUM_HHXS];   // regional controls adjusted for overrides
            reg.hhwocAdjusted = new int[2];
            reg.hhworkersAdjusted = new int[NUM_HHWORKERS];

            reg.hhwoc = new int[2];                   // regional total hh without children 0 = without, 1 = with
            reg.hhworkers = new int[NUM_HHWORKERS];  // regional total hh with workers by category, 0, 1, 2, 3+
            reg.hhworkersp = new double[NUM_HHWORKERS, NUM_HHWORKERS]; //distribution of workers categories by hh size

            for (i = 0; i < NUM_CTS; ++i)
            {
                ct[i] = new CTMASTER();
                ct[i].m = new MDHH[MAX_MGRAS_IN_CTS];
                for (j = 0; j < MAX_MGRAS_IN_CTS; ++j)
                {
                    ct[i].m[j] = new MDHH();
                    ct[i].m[j].hhworkers = new int[NUM_HHWORKERS];
                    ct[i].m[j].hhwoc = new int[2];
                    ct[i].m[j].hhXs = new int[NUM_HHXS];
                    ct[i].m[j].hhXsc = new double[NUM_HHXS];                // ct hh x size category computed
                    ct[i].m[j].hhXsp = new double[NUM_HHXS];                // mgra hh x size proportions derived with poisson function
                    ct[i].m[j].hhXspa = new double[NUM_HHXS];
                }  // end for j

                ct[i].hhworkers = new int[NUM_HHWORKERS];             // mgra hh by workers rounded
                ct[i].hhwoc = new int[2];                                // mgra hh wo children rounded
                ct[i].hhXs = new int[NUM_HHXS];                       // mgra hh x size category rounded
                ct[i].hhXsc = new double[NUM_HHXS];                // ct hh x size category computed
                ct[i].hhXsp = new double[NUM_HHXS];                // mgra hh x size proportions derived with poisson function
                ct[i].hhXspa = new double[NUM_HHXS];
                ct[i].hhXso = new int[NUM_HHXS];    // hhs overrides for special ct;
                ct[i].hhXsop = new double[NUM_HHXS];   // hhs overrides expressed as % of total hh
                ct[i].hhXsef = new double[NUM_HHXS];   // ct level hh x size error factors from base data
                ct[i].hhXs4 = new int[4];             // hh X size summed to 4 categories hhs = 1, hhs = 2, hhs = 3, hhs = 4+
                ct[i].hhwocp = new double[4];     // hh wo children % 4 categories hhs = 1, hhs = 2, hhs = 3, hhs = 4+
                ct[i].hhwocc = new double[4];   // ct hh wo children computed
                ct[i].hhworkersp = new double[NUM_HHWORKERS];
                ct[i].hhworkersc = new double[NUM_HHWORKERS];     // ct hh by workers computed

            }  // end for i

            try
            {
                //sqlCommand1 = new System.Data.SqlClient.SqlCommand();
                //sqlCommand1.CommandTimeout = 180;
                sqlCommand1.Connection = sqlConnection;
                BuildCTHHDetail(ct,reg);
                WriteToStatusBox("COMPLETED POPEST DETAILED HH RUN");

            } // end try

            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                return;
            } // end catch

        } // end doPDHHWork()

        //*************************************************************************************************
        #endregion

        #region CTHHDetail

        //procedures
        //   AdjustRegionwideDistribution()
        //   AdjustSeedHHS()
        //   AllocateTOMGRAS()
        //   BuildCTHHDetail()
        //   BuildSeed()
        //   ControlToLocal()
        //   DOFinishChecks()
        //-----------------------------------------------------------------------------------------------

        // AdjustRegionwideDistribution()
        //  Adjust the estimates for regionwide distributions
        //      1.  Derive regionwide factors 
        //      2.  Apply regionwide factors
        //      3.  reset old_rowtotals
        //      4.  Get row and col tots
        //      5.  compute diffratio for each ct

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   06/08/14   tbe  New procedure for deriving CT detailed HH data
        //   ------------------------------------------------------------------

        public bool AdjustRegionwideDistribution(int modelswitch, StreamWriter foutw, REG reg, CTMASTER[] ct, int[] rowtotals,
                                 int[] old_rowtotals, int[] coltotals, double[] rowdiffratio, int dimj)
        {
            string str = "";
            int i;
            int j;
            int[] passer = new int[dimj];
            bool doanother = false;

            double[] regfac = new double[dimj];
            // --------------------------------------------------------------------------------------------

            foutw.WriteLine("ADJUST FOR REGIONWIDE DISTRIBUTION");
            str = "REGIONWIDE FACTORS ,";
            // adjust for regionwide distributions

            // derive regionwide factor for indicated variable (modelswitch)
            // 1 - HHS regfac[j] = reg.hhXsAdjusted[j]/ coltotals[j]
            // 2 - HHWOC regfac[j] = reg.hhwoc[j]/coltotals[j]
            // 3 - HHWORKERS regfac[j] = reg.hhworkers[j]/coltotals[j]

            for (j = 0; j < dimj; ++j)
            {
                regfac[j] = 0;
                if (coltotals[j] > 0)
                {
                    if (modelswitch == 1)
                        regfac[j] = (double)reg.hhXs[j] / (double)coltotals[j];
                    else if (modelswitch == 2)
                        regfac[j] = (double)reg.hhwoc[j] / (double)coltotals[j];
                    else
                        regfac[j] = (double)reg.hhworkers[j] / (double)coltotals[j];
                } // end if

                coltotals[j] = 0;     // reset col controls
                str += regfac[j] + ",";
            }  // end for j

            foutw.WriteLine(str);

            // apply regionwide factors to each cell
            for (i = 0; i < NUM_CTS; ++i)
            {
                if (ct[i].HHSovr)
                    continue;
                Array.Clear(passer, 0, passer.Length);
                rowdiffratio[i] = 0;
                rowtotals[i] = 0;
                str = i + "," + ct[i].ctid + ",";
                for (j = 0; j < dimj; ++j)
                {
                    if (modelswitch == 1)
                    {
                        ct[i].hhXsc[j] = (double)ct[i].hhXs[j] * regfac[j];
                        ct[i].hhXs[j] = (int)(ct[i].hhXsc[j] + .5);
                        rowtotals[i] += ct[i].hhXs[j];
                        coltotals[j] += ct[i].hhXs[j];
                        passer[j] = ct[i].hhXs[j];

                    }  // end if
                    else if (modelswitch == 2)
                    {
                        ct[i].hhwocc[j] = (double)ct[i].hhwoc[j] * regfac[j];
                        ct[i].hhwoc[j] = (int)(ct[i].hhwocc[j] + .5);
                        rowtotals[i] += ct[i].hhwoc[j];
                        coltotals[j] += ct[i].hhwoc[j];
                        passer[j] = ct[i].hhwoc[j];
                    }   // end else if
                    else
                    {
                        ct[i].hhworkersc[j] = (double)ct[i].hhworkersc[j] * regfac[j];
                        ct[i].hhworkers[j] = (int)(ct[i].hhworkersc[j] + .5);
                        rowtotals[i] += ct[i].hhworkers[j];
                        coltotals[j] += ct[i].hhworkers[j];
                        passer[j] = ct[i].hhworkers[j];
                    }   // end else

                }   // end for j

                // derive row diff ratio
                if (rowtotals[i] > 0)
                    rowdiffratio[i] = (double)old_rowtotals[i] / (double)rowtotals[i];
                else
                    rowdiffratio[i] = 0;
                doanother = false;
                if (rowdiffratio[i] < .99 || rowdiffratio[i] > 1.01)
                    doanother = true;

                old_rowtotals[i] = rowtotals[i];
                WriteCTData(foutw, ct[i].ctid, passer, rowtotals[i], rowdiffratio[i], i, dimj);
            }   // end for i

            str = "COLUMN TOTALS,";
            for (j = 0; j < dimj; ++j)
            {
                str += coltotals[j] + ",";
                coltotals[j] = 0;          // zero col controls
            }   // end for j

            foutw.WriteLine(str);
            foutw.WriteLine("END OF ADJUST FOR REGIONWIDE DISTRIBUTION");
            foutw.Flush();
            return doanother;

        }  // end procedure AdjustRegionwideDistribution()

        //******************************************************************************************************

        // AdjustSeedHHS()
        //  Adjust the estimates for hhp 

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   07/08/11   tbe  New procedure for deriving CT detailed HH data
        //   ------------------------------------------------------------------

        public void AdjustSeedHHS(int[] passer, int hhp, int hh, int dimj)
        {
            int[] start = new int[dimj];
            int[] adjmax = new int[dimj];
            int j;
            int minimplied = 0, maximplied = 0;
            int subtotal = 0;
            int which = 0;
            int iter = 0;
            //-------------------------------------------------------------------------------------------------------

            for (j = 0; j < dimj - 1; ++j)  // use hhs1 - hhs6 for temp value
            {
                minimplied += (j + 1) * passer[j];
                maximplied += (j + 1) * passer[j];
            }   // end for j

            // add the hhs7 value
            minimplied += dimj * passer[dimj - 1];
            maximplied += 10 * passer[dimj - 1];

            // check actual hhp in range of min and max

            if (hhp < minimplied)
                which = 1;
            else if (hhp > maximplied)
                which = 2;

            switch (which)
            {

                case 1:     // actual hhp is less than min implied - reduce the implied
                    for (j = 1; j < dimj; ++j)
                    {
                        start[j] = passer[j];
                        adjmax[j] = (int)(.90 * passer[j] + .5);
                    }  // end for j
                    iter = 0;

                    while (minimplied > hhp && iter < 1000)
                    {
                        minimplied = 0;
                        subtotal = 0;
                        for (j = 1; j < dimj; ++j)
                        {
                            if (passer[j] > adjmax[j])
                                --passer[j];

                            minimplied += passer[j] * (j + 1);
                            subtotal += passer[j];
                        }  // end for j
                        passer[0] = hh - subtotal;
                        minimplied += passer[0];
                        ++iter;
                    }  // end while
                    if (iter >= 1000 && minimplied < hhp)
                        MessageBox.Show("AdjSeed didn't converge on minimplied adjustment");

                    break;

                case 2:    // actual hhp is greater than the max implied - increase the hhs categories
                    iter = 0;
                    while (maximplied < hhp && iter < 1000)
                    {
                        maximplied = 0;
                        subtotal = 0;
                        for (j = 1; j < dimj; ++j)
                        {
                            ++passer[j];

                            maximplied += passer[j] * (j + 1);
                            subtotal += passer[j];
                        }  // end for j
                        passer[0] = hh - subtotal;
                        maximplied += passer[0];
                        ++iter;
                    }  // end while
                    if (iter >= 1000 && minimplied < hhp)
                        MessageBox.Show("AdjSeed didn't converge on minimplied adjustment");
                    break;
                default:
                    return;
            }  // end swich

        }  //  End procedure AdjustSeedHHS()   

        //***********************************************************************************************************
        // AdjustSeedHHSMGRA()
        //  Adjust the estimates for hhp 

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   07/08/11   tbe  New procedure for deriving CT detailed HH data
        //   ------------------------------------------------------------------

        public void AdjustSeedHHSMGRA(int[] passer, int hhp, int hh, int dimj)
        {
            int[] start = new int[dimj];
            int[] adjmax = new int[dimj];
            int[] savepasser = new int[dimj];
            int j;
            int minimplied = 0, maximplied = 0;
            int subtotal = 0;
            int which = 0;
            int iter = 0;
            int urange = 0;
            int lrange = 0;
            //-------------------------------------------------------------------------------------------------------
            lrange = (int)((double)hhp * .95);
            urange = (int)((double)hhp * 1.05);

            for (j = 0; j < dimj; ++j)
                savepasser[j] = passer[j];
            for (j = 0; j < dimj - 1; ++j)  // use hhs1 - hhs6 for temp value
            {
                minimplied += (j + 1) * passer[j];
                maximplied += (j + 1) * passer[j];
            }   // end for j

            // add the hhs7 value
            minimplied += dimj * passer[dimj - 1];
            maximplied += 10 * passer[dimj - 1];

            // check actual hhp in range of min and max

            if (minimplied > urange)
                which = 1;
            else if (maximplied < lrange)
                which = 2;
            else
                which = 0;

            switch (which)
            {

                case 1:     // min implied exceeds 105 % of hhp - reduce the implied
                    for (j = 1; j < dimj; ++j)
                    {
                        start[j] = passer[j];
                        adjmax[j] = (int)(.9 * passer[j]);
                    }  // end for j
                    iter = 0;

                    int k = 0;
                    while (minimplied > urange && iter < 1000)
                    {
                        ++k;
                        if (k == 7)
                            k = 0;
                        minimplied = 0;
                        subtotal = 0;
                        if (passer[k] > adjmax[k] && passer[k] > 0)
                            --passer[k];
                        for (j = 1; j < 7; ++j)
                        {
                            minimplied += passer[j] * (j + 1);
                            subtotal += passer[j];
                        }  // end for j
                        passer[0] = hh - subtotal;
                        minimplied += passer[0];
                        ++iter;
                    }  // end while
                    if (iter >= 1000 && minimplied < hhp)
                        MessageBox.Show("AdjSeed didn't converge on minimplied adjustment");

                    break;

                case 2:    // max implied is less than 95% of hhp - increase the hhs categories
                    iter = 0;
                    k = 0;
                    while (maximplied < lrange && iter < 1000)
                    {
                        maximplied = 0;
                        // move each bin up 1 and recompute
                        for (j = 0; j < 6; ++j)
                        {
                            if (passer[j] > 0)
                            {
                                --passer[j];
                                ++passer[j + 1];
                            }  // end if

                        }  // end for j

                        for (j = 0; j < 7; ++j)
                        {
                            maximplied += passer[j] * (j + 1);
                        }  // end for j

                        if (passer[0] < 0)
                            MessageBox.Show("In Adjseed case 2, adjustment yields < 0");

                        ++iter;
                    }  // end while
                    if (iter >= 1000 && minimplied < hhp)
                        MessageBox.Show("AdjSeed didn't converge on minimplied adjustment");
                    break;
                default:
                    return;
            }  // end swich

        }  //  End procedure AdjustSeedHHSMGRA()   

        //***********************************************************************************************************

        // AllocateToMGRAS()
        // Allocate the popest CT detailed HH data to mgras using Pachinko
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   06/21/11   tbe  New procedure for deriving CT detailed HH data
        //   ------------------------------------------------------------------
        public void AllocateToMGRAS(CTMASTER[] ct, int dimi)
        {
            int i, j, k, mcount, target, ret, pcounter;
            int[] passer = new int[7];     // maximum number of elements to be passed to Pachinko
            int[] master1 = new int[NUM_HHXS];
            int[] savemaster1 = new int[NUM_HHXS];
            int[] master2 = new int[2];
            int[] master3 = new int[NUM_HHWORKERS];
            int[] tmgrahh = new int[MAX_MGRAS_IN_CTS];
            int[,] tmgrahhs = new int[MAX_MGRAS_IN_CTS, 7];
            int[] torig_index = new int[MAX_MGRAS_IN_CTS];
            string str = "", str1 = "";
            FileStream foutm;
            //-----------------------------------------------------------------------------------------
            try
            {
                foutm = new FileStream(networkPath + "pdmgra", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter foutmw = new StreamWriter(foutm);

            // seed the mgras for HHXs
            BuildSeedMGRAS(ct);

            for (i = 0; i < dimi; ++i)
            {
                Array.Clear(tmgrahhs, 0, tmgrahhs.Length);
                Array.Clear(tmgrahh, 0, tmgrahh.Length);
                Array.Clear(torig_index, 0, torig_index.Length);
                Array.Clear(savemaster1, 0, savemaster1.Length);

                // number of nmgra in ct[i]
                mcount = ct[i].num_mgras;
                pcounter = 0;
                WriteToStatusBox("  Processing CT " + (i + 1).ToString() + " ID = " + ct[i].ctid + " Number of MGRAS = " + mcount);
                str1 = i + "," + ct[i].ctid + ",";
                int tstop = 0;
                if (ct[i].ctid == 2904)
                    tstop = 1;

                // start with hhxs  
                // here is the issue here.  we are doing a 2-dim controlling, but hhp is the third simension that must be considered
                // because the pachinko essentially assigns hh to size categories based on the ct distribution, it is likely that there will be 
                // mgras with implied hhp (hhxs * size) that doesn't closely match hhp coming from popest.
                // therefore we need to do some adjustments
                //  1. for mgras with only 1 hh and there are about 600 of them, we have to assign the hhxs according to the hhp
                //  2. then we have to subtract that assignment from the ct controls and exclude that mgra from the pachinko

                // for each ct, the first adjustment is done here when we build the master1 array
                for (j = 0; j < NUM_HHXS; ++j)
                {
                    str1 += ct[i].hhXs[j] + ",";
                    master1[j] = ct[i].hhXs[j];
                } // end for j

                // adjust the master1 for mgras with hh = 1
                int hi;
                for (k = 0; k < mcount; ++k)
                {
                    if (ct[i].m[k].hh == 1)
                    {
                        hi = ct[i].m[k].hhp - 1;
                        if (hi >= 0)
                        {
                            ct[i].m[k].hhXs[hi] = 1;
                            if (master1[hi] > 0)
                                master1[hi] -= 1;
                            ct[i].m[k].hhis1 = true;
                        }   // end if
                    } // end if
                }  // end for k

                // now store the remaining master before we go to pachinko
                for (k = 0; k < 7; ++k)
                    savemaster1[k] = master1[k];

                for (j = 0; j < 2; ++j)
                {
                    str1 += ct[i].hhwoc[j] + ",";
                    master2[j] = ct[i].hhwoc[j];
                }   // end for j

                for (j = 0; j < NUM_HHWORKERS; ++j)
                {
                    str1 += ct[i].hhworkers[j] + ",";
                    master3[j] = ct[i].hhworkers[j];
                }   // end for j

                //foutmw.WriteLine(str1);
                //foutmw.Flush();

                for (j = 0; j < mcount; ++j)
                {
                    Array.Clear(passer, 0, passer.Length);

                    if (ct[i].m[j].hh > 0)
                    {
                        // start with HHS   - for hhxs we are only going to process mgras where hh > 1

                        if (!ct[i].m[j].hhis1)
                        {
                            
                            // replace the values in the temp mgra array before final update - remember, these are only mgras where hh > 1
                            for (k = 0; k < 7; ++k)
                            {
                                tmgrahhs[pcounter, k] = ct[i].m[j].hhXs[k];
                            }  // end for k
                            torig_index[pcounter] = j;
                            tmgrahh[pcounter++] = ct[i].m[j].hh;  // store the mgra hh and increment the temp counter

                        }  // end if

                        // now do hh wo children
                        Array.Clear(passer, 0, passer.Length);

                        target = ct[i].m[j].hh;
                        ret = CU.cUtil.PachinkoWithMasterDecrement(target, master2, passer, 2);
                        if (ret >= 40000)
                        {
                            MessageBox.Show("Pachinko did not resolve in 40000 iterations for CT " + ct[i].ctid.ToString() + " mgra " + ct[i].m[j].mgraid.ToString());
                        }     /* end if */

                        for (k = 0; k < 2; ++k)
                        {
                            ct[i].m[j].hhwoc[k] = passer[k];
                        }   // end for k

                        Array.Clear(passer, 0, passer.Length);

                        // now do workers

                        target = ct[i].m[j].hh;
                        ret = CU.cUtil.PachinkoWithMasterDecrement(target, master3, passer, NUM_HHWORKERS);
                        if (ret >= 40000)
                        {
                            MessageBox.Show("Pachinko did not resolve in 40000 iterations for CT " + ct[i].ctid.ToString() + " mgra " + ct[i].m[j].mgraid.ToString());
                        }     /* end if */

                        for (k = 0; k < NUM_HHWORKERS; ++k)
                        {
                            ct[i].m[j].hhworkers[k] = passer[k];
                        }   // end for k               

                    }  // end if

                }   // end for j

                // now recontrol the adjusted mgra hhxs data using update
                // tmgrahhs stores the hhs data for the mgras with hh > 1
                // tmgrahh stores the mgra hh totals
                // master1 stores the adjusted ct totals
                // pcounter is the m=number of remaining rows
                if (pcounter > 0)
                {
                    CU.cUtil.update(pcounter, 7, tmgrahhs, tmgrahh, savemaster1);

                    // at this point the tmgrahhs array should have been readjusted so that the ct hhxs col sums were right and the row sums mgra hh were right
                    // replace the revised mgra data back to the ct.m structure
                    for (k = 0; k < pcounter; ++k)
                    {
                        j = torig_index[k];
                        for (int l = 0; l < 7; ++l)
                            ct[i].m[j].hhXs[l] = tmgrahhs[k, l];
                    }   // end for k
                }  // end if

                // have to do writes down here after final update controlling for hhsx
                for (j = 0; j < mcount; ++j)
                {
                    str = fyear + "," + ct[i].ctid + ",";
                    str += ct[i].m[j].mgraid + ",";
                    if (ct[i].m[j].hh > 0)
                    {

                        for (k = 0; k < NUM_HHXS; ++k)
                        {
                            str += ct[i].m[j].hhXs[k] + ",";
                        }  //end for k
                        for (k = 0; k < 2; ++k)
                        {
                            str += ct[i].m[j].hhwoc[k] + ",";
                        }   // end for k
                        for (k = 0; k < NUM_HHWORKERS; ++k)
                        {
                            str += ct[i].m[j].hhworkers[k] + ",";
                        }   // end for k
                        str += ct[i].m[j].hh;
                    }  // end if
                    else
                    {
                        str += "0,0,0,0,0,0,0,0,0,0,0,0,0,0";
                    }  // end else
                    foutmw.WriteLine(str);
                    foutmw.Flush();
                }  // end for j

            }  // end for i                  
            foutmw.Flush();
            foutmw.Close();

        }// end procedure AllocateTOMGRAS()

        //***************************************************************************************************

        // BuildCTHHDetail()
        // Computes Detailed CT-level data for popsyn
        // extracts hh and hhp from popest_mgra data set, aggregating to CT, computes hhs

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   06/08/11   tbe  New procedure for deriving CT detailed HH data
        //   07/08/11   tbe  added code add overrides for some special, namely mil, tracts and
        //                   exclude them from controlling
        //   ------------------------------------------------------------------

        public void BuildCTHHDetail(CTMASTER[] ct,REG reg)
        {

            int i, j;
            int[,] matrx = new int[NUM_CTS, NUM_HHXS];  //matrix to store 2 dimensional ct, hhs array for finish routines

           
            bool doanother = true;
            int[] rowtotals = new int[NUM_CTS];
            int[] old_rowtotals = new int[NUM_CTS];
            int[] nurowt = new int[NUM_CTS];

            int[] coltotals = new int[NUM_HHXS];

            double[] tempfac = new double[NUM_HHXS], rowdiffratio = new double[NUM_CTS];

            FileStream fout;		//file stream class

            //-----------------------------------------------------------------------
            
            ExtractCTHHData(ct, reg);
            // open output file
            try
            {
                fout = new FileStream(networkPath + "pct", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);

            // build the seed value for hhXs
            WriteToStatusBox("Building Seed for HH Size");
            BuildSeed(1, foutw, reg, ct, rowtotals, old_rowtotals, coltotals, NUM_HHXS);

            // at this point we should have the first rows (CT) derived for the first iteration
            WriteToStatusBox("Controlling for HH Size");
            while (doanother)
            {
                doanother = AdjustRegionwideDistribution(1, foutw, reg, ct, rowtotals, old_rowtotals, coltotals, rowdiffratio, NUM_HHXS);

                doanother = ControlToLocal(1, foutw, ct, rowtotals, old_rowtotals, coltotals, rowdiffratio, NUM_HHXS);

            }   // end while

            // logic for finish routines for hhXs
            WriteToStatusBox("Finish Routines for HH Size");
            DoFinishChecks(1, foutw, ct, reg, rowtotals, coltotals, NUM_HHXS);

            //---------------------------------------------------------------------------------------------------
            // build the seed value for hhwoc
            WriteToStatusBox("Building Seed for HH w kids");
            BuildSeed(2, foutw, reg, ct, rowtotals, old_rowtotals, coltotals, 2);
            // At this point we have the initial cut for the hh wo kids array
            // the next step is to control using pachinko to get the distribution finished, 
            // this pachinko uses an upper bound that constrains the hh w kids*factor = kids

            // get the row and col totals (row totals should be ok)
            Array.Clear(rowtotals, 0, rowtotals.Length);
            Array.Clear(coltotals, 0, coltotals.Length);
            WriteToStatusBox("Controlling for HH w Kids");
            for (i = 0; i < NUM_CTS; ++i)
            {
                for (j = 0; j < 2; ++j)
                {
                    rowtotals[i] += ct[i].hhwoc[j];
                    coltotals[j] += ct[i].hhwoc[j];
                }   // end for j
            }  // end for i

            WriteToStatusBox("Finish Routines for HH w Kids");
            DoFinishChecks(2, foutw, ct, reg, rowtotals, coltotals, 2);

            //---------------------------------------------------------------------------------------------------
            // build the seed value for hhworkers
            WriteToStatusBox("Building Seed for HH Workers");
            BuildSeed(3, foutw, reg, ct, rowtotals, old_rowtotals, coltotals, NUM_HHWORKERS);

            doanother = true;
            WriteToStatusBox("Controlling Seed for HH Workers");
            while (doanother)
            {
                doanother = AdjustRegionwideDistribution(3, foutw, reg, ct, rowtotals, old_rowtotals, coltotals, rowdiffratio, NUM_HHWORKERS);

                doanother = ControlToLocal(3, foutw, ct, rowtotals, old_rowtotals, coltotals, rowdiffratio, NUM_HHWORKERS);

            }   // end while
            WriteToStatusBox("Finish Routines for HH Workers");
            DoFinishChecks(3, foutw, ct, reg, rowtotals, coltotals, NUM_HHWORKERS);

            WriteToStatusBox("Writing CT data");
            BulkLoadPOPESTDHH(ct, fyear);

            // now allocate ct data to mgras
            WriteToStatusBox("Allocating to MGRAs");
            AllocateToMGRAS(ct, NUM_CTS);

            WriteToStatusBox("Writing MGRA data");
            BulkLoadMGRADHH();

            fout.Flush();
            fout.Close();

        }  // end procedure BuildCTHHDetail()

        //**********************************************************************************************************

        //BuildSeed()
        //  Derives the CT HH  Seed - based on the value of modelswitch
        //  modelswitch = 1 -> hhXs
        //      1.  Apply poisson
        //      2.  Apply error factor
        //      3.  Normalize to 1
        //      4.  Get roww and col tots

        //  modelswitch = 2 -> hhwoc

        //  modelswitch = 3 -> hhworkers

        // parameters
        // modelswitch - decides which cse we are doing
        // foutw - output streamwriter
        // reg - regional control data class
        // ct - CT data class
        // rowtotals - computed row totals CTs
        // coltotals - computed column totals data categories
        // dimj - dimension of data categories
        //        NUM_HHXS    - hhxs categories
        //        2           - hh w-wo categories
        //        NUM_HHWORKERS - hh workers categories

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   06/08/14   tbe  New procedure for deriving CT detailed HH data
        //   ------------------------------------------------------------------

        public void BuildSeed(int modelswitch, StreamWriter foutw, REG reg, CTMASTER[] ct, int[] rowtotals,
                              int[] old_rowtotals, int[] coltotals, int dimj)
        {
            string str = "", strs = "";
            int i;
            int j;
            int grandtotc = 0, grandtotr = 0;
            int[] passer = new int[dimj];
            double[] rowdiffratio = new double[NUM_CTS];
            double lambda;
            double fact, factot, factot1;
            FileStream foutp;
            //-------------------------------------------------------------------------------------------

            // open output file
            try
            {
                foutp = new FileStream(networkPath + "pctp", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter foutpw = new StreamWriter(foutp);

            // label this iteration

            if (modelswitch == 1)
                strs = "CT HHXS SEED";
            else if (modelswitch == 2)
                strs = "CT HHWOC SEED";
            else
                strs = "CT HHWORKERS SEED";

            foutw.WriteLine(strs);
            foutw.Flush();

            // if modelswitch == 1 - HHXS
            // Build initial CT hhs by category using ct distributions and poisson formula
            // lambda = hhs -1
            // n = hhs category ( 0 - 6)
            // poisson formula = (lambda^n * exp(-lambda))(n!) 

            // if modelswitch == 2 - HHWOC
            // build initial CT hhwoc by multiplying ct[i].hhXs4[j] * hh wo children factors

            for (i = 0; i < NUM_CTS; ++i)
            {
                str = i + 1 + "," + ct[i].ctid + ",";
                rowtotals[i] = 0;
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(rowdiffratio, 0, rowdiffratio.Length);

                switch (modelswitch)
                {
                    case 1:     //hhXs
                        {
                            if (ct[i].hh == 0)
                                break;
                            if (ct[i].HHSovr)       // is this ct overriden - apply overrides % to hh
                            {
                                for (j = 0; j < dimj; ++j)
                                {
                                    passer[j] = ct[i].hhXs[j];
                                }  // end for

                            }   // end if
                            else
                            {
                                if (ct[i].hhs == 1)
                                    lambda = 1;
                                else if (ct[i].hhs == 0)
                                    lambda = 0;
                                else
                                    lambda = ct[i].hhs - 1;
                                fact = 1;
                                factot = 0;
                                factot1 = 0;
                                for (j = 0; j < dimj; ++j)
                                {
                                    fact *= j;
                                    if (fact == 0)
                                        fact = 1;
                                    // use the poisson distribution to get the initial ct proportions
                                    ct[i].hhXsp[j] = (Math.Pow(lambda, (double)j) * Math.Exp(-lambda)) / fact;
                                    str += ct[i].hhXsp[j] + ",";

                                    // apply the base year error factors (derivedfrom 2000 Census)
                                    ct[i].hhXspa[j] = ct[i].hhXsp[j] * ct[i].hhXsef[j];
                                    factot += ct[i].hhXspa[j];
                                }   // end for j
                                foutpw.WriteLine(str);
                                foutpw.Flush();

                                // normalize to 1.0 for distributions
                                for (j = 0; j < dimj - 1; ++j)  // do the first 6
                                {
                                    ct[i].hhXspa[j] /= factot;
                                    factot1 += ct[i].hhXspa[j];
                                }   // end for j

                                ct[i].hhXspa[dimj - 1] = 1 - factot1; // fill the last as residual
                                if (ct[i].hhXspa[dimj - 1] < 0)   // constrain to 0
                                    ct[i].hhXspa[dimj - 1] = 0;

                                for (j = 0; j < dimj; ++j)
                                {
                                    // now get the first ct (row) estimate as proportion * hh
                                    ct[i].hhXsc[j] = ct[i].hhXspa[j] * ct[i].hh;    //derive floating point value
                                    ct[i].hhXs[j] = (int)(ct[i].hhXsc[j] + .5);   //round up
                                    rowtotals[i] += ct[i].hhXs[j];
                                    passer[j] = ct[i].hhXs[j];
                                }   // end for j

                                // at this point we have hhs1 - hhs7 initial seed estimates.  We need to do validity checks on the initial distribution
                                // we'll estimate a minimum and maximum implied hhp from this distribution.  if the estimated minimum is greater than the 
                                // actual hhp, adjust the hhs categories down.  This process has several steps.  the first is to assign some kind of threshold
                                // for reducing the categories.  We'll start with 10%  that is we'll only reduce a category by up to 10% of its starting value
                                // this may change after we review the results.  This precludes emiminating the hh in a category before the implied minimum hhp gets to the
                                // actual hhp.  It's entirely possible that the rounding exercises will undo some of this. The algorithm actually works from high to low,
                                // hhs7 , hhs6, hhs5 etc, reducing by 1.  HHS1 is computed as the residual of HH - Sum(HHS2 - HHs7).  The implied max and min are recomputed
                                // and once the implied min gets under the actual. the process is stopped.
                                // for the minumim we compute min = 1*HHS1 + 2*HHS2 +...+7*HHS7;  the max is developed similarly, except that HHS7 is multiplied by 10 (a suggested
                                // starting point.  the census uses 20

                                // if the actual hhp is higher than the maximum implied, increas the count in each category, starting with HHS2 going to HHS7, computing HHS1 as
                                // a residual as outlined above.

                                // restore the adjusted values
                                AdjustSeedHHS(passer, ct[i].hhp, ct[i].hh, dimj);
                                for (j = 0; j < dimj; ++j)
                                {
                                    ct[i].hhXs[j] = passer[j];
                                    rowtotals[i] += ct[i].hhXs[j];
                                }   // end for j
                            }   // end else not overriden

                            // sum the hhXs into 4 categories
                            for (j = 0; j < 3; ++j)
                                ct[i].hhXs4[j] = ct[i].hhXs[j];
                            ct[i].hhXs4[3] = ct[i].hhXs[3] + ct[i].hhXs[4] + ct[i].hhXs[5] + ct[i].hhXs[6];

                        }  // end case 1
                        break;

                    case 2:
                        {
                            // HHWOC - new methodology coded 08/15/2012
                            // start with # kids from popest bu CT
                            // compute HHWOC as hh1 * hhwocp1 + hh2 * hhwocp2 + hh3 * hhwocp3 + (hh4+hh5+hh6+hh7) * hhwocp4
                            // reset cases where kids = 0  if kids = 0, hhwoc = 0
                            // process overrides hhwoc = round(hh * override%)
                            // set hhwc = hh - hhwoc
                            // if hhwc > kids, hhwc = kids
                            // hhwoc = hh - hhwc
                            // control to region
                            //---------------------------------------------------------------------------------------------------

                            if (ct[i].hh == 0)   // skip all of this if there are no HH
                                break;

                            ct[i].hhwoc[1] = 0;  // 1 element is hh w kids this will be deretmined as a residual
                            ct[i].hhwoc[0] = 0;

                            ct[i].hhwoc[0] = (int)(ct[i].hhXs4[0] + (double)ct[i].hhXs4[1] * ct[i].hhwocp[1] +
                                            (double)ct[i].hhXs4[2] * ct[i].hhwocp[2] + (double)ct[i].hhXs4[3] * ct[i].hhwocp[3]);

                            ct[i].hhwoc[1] = ct[i].hh - ct[i].hhwoc[0];
                            if (ct[i].hhwoc[1] > ct[i].kids)   // constrain hhwc to kids
                            {
                                ct[i].hhwoc[1] = ct[i].kids;   // reset
                                ct[i].hhwoc[0] = ct[i].hh - ct[i].hhwoc[1];  // recompute hhwoc
                            }  // end if

                            for (j = 0; j < dimj; ++j)
                            {
                                rowtotals[i] += ct[i].hhwoc[j];
                                passer[j] = ct[i].hhwoc[j];
                            }  // end for j

                        }  // end case 2
                        break;

                    case 3:   //HHWORKERS
                        {
                            if (ct[i].hh == 0)
                                break;
                            for (int k = 0; k < 4; ++k)
                            {

                                for (j = 0; j < dimj; ++j)
                                {
                                    // notice we're multiplying the col values (%workers) * rows (HHS)
                                    ct[i].hhworkersc[k] = (double)ct[i].hhXs4[j] * reg.hhworkersp[j, k];
                                    ct[i].hhworkers[k] = (int)(ct[i].hhworkersc[k] + .5);  //round up

                                }   // end for j 
                                rowtotals[i] += ct[i].hhworkers[k];
                                passer[k] = ct[i].hhworkers[k];
                            }   // end for k
                        }  // end case 3
                        break;

                }  // end switch

                // write these data to ascii

                WriteCTData(foutw, ct[i].ctid, passer, rowtotals[i], rowdiffratio[i], i, dimj);
                grandtotr += rowtotals[i];
                // assign old rowtotals = rowtotals
                old_rowtotals[i] = rowtotals[i];
            }  // end for i
            foutpw.Flush();
            foutpw.Close();

            // get the row and column totals
            // get column totals
            str = "COLUMN TOTALS ";
            for (j = 0; j < dimj; ++j)
            {
                coltotals[j] = 0;
                for (i = 0; i < NUM_CTS; ++i)
                {
                    if (modelswitch == 1)
                        coltotals[j] += ct[i].hhXs[j];
                    else if (modelswitch == 2)
                        coltotals[j] += ct[i].hhwoc[j];
                    else
                        coltotals[j] += ct[i].hhworkers[j];
                }  // end for i

                grandtotc += coltotals[j];
                str += coltotals[j] + ",";
            }   // end for j

            foutw.WriteLine(str);
            if (modelswitch == 1)
                str = "REG HH SIZE CONTROLS, ";
            else if (modelswitch == 2)
                str = "REG HH WO CHILDREN CONTROLS,";
            else
                str = "REG HH WORKERS CONTROLS,";

            for (j = 0; j < dimj; ++j)
            {
                if (modelswitch == 1)
                    str += reg.hhXsAdjusted[j] + ",";
                else if (modelswitch == 2)
                    str += reg.hhwoc[j] + ",";
                else if (modelswitch == 3)
                    str += reg.hhworkers[j] + ",";
            } // end for j;

            foutw.WriteLine(str);

            foutw.WriteLine("END " + strs + " - grandtotc = " + grandtotc + " grandtotr = " + grandtotr);
            foutw.WriteLine("");
            foutw.Flush();

        }  // end procedure BuildSeed()

        //***************************************************************************************************

        //BuildSeedMGRA()
        //  Derives the MGRA HH  Seed - based on the value of modelswitch
        //  modelswitch = 1 -> hhXs
        //      1.  Apply poisson
        //      2.  Apply error factor
        //      3.  Normalize to 1
        //      4.  Get roww and col tots

        // parameters
        // modelswitch - decides which cse we are doing
        // foutw - output streamwriter
        // reg - regional control data class
        // ct - CT data class
        // rowtotals - computed row totals CTs
        // coltotals - computed column totals data categories
        // dimj - dimension of data categories
        //        NUM_HHXS    - hhxs categories
        //        2           - hh w-wo categories
        //        NUM_HHWORKERS - hh workers categories

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   04/01/12   tbe  New procedure for deriving MGRA detailed HHS data
        //   ------------------------------------------------------------------

        public void BuildSeedMGRAS(CTMASTER[] ct)
        {
            int i;
            int j, k;
            int mcount;
            double lambda;
            double fact, factot, factot1;
            //-------------------------------------------------------------------------------------------

            for (i = 0; i < NUM_CTS; ++i)
            {
                mcount = ct[i].num_mgras;
                for (j = 0; j < mcount; ++j)
                {
                    if (ct[i].m[j].hh == 0)
                        break;
                    if (ct[i].m[j].hhs == 1)
                        lambda = 1;
                    else if (ct[i].m[j].hhs == 0)
                        lambda = 0;
                    else
                        lambda = ct[i].m[j].hhs - 1;
                    fact = 1;
                    factot = 0;
                    factot1 = 0;
                    for (k = 0; k < 7; ++k)
                    {
                        fact *= k;
                        if (fact == 0)
                            fact = 1;
                        // use the poisson distribution to get the initial ct proportions
                        try
                        {
                            ct[i].m[j].hhXspa[k] = (Math.Pow(lambda, (double)k) * Math.Exp(-lambda)) / fact;
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show(exc.ToString(), exc.GetType().ToString());

                        }  // end catch

                        factot += ct[i].m[j].hhXspa[k];
                    }   // end for k

                    // normalize to 1.0 for distributions
                    for (k = 0; k < 6; ++k)  // do the first 6
                    {
                        ct[i].m[j].hhXspa[k] /= factot;
                        factot1 += ct[i].m[j].hhXspa[k];
                    }   // end for k

                    ct[i].m[j].hhXspa[6] = 1 - factot1; // fill the last as residual
                    if (ct[i].m[j].hhXspa[6] < 0)   // constrain to 0
                        ct[i].m[j].hhXspa[6] = 0;

                    for (k = 0; k < 7; ++k)
                    {
                        // now get the first ct (row) estimate as proportion * hh
                        ct[i].m[j].hhXsc[k] = ct[i].m[j].hhXspa[k] * ct[i].m[j].hh;    //derive floating point value
                        ct[i].m[j].hhXs[k] = (int)(ct[i].m[j].hhXsc[k] + .5);   //round up

                    }   // end for k

                }  // end for j
            }  // end for i

        }  // end procedure BuildSeedMGRA()

        //***************************************************************************************************
        // Control to Local()
        // 
        //      1.  Apply diffratio
        //      2.  reset old_rowtotals
        //      3.  Get row and col tots

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   06/08/14   tbe  New procedure for deriving CT detailed HH data
        //   ------------------------------------------------------------------

        public bool ControlToLocal(int modelswitch, StreamWriter foutw, CTMASTER[] ct, int[] rowtotals,
                                 int[] old_rowtotals, int[] coltotals, double[] rowdiffratio, int dimj)
        {
            int i, j;
            int[] passer = new int[dimj];
            string str = "";
            bool doanother = true;

            // control to local
            foutw.WriteLine("CONTROL TO LOCAL");
            for (i = 0; i < NUM_CTS; ++i)
            {

                str = ct[i].ctid + ",";
                rowtotals[i] = 0;
                if (ct[i].HHSovr)
                    continue;
                for (j = 0; j < dimj; ++j)
                {
                    if (modelswitch == 1)
                    {
                        ct[i].hhXsc[j] *= rowdiffratio[j];
                        ct[i].hhXs[j] = (int)(ct[i].hhXs[j] + .5);
                        rowtotals[i] += ct[i].hhXs[j];
                        coltotals[j] += ct[i].hhXs[j];
                        str += ct[i].hhXs[j] + ",";
                    }   // end if
                    else if (modelswitch == 2)
                    {
                        ct[i].hhwocc[j] *= rowdiffratio[j];
                        ct[i].hhwoc[j] = (int)(ct[i].hhwocc[j] + .5);
                        rowtotals[i] += ct[i].hhwoc[j];
                        coltotals[j] += ct[i].hhwoc[j];
                        str += ct[i].hhwoc[j] + ",";
                    } // end else if
                    else
                    {
                        ct[i].hhworkersc[j] *= rowdiffratio[j];
                        ct[i].hhworkers[j] = (int)(ct[i].hhworkersc[j] + .5);
                        rowtotals[i] += ct[i].hhworkers[j];
                        coltotals[j] += ct[i].hhworkers[j];
                        str += ct[i].hhworkers[j] + ",";
                    }
                }  // end for j

                str += rowtotals[i] + ",";

                if (rowtotals[i] > 0)
                    rowdiffratio[i] = (double)old_rowtotals[i] / (double)rowtotals[i];
                else
                    rowdiffratio[i] = 0;
                doanother = false;

                str += rowdiffratio[i];
                foutw.WriteLine(str);
                old_rowtotals[i] = rowtotals[i];
            }   // end for i

            str = "COLUMN TOTALS" + ",";
            for (j = 0; j < dimj; ++j)
                str += coltotals[j] + ",";
            foutw.WriteLine(str);
            foutw.WriteLine("END OF CONTROL TO LOCAL");
            foutw.Flush();
            // check for rowdiffratio in range

            for (i = 0; i < NUM_CTS; ++i)
            {
                if (ct[i].HHSovr)
                    continue;
                if ((rowdiffratio[i] < .99 || rowdiffratio[i] > 1.01) && rowdiffratio[i] != 0)
                {
                    doanother = true;
                    break;
                }  // end if
            }  // end for i

            return doanother;
        }   // end procedure ControlToLocal()

        //******************************************************************************************************

        // DoFinishChecks()
        // build temp parms for calling finish routines
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   06/08/14   tbe  New procedure for deriving CT detailed HH data
        //   ------------------------------------------------------------------
        public void DoFinishChecks(int modelswitch, StreamWriter foutw, CTMASTER[] ct, REG reg, int[] rowtotals, int[] coltotals, int dimj)
        {
            int i, j;
            bool call_finish1, call_finish2;
            int[] indexer = new int[NUM_CTS];
            int[] dcontrol = new int[NUM_CTS];
            int[,] dother = new int[NUM_CTS, 7];
            int[,] matrx = new int[NUM_CTS, dimj];
            int[] nurowt = new int[NUM_CTS], check1 = new int[dimj];
            int[] ovr = new int[NUM_CTS];
            int[] save1b = new int[NUM_CTS];  // these are temp arrays used to store values befoe and after controlling
            int[] save2b = new int[NUM_CTS];
            int[] save1a = new int[NUM_CTS];
            int[] save2a = new int[NUM_CTS];
            string str = "";
            //-----------------------------------------------------------------------------------------

            call_finish1 = false;
            call_finish2 = false;
            Array.Clear(ovr, 0, ovr.Length);

            for (i = 0; i < NUM_CTS; ++i)
            {
                if (ct[i].HHSovr && modelswitch == 1)
                {
                    ovr[i] = 1;
                    nurowt[i] = 0;
                }
                else
                    nurowt[i] = ct[i].hh;

                if (!ct[i].HHSovr && !call_finish1 && (rowtotals[i] != ct[i].hh))
                    call_finish1 = true;

                for (j = 0; j < dimj; ++j)
                {
                    if (modelswitch == 1)
                    {
                        if (!ct[i].HHSovr)
                            matrx[i, j] = ct[i].hhXs[j];
                        else
                            matrx[i, j] = 0;
                    }   // end if

                    else if (modelswitch == 2)
                    {
                        // check for threshold before filling controlling arrrays
                        // if the hh w kids is > 500
                        //if (ct[i].hhwoc[1] < 500)
                        //{
                        //    matrx[i, j] = 0; // don't pass this value
                        //    ct[i].HHWOCovr = true;   // set the override flag
                        //    reg.hhwocAdjusted[j] -= ct[i].hhwoc[j];  // adjust the regional controls to exclude the overridden ct
                        //}   // end if

                        //else
                        matrx[i, j] = ct[i].hhwoc[j];
                    }  // end else if
                    else
                        matrx[i, j] = ct[i].hhworkers[j];
                }  // end for j
            }   // end for i

            // sort the data before sending to the finish routines;  
            // this uses 3 arrays
            // the first array indexer carries the original index in the ct array
            // the second array dcontrol carries the controlling variable for the finish checks, hh
            // the third array carries the actual data being sorted

            for (i = 0; i < NUM_CTS; ++i)
            {
                indexer[i] = i;
                if (modelswitch == 2)   // for hh w kids sort them on descending value of hh with kids; 
                    dcontrol[i] = ct[i].hhwoc[1];
                for (j = 0; j < dimj; ++j)
                    dother[i, j] = matrx[i, j];
            }  // end for i

            if (modelswitch == 2)
                CU.cUtil.DescendingSortMulti(indexer, dcontrol, dother, NUM_CTS, dimj);

            //check for finish1
            for (j = 0; j < dimj; ++j)
            {
                if (modelswitch == 1)
                    check1[j] = reg.hhXsAdjusted[j];
                else if (modelswitch == 2)
                    check1[j] = reg.hhwocAdjusted[j];
                else
                    check1[j] = reg.hhworkers[j];
            }  // end for j

            if (call_finish1)
            {
                CU.cUtil.finish1(dother, nurowt, check1, NUM_CTS, dimj);
            }  // end if

            // get new row and col totals
            Array.Clear(rowtotals, 0, rowtotals.Length);
            Array.Clear(coltotals, 0, coltotals.Length);

            for (i = 0; i < NUM_CTS; ++i)
            {
                for (j = 0; j < dimj; ++j)
                {
                    rowtotals[i] += dother[i, j];
                    coltotals[j] += dother[i, j];
                }   // end for j
            }   // end for i

            // check for finish2
            for (j = 0; j < dimj; ++j)
            {

                if (!call_finish2 && coltotals[j] != check1[j])
                {
                    call_finish2 = true;
                    break;
                }  // end if

            }   // end for i

            // store the hh w kids before finish routines
            if (modelswitch == 2)
            {
                for (i = 0; i < NUM_CTS; ++i)
                {
                    save1b[i] = dother[i, 0];
                    save2b[i] = dother[i, 1];
                }   // end for i
            }  // end if

            if (call_finish2)
                CU.cUtil.finish2(dother, check1, NUM_CTS, dimj);

            // store the hh w kids before finish routines
            if (modelswitch == 2)
            {
                for (i = 0; i < NUM_CTS; ++i)
                {
                    save1a[i] = dother[i, 0];
                    save2a[i] = dother[i, 1];
                }   // end for i
            }  // end if
            //back from finish routines
            // restore the order of the finished data
            if (modelswitch == 2)
            {
                for (i = 0; i < NUM_CTS; ++i)
                {
                    int inew = indexer[i];
                    for (j = 0; j < dimj; ++j)
                        matrx[inew, j] = dother[i, j];
                }  // end for j
            }   // end if
            else
            {
                for (i = 0; i < NUM_CTS; ++i)
                {
                    for (j = 0; j < dimj; ++j)
                        matrx[i, j] = dother[i, j];
                }  // end for j
            }  // end else

            // get new row and col totals
            Array.Clear(rowtotals, 0, rowtotals.Length);
            Array.Clear(coltotals, 0, coltotals.Length);
            for (i = 0; i < NUM_CTS; ++i)
            {
                for (j = 0; j < dimj; ++j)
                {
                    rowtotals[i] += matrx[i, j];
                    coltotals[j] += matrx[i, j];
                }   // end for j
            }   // end for i

            foutw.WriteLine("AFTER FINISH ROUTINES");

            // replace matrix data in ct data class
            for (i = 0; i < NUM_CTS; ++i)
            {
                if (ct[i].HHSovr && ct[i].HHWOCovr)
                    continue;
                str = ct[i].ctid.ToString() + ",";
                for (j = 0; j < dimj; ++j)
                {
                    if (modelswitch == 1)
                    {
                        if (!ct[i].HHSovr)
                            ct[i].hhXs[j] = matrx[i, j];
                    }  // end if
                    else if (modelswitch == 2)
                    {
                        if (!ct[i].HHWOCovr)
                            ct[i].hhwoc[j] = matrx[i, j];
                    }  // end else if
                    else
                        ct[i].hhworkers[j] = matrx[i, j];

                    str += matrx[i, j] + ",";

                }   // end for j
                str += rowtotals[i].ToString();
                foutw.WriteLine(str);
            }   // end for i
            str = " COLUMN TOTALS, ";
            for (j = 0; j < dimj; ++j)
                str += coltotals[j] + ",";
            foutw.WriteLine(str);

            str = "REG HHS CONTROLS, ";
            for (j = 0; j < dimj; ++j)
            {
                if (modelswitch == 1)
                    str += reg.hhXsAdjusted[j] + ",";
                else if (modelswitch == 2)
                    str += reg.hhwoc[j] + ",";
                else
                    str += reg.hhworkers[j] + ",";
            }  // ed for j

            foutw.WriteLine(str);

            foutw.Flush();

        }   // end DoFinishChecks()

        //***********************************************************************************
        #endregion

        #region Miscellaneous utilities

        // procedures

        //   GetCTIndex() - determine the index of the ct with ctid
        //   ProcessParms() - Build the table names from runtime parms

        //   WritemgraData() - Write the controlled data to ASCII for bulk loading 
        //   WriteCTData() - write ct data to ASCII
        //   WriteToStatusBox - display status text

        //---------------------------------------------------------------------------------------------------------

        //GetCTIndex()

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/09/11   tb   initial coding

        //   ------------------------------------------------------------------

        public Int32 GetCTIndex(CTMASTER[] ct, int i)
        {
            int j;
            int ret = 9999;
            for (j = 0; j < NUM_CTS; ++j)
            {
                if (ct[j].ctid == i)
                {
                    ret = j;
                    break;
                }   // end if
            } // end for j
            return ret;
        } // end procedure GetCTIndex

        //**************************************************************************************       

        //  WriteCTData() 
        // Write the ct data to ASCII 

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/11   tb   initial coding

        //   ------------------------------------------------------------------
        public void WriteCTData(StreamWriter foutt, int ctid, int[] passer, int rowtotal, double diffratio, int ii, int dimj)
        {
            string str = (ii + 1) + "," + ctid.ToString() + ",";
            for (int k = 0; k < dimj; k++)
            {
                str += passer[k] + ",";

            } // end for k    
            str += rowtotal + "," + diffratio;
            try
            {
                foutt.WriteLine(str);
                foutt.Flush();
            }
            catch (IOException exc)      //exceptions here
            {
                MessageBox.Show(exc.Message + " File Write Error");
                return;
            }

        } // end procedure WriteCTData

        //**************************************************************************

        // WriteToStatusBox()
        //Display the current processing status to the form


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
            } // end if
            // Invoked from another thread.  Show progress asynchronously.
            else
            {
                WriteDelegate write = new WriteDelegate(WriteToStatusBox);
                Invoke(write, new object[] { status });
            }
        }     //end WriteToStatusBox

        //*****************************************************************************
        #endregion

        #region pdhh extraction utilities
        // procedures

        //   ExtractCTHHData()

        //-----------------------------------------------------------------------------------

        //ExtractCTHHData()

        // populate ct array with hh data from popest_mgra
        // db tables
        // the hh and hhp data for the CT come from the mgra estimates table      

        // the regional %distributions of workers by HHS are in the form of a 2-dimensional matrix
        // the matrix is dimensioned (row) HHS by col #workers

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/09/11   tb   started initial coding
        //   ------------------------------------------------------------------
        public void ExtractCTHHData(CTMASTER[] ct, REG reg)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int i = 0, j = 0, increm = 0;
            int index;
            int[] tempreg = new int[NUM_HHXS];
            //----------------------------------------------------------------

            WriteToStatusBox("Filling CT Arrays");
            for (i = 0; i < NUM_HHXS; ++i)
            {
                if (i < NUM_HHWORKERS)
                    reg.hhworkers[i] = new int();
                if (i < 2)
                {
                    reg.hhwoc[i] = new int();
                    reg.hhwocAdjusted[i] = new int();
                }
                reg.hhXs[i] = new int();
            }  // end for i

            // fill regional HH controls
            sqlCommand1.CommandText = String.Format(appSettings["selectAllWhere"].Value, TN.popestControlsHHRegion, fyear);
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    increm = 1;     // skip estimates_year
                    for (i = 0; i < NUM_HHXS; ++i)
                    {
                        reg.hhXs[i] = rdr.GetInt32(increm++); // skip estimates_year in mapping query results
                    }   // end for i
                    reg.hh = rdr.GetInt32(increm++);
                    reg.hhp = rdr.GetInt32(increm++);
                    reg.hhwoc[1] = rdr.GetInt32(increm++);
                    reg.hhwoc[0] = rdr.GetInt32(increm++);
                    reg.hhwocAdjusted[0] = reg.hhwoc[0];
                    reg.hhwocAdjusted[1] = reg.hhwoc[1];

                    for (i = 0; i < NUM_HHWORKERS; ++i)
                    {
                        reg.hhworkers[i] = rdr.GetInt32(increm++);
                    }   // end for

                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }

            // fill regional HH X workers
            // this is a 2-dimension array - fills by row each row represents the  distribution of workers in HHSize (row)

            sqlCommand1.CommandText = String.Format(appSettings["selectAllWhere"].Value, TN.distributionHHWorkersRegion, fyear);
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    int row = rdr.GetInt32(1) - 1;   // skip estimates_year
                    for (j = 0; j < NUM_HHWORKERS; ++j)
                    {
                        reg.hhworkersp[row, j] = rdr.GetDouble(j + 2);
                    }   // end for i

                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }
            // get HH and HHP from mgra data table
            // fill regional HH and HHP
            this.sqlCommand1.CommandText = String.Format(appSettings["selectPDHH1"].Value, TN.popestMGRA, fyear);
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    reg.hh = rdr.GetInt32(0);
                    reg.hhp = rdr.GetInt32(1);
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }

            // fill ct HH from mgra data
            sqlCommand1.CommandText = String.Format(appSettings["selectPDHH2"].Value, TN.popestMGRA, TN.xref, fyear);

            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                i = 0;
                while (rdr.Read())
                {
                    ct[i].ctid = rdr.GetInt32(0);
                    ct[i].hh = rdr.GetInt32(1);
                    ct[i].hhp = rdr.GetInt32(2);
                    if (ct[i].hh > 0)
                        ct[i].hhs = (double)(ct[i].hhp) / (double)(ct[i].hh);
                    else
                        ct[i].hhs = 0;
                    ++i;
                    if (i >= NUM_CTS)
                        break;
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }

            // get ct error factors
            this.sqlCommand1.CommandText = String.Format(appSettings["selectPDHH3"].Value, TN.errorFactorsHHSCT, fyear);
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    // skip 1st index which has estimates_year
                    i = rdr.GetInt32(1);
                    index = GetCTIndex(ct, i);
                    for (j = 0; j < NUM_HHXS; ++j)
                        ct[index].hhXsef[j] = rdr.GetDouble(2 + j);

                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }

            // get ct hhwoc %
            sqlCommand1.CommandText = String.Format(appSettings["selectPDHH3"].Value, TN.distributionHHWOC, fyear);

            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    // skip first index popest year
                    i = rdr.GetInt32(1);
                    index = GetCTIndex(ct, i);

                    if (index == 9999)
                    {
                        MessageBox.Show("Bad CT Index on ct = " + i);
                    }
                    ct[index].hhwocp = new double[4];
                    for (j = 0; j < 4; ++j)
                        ct[index].hhwocp[j] = rdr.GetDouble(2 + j);

                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }

            // get ct overrides
            if (useOverrides)
            {
                sqlCommand1.CommandText = String.Format(appSettings["selectPDHH3"].Value, TN.overridesHHSDetail, fyear);
                Array.Clear(tempreg, 0, tempreg.Length);
                try
                {
                    sqlConnection.Open();
                    rdr = sqlCommand1.ExecuteReader();
                    while (rdr.Read())
                    {
                        // skip first index popest year
                        i = rdr.GetInt32(1);
                        index = GetCTIndex(ct, i);
                        if (index == 9999)
                        {
                            MessageBox.Show("Bad CT Index on ct = " + i);
                        } // end if
                        ct[index].HHSovr = true;
                        ct[index].hhXsop = new double[NUM_HHXS];
                        ct[index].hhXs = new int[NUM_HHXS];
                        int temp1 = 0;
                        for (j = 1; j < NUM_HHXS; ++j)  // skip first bin, cause it gets computed as a residual
                        {
                            ct[index].hhXsop[j] = rdr.GetDouble(2 + j);   // the overrides is a float %
                            ct[index].hhXs[j] = (int)(ct[index].hhXsop[j] * (double)ct[index].hh);  // apply the % to total hh
                            temp1 += ct[index].hhXs[j];  // save the cumulative hh
                            tempreg[j] += ct[index].hhXs[j]; //here is the adjustment to the regional total
                        }   // end for j

                        ct[index].hhXs[0] = ct[index].hh - temp1;  // derive the first bin as a residual
                        if (ct[index].hhXs[0] < 0)  //constrain to 0
                            ct[index].hhXs[0] = 0;
                        tempreg[0] += ct[index].hhXs[0];  // add to regional adjustment

                    }   // end while

                }  // end try
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString(), exc.GetType().ToString());

                }  // end catch
                finally
                {
                    sqlConnection.Close();
                }
            }   // end if

            // adjust the regional controls for the sum of the overrides
            for (j = 0; j < NUM_HHXS; ++j)
                reg.hhXsAdjusted[j] = reg.hhXs[j] - tempreg[j];

            // get ratio kids/hh fro hh with kids from 2000 census
            this.sqlCommand1.CommandText = String.Format(appSettings["selectPDHH4"].Value, TN.ct10Kids);

            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {

                    i = rdr.GetInt32(0);
                    index = GetCTIndex(ct, i);
                    if (index == 9999)
                    {
                        MessageBox.Show("Bad CT Index on ct = " + i);
                    }
                    try
                    {
                        ct[index].kids_hh_wkids = rdr.GetDouble(1);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.ToString(), exc.GetType().ToString());

                    }  // end catch
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }
            // get total children 0 = 17
            this.sqlCommand1.CommandText = String.Format(appSettings["selectPDHH5"].Value, TN.detailedPopTabCT, fyear);
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {

                    i = rdr.GetInt32(0);
                    index = GetCTIndex(ct, i);
                    if (index == 9999)
                    {
                        MessageBox.Show("Bad CT Index on ct = " + i);
                    }
                    ct[index].kids = rdr.GetInt32(1);
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }

            // fill the ct mgra list
            for (i = 0; i < NUM_CTS; ++i)
            {
                int counter = 0;
                ct[i].num_mgras = 0;
                this.sqlCommand1.CommandText = String.Format(appSettings["selectPDHH6"].Value, TN.popestMGRA, TN.xref, ct[i].ctid, fyear);

                try
                {
                    sqlConnection.Open();
                    rdr = sqlCommand1.ExecuteReader();
                    while (rdr.Read())
                    {
                        //ct[i].m[counter] = new MDHH();
                        ct[i].m[counter].mgraid = rdr.GetInt32(0);
                        ct[i].m[counter].hh = rdr.GetInt32(1);
                        ct[i].m[counter].hhp = rdr.GetInt32(2);
                        if (ct[i].m[counter].hh > 0)
                            ct[i].m[counter].hhs = (double)ct[i].m[counter].hhp / (double)ct[i].m[counter].hh;
                        ++counter;

                    }   // end while
                    rdr.Close();
                    ct[i].num_mgras = counter;
                }  // end try
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString(), exc.GetType().ToString());

                }  // end catch
                finally
                {
                    sqlConnection.Close();
                }  // end finally
            }  // end for i

        } // end procedure ExtractCTHHDATA

        //*********************************************************************************************


        #endregion

        #region SQL command procedures

        // procedures
        //    BulkLoadMGRADHH() - Bulk loads ASCII to popest MGRA     
        //    BulkLoadPOPESTDHH() - run sql commands truncate and reload detailed hh data
        //------------------------------------------------------------------------------------------------


        /*  BulkLoadMGRADHH() */
        /// Bulk loads ASCII to popest MGRA

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------
        public void BulkLoadMGRADHH()
        {
            string fo;

            fo = networkPath + "pdmgra";

            try
            {
                sqlConnection.Open();
                WriteToStatusBox("TRUNCATING DETAILED POPEST MGRA HH TABLE");
                sqlCommand1.CommandText = String.Format(appSettings["deleteFrom"].Value, TN.popestHHDetailMGRA, fyear);
                sqlCommand1.ExecuteNonQuery();

                WriteToStatusBox("BULK LOADING DETAILED POPEST MGRA HH TABLE");
                sqlCommand1.CommandTimeout = 180;
                sqlCommand1.CommandText = String.Format(appSettings["bulkInsert"].Value, TN.popestHHDetailMGRA, fo);
                sqlCommand1.ExecuteNonQuery();

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlConnection.Close();
            }
        } // end procedure BulkLoadMGRADHH()       

        //**********************************************************************************************

        /*  bulkLoadPOPESTDHH() */
        /// Run SQL commands to truncate and reload detailed popest HH data

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/18/11   tb   initial coding

        //   ------------------------------------------------------------------

        public void BulkLoadPOPESTDHH(CTMASTER[] ct, int fyear)
        {
            int i;
            int j;

            WriteToStatusBox("LOADING CT DETAILED HH DATA");
            sqlCommand1.Connection = sqlConnection;
            try
            {
                sqlConnection.Open();
                sqlCommand1.CommandText = String.Format(appSettings["deleteFrom"].Value, TN.popestHHDetailTabCT, fyear);
                sqlCommand1.ExecuteNonQuery();

            }    // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }  // end catch
            finally
            {
                sqlConnection.Close();
            }  // end finally

            string tex = "";
            for (i = 0; i < NUM_CTS; ++i)
            {
                tex = " values (" + fyear + "," + ct[i].ctid + ",";

                for (j = 0; j < NUM_HHXS; ++j)
                    tex += ct[i].hhXs[j] + ",";
                for (j = 0; j < 2; ++j)
                    tex += ct[i].hhwoc[j] + ",";
                for (j = 0; j < NUM_HHWORKERS - 1; ++j)
                    tex += ct[i].hhworkers[j] + ",";
                tex += ct[i].hhworkers[NUM_HHWORKERS - 1] + ")";
                sqlCommand1.CommandText = String.Format(appSettings["insertInto"].Value, TN.popestHHDetailTabCT, tex);

                try
                {
                    sqlConnection.Open();
                    sqlCommand1.ExecuteNonQuery();

                }    // end try
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                }  // end catch
                finally
                {
                    sqlConnection.Close();
                }  // end finally

            }  // end for
        }   // end procedure BulkLoadPOPESTDHH()

        //**********************************************************************************************************************

        #endregion
        #region Miscellaneous button handlers

        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void pdhh_Load(object sender, EventArgs e)
        {

        }
        //********************************************************************
        #endregion

        /* processParams() */

        // Build the table names from runtime parms

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/02/04   tb   initial recoding - moved verification steps to separate routine

        //   ------------------------------------------------------------------

        public void processParams(string year, ref int fyear, ref int lyear)
        {
            useOverrides = checkBox1.Checked;
            fyear = int.Parse(year);
            lyear = fyear - 1;
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                appSettings = config.AppSettings.Settings;
                connectionStrings = config.ConnectionStrings.ConnectionStrings;

                networkPath = String.Format(appSettings["networkPath"].Value);

                MAX_CITIES = int.Parse(appSettings["MAX_CITIES"].Value);
                MAX_MGRAS_IN_CITY = int.Parse(appSettings["MAX_MGRAS_IN_CITY"].Value);
                MAX_MGRAS_IN_CTS = int.Parse(appSettings["MAX_MGRAS_IN_CTS"].Value);
                MAX_POPEST_EXCEPTIONS = int.Parse(appSettings["MAX_POPEST_EXCEPTIONS"].Value);
                NUM_HHWORKERS = int.Parse(appSettings["NUM_HHWORKERS"].Value);
                NUM_HHXS = int.Parse(appSettings["NUM_HHXS"].Value);
                NUM_CTS = int.Parse(appSettings["NUM_CTS"].Value);
                NUM_MGRAS = int.Parse(appSettings["NUM_MGRAS"].Value);

                sqlConnection.ConnectionString = connectionStrings["ConcepDBConnectionString"].ConnectionString;
                this.sqlCommand1.Connection = this.sqlConnection;

                TN.ct10Kids = String.Format(appSettings["ct10Kids"].Value);
                TN.detailedPopTabCT = String.Format(appSettings["detailedPopTabCT"].Value);
                TN.distributionHHWorkersRegion = String.Format(appSettings["distributionHHWorkersRegion"].Value);
                TN.distributionHHWOC = String.Format(appSettings["distributionHHWOC"].Value);
                TN.errorFactorsHHSCT = String.Format(appSettings["errorFactorsHHSCT"].Value);
                TN.overridesHHSDetail = String.Format(appSettings["overridesHHSDetail"].Value);
                TN.popestControlsHHRegion = String.Format(appSettings["popestControlsHHRegion"].Value);
                TN.popestHHDetailMGRA = String.Format(appSettings["popestHHDetailMGRA"].Value);
                TN.popestHHDetailTabCT = String.Format(appSettings["popestHHDetailTabCT"].Value);
                TN.popestMGRA = String.Format(appSettings["popestMGRA"].Value);
                TN.xref = String.Format(appSettings["xref"].Value);

            }  // end try

            catch (ConfigurationErrorsException c)
            {
                throw c;
            }

        } // end procedure processParams()

        //************************************************************************************       

               
    } // end public class pdhh
    
    //*********************************************************************************************
}
