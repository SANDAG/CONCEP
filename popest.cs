/* 
 * Source File: popest.cs
 * Program: concep
 * Version 4
 * Programmer: tbe
 * Description:
 *		This is the POPEST MGRAS component of CONCEP
 *		Version 4 introduces concep.config.exe to store all global constants, queries, table names and file names
 *		version 3.5 has changes to use Series 13 geogaphies
 *		version 3.3 adds computations for CT-level HH detail, including HH by size of HH, HH by presence of children and HH by # workers
 *		            steps: build CT-level HH, HHP from mgra data
 *		                   load CT HH by HHS category distributions from Census 2000
 *		                   use those distributions to get initial estimate of CT HH by HHS category using poisson distribution formula
 *		                   do IPF to control HH to CT and HHS by category to regional totals
 *		                   the issue then becomes getting as close to regional HHP as possible by reconstituting HHP from HHS by category
 *		                   We'll also be doing similar distributions for HH by presence of children and HH by workers
 *		                   TBD - discription of those processes
 *      version 3.1 uses hs from landcore rather than deriving a change file and only works for 
 *      popest 2004 and popest 2005
 *      version 3.0 for SGRAs
 *      version 2.5 is a recode eliminating split tract processing using city totals for controls
 *      version 2.0 adds the detail for structure type for hs and hh
 * 
 * NOTE:  FOR EACH USER THAT WANTS TO RUN CONCEP FROM THE NETWORK VERSION, THE SECURITY STUFF HAS TO BE COMPLETED
 *             THERE IS A PROCESS OUTLINED IN THE FORMAL DOC.
 *			   THIS ALSO INCLUDES ESTABLISHING A STRONG NAME FOR THE ASSEMBLY
 *                STEPS:  NAVIGATE TO C:\Program Files\Microsoft Visual Studio .NET 2003\SDK\v1.1\Bin
 *                        execute the StrongName Utility sn.exe - k <filename.snk>  (example: AggData.snk)
 *                        copy this file to the project directory  <project>\obj\release and <project>\obj\debug
 *						  in the AssemblyInfo.cs file, add the name to the AssemblyKeyFile
 *						  compile and distribute the entire directory include the ".snk" to the network path
 */
/*   Database Description:
 *		SQL Server Database: concep
 *			Tables:
 *			popest_MGRA	: reduced data set POPEST MGRAs, indexed by estimates_year
 *          controls_popest_city : adjusted dof city controls, indexed by estimates_year
 *          controls_popest_HH_region : regional totals for HHS X category
 *          popest_HHdetail_tab_ct:  tabular output from detailed HH calcs
 *          operational_parameters_popest_HHdetail_ct : detailed HH input parms (this is a normalized i

 
*/						
 //Revision History
 //   Date       By   Description
 //   ------------------------------------------------------------------
 //   06/05/02   tb   initial coding
 //   06/06/02   tb   got PSGRA it running for 2001 
 //   06/10/02   tb   got POPEST running for 2001 
 //   06/11/02   tb   split the code to separate modules 
 //   09/20/02   tb   updated for new sr10 mgras and census allocation for sf1
 //   02/25/03   tb   updated for structure type detail for hs and hh
 //   04/08/04   tb   added validation code and multiple thread stuff
 //   06/11/04   tb   recode for ver 2.5 - eliminating split tracts
 //   06/16/04   tb   added vac overrides for mgra hs_sf
 //   07/01/04   tb   added error checking for hs < 0 with console messages
 //   02/01/05   tb   recode for version 3.0 SGRAs
 //   10/04/05   tb   changes in files to handle combined gq_civ and mil based on parcels
 //   10/07/05   tb   changes for Version 3.1 using hs derived from landcore aggregated to mgra
 //   06/08/11   tb   changes for Version 3.3 CT HH detail
 //   07/06/12   tb   changes for Version 3.5 Series 13 estimates
 //   10/23/12   tb   changes for Version 4
 //   ------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Configuration;


namespace PMGRA
{
    // need this to use the WriteToStatusBox on different thread
   
    /// Summary description for popest.
    /// </summary>
    public class pmgra : System.Windows.Forms.Form
    { 
        delegate void WriteDelegate(string status);
        public class address_data
        {
            public int mgra;
            public int sf;
            public int sfmu;
            public int mf;
            public int mh;
            public int hs;
        } // end class

        public class CityData
        {
            public int city;
            public int gq;
            public int gq_civ;
            public int gq_mil;
            public int gq_civ_college;
            public int gq_civ_other;
            public int hs;
            public int hs_sf;
            public int hs_sfmu;
            public int hs_mf;
            public int hs_mh;
            public int hhp;
            public int hh;
            public int hh_sf;
            public int hh_sfmu;
            public int hh_mf;
            public int hh_mh;
            public int pop;
            public double occ_sf;
            public double occ_sfmu;
            public double occ_mf;
            public double occ_mh;
            public double hhs;
            public int chg_sf;
            public int chg_sfmu;
            public int chg_mf;
            public int chg_mh;
            public int chg_hs;
            public int abschg_hs;
            public address_data[] mu;
            public int num_mgras;
            public int unit_flag;
        } // end class

        // POPEST datailed HH data classes

        public class REG  //regional control for detailed HH data class
        {
            public int hhp;   //regional hhp control
            public int hh;    // regional hh control

        } // end class

        public class CTMASTER  //CT-level detailed HH data class
        {
            public int ctid;
            public int hhp;
            public int hhpc;                          // ct hhp reconstituted
            public int hh;                            // ct total hh
            public double hhs;                        // ct hhs
            public int num_mgras;
            public int kids;           // number of children 0 - 17
            public int kidsl;          // minimum number of children computed from hhwc * 1
            public int kidsu;          // maximum number of kids computed from hhwc * 3(or some other acceptable number like 2.5)
            public double kids_hh_wkids;  // number of kids/hh with kids from 2000 census

        }  // end class*

        public class PMGRA //main popest data class
        {
            public int mgra;
            public int city;
            public int ct;
            public int cityct10;
            public int gq;
            public int gq_civ;
            public int gq_mil;
            public int gq_civ_college;
            public int gq_civ_other;
            public int hs;
            public int hs_sf;
            public int hs_sfmu;
            public int hs_mf;
            public int hs_mh;
            public int hhp;
            public int hh;
            public int hh_sf;
            public int hh_sfmu;
            public int hh_mf;
            public int hh_mh;
            public int pop;
            public int old_hs;
            public double occ;
            public double occ_sf;
            public double occ_sfmu;
            public double occ_mf;
            public double occ_mh;
            public double hhs;

        }  // end class

        public class MASTER
        {
            public int counter;
            public PMGRA[] d;
        }

        public class TableNames
        {
            public string GQControlled;
            public string popestMGRA;
            public string popestControls;
            public string popestUpdate;
            public string popestVacOvr;
            public string popestHHSOvr;
            public string xref;
        } // end tablenames

        public static int MAX_MGRAS_IN_CITY;  // max number of MGRAs in any city
        public static int MAX_CITIES;
        public static int NUM_MGRAS;
        public static int MAX_POPEST_EXCEPTIONS;
        public static int V_HS_RATE;
        public static int V_POP_RATE;
        public static int NUM_CTS;     // number of census tracts (series 12)
        public static int MAX_MGRAS_IN_CTS;  //maximum number of mgras in any ct

        public Configuration config;
        public KeyValueConfigurationCollection appSettings;
        public ConnectionStringSettingsCollection connectionStrings;

        public TableNames TN = new TableNames();
        public string networkPath;

        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label label2;
        private System.Data.SqlClient.SqlCommand sqlCommand1;
        private System.Data.SqlClient.SqlConnection sqlConnection;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkOverrides;
        private System.Windows.Forms.Button btnRunPMGRA;
        private System.Windows.Forms.ComboBox txtYear;
        private IContainer components;
        private int fyear;
        private int lyear;
        private bool useOverrides;

        public pmgra()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            this.label3 = new System.Windows.Forms.Label();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.btnExit = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.btnRunPMGRA = new System.Windows.Forms.Button();
            this.sqlCommand1 = new System.Data.SqlClient.SqlCommand();
            this.sqlConnection = new System.Data.SqlClient.SqlConnection();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.chkOverrides = new System.Windows.Forms.CheckBox();
            this.txtYear = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(32, 296);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 26);
            this.label3.TabIndex = 12;
            this.label3.Text = "Status";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(24, 208);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(448, 80);
            this.txtStatus.TabIndex = 11;
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
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
            this.label2.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(112, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(152, 24);
            this.label2.TabIndex = 9;
            this.label2.Text = "Estimates Year";
            // 
            // btnRunPMGRA
            // 
            this.btnRunPMGRA.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRunPMGRA.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunPMGRA.Location = new System.Drawing.Point(24, 144);
            this.btnRunPMGRA.Name = "btnRunPMGRA";
            this.btnRunPMGRA.Size = new System.Drawing.Size(96, 58);
            this.btnRunPMGRA.TabIndex = 15;
            this.btnRunPMGRA.Text = "Run ";
            this.btnRunPMGRA.UseVisualStyleBackColor = false;
            this.btnRunPMGRA.Click += new System.EventHandler(this.btnRunPMGRA_Click);
            // 
            // sqlConnection
            // 
            this.sqlConnection.ConnectionString = "Data Source=PILA\\SDGINTDB;Initial Catalog=concep_test;Persist Security Info=True;" +
    "User ID=concep_app;Password=c0nc3p_@pp";
            this.sqlConnection.FireInfoMessageEventOnUserErrors = false;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(24, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(144, 40);
            this.panel1.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Book Antiqua", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Blue;
            this.label1.Location = new System.Drawing.Point(-8, -1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(145, 40);
            this.label1.TabIndex = 0;
            this.label1.Text = "POPEST";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // chkOverrides
            // 
            this.chkOverrides.Checked = true;
            this.chkOverrides.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOverrides.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkOverrides.Location = new System.Drawing.Point(270, 64);
            this.chkOverrides.Name = "chkOverrides";
            this.chkOverrides.Size = new System.Drawing.Size(288, 24);
            this.chkOverrides.TabIndex = 18;
            this.chkOverrides.Text = "Use Vac and HHS  Overrides";
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
            this.txtYear.Location = new System.Drawing.Point(24, 64);
            this.txtYear.Name = "txtYear";
            this.txtYear.Size = new System.Drawing.Size(72, 31);
            this.txtYear.TabIndex = 19;
            // 
            // pmgra
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(581, 357);
            this.Controls.Add(this.txtYear);
            this.Controls.Add(this.chkOverrides);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnRunPMGRA);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.label2);
            this.Menu = this.mainMenu1;
            this.Name = "pmgra";
            this.Text = "CONCEP Version 4 - POPEST";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region PMGRA Run button processing

        /*  btnRunPMGRA_Click() */

        /// method invoker for run button - starts another thread
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   04/08/04   tb   added new thread code

        //   ------------------------------------------------------------------
        private void btnRunPMGRA_Click(object sender, System.EventArgs e)
        {
            //build the table names from runtime args
            processParams(txtYear.SelectedItem.ToString(), ref fyear, ref lyear);
            MethodInvoker mi = new MethodInvoker(beginPMGRAWork);
            mi.BeginInvoke(null, null);
        } // end method btnRunPMGRA_Click()

        //***********************************************************************************************

        /*  beginPMGRAwork() */

        /// POPEST MGRA Main
 
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding
        //   04/08/04   tb   added code for separate threads and try/catch for all sql statements
        //   07/01/04   tb   additional checks for inconsistencies in change data
        //   ------------------------------------------------------------------
        private void beginPMGRAWork()
        {
            int i,j;
            CityData[] city = new CityData[MAX_CITIES];
            MASTER[] md = new MASTER[MAX_CITIES];

            try
            {
                sqlCommand1 = new System.Data.SqlClient.SqlCommand();
                sqlCommand1.CommandTimeout = 180;
                sqlCommand1.Connection = sqlConnection;
                for (i = 0; i < MAX_CITIES; ++i)
                {
                    city[i] = new CityData();
                    md[i] = new MASTER();
                    city[i].mu = new address_data[MAX_MGRAS_IN_CITY];
                    for (j = 0; j < MAX_MGRAS_IN_CITY; ++j)
                        city[i].mu[j] = new address_data();
                    md[i].d = new PMGRA[MAX_MGRAS_IN_CITY];

                }  // end for i

                //fill the city arrays
                FillCityArrays(city);
                ExecuteMGRAUpdates();
                DoHS(city);
                ControlMGRAAll(city, md);
                BulkLoadPOPEST();
                    
                WriteToStatusBox("COMPLETED POPEST TO MGRA RUN");
             
            } // end try

            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                return;
            } // end catch

        } // end beginPMGRAWork()

        //*************************************************************************************************
        #endregion

        #region PMGRA processing

        // procedures
        //    ControlMGRAAll() - Control hs, hh, gq_civ and hhp
        //    DoHS() - Process hs_chg_mgra data and control to city changes 
        // --------------------------------------------------------------------------------

        /*  ControlMGRAAll() */

        /// Control hs, hh, gq_civ and hhp

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/10/02   tb   initial coding

        //   ------------------------------------------------------------------
        public void ControlMGRAAll(CityData[] s, MASTER[] md)
        {
            int i, j, k, target, ii, kk, real_index;
            int status, mgra1, city;
            FileStream fout;		//file stream class

            System.Data.SqlClient.SqlDataReader rdr;
            int[] mgra_list = new int[NUM_MGRAS];
            int[] limit = new int[MAX_MGRAS_IN_CITY];     //lower or upper bound used to restrict +/- controlling
            int[] passer = new int[MAX_MGRAS_IN_CITY];	  //temp array with actual sorted data tobe controlled
            int[] t_index = new int[MAX_MGRAS_IN_CITY];	  //array storing real index of data after sorting
            int[] mdata = new int[22];			  //bound array to query result
            double[] mdataf = new double[6];
            //-------------------------------------------------------------------------
            // open output file
            try
            {
                fout = new FileStream(networkPath + String.Format(appSettings["pmgra_out"].Value), FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);

            /* cycle through each city, sum mgras and control to totals */
            WriteToStatusBox("CONTROLLING HS, HH AND GQ DATA");

            sqlCommand1.CommandText = String.Format(appSettings["selectPOPEST3"].Value, TN.popestMGRA, fyear, lyear);

            try
            {
                sqlConnection.Open();     //open the connection
                rdr = this.sqlCommand1.ExecuteReader();     //open the data reader

                // order on mdata array 0:mgra; 1:cityct10; 2:city; 3:ct; 4:hs; 5:sf; 6:sfmu;
                // 7:mf; 8:mh; 9:gq;
                //  10:gq_civ; 11:gq_mil; 12:gq_civ_college, 13: gq_civ_other, 14:hhp; 15:hh; 16:hh_sf; 17:hh_sfmu; 18:hh_mf; 19:hh_mh; 
                //  20:old hs (previous year)  
                //  mdataf 0:vac_sf; 1:vac_mf; 2:vac_mh;3:vac; 4:hhs 

                int sumCV = 0;

                while (rdr.Read())
                {
                    mdata[0] = rdr.GetInt32(0);
                    mdata[1] = rdr.GetInt32(1);
                    mdata[2] = rdr.GetByte(2);

                    for (i = 3; i < 21; ++i)
                        mdata[i] = rdr.GetInt32(i);
                    for (i = 0; i < mdataf.Length; i++)
                        mdataf[i] = rdr.GetDouble(i + 21);

                    mgra1 = mdata[0] - 1;
                    city = mdata[2];   // assign the city id here
                    if (city == 2)
                        sumCV += mdata[4];
                    int tstop = 0;
                    if (city < MAX_CITIES)
                    {
                        j = md[city].counter;
                        md[city].d[j] = new PMGRA();
                        mgra1 = mdata[0];
                        if (mgra1 == 1)
                            tstop = 1;
                        md[city].d[j].mgra = mgra1;
                        md[city].d[j].cityct10 = mdata[1];
                        md[city].d[j].city = mdata[2];
                        md[city].d[j].ct = mdata[3];
                        md[city].d[j].hs = mdata[4];
                        md[city].d[j].hs_sf = mdata[5];
                        md[city].d[j].hs_sfmu = mdata[6];
                        md[city].d[j].hs_mf = mdata[7];
                        md[city].d[j].hs_mh = mdata[8];
                        md[city].d[j].gq = mdata[9];
                        md[city].d[j].gq_civ = mdata[10];
                        md[city].d[j].gq_mil = mdata[11];
                        md[city].d[j].gq_civ_college = mdata[12];
                        md[city].d[j].gq_civ_other = mdata[13];
                        md[city].d[j].hhp = mdata[14];
                        md[city].d[j].hh = mdata[15];
                        md[city].d[j].hh_sf = mdata[16];
                        md[city].d[j].hh_sfmu = mdata[17];
                        md[city].d[j].hh_mf = mdata[18];
                        md[city].d[j].hh_mh = mdata[19];
                        md[city].d[j].old_hs = mdata[20];

                        md[city].d[j].occ_sf = 1 - mdataf[0];
                        md[city].d[j].occ_sfmu = 1 - mdataf[1];
                        md[city].d[j].occ_mf = 1 - mdataf[2];
                        md[city].d[j].occ_mh = 1 - mdataf[3];
                        md[city].d[j].occ = 1 - mdataf[4];
                        md[city].d[j].hhs = mdataf[5];

                        // compute the hhs ; load the regional value if beyond bounds
                        //if (md[city].d[j].hh > 0)
                        //md[city].d[j].hhs = (double)md[city].d[j].hhp / (double)md[city].d[j].hh;
                        if (md[city].d[j].hhs < 1 || md[city].d[j].hhs > 7.0)
                            md[city].d[j].hhs = s[city].hhs;
                        // compute the structure occupancy rates and control
                        // load regional values if beyond bounds
                        md[city].counter++;
                    } // end if
                } // end while
                rdr.Close();

            } // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlConnection.Close();
            }

            for (ii = 1; ii < MAX_CITIES; ++ii)
            {
                int sfsum = 0;
                WriteToStatusBox("Controlling city # " + ii.ToString());

                /* hs_sf */
                j = md[ii].counter;
                target = s[ii].hs_sf;
                //zero the arrays used in the controlling                
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write the data to temporary arrays for sorting before controlling
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hs_sf;
                    t_index[kk] = kk;
                }  // end for kk
                //sort the data in ascending order - this
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call the controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 1);
                if (status > 0)
                {
                    MessageBox.Show("hs_sf roundit city # " + ii.ToString() + " diff = " + status.ToString());
                }  // end if

                /* restore the rounded data to the actual data arrays*/
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hs_sf = passer[kk];
                    sfsum += passer[kk];
                }  // end for

                /* hs_sfmu */
                j = md[ii].counter;
                target = s[ii].hs_sfmu;
                //zero the arrays used in the controlling                
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write the data to temporary arrays for sorting before controlling
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hs_sfmu;
                    t_index[kk] = kk;
                }  // end for
                //sort the data in ascending order - this
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call the controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 1);
                if (status > 0)
                {
                    MessageBox.Show("hs_sfmu roundit city # " + ii.ToString() + " diff = " + status.ToString());
                } // end for

                /* restore the rounded data to the actual data arrays*/
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hs_sfmu = passer[kk];
                }  // end for

                /* hs_mf */
                j = md[ii].counter;
                target = s[ii].hs_mf;
                //zero the arrays used in the controlling
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write the data to temporary arrays for sorting before controlling
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hs_mf;
                    t_index[kk] = kk;
                }     // end for
                //sort the data in ascending order - this
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call the controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 1);
                if (status > 0)
                    MessageBox.Show("hs_mf roundit city # " + ii.ToString() + " diff = " + status.ToString());

                /* restore the rounded data to the actual data arrays*/
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hs_mf = passer[kk];
                } // end for

                /* hs_mh */
                j = md[ii].counter;
                target = s[ii].hs_mh;
                //zero the arrays used in the controlling
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write the data to temporary arrays for sorting before controlling
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hs_mh;
                    t_index[kk] = kk;
                }     // end for
                //sort the data in ascending order - this
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call the controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 1);
                if (status > 0)
                    MessageBox.Show("hs_mh roundit city # " + ii.ToString() + " diff = " + status.ToString());

                /* restore the rounded data to the actual data arrays*/
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hs_mh = passer[kk];
                    md[ii].d[real_index].hs = md[ii].d[real_index].hs_sf +
                        md[ii].d[real_index].hs_mf + md[ii].d[real_index].hs_mh;
                }     /* end for kk */

                //gq_civ_college
                j = md[ii].counter;
                target = s[ii].gq_civ_college;
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write the data to temporary arrays for sorting before controlling
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].gq_civ_college;
                    t_index[kk] = kk;
                } // end for kk
                //sort data in ascending order
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call controlling routine
                status = CU.cUtil.Roundit(passer, limit, target, j, 1);
                if (status > 0)
                    MessageBox.Show("gq_civ_college roundit city # " + ii.ToString() + " diff = " + status.ToString());

                //restore controlled data to actual data arrays
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].gq_civ_college = passer[kk];
                } // end for kk

                //gq_civ_other
                j = md[ii].counter;
                target = s[ii].gq_civ_other;
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write the data to temporary arrays for sorting before controlling
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].gq_civ_other;
                    t_index[kk] = kk;
                } // end for kk
                //sort data in ascending order
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call controlling routine
                status = CU.cUtil.Roundit(passer, limit, target, j, 1);
                if (status > 0)
                    MessageBox.Show("gq_civ_other roundit city # " + ii.ToString() + " diff = " + status.ToString());

                //restore controlled data to actual data arrays
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].gq_civ_other = passer[kk];
                    md[ii].d[real_index].gq = md[ii].d[real_index].gq_civ_college + md[ii].d[real_index].gq_civ_other;
                } // end for kk

                /* hh_sf */
                /* compute the hh from city rates */
                for (k = 0; k < j; k++)
                    md[ii].d[k].hh_sf = (int)((double)(md[ii].d[k].hs_sf) * md[ii].d[k].occ_sf);

                /* control the hh_sf data */
                j = md[ii].counter;     //set the index control

                target = s[ii].hh_sf;
                //zero the temporary arrays
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write data to temporary arrays for sorting
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hh_sf;
                    limit[kk] = md[ii].d[kk].hs_sf;
                    t_index[kk] = kk;
                }     /* end for */

                //sort in ascending order
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 2);
                if (status > 0)
                    MessageBox.Show("hh_sf roundit city # " + ii.ToString() + " diff = " + status.ToString());

                /* restore the rounded data */
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hh_sf = passer[kk];
                }     /* end for kk */

                /* hh_sfmu */
                /* compute the hh from city rates */
                for (k = 0; k < j; ++k)
                {
                    md[ii].d[k].hh_sfmu = (int)((double)(md[ii].d[k].hs_sfmu) * md[ii].d[k].occ_sfmu);
                } // end for k

                /* control the hh_sfmu data */
                j = md[ii].counter;     //set the index control

                target = s[ii].hh_sfmu;
                //zero the temporary arrays
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write data to temporary arrays for sorting
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hh_sfmu;
                    limit[kk] = md[ii].d[kk].hs_sfmu;
                    t_index[kk] = kk;
                } // end for kk

                //sort in ascending order
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 2);
                if (status > 0)
                {
                    MessageBox.Show("hh_sfmu roundit city # " + ii.ToString() + " diff = " + status.ToString());
                } // end if

                /* restore the rounded data */
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hh_sfmu = passer[kk];
                } // end for kk

                /* hh_mf */
                /* compute the hh from city rates */
                for (k = 0; k < j; ++k)
                {
                    md[ii].d[k].hh_mf = (int)((double)(md[ii].d[k].hs_mf) * md[ii].d[k].occ_mf);
                }     /* end for k */

                /* control the hh_mf data */

                j = md[ii].counter;     //set the index control

                target = s[ii].hh_mf;
                //zero the temporary arrays
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write data to temporary arrays for sorting
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hh_mf;
                    limit[kk] = md[ii].d[kk].hs_mf;
                    t_index[kk] = kk;
                }     /* end for */

                //sort in ascending order
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 2);
                if (status > 0)
                    MessageBox.Show("hh roundit city # " + ii.ToString() + " diff = " + status.ToString());

                /* restore the rounded data */
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hh_mf = passer[kk];
                }     /* end for kk */

                /* hh_mh */
                /* compute the hh from city rates */
                for (k = 0; k < j; ++k)
                {
                    md[ii].d[k].hh_mh = (int)((double)(md[ii].d[k].hs_mh) * md[ii].d[k].occ_mh);
                }     /* end for k */

                /* control the hh_mh data */

                j = md[ii].counter;     //set the index control

                target = s[ii].hh_mh;
                //zero the temporary arrays
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write data to temporary arrays for sorting
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hh_mh;
                    limit[kk] = md[ii].d[kk].hs_mh;
                    t_index[kk] = kk;
                }     /* end for */

                //sort in ascending order
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 2);
                if (status > 0)
                    MessageBox.Show("hh roundit city # " + ii.ToString() + " diff = " + status.ToString());

                /* restore the rounded data */
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hh_mh = passer[kk];
                    md[ii].d[real_index].hh = md[ii].d[real_index].hh_sf + md[ii].d[real_index].hh_sfmu + md[ii].d[real_index].hh_mf + md[ii].d[real_index].hh_mh;
                } // end for kk

                /* compute the hhp */
                for (k = 0; k < j; ++k)
                {

                    md[ii].d[k].hhp = (int)((double)(md[ii].d[k].hh) * md[ii].d[k].hhs);
                    if (md[ii].d[k].hhp < md[ii].d[k].hh)
                        md[ii].d[k].hhp = md[ii].d[k].hh;
                }     //end for k

                /* control the hhp data */
                j = md[ii].counter;

                target = s[ii].hhp;
                //zero the temporary arrays
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write data to temp arrays for sorting
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].hhp;
                    limit[kk] = md[ii].d[kk].hh;
                    t_index[kk] = kk;
                } // end for kk

                //sort in ascending order
                CU.cUtil.AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = CU.cUtil.Roundit(passer, limit, target, j, 3);
                if (status > 0)
                    MessageBox.Show("hhp roundit city # " + ii.ToString() + " diff = " + status.ToString());

                /* restore the rounded data */
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];

                    //if (real_index == 0)
                    //tstop = 1;
                    md[ii].d[real_index].hhp = passer[kk];
                    if (md[ii].d[real_index].hh > 0)
                        md[ii].d[real_index].hhs = (double)md[ii].d[real_index].hhp / (double)md[ii].d[real_index].hh;
                } // end for kk
                //write this mgra to ascii for bulk-loading
                WriteMGRAData(foutw, md, ii, j);
            } // end for ii
            foutw.Close();
            fout.Close();     //close the output ascii file
        }  // end procedure ControlMGRAAll

        //******************************************************************************************************************

        // procedure DoHS
        /// Process hs data 
       
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding
        //   10/07/05   tb   changes for Version 3.1 using landcore HS totals
        //   ------------------------------------------------------------------

        public void DoHS(CityData[] s)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int city;
            int cnt;
            int i, j;
            int sf, sfmu, mf, mh, hs;
            int mgra;

            /* HS from hs table */
            WriteToStatusBox("EXTRACTING HS DATA");

            sqlCommand1.CommandText = String.Format(appSettings["selectPOPEST2"].Value, TN.popestMGRA, TN.xref, fyear);

            try
            {
                sqlConnection.Open();
                rdr = this.sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    mgra = rdr.GetInt32(0);
                    city = rdr.GetByte(1);
                    sf = rdr.GetInt32(2);
                    sfmu = rdr.GetInt32(3);
                    mf = rdr.GetInt32(4);
                    mh = rdr.GetInt32(5);
                    hs = sf + sfmu + mf + mh;
                    if (city == 99)
                    {
                        MessageBox.Show("BAD SPLIT INDEX ; CITY = " + city.ToString());
                    } // end if
                    cnt = s[city].num_mgras;
                    s[city].mu[cnt].mgra = mgra;
                    s[city].mu[cnt].sf = sf;
                    s[city].mu[cnt].sfmu = sfmu;
                    s[city].mu[cnt].mf = mf;
                    s[city].mu[cnt].mh = mh;
                    s[city].mu[cnt].hs = hs;
                    s[city].hs += hs;
                    s[city].abschg_hs += Math.Abs(hs);
                    ++s[city].num_mgras;
                }  // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }  // end finally


            for (i = 1; i < MAX_CITIES; ++i)
            {
                WriteToStatusBox("Updating City # " + i.ToString());

                if (s[i].abschg_hs != 0)
                {
                    for (j = 0; j < s[i].num_mgras; ++j)
                    {
                        //build a sql query and update for this mgra
                        sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST1"].Value, TN.popestMGRA, s[i].mu[j].hs, s[i].mu[j].sf,
                                                        s[i].mu[j].sfmu, s[i].mu[j].mf, s[i].mu[j].mh, fyear, s[i].mu[j].mgra);

                        try
                        {
                            sqlConnection.Open();
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
                    } // end for j
                }  // end if
            }  // end for i

        }  // end procedure doHS

        //***************************************************************************************************
        #endregion

        #region Miscellaneous utilities

        // procedures
        
        //   GetCTIndex() - determine the index of the ct with ctid
        //   processParams() - Build the table names from runtime parms
       
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
    
        /* processParams() */

        // Build the table names from runtime parms

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/02/04   tb   initial recoding - moved verification steps to separate routine

        //   ------------------------------------------------------------------

        public void processParams(string year, ref int fyear, ref int lyear)
        {
            useOverrides = chkOverrides.Checked;
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
                V_HS_RATE = int.Parse(appSettings["V_HS_RATE"].Value);
                V_POP_RATE = int.Parse(appSettings["V_POP_RATE"].Value);
                NUM_CTS = int.Parse(appSettings["NUM_CTS"].Value);
                NUM_MGRAS = int.Parse(appSettings["NUM_MGRAS"].Value);

                sqlConnection.ConnectionString = connectionStrings["ConcepDBConnectionString"].ConnectionString;
                this.sqlCommand1.Connection = this.sqlConnection;

                TN.GQControlled = String.Format(appSettings["GQControlled"].Value);
                TN.popestMGRA = String.Format(appSettings["popestMGRA"].Value);
                TN.popestControls = String.Format(appSettings["popestControls"].Value);
                TN.popestUpdate = String.Format(appSettings["popestUpdate"].Value);
                TN.popestVacOvr = String.Format(appSettings["popestVacOvr"].Value);
                TN.popestHHSOvr = String.Format(appSettings["popestHHSOvr"].Value);
                TN.xref = String.Format(appSettings["xref"].Value);

            }  // end try

            catch (ConfigurationErrorsException c)
            {
                throw c;
            }
        } // end procedure processParams()

        //************************************************************************************

        // WriteMGRAData() 
        /// Write the controlled data to ASCII for bulk loading
       
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/10/02   tb   initial coding

        //   ------------------------------------------------------------------
        public void WriteMGRAData(StreamWriter foutw, MASTER[] md, int ii, int j)
        {
            string str;
            for (int k = 0; k < j; k++)
            {
                md[ii].d[k].pop = md[ii].d[k].hhp + md[ii].d[k].gq;
                str = md[ii].d[k].mgra + ",";
                str += md[ii].d[k].cityct10 + ",";
                str += md[ii].d[k].city + ",";
                str += md[ii].d[k].ct + ",";
                str += md[ii].d[k].pop + ",";
                str += md[ii].d[k].hhp + ",";
                str += md[ii].d[k].gq + ",";
                str += md[ii].d[k].gq_civ + ","; 
                str += md[ii].d[k].gq_mil + ",";
                str += md[ii].d[k].gq_civ_college + ","; 
                str += md[ii].d[k].gq_civ_other + ",";
               
                str += md[ii].d[k].hs + ",";
                str += md[ii].d[k].hs_sf + ",";
                str += md[ii].d[k].hs_sfmu + ",";
                str += md[ii].d[k].hs_mf + ",";
                str += md[ii].d[k].hs_mh + ",";
                str += md[ii].d[k].hh + ",";
                str += md[ii].d[k].hh_sf + ",";
                str += md[ii].d[k].hh_sfmu + ",";
                str += md[ii].d[k].hh_mf + ",";
                str += md[ii].d[k].hh_mh;
                
                try
                {
                    foutw.WriteLine(str);
                    foutw.Flush();
                }
                catch (IOException exc)      //exceptions here
                {
                    MessageBox.Show(exc.Message + " File Write Error");
                    return;
                }
            } // end for k
        } // end procedure WriteMGRAData

        //******************************************************************************************

        //  WriteCTData() 
        //  Write the ct data to ASCII 
       
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/11   tb   initial coding

        //   ------------------------------------------------------------------
        public void WriteCTData(StreamWriter foutt, int ctid,int [] passer, int rowtotal, double diffratio, int ii,int dimj)
        {
            string str = (ii + 1) + "," + ctid.ToString() + ",";
            for (int k = 0; k <dimj ; k++)
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

        #region Pmgra extraction utilities
        // procedures
        //   FillCityArrays()
        // ----------------------------------------------------------------------------

        /* FillCityArrays() */

        // Populate city popest arrays
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------
        public void FillCityArrays(CityData[] s)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            byte city;     //city keeps local index

            WriteToStatusBox("Filling City Arrays");
            sqlCommand1.CommandText = String.Format(appSettings["selectPOPEST1"].Value, TN.popestControls, fyear);
            //this.sqlCommand1.CommandText = "Select city,hs,hs_sf,hs_sfmu,hs_mf,hs_mh,hh,hh_sf,hh_sfmu,hh_mf,hh_mh,"
            //    + "hhp,gq,gq_civ,gq_mil,gq_civ_college,gq_civ_other,vac_sf,vac_sfmu,vac_mf,vac_mh,hhs from "
            //    + tn.control_table + " where estimates_year = " + fyear + " order by city";
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    city = rdr.GetByte(0);
                    s[city].hs = rdr.GetInt32(1);
                    s[city].hs_sf = rdr.GetInt32(2);
                    s[city].hs_sfmu = rdr.GetInt32(3);
                    s[city].hs_mf = rdr.GetInt32(4);
                    s[city].hs_mh = rdr.GetInt32(5);
                    s[city].hh = rdr.GetInt32(6);
                    s[city].hh_sf = rdr.GetInt32(7);
                    s[city].hh_sfmu = rdr.GetInt32(8);
                    s[city].hh_mf = rdr.GetInt32(9);
                    s[city].hh_mh = rdr.GetInt32(10);
                    s[city].hhp = rdr.GetInt32(11);
                    s[city].gq = rdr.GetInt32(12);
                    s[city].gq_civ = rdr.GetInt32(13);
                    s[city].gq_mil = rdr.GetInt32(14);
                    s[city].gq_civ_college = rdr.GetInt32(15); 
                    s[city].gq_civ_other = rdr.GetInt32(16);
                    s[city].occ_sf = 1 - rdr.GetDouble(17);
                    s[city].occ_sfmu = 1 - rdr.GetDouble(18);
                    s[city].occ_mf = 1 - rdr.GetDouble(19);
                    s[city].occ_mh = 1 - rdr.GetDouble(20);
                    s[city].hhs = rdr.GetDouble(21);
                    
                }  // end while

                rdr.Close();

            }  // end try

            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // and catch
            finally
            {
                sqlConnection.Close();
            }

        }  // end procedure FillCityArrays()

        //*********************************************************************

        #endregion

        #region SQL command procedures

        // procedures
        //    BulkLoadPOPEST() - Bulk loads ASCII to POPEST MGRA
        //	  ExecuteMGRAUpdates() - Run SQL commands to populate new MGRA table and execute updates
        //------------------------------------------------------------------------------------------------

        /*  BulkLoadPOPEST() */
        /// Bulk loads ASCII to POPEST MGRA
        
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------
        public void BulkLoadPOPEST()
        {
            string fo;
            string tex = "";
            string ttex = "";
            fo = networkPath + String.Format(appSettings["pmgra_out"].Value); ;
            WriteToStatusBox("TRUNCATING POPEST UPDATE TABLE");
            try
            {
                sqlConnection.Open();
                sqlCommand1.CommandText = String.Format(appSettings["truncate"].Value, TN.popestUpdate);
                sqlCommand1.ExecuteNonQuery();

                WriteToStatusBox("BULK LOADING POPEST MGRA TABLE");
                sqlCommand1.CommandTimeout = 180;

                sqlCommand1.CommandText = String.Format(appSettings["bulkInsert"].Value, TN.popestUpdate, fo);
                sqlCommand1.ExecuteNonQuery();

                WriteToStatusBox("UPDATE POPEST MGRA FROM POPEST MGRA UPDATE TABLE");
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST2"].Value, TN.popestMGRA, TN.popestUpdate, fyear);
                sqlCommand1.ExecuteNonQuery();

                tex = " hs = hs_sf+hs_sfmu+hs_mf+hs_mh, gq = gq_civ+gq_mil, pop = gq_civ+gq_mil+hhp";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST3"].Value, TN.popestMGRA, tex, fyear);
                sqlCommand1.ExecuteNonQuery();

                tex = "gq_civ = gq_civ_college + gq_civ_other";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST3"].Value, TN.popestMGRA, tex, fyear);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac = 0,vac_sf = 0, vac_mf = 0, vac_mh = 0";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST3"].Value, TN.popestMGRA, tex, fyear);
                sqlCommand1.ExecuteNonQuery();

                //update the vacancy rates and check for legit values
                tex = "vac = round(1 - cast(hh as float)/cast(hs as float),3)";
                ttex = " HS > 0";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac_sf = round(1 - cast(hh_sf as float)/cast(hs_sf as float),3)";
                ttex = " HS_sf > 0";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac_sfmu = round(1 - cast(hh_sfmu as float)/cast(hs_sfmu as float),3)";
                ttex = " HS_sfmu > 0";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac_mf = round(1 - cast(hh_mf as float)/cast(hs_mf as float),3)";
                ttex = " HS_mf > 0";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac_mh = round(1 - cast(hh_mh as float)/cast(hs_mh as float),3)";
                ttex = " HS_mh > 0";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "hhs = cast(hhp as float)/cast(hh as float)";
                ttex = " hh > 0";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac_sf = .05";
                ttex = " vac_sf = 1";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac_sfmu = .05";
                ttex = " vac_sfmu = 1";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac_mf = .05";
                ttex = " vac_mf = 1";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
                sqlCommand1.ExecuteNonQuery();

                tex = "vac_mh = .05";
                ttex = " vac_mh = 1";
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST4"].Value, TN.popestMGRA, tex, fyear, ttex);
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
        } // end procedure BulkLoadPopest()       

        //**********************************************************************************************


        /*  ExecuteMGRAUpdates() */
        /// Run SQL commands to populate new MGRA table and execute updates

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------

        public void ExecuteMGRAUpdates()
        {
            WriteToStatusBox("BUILDING INITIAL MGRA TABLE");
            sqlCommand1.Connection = sqlConnection;

            sqlCommand1.CommandText = String.Format(appSettings["deleteFrom"].Value, TN.popestMGRA, fyear);
            try
            {
                sqlConnection.Open();
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

            //copy the old mGRA to the new
            sqlCommand1.CommandText = String.Format(appSettings["insertPOPEST1"].Value, TN.popestMGRA, fyear, lyear);
            try
            {
                sqlConnection.Open();
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

            //update with gq_civ
            sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST11"].Value, TN.popestMGRA, fyear);
            try
            {
                sqlConnection.Open();
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

            sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST5"].Value, TN.popestMGRA, TN.GQControlled, fyear);
            try
            {
                sqlConnection.Open();
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

            //update with gq total
            sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST6"].Value, TN.popestMGRA, fyear);
            try
            {
                sqlConnection.Open();
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

            //update mgra vacancy rates if applicable
            if (useOverrides)
            {
                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST8"].Value, TN.popestMGRA, TN.popestVacOvr, fyear);
                try
                {
                    sqlConnection.Open();
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

                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST9"].Value, TN.popestMGRA, TN.popestVacOvr, fyear);
                try
                {
                    sqlConnection.Open();
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

                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST10"].Value, TN.popestMGRA, TN.popestVacOvr, fyear);
                try
                {
                    sqlConnection.Open();
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

                sqlCommand1.CommandText = String.Format(appSettings["updatePOPEST7"].Value, TN.popestMGRA, TN.popestHHSOvr, fyear);
                try
                {
                    sqlConnection.Open();
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

            }  // end if

        } // end procedure ExecuteMGRAUpdates

        //***********************************************************************************************

                    
        #endregion
        #region Miscellaneous button handlers
       
        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
        //********************************************************************
        #endregion
        
    } // end public class pmgra

    //*********************************************************************************************
}   // end namespace
