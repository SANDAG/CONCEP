/* 
 * Source File: Census2010.cs
 * Program: concep
 * Version 4.0
 * Programmer: tbe
 * Description:
 *		This is the 2010 Census Allocation to MGRA
 *		Version 4 introduces concep.config.exe to store all global constants, queries, table names and file names
 *		version 3.5 adds changes to do Series 13 MGRAs
 *		    to get the SANDAG HS > = Census HH
 *		    we added processing to do the allocation from two different summary groups, 
 *		    supersplits which are combinations of city CTs 
 *		    and supergroups (SG) which are combinations of city ct10 block group
 *		    The processing to do supersplits worked, so we just adjusted array sizes where necessaryu to handle SG;
 *		    While the nomenclature might say supersplit, the do_SG switch (hard coded) in AllocateCensus determines the
 *		    max size of the data storage arrays.
 *		    Most of the differences in the processing is handled by the input routine that extract and store census data and cross reference info
 *		version 3.4 adds allocating 2010 Census to MGRA
 */
/*   Database Description:
 *		SQL Server Database:Census, landcore (where identified)
 *			Tables:
 *			census_2010_sf1_blockadjusted     census records ctblock level
 *			census_2010_hhwc_blockadjusted    census records with hhwc and hhwoc ctblock level
 *			xref_ct10block_mgra_manyto1       ctblock to mgra for 1-1 and many - 1
 *			census_2010_mgra                  mgra census output
 *			xref_mgra_sr13                    xref mgra to supersplit
 *			starting_gq                       lckey starting gq fromseries 13
 *			census_GQ_detail_overrides        gq detail overrides
 *			census_ct10bg_gq                  ct10bg level gq detail from census data set
 *			wilma                             sr13 xref for lckey to cityct10bg and ct10bg for gq detail processing - this is a copy of landcore.gis.landcorewilma
 *			                                  we use a copy because landcorewilma has data types that don't work in this program
 *			gq_lu_priority                    lu priority for assigning gq detail
*/
//Revision History
 //   Date       By   Description
 //   ------------------------------------------------------------------
 //   01/31/12   tbe  changes for Version 3.4 Allocate 2010 Census to MGRA
 //   05/17/12   tbe  changes for version 3.5 allocate to Series 13 mgras
 //   06/13/12   tbe  changes to add processing to handle doing the census allocation from super groups (SG)
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

namespace CENSUS
{
    // need this to use the WriteToStatusBox on different thread
    delegate void WriteDelegate(string status);
    /// Summary description for popestDHH.
    /// </summary>
    public class census : System.Windows.Forms.Form
    {

        public Configuration config;
        public KeyValueConfigurationCollection appSettings;
        public ConnectionStringSettingsCollection connectionStrings;

        public class TableNames
        {
            public string census_detailed_pop_tab_ct;
            public string census_detailed_pop_control_ct;
            public string census_gq_ctbg;
            public string census_gq_cityctbg;
            public string census_gq_lckey;
            public string census_hhwc_input;     // census block hhwc table name 
            public string census_input;         //census block data table name
            public string census_2010_mgra_revised_agegroups;
            public string census_mgra;          // detailed 2010 census mgra data
            public string census_mgra_pop;      // detailed mgra pop data
            public string census_2010_supersplit;
            public string census_2010_partialsupersplit;
            public string ctblock_mgra_manyto1;  // ctblock to mgra for 1-1 and many to 1
            public string gq_lu_priority;
            public string startingGQ;
            public string wilma;
            public string xref;
        }  // end class

        public string networkPath;
        public TableNames TN = new TableNames();

        public int MAX_CT_PAIRS = 20;
        public int MAX_CONTROL_CTS = 52;            // max number of CTS to be controlled in detailed pop allocation
        public int MAX_MGRAS_IN_SUPERSPLITS = 474;  // max number of MGRAs in any supersplit
        public int MAX_MGRAS_IN_SG = 166;           // max number of mgras in any Supergroup
                                                    // since the numer of mgras in SG is smaller than supersplits, in code we will
                                                    // leave arrays dimensiond to supersplits
        public int MAX_MGRAS_IN_CTS = 474;         // max number of MGRAs in any CT10
        public int NUM_CITIES = 20;       // number of cities
        public int NUM_MGRAS = 23002;     //number of mgras
        public int NUM_CTS = 627;     // number of Census tracts (series 13)
        public int NUM_SPLIT_TRACTS = 775;   // number of split tracts (city, ct)
        public int NUM_SUPERSPLITS = 755;    // number of super splits (combined split tracts)
        public int NUM_SG = 1856;            // number of supergroups (city ct10 block group combinations
        public int MAX_CENSUS_DATA_RECORDS = 30000;  // max number of census block data records
        public int MAX_LCKEYS_IN_CTBG = 4612;    // maximum number of lckeys in any ctbg
        public int MAX_LCKEYS_IN_CITYCTBG = 7349;  //maximum number of lckeys in any cityctbg grouping
        public int NUM_CTBG = 803;     // number of distinct ct bg with gq in census data
        public int NUM_CITYCTBG = 781;  // number of distinct city ct bg with census gq data
        public int MAX_LCKEYS_IN_MGRA = 910;   //maximum number of lckey in any mgra

        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Button btnExit;
        private System.Data.SqlClient.SqlCommand sqlCommand1;
        private System.Data.SqlClient.SqlConnection sqlCnnConcep;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuItem4;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnValidate;
        private System.Windows.Forms.CheckBox chkOverrides;
        private System.Windows.Forms.Button btnRunCensus;
        private IContainer components;
        private CheckBox chkDoBlockAllocation;
        private CheckBox chkDoDetailedPop;

        public census()
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
            this.btnRunCensus = new System.Windows.Forms.Button();
            this.sqlCommand1 = new System.Data.SqlClient.SqlCommand();
            this.sqlCnnConcep = new System.Data.SqlClient.SqlConnection();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.btnValidate = new System.Windows.Forms.Button();
            this.chkOverrides = new System.Windows.Forms.CheckBox();
            this.chkDoBlockAllocation = new System.Windows.Forms.CheckBox();
            this.chkDoDetailedPop = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(21, 364);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 31);
            this.label3.TabIndex = 12;
            this.label3.Text = "Status";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(24, 138);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(554, 223);
            this.txtStatus.TabIndex = 11;
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(116, 74);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(96, 58);
            this.btnExit.TabIndex = 10;
            this.btnExit.Text = "Return";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnRunCensus
            // 
            this.btnRunCensus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRunCensus.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunCensus.Location = new System.Drawing.Point(24, 74);
            this.btnRunCensus.Name = "btnRunCensus";
            this.btnRunCensus.Size = new System.Drawing.Size(96, 58);
            this.btnRunCensus.TabIndex = 15;
            this.btnRunCensus.Text = "Run ";
            this.btnRunCensus.UseVisualStyleBackColor = false;
            this.btnRunCensus.Click += new System.EventHandler(this.btnRunCensus_Click);
            // 
            // sqlCnnConcep
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
            this.panel1.Size = new System.Drawing.Size(565, 40);
            this.panel1.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Book Antiqua", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Blue;
            this.label1.Location = new System.Drawing.Point(-8, -1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(605, 40);
            this.label1.TabIndex = 0;
            this.label1.Text = " 2010 Census Allocation  (Series 13 MGRAS)";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this.chkOverrides.Location = new System.Drawing.Point(0, 0);
            this.chkOverrides.Name = "chkOverrides";
            this.chkOverrides.Size = new System.Drawing.Size(104, 24);
            this.chkOverrides.TabIndex = 0;
            // 
            // chkDoBlockAllocation
            // 
            this.chkDoBlockAllocation.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDoBlockAllocation.Location = new System.Drawing.Point(239, 74);
            this.chkDoBlockAllocation.Name = "chkDoBlockAllocation";
            this.chkDoBlockAllocation.Size = new System.Drawing.Size(209, 30);
            this.chkDoBlockAllocation.TabIndex = 36;
            this.chkDoBlockAllocation.Text = "Do BlockAllocation";
            // 
            // chkDoDetailedPop
            // 
            this.chkDoDetailedPop.Checked = true;
            this.chkDoDetailedPop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDoDetailedPop.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDoDetailedPop.Location = new System.Drawing.Point(239, 102);
            this.chkDoDetailedPop.Name = "chkDoDetailedPop";
            this.chkDoDetailedPop.Size = new System.Drawing.Size(209, 30);
            this.chkDoDetailedPop.TabIndex = 37;
            this.chkDoDetailedPop.Text = "Do Detailed Pop";
            // 
            // census
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(597, 397);
            this.Controls.Add(this.chkDoDetailedPop);
            this.Controls.Add(this.chkDoBlockAllocation);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnRunCensus);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnExit);
            this.Menu = this.mainMenu1;
            this.Name = "census";
            this.Text = "CONCEP Version 4 - 2010 Census Allocation (Series 13 MGRAS)";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region Census Run button processing

        /*  btnRunCensus_Click() */

        /// method invoker for run button - starts another thread
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   01/30/12   tb  initial coding

        //   ------------------------------------------------------------------
        private void btnRunCensus_Click(object sender, System.EventArgs e)
        {
            //build the table names from runtime args
           
            processParams();
            MethodInvoker mi = new MethodInvoker(beginCensusWork);
            mi.BeginInvoke(null, null);
        } // end method btnRunCensus_Click()

        //***********************************************************************************************

        /*  beginCensusWork() */

        /// Census Main
 
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   01/31/2012   tb   initial coding
      
        //   ------------------------------------------------------------------
        private void beginCensusWork()
        {
            bool do_SG = false;    // do_SG true means do allocation using Super Groups (city ct10 block group combos)
                                   // otherwise use supersplits
            int max_size;

            do_SG = true;
            if (do_SG)
                max_size = NUM_SG;
            else
                max_size = NUM_SUPERSPLITS;

            try
            {
                sqlCommand1 = new System.Data.SqlClient.SqlCommand();
                sqlCommand1.CommandTimeout = 180;
                sqlCommand1.Connection = sqlCnnConcep;
                AllocateCensus(do_SG,max_size);
                WriteToStatusBox("COMPLETED CENSUS ALLOCATION RUN");
            } // end try

            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            } // end catch

        } // end beginCensusWork()

        //*************************************************************************************************
        #endregion

        #region CensusDetail

        //procedures

        //   AllocateCensus()
        //   BuildMGRAPopDetail()
        //   BuildMGRA1()
        //   BuildMGRA2()
        //   BuildGQDetail()

        //-----------------------------------------------------------------------------------------------       

        //  AllocateCensus()
        //  Allocates census to mgras
        // 

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   01/30/12   tbe  New procedure for allocating 2010 Census to MGRAs
        //   06/13/12   tbe  Adding processing to do allocation from Supergroups or Supersplits

        //   ------------------------------------------------------------------

        public void AllocateCensus(bool do_SG, int MSIZE)
        {
            // MSIZE carrys the max array size for the methodology to do allocation
            // from SG, MSIZE = NUM_SG; for Supersplits, MSIZE = NUM_SUPERSPLITS

            CT10BLOCKS[] census_records = new CT10BLOCKS[MAX_CENSUS_DATA_RECORDS];      //ct block data class
            SUPERSPLITS[] super = new SUPERSPLITS[MSIZE];          //supersplit data class
            SUPERSPLITS[] psuper = new SUPERSPLITS[MSIZE];          //supersplits with ctblocks that are 1 - many
            MGRAS[] mgra = new MGRAS[NUM_MGRAS];                 //mgra data class
            CTBG[] ctbg = new CTBG[NUM_CTBG];
            CTBG[] cityctbg = new CTBG[NUM_CITYCTBG];

            CTPOP[] ctpop = new CTPOP[NUM_CTS];

            int[] control_cts = new int[MAX_CONTROL_CTS]; ;
            int num_control_cts = 0;
            int num_ct10blocks = 0;
            //int num_pairs = 0;
            int i;
            int which_model = 2;        // set this for building detailed GQ : 2 = use cityctbg; 1 = use ctbg

            //-----------------------------------------------------------------------
            //init mgra class 
            for (i = 0; i < NUM_MGRAS; ++i)
            {
                mgra[i] = new MGRAS();
                mgra[i].data = new CENSUSDATA();
                mgra[i].lck = new LCKEY_DATA[MAX_LCKEYS_IN_MGRA];
            }  // end for i

            ExtractCTPopDetail(ctpop, control_cts, ref num_control_cts, mgra);
            if (chkDoBlockAllocation.Checked)
            {

                ExtractXREFData(census_records, super, psuper, mgra, ref num_ct10blocks, do_SG, MSIZE);

                ExtractCensusData(census_records, super, psuper, mgra, num_ct10blocks, do_SG, MSIZE);

                if (which_model == 1)
                {
                    ExtractCTBG_GQDATA(ctbg, mgra, which_model);
                    BuildGQDetail(mgra, ctbg, MAX_LCKEYS_IN_CTBG, NUM_CTBG);
                }
                else
                {
                    ExtractCTBG_GQDATA(cityctbg, mgra, which_model);
                    BuildGQDetail(mgra, cityctbg, MAX_LCKEYS_IN_CITYCTBG, NUM_CITYCTBG);
                }  // end else

                // do ct10block to mgra for 1 - 1 and many to 1

                BuildMGRA1(census_records, mgra, psuper, num_ct10blocks);
                //WriteMGRAData(mgra);
                //BulkLoadCensusMGRA(tn);
                if (which_model == 1)
                    BuildMGRA2(super, mgra, psuper, ctbg, MSIZE);
                else
                    BuildMGRA2(super, mgra, psuper, cityctbg, MSIZE);

                WriteMGRAData(mgra);
                BulkLoadCensusMGRA();

                BuildLCKEYGQDetail(mgra);

            }  // end if

            if (chkDoDetailedPop.Checked)
            {
                //BuildMGRAPopDetail(ctpop, ctpopc, pairs,num_pairs,mgra);  this is a pass 1 call replaced by pass 2
                BuildMGRAPopDetail(ctpop, control_cts, num_control_cts, mgra);
                WriteMGRADetailedPopData(mgra);
                BulkLoadMGRADetailedPopData();
            }   // end if

        }  // end procedure AllocateCensus()

        //**********************************************************************************************************

        // BuildMGRAPopDetail()
        // Computes Detailed MGRA pop detail, ethnicity x sex x age group
        // Uses CT distributions and controls to mgra marginals for ethnicity and sex

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   03/6/12   tbe  New procedure for allocating 2010 Census pop detail to mgras
        //   07/02/12  tbe  Modified allocation scheme to handle screwy age, sex distributions
        //                  resulting from moving pop across ct boundaries in supergroup pop allocation

        //   ------------------------------------------------------------------

        public void BuildMGRAPopDetail(CTPOP[] ctpop, int[] control_cts, int num_control_cts, MGRAS[] mgra)
        {
            int[,] tempage = new int[NUM_MGRAS, 40];               // 8 ethnicities, 20 male ages, 20 female ages
            int[] tempage_index = new int[NUM_MGRAS];             // stores the index (mgraid) of tempage array
            int[] ethmargin = new int[NUM_MGRAS];                // ethnicity marginals
            int[] agemargin = new int[40];                          // combined age groups marginals
            int[] regagemargin = new int[40];                       // regional combined age, sex
            int[] cteth = new int[8];
            int[] passer = new int[40];
            int h, i, j, k;
            int ctid;
            int row_counter;
            int control_count_mgras = 0;
            int num_mgras = 0;
            int rt, ct;
            int tstop = 0;
            int aplace;
            int mgid;
            FileStream fout;		//file stream class
            string str = "";
            //-----------------------------------------------------------------------------------------------------
            // here's the plan for controlling the mgra detailed age by sex and ethnicity
            // PROBLEM STATEMENT:  because of the way that the census data are delivered and the way that we built sr12 and sr13  mgras, we have a disconnect
            // with respect to detailed age, sex, ethnicity data for mgras.
            // Specifically, ethnicity totals and total age distributions for mgras were derived from what we call supersplits;
            // these are combinations of census blocks that summ to city census tract combinations.  In some cases, we had to combine split-tracts
            // because the blocks crossed city boundaries.  Hence the supersplits.  The resulting mgra data sum to supersplit totals
            // Next, the detailed age, sex by ethnicity data from the census are at the whole ct level.  Since mgras DO NOT necessarily sum to CTs,
            // we can't use the CT data to control the mgra distributions.  
            // So, here is the plan:
            // 1.  We will use the derived (from supersplit) mgra ethnicity totals as row controls
            // 2.  We can seed an initial mgra age distribution by ethnicity based on the CT distribution just to get an initial cur of the UPDATE array
            // 3.  By necessity, we'll have to use the regional age, sex detail for each ethnic group as the UPDATE col controls
            // 4.  We'll let UPDATE control the resulting 23002 X 40 array the row and col controls for each ethnic group (8 passes)
            // 5.  The resulting distribution for each mgra by sex, age and ethnicity will be the estimate.
            // 6.  Sum these to get total age, sex for each mgra and 
            // 7.  WE WILL PROBABLY NOT MATCH THE DATA IN CENSUS_2010_MGRA, SO THESE WILL HAVE TO BE REPLACED - in fact, we've eliminated the 
            //     detailed age stuff from the mgra table for sr13

            // Modifications for Series 13 detailed pop - Alternate Pass 1 (P1)
            // it appears that in doing the supergroup allocations, we ended up moving pop across ct boundaries.  the result is that some pairs (including 2 sets of 3) of CTs
            // get wonky MGRA age, sex distributions.  The plan is to isolate these pairs of cts and do their allocations as a combination.  We'll start with
            // the weird CTs, build N sets (at this writing there are 17 pairs), apply the combined distribution to the mgras in a pachinko to seed the array
            // then control to the combined totals.  After all N sets are finished, we run the remaining CTs through the regular controlling, using the reduced 
            // regional totals as the column controls.

            // Modifications for Series 13 Detailed pop - Alternate Pass 2 For now it retains the original procedure call
            // It appears that after trying all kinds of controlling to meet retional totals that we modify too many cts that are straightforward allocations to its mgras
            // The city totals for sex and age (mainly sex) are still too effected by controlling.  This effort represents pass 2 to find a better allocations scheme.
            // The reality is that for most CTs, a straight pachinko to its mgras yields good results.  There are, however, 52 CTs that are effected by boundary irregularities \
            // for which controlling messes up the mix.  
            // This pass takes a new approace: straight pachinko CTs to mgras for all the "good" CTs.  Then use the major controlling methodology only for the remaining
            // CTs.  In this way, the controlling for Wonky CTs wont effect good CTs (CT total pop by sex and age, matches mgra sums).
            // From the previous code, we have to eliminate the processing for pairs or combinations that get controlled separately.  We'll dummy out the extraction code.
            // Change the order of the processing to do straigh pachinko on all but the wonky CTs (there are initially 52).  We have to add extraction code to populate the 
            // control_cts array.  Then use the regular stacked controlling on just the 52.  The way that we rewrote the controlling logic in Pass 1 should accommodate 
            // this step with minor changes.

            WriteToStatusBox("Building Detailed MGRA pop");

            // open output file
            try
            {
                fout = new FileStream(networkPath + "z1", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);
            foutw.AutoFlush = true;

            // at this point we've built the alternate sets for the ct pairs
            // 
            int p = 0;

            for (i = 0; i < 8; ++i) // ethnicity loop
            {
                WriteToStatusBox(" For Good CTs, Processing ethnicity number " + (i + 1).ToString());

                for (h = 0; h < NUM_CTS; ++h)
                {
                    Array.Clear(tempage, 0, tempage.Length);
                    Array.Clear(ethmargin, 0, ethmargin.Length);
                    Array.Clear(regagemargin, 0, regagemargin.Length);
                    Array.Clear(tempage_index, 0, tempage_index.Length);
                    bool a_bad_ct = false;

                    row_counter = 0;
                    ctid = ctpop[h].ct10;
                    // scan the control_cts list to see if this is one of them - if so, skip it
                    for (p = 0; p < num_control_cts; ++p)
                    {
                        if (ctid == control_cts[p])
                        {
                            a_bad_ct = true;
                            break;
                        }   // end if
                    }  // end for

                    if (a_bad_ct)
                        continue;

                    num_mgras = ctpop[h].num_mgras;

                    for (j = 0; j < num_mgras; ++j)
                    {
                        Array.Clear(agemargin, 0, agemargin.Length);
                        mgid = ctpop[h].mgra_ids[j] - 1;
                        tempage_index[row_counter] = mgid;
                        if (mgid == 11996)
                            tstop = 1;
                        if (i == 0)
                            ethmargin[row_counter] = mgra[mgid].data.hisp[0];
                        else
                            ethmargin[row_counter] = mgra[mgid].data.nhisp[i];

                        aplace = 0;   // initialize the array place holder

                        // fill agemargin with regional detailed pop
                        for (k = 0; k < 20; ++k)  // males
                            agemargin[aplace++] = ctpop[h].ragem[i + 1, k];
                        for (k = 0; k < 20; ++k)   // females
                            agemargin[aplace++] = ctpop[h].ragef[i + 1, k];

                        // use pachinko to fill a first cut on tempage (mgra age distributions) to get rows filled with values that
                        // match row totals (mgra,ethnicity)
                        // then let update sort out the column totals
                        Array.Clear(passer, 0, passer.Length);
                        if (ethmargin[row_counter] > 0)
                        {
                            CU.cUtil.PachinkoWithMasterNoDecrement(ethmargin[row_counter], agemargin, passer, 40);
                        }  // end if
                        for (k = 0; k < 40; ++k)
                        {
                            tempage[row_counter, k] = passer[k];
                        }  // end for k

                        ++row_counter;
                    }   // end for j

                    for (k = 0; k < 40; ++k)
                    {
                        regagemargin[k] += agemargin[k];
                    }  // end for k

                    rt = 0;
                    ct = 0;
                    for (p = 0; p < num_mgras; ++p)
                        rt += ethmargin[p];
                    for (p = 0; p < 40; ++p)
                        ct += regagemargin[p];

                    // at this point the tempage array should contain 20 m and 20 f age estimates for each ethnicity
                    // now we pass this to update to control in two directions
                    CU.cUtil.update(num_mgras, 40, tempage, ethmargin, regagemargin);
                    try
                    {
                        for (j = 0; j < num_mgras; ++j)
                        {
                            aplace = 0;
                            mgid = tempage_index[j];
                            str = (mgid + 1).ToString() + ",";
                            for (k = 0; k < 20; ++k)
                            {
                                mgra[mgid].data.ragem[i, k] = tempage[j, aplace++];
                                str += mgra[mgid].data.ragem[i, k] + ",";
                            }  // end for k
                            for (k = 0; k < 20; ++k)
                            {
                                mgra[mgid].data.ragef[i, k] = tempage[j, aplace++];
                                str += mgra[mgid].data.ragef[i, k] + ",";
                            }  // end for k
                            //foutw.WriteLine(str);
                        }  // end for j

                    }  // end try
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.ToString(), exc.GetType().ToString());

                    }  // end catch
                }   // end for h
            }   // end for i

            // this should be the end of the good CTs processing
            // now we do the remaining controlled cts in normal fashion

            //----------------------------------------------------------------------------------------------------------------------------------
            for (i = 0; i < 8; ++i) // ethnicity loop
            {
                WriteToStatusBox(" Processing ethnicity number " + (i + 1).ToString());
                Array.Clear(tempage, 0, tempage.Length);
                Array.Clear(ethmargin, 0, ethmargin.Length);
                Array.Clear(regagemargin, 0, regagemargin.Length);
                control_count_mgras = 0;
                row_counter = 0;
                int ctindex = 0;

                for (h = 0; h < num_control_cts; ++h)
                {
                    ctindex = GetCT10Index(ctpop, control_cts[h]);

                    num_mgras = ctpop[ctindex].num_mgras;

                    control_count_mgras += num_mgras;  // store the count of mgras that exclude those in the paired CT calculations
                    for (j = 0; j < num_mgras; ++j)
                    {
                        Array.Clear(agemargin, 0, agemargin.Length);
                        mgid = ctpop[ctindex].mgra_ids[j] - 1;
                        tempage_index[row_counter] = mgid;
                        if (mgid == 11996)
                            tstop = 1;
                        if (i == 0)
                            ethmargin[row_counter] = mgra[mgid].data.hisp[0];
                        else
                            ethmargin[row_counter] = mgra[mgid].data.nhisp[i];

                        aplace = 0;   // initialize the array place holder

                        // fill agemargin with regional detailed pop
                        for (k = 0; k < 20; ++k)  // males
                            agemargin[aplace++] = ctpop[ctindex].ragem[i + 1, k];
                        for (k = 0; k < 20; ++k)   // females
                            agemargin[aplace++] = ctpop[ctindex].ragef[i + 1, k];

                        // use pachinko to fill a first cut on tempage (mgra age distributions) to get rows filled with values that
                        // match row totals (mgra,ethnicity)
                        // then let update sort out the column totals
                        Array.Clear(passer, 0, passer.Length);
                        if (ethmargin[row_counter] > 0)
                        {
                            CU.cUtil.PachinkoWithMasterNoDecrement(ethmargin[row_counter], agemargin, passer, 40);
                        }  // end if
                        for (k = 0; k < 40; ++k)
                        {
                            tempage[row_counter, k] = passer[k];

                        }  // end for k

                        ++row_counter;
                    }   // end for j
                    for (k = 0; k < 40; ++k)
                    {
                        regagemargin[k] += agemargin[k];
                    }  // end for k
                }   // end for h

                rt = 0;
                ct = 0;
                for (h = 0; h < control_count_mgras; ++h)
                    rt += ethmargin[h];
                for (h = 0; h < 40; ++h)
                    ct += regagemargin[h];

                // at this point the tempage array should contain 20 m and 20 f age estimates foreach ethnicity
                // now we pass this to update to control in two directions
                CU.cUtil.update(control_count_mgras, 40, tempage, ethmargin, regagemargin);
                try
                {
                    for (j = 0; j < control_count_mgras; ++j)
                    {
                        aplace = 0;
                        mgid = tempage_index[j];
                        str = (mgid + 1).ToString() + ",";
                        for (k = 0; k < 20; ++k)
                        {
                            mgra[mgid].data.ragem[i, k] = tempage[j, aplace++];
                            str += mgra[mgid].data.ragem[i, k] + ",";
                        }  // end for k
                        for (k = 0; k < 20; ++k)
                        {
                            mgra[mgid].data.ragef[i, k] = tempage[j, aplace++];
                            str += mgra[mgid].data.ragef[i, k] + ",";
                        }  // end for k
                        //foutw.WriteLine(str);
                    }  // end for j

                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString(), exc.GetType().ToString());

                }  // end catch
            }   // end for i
            foutw.Close();

        }  // end procedure BuildMGRAPopDetail()

        //  *******************************************************************************************************

        //  BuildMGRA1()
        //  Build MGRA data from CT10blocks for 1 - 1 and many - 1

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   01/30/12   tbe  New procedure for allocating 2010 Census to MGRAs

        //   ------------------------------------------------------------------

        public void BuildMGRA1(CT10BLOCKS[] ctb, MGRAS[] mgra, SUPERSPLITS[] psuper, int num_ct10Blocks)
        {
            int i, j, mgid = 0, countsolo = 0, psid = 0; ;

            int a = 0, b = 0, c = 0;
            int[] allsolos = new int[NUM_MGRAS];
            // ------------------------------------------------------------------------------
            WriteToStatusBox("Building MGRAs from CT10BLOCKS with 1 - 1 and many - 1");
            // look for all ct10Blocks going to only 1 mgra

            for (i = 0; i < num_ct10Blocks; ++i)
            {
                try
                {
                    ctb[i].solo = false;

                    if (ctb[i].num_mgras == 1)
                    {
                        // assign CT10block data to mgra and set flag for finished
                        ctb[i].solo = true;
                        mgid = ctb[i].mgra_id - 1;
                        psid = mgra[mgid].superID - 1;
                        allsolos[countsolo] = mgid;

                        mgra[mgid].was_used = true;
                        ++countsolo;
                        if (mgra[mgid].data.sandag_hs == 0 && mgra[mgid].data.gq == 0)
                            continue;
                        //pop
                        a = ctb[i].data.pop;
                        b = ctb[i].data.popm;
                        c = ctb[i].data.popf;

                        mgra[mgid].data.pop += a;
                        mgra[mgid].data.popm += b;
                        mgra[mgid].data.popf += c;

                        //subtract this mgra's data from partial supersplit for residual
                        psuper[psid].data.pop -= a;
                        psuper[psid].data.popm -= b;
                        psuper[psid].data.popf -= c;

                        //age data totals, male, female
                        for (j = 0; j < 23; ++j)
                        {
                            a = ctb[i].data.aget[j];
                            b = ctb[i].data.agem[j];
                            c = ctb[i].data.agef[j];

                            mgra[mgid].data.aget[j] += a;
                            mgra[mgid].data.agem[j] += b;
                            mgra[mgid].data.agef[j] += c;

                            psuper[psid].data.aget[j] -= a;
                            psuper[psid].data.agem[j] -= b;
                            psuper[psid].data.agef[j] -= c;
                        }   // end for j

                        // ethnicity
                        for (j = 0; j < 8; ++j)
                        {
                            a = ctb[i].data.nhisp[j];
                            b = ctb[i].data.hisp[j];

                            mgra[mgid].data.nhisp[j] += a;
                            mgra[mgid].data.hisp[j] += b;

                            psuper[psid].data.nhisp[j] -= a;
                            psuper[psid].data.hisp[j] -= b;
                        } // end for j

                        // hhp
                        a = ctb[i].data.hhp;
                        b = ctb[i].data.hhp_own;
                        c = ctb[i].data.hhp_rent;

                        mgra[mgid].data.hhp += a;
                        mgra[mgid].data.hhp_own += b;
                        mgra[mgid].data.hhp_rent += c;

                        psuper[psid].data.hhp -= a;
                        psuper[psid].data.hhp_own -= b;
                        psuper[psid].data.hhp_rent -= c;

                        // hhs
                        for (j = 0; j < 7; ++j)
                        {
                            a = ctb[i].data.hhsx[j];

                            mgra[mgid].data.hhsx[j] += a;

                            psuper[psid].data.hhsx[j] -= a;

                        } // end for j

                        //hh
                        a = ctb[i].data.hh;
                        b = ctb[i].data.hh_own;
                        c = ctb[i].data.hh_rent;

                        mgra[mgid].data.hh += a;
                        mgra[mgid].data.hh_own += b;
                        mgra[mgid].data.hh_rent += c;

                        psuper[psid].data.hh -= a;
                        psuper[psid].data.hh_own -= b;
                        psuper[psid].data.hh_rent -= c;

                    }   // end if
                }   // end try

                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString(), exc.GetType().ToString());

                }  // end catch

            }   // end for i

        }  // end procedure BuildMGRA1()

        //***********************************************************************************************************

        //  BuildMGRA2()
        // Build MGRA data from the master geography for 1 - many

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   02/07/12   tbe  New procedure for allocating 2010 Census to MGRAs

        //   ------------------------------------------------------------------

        public void BuildMGRA2(SUPERSPLITS[] mg, MGRAS[] mgra, SUPERSPLITS[] ps, CTBG[] ctbg, int num_mg)
        {
            int i, j, k, mcount = 0, numcells = 0;
            int[] passer = new int[MAX_MGRAS_IN_SUPERSPLITS];
            int[] ppasser = new int[MAX_MGRAS_IN_SUPERSPLITS];
            int[] pindex = new int[MAX_MGRAS_IN_SUPERSPLITS];
            int[] constraint = new int[MAX_MGRAS_IN_SUPERSPLITS];
            int[] master = new int[MAX_MGRAS_IN_SUPERSPLITS];
            int ret = 0, target = 0, shortcount = 0;
            int lclsum = 0;
            int tstop = 0;
            int mgid = 0;
            int[] local_mgid = new int[MAX_MGRAS_IN_SUPERSPLITS];
            int[,] shorts = new int[3, 1000];
            int[,] tm = new int[MAX_MGRAS_IN_SUPERSPLITS, 7];
            //-----------------------------------------------------------------------------------------------------------
            WriteToStatusBox("Building MGRAs from SUPERSPLIT/SUPERGROUP (SG) allocations");
            //main processing loop
            for (i = 0; i < num_mg; ++i)
            {
                if (i == 1797)
                    tstop = 1;
                //rebuild the sandag_hs to include only those mgras that were not 1-1 or many 1
                // sandag_hs is the only variable used that does not come diretly from the census record, it gets added from the xref extract
                // so we have to resum the partial supersplit to include only the mgras that haven't been used
                mg[i].data.sandag_hs = 0;

                mcount = mg[i].num_mgras;

                for (j = 0; j < mcount; ++j)
                {
                    mgid = mg[i].mgra_ids[j] - 1;

                    mg[i].data.sandag_hs += mgra[mgid].data.sandag_hs;
                }   // end for j

                if (mg[i].data.sandag_hs < mg[i].data.hh)
                {
                    shorts[0, shortcount] = i;
                    shorts[1, shortcount] = mg[i].data.sandag_hs;
                    shorts[2, shortcount] = mg[i].data.hh;
                    continue;
                }   // end if

                // save mgra ids for this master geo
                Array.Clear(local_mgid, 0, local_mgid.Length);
                for (j = 0; j < mcount; ++j)
                {
                    mgid = mg[i].mgra_ids[j] - 1;

                    local_mgid[j] = mgid;
                }   // end for j
                Array.Clear(passer, 0, passer.Length);

                //-------------------------------------------------------------------------------------------------------
                // start with hh 

                if (mg[i].data.hh > 0) // if hh > 0
                {

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        // load the passer array with hh estimate unless this mgra already has some hh
                        if (mgra[mgid].data.hh == 0)
                            passer[j] = (int)(mg[i].data.occ_rate * (double)mgra[mgid].data.sandag_hs);

                        else
                            passer[j] = mgra[mgid].data.hh;
                        if (passer[j] > mgra[mgid].data.sandag_hs)
                            passer[j] = mgra[mgid].data.sandag_hs;
                        // set the constraint for hh < hs
                        constraint[j] = mgra[mgid].data.sandag_hs;
                    }   // end for j  

                    // use the supersplit hh total for controlling
                    target = mg[i].data.hh;
                    ret = CU.cUtil.Roundit(passer, constraint, target, mcount, 2);  // roundit using hs as limit

                    // restore the values
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        mgra[mgid].data.hh = passer[j];
                        if (mgra[mgid].data.sandag_hs > 0)
                            mgra[mgid].data.occ_rate = (double)mgra[mgid].data.hh / (double)mgra[mgid].data.sandag_hs;
                    }     // end for j

                    // should have hh filled - now do hh own, rent 
                    numcells = 2;
                    master[0] = ps[i].data.hh_own;
                    master[1] = ps[i].data.hh_rent;

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];

                        target = mgra[mgid].data.hh;
                        lclsum = 0;
                        passer[0] = mgra[mgid].data.hh_own;
                        passer[1] = mgra[mgid].data.hh_rent;
                        lclsum = passer[0] + passer[1];

                        if (target != lclsum)
                        {
                            master[0] += mgra[mgid].data.hh_own;
                            mgra[mgid].data.hh_own = 0;
                            master[1] += mgra[mgid].data.hh_rent;
                            mgra[mgid].data.hh_rent = 0;
                        }   // end if
                    }  // end for j

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];

                        target = mgra[mgid].data.hh;
                        if (target > 0)
                        {
                            Array.Clear(passer, 0, passer.Length);
                            lclsum = 0;
                            passer[0] = mgra[mgid].data.hh_own;
                            passer[1] = mgra[mgid].data.hh_rent;
                            lclsum = passer[0] + passer[1];

                            target = target - lclsum;
                            ret = 0;
                            if (target > 0)
                            {
                                ret = CU.cUtil.PachinkoWithMasterDecrement(target, master, passer, numcells);

                                if (ret >= 40000)
                                {
                                    MessageBox.Show("For HH_OWN, Pachinko did not resolve in 40000 iterations for master geo  " + (i + 1).ToString() + " mgra " + (mgid + 1).ToString());
                                }     /* end if */
                                mgra[mgid].data.hh_own = passer[0];
                                mgra[mgid].data.hh_rent = passer[1];

                            } // end if
                        }   // end else
                    }   // end for j

                    // hhs by category
                    // pachinko the mgras for hhsx with mg.data.hhsx as master and mgra.data.sandag_hs as target
                    numcells = 7;
                    for (j = 0; j < numcells; ++j)
                        master[j] = ps[i].data.hhsx[j];

                    Array.Clear(tm, 0, tm.Length);
                    // store some temp data for debugging - these are the hhsx data from the 1-1 and many -1 allocation
                    // it's suspected that some of the smaller mgras are getting hhsx data, but the hh count is getting zerod in earlier controlling
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        for (k = 0; k < numcells; ++k)
                            tm[j, k] = mgra[mgid].data.hhsx[k];
                    }  // end for j

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];

                        target = mgra[mgid].data.hh;
                        lclsum = 0;
                        for (k = 0; k < numcells; ++k)
                        {
                            passer[k] = mgra[mgid].data.hhsx[k];
                            lclsum += passer[k];
                        }  // end for k
                        if (target != lclsum)
                        {
                            for (k = 0; k < numcells; ++k)
                            {
                                master[k] += mgra[mgid].data.hhsx[k];
                                mgra[mgid].data.hhsx[k] = 0;
                            }   // end for k
                        }   // end if
                    }  // end for j

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];

                        target = mgra[mgid].data.hh;
                        if (target > 0)
                        {
                            Array.Clear(passer, 0, passer.Length);
                            lclsum = 0;
                            for (k = 0; k < numcells; ++k)
                            {
                                passer[k] = mgra[mgid].data.hhsx[k];
                                lclsum += passer[k];
                            }  // end for k
                            target = target - lclsum;
                            ret = 0;
                            if (target > 0)
                            {
                                ret = CU.cUtil.PachinkoWithMasterDecrement(target, master, passer, numcells);

                                if (ret >= 40000)
                                {
                                    MessageBox.Show("For HHSX, Pachinko did not resolve in 40000 iterations for master geo  " + (i + 1).ToString() + " mgra " + (mgid + 1).ToString());
                                }     /* end if */
                                for (k = 0; k < numcells; ++k)
                                {
                                    mgra[mgid].data.hhsx[k] = passer[k];

                                }   // end for k
                            } // end if
                        }   // end else
                    }   // end for j

                    // --------------------------------------------------------------------------------------------------------

                    // this is the alternative hhp computation
                    Array.Clear(passer, 0, passer.Length);
                    Array.Clear(constraint, 0, constraint.Length);
                    target = mg[i].data.hhp;

                    for (j = 0; j < mcount; ++j)
                    {
                        int hhp_t = 0;
                        mgid = local_mgid[j];
                        mgra[mgid].data.hhp = 0;  // reeet hp refore rebuilding to clear array
                        for (k = 0; k < 7; ++k)
                            hhp_t += mgra[mgid].data.hhsx[k] * (k + 1);

                        passer[j] = hhp_t;   // build initial hhp
                        if (passer[j] < mgra[mgid].data.hh)
                            passer[j] = mgra[mgid].data.hh;

                        constraint[j] = mgra[mgid].data.hh;                               // has to be greater than hh
                    }   // end for j 

                    ret = CU.cUtil.Roundit(passer, constraint, target, mcount, 3);  // roundit using hh as lower limit

                    // then, since we used the whole supersplit, we dont increment the hhp in the mgra, just take the rounded value
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        mgra[mgid].data.hhp = passer[j];
                        if (mgra[mgid].data.hh > 0)
                            mgra[mgid].data.hhs = (double)mgra[mgid].data.hhp / (double)mgra[mgid].data.hh;
                    }   // end for j

                    // should have hhp filled - now do hhp own, rent 
                    Array.Clear(passer, 0, passer.Length);
                    Array.Clear(ppasser, 0, ppasser.Length);
                    Array.Clear(pindex, 0, pindex.Length);

                    target = mg[i].data.hhp_own;
                    int icount = 0;
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        //if (mgid == 13759)
                        //    tstop = 1;
                        mgra[mgid].data.hhp_own = 0;  // init the hhp_own before rebuilding
                        passer[j] = (int)(mg[i].data.hhs * (double)mgra[mgid].data.hh_own);   // build initial hhp_own
                        if (passer[j] > mgra[mgid].data.hhp)
                            passer[j] = mgra[mgid].data.hhp;

                        if (passer[j] > 0)
                        {
                            ppasser[icount] = passer[j];
                            pindex[icount] = mgid;
                            constraint[icount++] = mgra[mgid].data.hhp;
                        }  // end if

                    }   // end for j 
                    ret = CU.cUtil.Roundit(ppasser, constraint, target, icount, 2);  // roundit using hhp as upper limit

                    for (j = 0; j < icount; ++j)
                    {
                        mgid = pindex[j];
                        mgra[mgid].data.hhp_own = ppasser[j];
                    }  // end for

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        mgra[mgid].data.hhp_rent = mgra[mgid].data.hhp - mgra[mgid].data.hhp_own;
                    }   // end for j

                    // build hhwc and hhwoc
                    Array.Clear(passer, 0, passer.Length);

                    double share_wc = 0;
                    if (mg[i].data.hh - mg[i].data.hhsx[0] > 0)
                        share_wc = (double)mg[i].data.hhwc / (double)(mg[i].data.hh - mg[i].data.hhsx[0]);
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        passer[j] = (int)(share_wc * (double)(mgra[mgid].data.hh - mgra[mgid].data.hhsx[0]));
                        constraint[j] = mgra[mgid].data.hh - mgra[mgid].data.hhsx[0];
                    }  // end for j
                    target = mg[i].data.hhwc;
                    ret = CU.cUtil.Roundit(passer, constraint, target, mcount, 2);  // roundit using hh as upper limit

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        mgra[mgid].data.hhwc = passer[j];
                        mgra[mgid].data.hhwoc = mgra[mgid].data.hh - mgra[mgid].data.hhwc;
                    }   // end for j

                }  // end if hh > 0

                // --------------------------------------------------------------------------------------------------------

                for (j = 0; j < mcount; ++j)
                {
                    mgid = local_mgid[j];
                    mgra[mgid].data.pop = mgra[mgid].data.gq + mgra[mgid].data.hhp;
                }  // end for j

                //--------------------------------------------------------------------------------------------------------

                if (mg[i].data.pop > 0)
                {
                    // pop by sex POPM and POPF
                    target = mg[i].data.popm;
                    Array.Clear(passer, 0, passer.Length);
                    // get mgra totals for popm and popf
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        if (mg[i].data.pop > 0)
                            passer[j] = (int)(((double)mg[i].data.popm / (double)mg[i].data.pop) * (double)mgra[mgid].data.pop);

                        if (passer[j] > mgra[mgid].data.pop)
                            passer[j] = mgra[mgid].data.pop;
                        constraint[j] = mgra[mgid].data.pop;
                    }   // end for j   

                    ret = CU.cUtil.Roundit(passer, constraint, target, mcount, 2);  // roundit with pop as upper limit

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        mgra[mgid].data.popm = passer[j];
                        mgra[mgid].data.popf = mgra[mgid].data.pop - mgra[mgid].data.popm;  // derive popf as residual
                        if (mgra[mgid].data.popf < 0)
                            MessageBox.Show(" mgra popf < 0 for " + mgid + 1 + " j = " + j);
                    }   // end for j 

                    // pachinko the mgras for agem with mg.data.agem as master and mgra.data.popm as target
                    numcells = 23;
                    for (j = 0; j < numcells; ++j)
                        master[j] = ps[i].data.agem[j];

                    // check for any mgras that dont sum; if so replace to the partial superset and zero before allocating
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        //if (mgid == 5372)
                        //    tstop = 1;
                        target = mgra[mgid].data.popm;
                        lclsum = 0;
                        for (k = 0; k < numcells; ++k)
                        {
                            passer[k] = mgra[mgid].data.agem[k];
                            lclsum += passer[k];
                        }  // end for k
                        if (target != lclsum)
                        {
                            for (k = 0; k < numcells; ++k)
                            {
                                master[k] += mgra[mgid].data.agem[k];
                                mgra[mgid].data.agem[k] = 0;
                            }   // end for k
                        }   // end if
                    }  // end for j

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        //if (mgid == 11)
                        //    tstop = 1;
                        target = mgra[mgid].data.popm;
                        if (target > 0)
                        {
                            Array.Clear(passer, 0, passer.Length);
                            lclsum = 0;
                            for (k = 0; k < numcells; ++k)
                            {
                                passer[k] = mgra[mgid].data.agem[k];
                                lclsum += passer[k];
                            }  // end for k
                            target = target - lclsum;
                            ret = 0;
                            if (target > 0)
                            {
                                ret = CU.cUtil.PachinkoWithMasterDecrement(target, master, passer, numcells);

                                if (ret >= 40000)
                                {
                                    MessageBox.Show("For HHSX, Pachinko did not resolve in 40000 iterations for master geo  " + (i + 1).ToString() + " mgra " + (mgid + 1).ToString());
                                }     /* end if */
                                for (k = 0; k < numcells; ++k)
                                {
                                    mgra[mgid].data.agem[k] = passer[k];

                                }   // end for k
                            } // end if
                        }   // end else
                    }   // end for j

                    //popf
                    // pachinko the mgras for agef with mg.data.agef as master and mgra.data.popf as target
                    numcells = 23;
                    for (j = 0; j < numcells; ++j)
                        master[j] = ps[i].data.agef[j];

                    // check for any mgras that dont sum; if so replace to the partial superset and zero before allocating
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        //if (mgid == 5372)
                        //    tstop = 1;
                        target = mgra[mgid].data.popf;
                        lclsum = 0;
                        for (k = 0; k < numcells; ++k)
                        {
                            passer[k] = mgra[mgid].data.agef[k];
                            lclsum += passer[k];
                        }  // end for k
                        if (target != lclsum)
                        {
                            for (k = 0; k < numcells; ++k)
                            {
                                master[k] += mgra[mgid].data.agef[k];
                                mgra[mgid].data.agef[k] = 0;
                            }   // end for k
                        }   // end if
                    }  // end for j

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        //if (mgid == 11)
                        //    tstop = 1;
                        target = mgra[mgid].data.popf;
                        if (target > 0)
                        {
                            Array.Clear(passer, 0, passer.Length);
                            lclsum = 0;
                            for (k = 0; k < numcells; ++k)
                            {
                                passer[k] = mgra[mgid].data.agef[k];
                                lclsum += passer[k];
                            }  // end for k
                            target = target - lclsum;
                            ret = 0;
                            if (target > 0)
                            {
                                ret = CU.cUtil.PachinkoWithMasterDecrement(target, master, passer, numcells);

                                if (ret >= 40000)
                                {
                                    MessageBox.Show("For HHSX, Pachinko did not resolve in 40000 iterations for master geo  " + (i + 1).ToString() + " mgra " + (mgid + 1).ToString());
                                }     /* end if */
                                for (k = 0; k < numcells; ++k)
                                {
                                    mgra[mgid].data.agef[k] = passer[k];

                                }   // end for k
                            } // end if
                        }   // end else
                    }   // end for j

                    // build mgra total pop by age
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        for (k = 0; k < numcells; ++k)
                        {
                            mgra[mgid].data.aget[k] = mgra[mgid].data.agem[k] + mgra[mgid].data.agef[k];

                        }   // end for k

                    }   // end for j

                    //--------------------------------------------------------------------------------------------------

                    // hisp and nhisp pop
                    // reset any hisp and nhisp that 
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        if (mgra[mgid].data.pop != mgra[mgid].data.hisp[0] + mgra[mgid].data.nhisp[0])
                        {
                            mgra[mgid].data.nhisp[0] = 0;
                            mgra[mgid].data.hisp[0] = 0;
                        }  // end if
                    }  // end for j;

                    target = mg[i].data.nhisp[0];
                    Array.Clear(passer, 0, passer.Length);

                    // get mgra totals for hisp and nhisp
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        if (mg[i].data.pop > 0)
                        {
                            passer[j] = (int)((double)mg[i].data.nhisp[0] / (double)mg[i].data.pop * mgra[mgid].data.pop);
                            constraint[j] = mgra[mgid].data.pop;  // set max value for nhisp = pop
                        }  // end if
                    }   // end for j

                    ret = CU.cUtil.Roundit(passer, constraint, target, mcount, 2);  // roundit with pop as constraint

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        mgra[mgid].data.nhisp[0] = passer[j];
                        mgra[mgid].data.hisp[0] = mgra[mgid].data.pop - mgra[mgid].data.nhisp[0];  // derive nhisp as residual
                    }   // end for j 

                    // pachinko the mgras for hisp with mg.data.hisp as master and mgra.data.hisp[0] as target

                    numcells = 7;
                    for (j = 0; j < numcells; ++j)
                        master[j] = ps[i].data.hisp[j + 1];

                    // check for any mgras that dont sum; if so replace to the partial superset and zero before allocating
                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        //if (mgid == 5372)
                        //    tstop = 1;
                        target = mgra[mgid].data.hisp[0];
                        lclsum = 0;
                        for (k = 0; k < numcells; ++k)
                        {
                            passer[k] = mgra[mgid].data.hisp[k + 1];
                            lclsum += passer[k];
                        }  // end for k
                        if (target != lclsum)
                        {
                            for (k = 0; k < numcells; ++k)
                            {
                                master[k] += mgra[mgid].data.hisp[k + 1];
                                mgra[mgid].data.hisp[k + 1] = 0;
                            }   // end for k
                        }   // end if
                    }  // end for j

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        //if (mgid == 11)
                        //    tstop = 1;
                        target = mgra[mgid].data.hisp[0];
                        if (target > 0)
                        {
                            Array.Clear(passer, 0, passer.Length);
                            lclsum = 0;
                            for (k = 0; k < numcells; ++k)
                            {
                                passer[k] = mgra[mgid].data.hisp[k + 1];
                                lclsum += passer[k];
                            }  // end for k
                            target = target - lclsum;
                            ret = 0;
                            if (target > 0)
                            {
                                ret = CU.cUtil.PachinkoWithMasterDecrement(target, master, passer, numcells);

                                if (ret >= 40000)
                                {
                                    MessageBox.Show("For HISP, Pachinko did not resolve in 40000 iterations for master geo  " + (i + 1).ToString() + " mgra " + (mgid + 1).ToString());
                                }     /* end if */
                                for (k = 0; k < numcells; ++k)
                                {
                                    mgra[mgid].data.hisp[k + 1] = passer[k];

                                }   // end for k
                            } // end if
                        }   // end else
                    }   // end for j

                    //nhisp
                    // pachinko the mgras for nhisp with mg.data.nhisp as master and mgra.data.nhisp[0] as target
                    //if (i == 5)
                    //tstop = 1;
                    numcells = 7;
                    for (j = 0; j < numcells; ++j)
                        master[j] = mg[i].data.nhisp[j + 1];

                    // check for any mgras that dont sum; if so replace to the partial superset and zero before allocating

                    for (j = 0; j < mcount; ++j)
                    {
                        mgid = local_mgid[j];
                        //if (mgid == 597)
                        //tstop = 1;
                        target = mgra[mgid].data.nhisp[0];

                        if (target > 0)
                        {
                            Array.Clear(passer, 0, passer.Length);
                            ret = CU.cUtil.PachinkoWithMasterDecrement(target, master, passer, numcells);

                            if (ret >= 40000)
                            {
                                MessageBox.Show("For NHISP, Pachinko did not resolve in 40000 iterations for master geo  " + (i + 1).ToString() + " mgra " + (mgid + 1).ToString());
                            }     /* end if */
                            for (k = 0; k < numcells; ++k)
                            {
                                mgra[mgid].data.nhisp[k + 1] = passer[k];

                            }   // end for k
                        } // end if
                        else
                        {
                            for (k = 0; k < numcells; ++k)
                            {
                                mgra[mgid].data.nhisp[k + 1] = 0;

                            }   // end for k
                        }  // end else
                    }   // end for j

                    //----------------------------------------------------------------------------------------------------

                }   // end if pop > 0

            }   // end for i

        }  // end procedure BuildMGRA2()

        //***************************************************************************************************************

        //  BuildGQDetail()
        //  Build GQ detail from ctbg data 

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   02/27/12   tbe  New procedure for allocating 2010 Census to MGRAs

        //   ------------------------------------------------------------------

        public void BuildGQDetail(MGRAS[] mgra, CTBG[] ctbg, int size, int counter)
        {

            // goal here is to use CTBG totals for 4 different kinds of GQ detail
            // then look through all the lckeys that are in that ctbg and if they have an appropriate land use, allocate proportionally
            // based on lu priority and gq at the lckey and then acreage if there are no gq

            // get an ctbg - it will have from 1 - 4 cats of gq - usually only 1, sometimes 2
            // go through the 4 cats, find a nonzero this has to get allocated to lckeys based on the land use
            // for example - the first category is corrections - it has 13 distinct land uses  0 - 12 and that can get gq
            // in the ctbg array, the land uses are prioritized - 0 being best ; the priority array is initialized to 999

            // in this ctbg, for corrections gq, grab every lckey that has a corrections land use with priorities < 999
            // sort them in ascending order 
            // start with lowest and run up keeping count         

            int i = 0;
            int j = 0;
            int k = 0;
            int n = 0;
            int pi;
            int tgq = 0;
            int ctbid = 0;
            int tstop = 0;
            int lcknum = 0;
            int scount = 0;
            int tindex, aindex;
            int mgid = 0;
            int[] temp1 = new int[size];
            int[] priority = new int[size];
            int[] temp3 = new int[size];
            int[] shortlist = new int[size];
            double tacres;
            double[] shortpropg = new double[size];
            double[] shortpropa = new double[size];
            string strr = "";

            FileStream fout;		//file stream class
            FileStream flckey;
            // -------------------------------------------------------------------------------------------

            WriteToStatusBox(" Building GQ Detail data");
            // open output file
            try
            {
                fout = new FileStream(networkPath + "gq_temp", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            try
            {
                flckey = new FileStream(networkPath + "gq_lckey_temp.csv", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);
            StreamWriter flckeyw = new StreamWriter(flckey);

            for (i = 0; i < counter; ++i)
            {
                lcknum = ctbg[i].num_lckeys;
                ctbid = ctbg[i].ct10bg;
                if (ctbid == 7203061)
                    tstop = 1;
                for (j = 0; j < 3; ++j)   // 3 kinds of gq
                {
                    if (ctbg[i].gqx[j] == 0) // if this category of gq == 0 skip everything
                        continue;

                    Array.Clear(temp1, 0, temp1.Length);
                    Array.Clear(priority, 0, priority.Length);
                    // load the temp array with the lckey dat for jth category
                    for (k = 0; k < lcknum; ++k)
                    {
                        priority[k] = ctbg[i].lck[k].p[j];
                        temp1[k] = k;   // store original index
                    }  // end for k

                    // sort the temp array -ascending sort needs 3 arrays - we only use 2
                    // temp1 stores the index; priority stores the priority
                    CU.cUtil.AscendingSort(temp1, priority, temp3, ctbg[i].num_lckeys);

                    // once the priorities are sorted - start down the list
                    if (priority[0] == 999)
                    {
                        // write exception to ascii file

                        strr = "No acceptable site found (priority = 999) for CT10BG = " +
                               ctbg[i].ct10bg.ToString() + " with GQ  = " + ctbg[i].gqx[j] + " and category = " + (j + 1).ToString();
                        foutw.WriteLine(strr);
                        foutw.Flush();

                        // next priority = 90 if the minimum prority = 90 assign the gq to the largest existing gq (if there is any) or the largest acreage lckey
                    }  // end if
                    else if (priority[0] == 90)  // this is the easiest case
                    {
                        aindex = 0;
                        tindex = 0;
                        scount = 0;
                        Array.Clear(shortlist, 0, shortlist.Length);
                        Array.Clear(shortpropg, 0, shortpropg.Length);
                        Array.Clear(shortpropa, 0, shortpropa.Length);
                        shortlist[scount++] = temp1[0];   // store the index of the min in the short list

                        // find the largest existing gq if any and the largest acres with the same priority
                        for (n = 1; n < lcknum; ++n)
                        {
                            if (priority[n] == priority[0])
                            {
                                shortlist[scount++] = temp1[n];  // stores the original index of the matchint priority
                            }
                        }   // end for
                        // at this time the shortlist contsins the indexes of the lckeys with the minimum priority

                        tacres = 0;
                        tgq = 0;
                        for (k = 0; k < scount; ++k)
                        {
                            pi = shortlist[k];              // get the original index of the sorted data

                            if (ctbg[i].lck[pi].gq > tgq)    // if that guy is > then the max gq, replace it
                            {
                                tindex = k;
                                tgq = ctbg[i].lck[pi].gq;
                            }  // end if
                            if (ctbg[i].lck[pi].acres > tacres)
                            {
                                aindex = k;
                                tacres = ctbg[i].lck[pi].acres;
                            }   // end if
                        }   // end for k

                        // aindex has the index of the lckey with the largest acres, tindex has the index of largest gq - use only for mil anc college, not other
                        if (ctbg[i].lck[tindex].gq > 0)
                        {
                            pi = shortlist[tindex];
                            ctbg[i].lck[pi].gqx[j] = ctbg[i].gqx[j];
                        }  // end if
                        else
                        {
                            pi = shortlist[aindex];
                            ctbg[i].lck[pi].gqx[j] = ctbg[i].gqx[j];
                        }  // end else

                    }   // end if temp2[0] == 90

                    else if ((priority[0] == 91 || priority[0] == 92) && j == 3)  // this is the easiest case
                    {
                        aindex = 0;
                        tindex = 0;
                        scount = 0;
                        Array.Clear(shortlist, 0, shortlist.Length);
                        Array.Clear(shortpropg, 0, shortpropg.Length);
                        Array.Clear(shortpropa, 0, shortpropa.Length);
                        shortlist[scount++] = temp1[0];   // store the index of the min in the short list

                        // find the largest existing gq if any and the largest acres with the same priority
                        for (n = 1; n < lcknum; ++n)
                        {
                            if (priority[n] == priority[0])
                            {
                                shortlist[scount++] = temp1[n];  // stores the original index of the matchint priority
                            }
                        }   // end for
                        // at this time the shortlist contsins the indexes of the lckeys with the minimum priority

                        tacres = 0;
                        tgq = 0;
                        for (k = 0; k < scount; ++k)
                        {
                            pi = shortlist[k];              // get the original index of the sorted data

                            if (ctbg[i].lck[pi].gq > tgq)    // if that guy is > then the max gq, replace it
                            {
                                tindex = k;
                                tgq = ctbg[i].lck[pi].gq;
                            }  // end if
                            if (ctbg[i].lck[pi].acres > tacres)
                            {
                                aindex = k;
                                tacres = ctbg[i].lck[pi].acres;
                            }   // end if
                        }   // end for k

                        // aindex has the index of the lckey with the largest acres, tindex has the index of largest gq - use only fo rmil and college
                        if (ctbg[i].lck[tindex].gq > 0 && j < 2)
                        {
                            pi = shortlist[tindex];
                            ctbg[i].lck[pi].gqx[j] = ctbg[i].gqx[j];
                        }  // end if
                        else
                        {
                            pi = shortlist[aindex];
                            ctbg[i].lck[pi].gqx[j] = ctbg[i].gqx[j];
                        }  // end else

                    }   // end if priority = 91 or 92

                    else    // the priority < 90 , so we have to scan the array looking for all occurrances of the minimum value
                    {
                        // then build an array of the gqs and acres for each lckey with the minimum value and allocate proportionately
                        // scan the priority array looking for match to minimum

                        aindex = 0;
                        tindex = 0;
                        scount = 0;
                        Array.Clear(shortlist, 0, shortlist.Length);
                        shortlist[scount++] = temp1[0];   // store the index of the min in the short list

                        for (n = 1; n < lcknum; ++n)
                        {
                            if (priority[n] == priority[0])
                            {
                                shortlist[scount++] = temp1[n];  // stores the original index of the matchint priority
                            }  // end if
                        }   // end for


                        // at this time the shortlist contains the indexes of the lckeys with the minimum priority

                        // derive the proportions based on gq if any or acres

                        tacres = 0;
                        tgq = 0;
                        for (k = 0; k < scount; ++k)
                        {
                            pi = shortlist[k];              // get the original index of the sorted data
                            shortpropg[k] = ctbg[i].lck[pi].gq;
                            shortpropa[k] = ctbg[i].lck[pi].acres;
                            tgq += (int)shortpropg[k];
                            tacres += shortpropa[k];

                        }   // end for k

                        // derive proportions
                        for (k = 0; k < scount; ++k)
                        {
                            if (tgq > 0)
                                shortpropg[k] = shortpropg[k] / tgq;
                            if (tacres > 0)
                                shortpropa[k] = shortpropa[k] / tacres;
                        }  // end for k

                        int rtgq = 0;
                        for (k = 0; k < scount - 1; ++k)
                        {
                            pi = shortlist[k];
                            if (tgq > 0 && j < 2)
                            {
                                ctbg[i].lck[pi].gqx[j] = (int)((double)ctbg[i].gqx[j] * shortpropg[k]);
                                rtgq += ctbg[i].lck[pi].gqx[j];
                            }  // end else
                            else
                            {
                                ctbg[i].lck[pi].gqx[j] = (int)((double)ctbg[i].gqx[j] * shortpropa[k]);
                                rtgq += ctbg[i].lck[pi].gqx[j];
                            }  // end else
                        }  // end for
                        // store the remaining lcke as remainder to ensure = tgq;
                        pi = shortlist[scount - 1];
                        ctbg[i].lck[pi].gqx[j] = ctbg[i].gqx[j] - rtgq;
                    }  // end else

                }  // end for j 
                strr = "For CT10BG " + ctbg[i].ct10bg + " coll = " + ctbg[i].gqx[0] + " mil = " + ctbg[i].gqx[1] + " other = " + ctbg[i].gqx[2] + " GQ = " + ctbg[i].gq;
                foutw.WriteLine(strr);
                for (k = 0; k < lcknum; ++k)
                {
                    for (n = 0; n < 3; ++n)
                        ctbg[i].lck[k].revised_gq += ctbg[i].lck[k].gqx[n];

                    if (ctbg[i].lck[k].revised_gq > 0)
                    {
                        strr = "  lckey = " + ctbg[i].lck[k].id + " lu = " + ctbg[i].lck[k].lu +
                            " coll = " + ctbg[i].lck[k].gqx[0] + " mil = " + ctbg[i].lck[k].gqx[1] + " other = " + ctbg[i].lck[k].gqx[2] + " revised_gq = " + ctbg[i].lck[k].revised_gq;
                        foutw.WriteLine(strr);
                        foutw.Flush();
                        strr = ctbg[i].lck[k].id + "," + ctbg[i].lck[k].mgra_id + ",";
                        for (n = 0; n < 3; ++n)
                            strr += ctbg[i].lck[k].gqx[n] + ",";
                        strr += ctbg[i].lck[k].revised_gq;
                        flckeyw.WriteLine(strr);
                        flckeyw.Flush();
                    }  // end if

                } // end for k

            }   // end for i

            foutw.Flush();
            foutw.Close();
            fout.Close();
            flckeyw.Flush();
            flckeyw.Close();
            flckey.Close();
            BulkLoadLCKEYGQ();

            for (i = 0; i < NUM_MGRAS; ++i)
            {
                mgra[i].data.gq = 0;
            } // end for i

            try
            {
                for (i = 0; i < counter; ++i)
                {
                    lcknum = ctbg[i].num_lckeys;
                    // sum the lckey data to mgras
                    for (j = 0; j < lcknum; ++j)
                    {
                        mgid = ctbg[i].lck[j].mgra_id - 1;
                        if (mgid == 1819)
                            tstop = 1;
                        for (k = 0; k < 3; ++k)
                            mgra[mgid].data.gqx[k] += ctbg[i].lck[j].gqx[k];
                        mgra[mgid].data.gq += ctbg[i].lck[j].revised_gq;

                    }  // end for j
                }   // end for i                
            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally
        }  // end procedure BuildGQDetail()

        //*****************************************************************************************************************

        //  BuildLCKEYGQDetail()
        //  Build LCKEY GQ detail from mgra totals using same algorithm we used for ctbg - this resorts lckey stuff based on controlled mgra data 

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------

        //   03/19/12   tbe  New procedure for allocating 2010 Census to MGRAs

        //   ------------------------------------------------------------------

        public void BuildLCKEYGQDetail(MGRAS[] mgra)
        {

            // goal here is to use CTBG totals for 4 different kinds of GQ detail
            // then look through all the lckeys that are in that ctbg and if they have an appropriate land use, allocate proportionally
            // based on lu priority and gq at the lckey and then acreage if there are no gq

            // get an ctbg - it will have from 1 - 4 cats of gq - usually only 1, sometimes 2
            // go through the 4 cats, find a nonzero this has to get allocated to lckeys based on the land use
            // for example - the first category is corrections - it has 13 distinct land uses  0 - 12 and that can get gq
            // in the ctbg array, the land uses are prioritized - 0 being best ; the priority array is initialized to 999

            // in this ctbg, for corrections gq, grab every lckey that has a corrections land use with priorities < 999
            // sort them in ascending order 
            // start with lowest and run up keeping count         

            int i = 0;
            int j = 0;
            int k = 0;
            int n = 0;
            int pi;
            int tgq = 0;

            //int tstop = 0;
            int lcknum = 0;
            int scount = 0;
            int tindex, aindex;

            int[] temp1 = new int[MAX_LCKEYS_IN_MGRA];
            int[] priority = new int[MAX_LCKEYS_IN_MGRA];
            int[] temp3 = new int[MAX_LCKEYS_IN_MGRA];
            int[] shortlist = new int[MAX_LCKEYS_IN_MGRA];
            double tacres;
            double[] shortpropg = new double[MAX_LCKEYS_IN_MGRA];
            double[] shortpropa = new double[MAX_LCKEYS_IN_MGRA];
            string strr = "";

            FileStream fout;		//file stream class
            FileStream flckey;
            // -------------------------------------------------------------------------------------------

            WriteToStatusBox(" Rebuilding LCKEY GQ Detail data");
            // open output file
            try
            {
                fout = new FileStream(networkPath + "lckey_temp", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            try
            {
                flckey = new FileStream(networkPath + "gq_lckey_temp.csv", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);
            StreamWriter flckeyw = new StreamWriter(flckey);
            int tstop = 0;
            for (i = 0; i < NUM_MGRAS; ++i)
            {
                if (i == 1819)
                    tstop = 1;
                if (mgra[i].data.gq == 0)
                    continue;

                lcknum = mgra[i].num_lckeys;
                if (lcknum == 0)
                    continue;
                for (j = 0; j < 3; ++j)   // 3 kinds of gq
                {
                    if (mgra[i].data.gqx[j] == 0) // if this category of gq == 0 skip everything
                        continue;

                    Array.Clear(temp1, 0, temp1.Length);
                    Array.Clear(priority, 0, priority.Length);
                    // load the temp array with the lckey dat for jth category
                    for (k = 0; k < lcknum; ++k)
                    {
                        priority[k] = mgra[i].lck[k].p[j];
                        temp1[k] = k;   // store original index
                    }  // end for k

                    // sort the temp array -ascending sort needs 3 arrays - we only use 2
                    // temp1 stores the index; priority stores the priority
                    CU.cUtil.AscendingSort(temp1, priority, temp3, lcknum);

                    // once the priorities are sorted - start down the list
                    if (priority[0] == 999)
                    {
                        // write exception to ascii file

                        strr = "No acceptable site found (priority = 999) for NGRA = " + (i + 1) + " with GQ  = " + mgra[i].data.gqx[j] + " and category = " + (j + 1).ToString();
                        foutw.WriteLine(strr);
                        foutw.Flush();

                        // next priority = 90 if the minimum prority = 90 assign the gq to the largest existing gq (if there is any) or the largest acreage lckey
                    }  // end if
                    else if (priority[0] == 90)  // this is the easiest case
                    {
                        aindex = 0;
                        tindex = 0;
                        scount = 0;
                        Array.Clear(shortlist, 0, shortlist.Length);
                        Array.Clear(shortpropg, 0, shortpropg.Length);
                        Array.Clear(shortpropa, 0, shortpropa.Length);
                        shortlist[scount++] = temp1[0];   // store the index of the min in the short list

                        // find the largest existing gq if any and the largest acres with the same priority
                        for (n = 1; n < lcknum; ++n)
                        {
                            if (priority[n] == priority[0])
                            {
                                shortlist[scount++] = temp1[n];  // stores the original index of the matchint priority
                            } // end if
                        }   // end for
                        // at this time the shortlist contsins the indexes of the lckeys with the minimum priority

                        tacres = 0;
                        tgq = 0;
                        for (k = 0; k < scount; ++k)
                        {
                            pi = shortlist[k];              // get the original index of the sorted data

                            if (mgra[i].lck[pi].gq > tgq)    // if that guy is > then the max gq, replace it
                            {
                                tindex = k;
                                tgq = mgra[i].lck[pi].gq;
                            }  // end if
                            if (mgra[i].lck[pi].acres > tacres)
                            {
                                aindex = k;
                                tacres = mgra[i].lck[pi].acres;
                            }   // end if
                        }   // end for k

                        // aindex has the index of the lckey with the largest acres, tindex has the index of largest gq - use only for mil and college, not other
                        if (mgra[i].lck[tindex].gq > 0)
                        {
                            pi = shortlist[tindex];
                            mgra[i].lck[pi].gqx[j] = mgra[i].data.gqx[j];
                        }  // end if
                        else
                        {
                            pi = shortlist[aindex];
                            mgra[i].lck[pi].gqx[j] = mgra[i].data.gqx[j];
                        }  // end else

                    }   // end if temp2[0] == 90

                    else if ((priority[0] == 91 || priority[0] == 92) && j == 3)  // this is the easiest case
                    {
                        aindex = 0;
                        tindex = 0;
                        scount = 0;
                        Array.Clear(shortlist, 0, shortlist.Length);
                        Array.Clear(shortpropg, 0, shortpropg.Length);
                        Array.Clear(shortpropa, 0, shortpropa.Length);
                        shortlist[scount++] = temp1[0];   // store the index of the min in the short list

                        // find the largest existing gq if any and the largest acres with the same priority
                        for (n = 1; n < lcknum; ++n)
                        {
                            if (priority[n] == priority[0])
                            {
                                shortlist[scount++] = temp1[n];  // stores the original index of the matchint priority
                            }
                        }   // end for
                        // at this time the shortlist contsins the indexes of the lckeys with the minimum priority

                        tacres = 0;
                        tgq = 0;
                        for (k = 0; k < scount; ++k)
                        {
                            pi = shortlist[k];              // get the original index of the sorted data

                            if (mgra[i].lck[pi].gq > tgq)    // if that guy is > then the max gq, replace it
                            {
                                tindex = k;
                                tgq = mgra[i].lck[pi].gq;
                            }  // end if
                            if (mgra[i].lck[pi].acres > tacres)
                            {
                                aindex = k;
                                tacres = mgra[i].lck[pi].acres;
                            }   // end if
                        }   // end for k

                        // aindex has the index of the lckey with the largest acres, tindex has the index of largest gq - use only for mil and college
                        if (mgra[i].lck[tindex].gq > 0 && j < 2)
                        {
                            pi = shortlist[tindex];
                            mgra[i].lck[pi].gqx[j] = mgra[i].data.gqx[j];
                        }  // end if
                        else
                        {
                            pi = shortlist[aindex];
                            mgra[i].lck[pi].gqx[j] = mgra[i].data.gqx[j];
                        }  // end else

                    }   // end if priority = 91 or 92

                    else    // the priority < 90 , so we have to scan the array looking for all occurrances of the minimum value
                    {
                        // then build an array of the gqs and acres for each lckey with the minimum value and allocate proportionately
                        // scan the priority array looking for match to minimum

                        aindex = 0;
                        tindex = 0;
                        scount = 0;
                        Array.Clear(shortlist, 0, shortlist.Length);
                        shortlist[scount++] = temp1[0];   // store the index of the min in the short list

                        for (n = 1; n < lcknum; ++n)
                        {
                            if (priority[n] == priority[0])
                            {
                                shortlist[scount++] = temp1[n];  // stores the original index of the matchint priority
                            }  // end if
                        }   // end for


                        // at this time the shortlist contains the indexes of the lckeys with the minimum priority

                        // derive the proportions based on gq if any or acres

                        tacres = 0;
                        tgq = 0;
                        for (k = 0; k < scount; ++k)
                        {
                            pi = shortlist[k];              // get the original index of the sorted data
                            try
                            {
                                shortpropg[k] = mgra[i].lck[pi].gq;
                                shortpropa[k] = mgra[i].lck[pi].acres;
                                tgq += (int)shortpropg[k];
                                tacres += shortpropa[k];
                            }
                            catch (Exception exc)
                            {
                                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                            }  // end catch

                        }   // end for k

                        // derive proportions
                        for (k = 0; k < scount; ++k)
                        {
                            if (tgq > 0)
                                shortpropg[k] = shortpropg[k] / tgq;
                            if (tacres > 0)
                                shortpropa[k] = shortpropa[k] / tacres;
                        }  // end for k

                        int rtgq = 0;
                        for (k = 0; k < scount - 1; ++k)
                        {
                            pi = shortlist[k];
                            if (tgq > 0 && j < 2)
                            {
                                mgra[i].lck[pi].gqx[j] = (int)((double)mgra[i].data.gqx[j] * shortpropg[k]);
                                rtgq += mgra[i].lck[pi].gqx[j];
                            }  // end else
                            else
                            {
                                mgra[i].lck[pi].gqx[j] = (int)((double)mgra[i].data.gqx[j] * shortpropa[k]);
                                rtgq += mgra[i].lck[pi].gqx[j];
                            }  // end else
                        }  // end for
                        // store the remaining lcke as remainder to ensure = tgq;
                        pi = shortlist[scount - 1];
                        mgra[i].lck[pi].gqx[j] = mgra[i].data.gqx[j] - rtgq;
                    }  // end else

                }  // end for j 
                strr = "For MGRA " + (i + 1) + " coll = " + mgra[i].data.gqx[0] + " mil = " + mgra[i].data.gqx[1] + " other = " + mgra[i].data.gqx[2] + " GQ = " + mgra[i].data.gq;
                foutw.WriteLine(strr);
                for (k = 0; k < lcknum; ++k)
                {
                    for (n = 0; n < 3; ++n)
                        mgra[i].lck[k].revised_gq += mgra[i].lck[k].gqx[n];

                    if (mgra[i].lck[k].revised_gq > 0)
                    {
                        strr = "  lckey = " + mgra[i].lck[k].id + " lu = " + mgra[i].lck[k].lu + " coll = " + mgra[i].lck[k].gqx[0] +
                               " mil = " + mgra[i].lck[k].gqx[1] + " other = " + mgra[i].lck[k].gqx[2] + " revised_gq = " + mgra[i].lck[k].revised_gq;
                        foutw.WriteLine(strr);
                        foutw.Flush();
                        strr = mgra[i].lck[k].id + "," + (i + 1) + ",";
                        for (n = 0; n < 3; ++n)
                            strr += mgra[i].lck[k].gqx[n] + ",";
                        strr += mgra[i].lck[k].revised_gq;
                        flckeyw.WriteLine(strr);
                        flckeyw.Flush();
                    }  // end if

                } // end for k

            }   // end for i

            foutw.Flush();
            foutw.Close();
            fout.Close();
            flckeyw.Flush();
            flckeyw.Close();
            flckey.Close();
            BulkLoadLCKEYGQ();

        }  // end procedure BuildLCKEYGQDetail()

        //*****************************************************************************************************************
        #endregion

        #region Miscellaneous utilities

        // procedures

        //   BulkLoadCensusMGRA() - bulk load ascii to db table
        //   BulkLoadMGRADetailedPopData() - bulk load the ascii to db table
        //   BulkLoadLCKEYGQ() - bulk load the lckey allocated gq detail
        //   BulkLoadCensusSupersplit() - bulk load the supersplit or partialsupersplit data
        //   ExtractCensusData()  - extract Census data records
        //   ExtractCTBG_GQDATA() - extract Detailed GQ data for CTBG
        //   ExtractCTPopDetail() - fill ct10 detailed pop ASE
        //   ExtractXrefData()  - build the xref table
        //   GetCT10Index() - return the index of the id passed
        //   GetCTBGIndex()     - return the index of the id passed
        //   GetCT10BlockIndex() - determine the index of the ct10block with ctid
        //   ProcessParms() - Build the table names from runtime parms

        //   WriteMGRAData() - Write the controlled data to ASCII for bulk loading 
        //   WriteMGRADetailedPopData() - write the detailed pop data to ASCII for bulk loading
        //   WriteToStatusBox - display status text

        //---------------------------------------------------------------------------------------------------------

        //  BulkLoadCensusMGRA() */
        //  Bulk loads ASCII to popest MGRA

        //  Revision History
        //  Date       By   Description
        //  ------------------------------------------------------------------
        //  02/10/2012   tb   initial coding

        //  ------------------------------------------------------------------

        public void BulkLoadCensusMGRA()
        {
            string fo, fa;

            fo = networkPath + String.Format(appSettings["censusMGRATemp"].Value);
            fa = String.Format(appSettings["censusDetailedPopTemp"].Value);
            sqlCommand1.CommandTimeout = 180;
            WriteToStatusBox("TRUNCATING Census MGRATABLE");
            sqlCommand1.CommandText = String.Format(appSettings["truncate"].Value, TN.census_mgra);
            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlCnnConcep.Close();
            }

            WriteToStatusBox("BULK LOADING Census MGRA TABLE");

            sqlCommand1.CommandText = String.Format(appSettings["bulkInsert"].Value, TN.census_mgra, fo);

            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlCnnConcep.Close();
            }

            WriteToStatusBox("TRUNCATING Census Detailed Pop temp table");
            sqlCommand1.CommandText = String.Format(appSettings["truncate"].Value, TN.census_mgra_pop);

            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlCnnConcep.Close();
            }

            WriteToStatusBox("BULK LOADING Census MGRA TABLE");
            sqlCommand1.CommandText = String.Format(appSettings["bulkInsert"].Value, TN.census_mgra_pop, fa);
            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }

            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlCnnConcep.Close();
            }

            WriteToStatusBox("TRUNCATING Revised MGRA Age group table");
            sqlCommand1.CommandText = String.Format(appSettings["truncate"].Value, TN.census_2010_mgra_revised_agegroups);
            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlCnnConcep.Close();
            }

            string tex = "";
            WriteToStatusBox("Rebuilding Revised MGRA Age group table");
            tex = "SELECT mgra,pop,pop_0to4,pop_5to9,pop_10to14,pop_15to17,pop_18to19,pop_20+ pop_21+ pop_22to24 as pop_20to24," +
                   "pop_25to29,pop_30to34,pop_35to39,pop_40to44,pop_45to49,pop_50to54,pop_55to59,pop_60to61,pop_62to64,pop_65to66 + pop_67to69 as pop_65to69," +
                   "pop_70to74,pop_75to79,pop_80to84,pop_85plus,popm,popm_0to4,popm_5to9,popm_10to14,popm_15to17,popm_18to19, " +
                   "popm_20+popm_21+ popm_22to24 as popm_20to24,popm_25to29,popm_30to34,popm_35to39,popm_40to44,popm_45to49,popm_50to54,popm_55to59," +
                   "popm_60to61,popm_62to64,popm_65to66+popm_67to69 as popm_65to69,popm_70to74,popm_75to79,popm_80to84,popm_85plus,popf,popf_0to4,popf_5to9," +
                   "popf_10to14,popf_15to17,popf_18to19,popf_20+popf_21+popf_22to24 as popf_20to24,popf_25to29,popf_30to34,popf_35to39,popf_40to44," +
                   "popf_45to49,popf_50to54,popf_55to59,popf_60to61,popf_62to64,popf_65to66+popf_67to69 as popf_65to69,popf_70to74,popf_75to79,popf_80to84," +
                   "popf_85plus,hisp,nhispw,nhispb,nhispi,nhispa,nhisph,nhispo,nhisp2 FROM census.dbo.census_detailed_pop_temp";
            sqlCommand1.CommandText = String.Format(appSettings["insertInto"].Value, TN.census_2010_mgra_revised_agegroups, tex);


            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlCnnConcep.Close();
            }

        } // end procedure BulkLoadCensusMGRA()       

        //**********************************************************************************************************************

        //  BulkLoadMGRADetailedPopData() */
        //  Bulk loads ASCII to popest MGRA

        //  Revision History
        //  Date       By   Description
        //  ------------------------------------------------------------------
        //  02/10/2012   tb   initial coding

        //  ------------------------------------------------------------------

        public void BulkLoadMGRADetailedPopData()
        {
            string fo;
            sqlCommand1.CommandTimeout = 180;
            WriteToStatusBox("TRUNCATING Census MGRA Detailed Pop TABLE");
            fo = networkPath + "census_mgra_pop_temp";
            sqlCommand1.CommandText = String.Format(appSettings["truncateTAble"].Value, TN.census_mgra_pop);
            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlCnnConcep.Close();
            }

            WriteToStatusBox("BULK LOADING Census MGRA Detailed Pop TABLE");
            sqlCommand1.CommandText = String.Format(appSettings["bulkInsert"].Value, TN.census_mgra_pop, fo);
            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlCnnConcep.Close();
            }
        } // end procedure BulkLoadCensusMGRA()       

        //**********************************************************************************************************************

        public void BulkLoadLCKEYGQ()
        {
            string fo;

            fo = networkPath + "gq_lckey_temp.csv";
            WriteToStatusBox("TRUNCATING LCKEY GQ table");
            sqlCommand1.CommandText = String.Format(appSettings["truncate"].Value, TN.census_gq_lckey);
            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                sqlCnnConcep.Close();
            }

            WriteToStatusBox("BULK LOADING LCKEY GQ TABLE");
            sqlCommand1.CommandTimeout = 180;
            sqlCommand1.CommandText = String.Format(appSettings["bulkInsert"].Value, TN.census_gq_lckey, fo);

            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlCnnConcep.Close();
            }

        } // end procedure BulkLoadLCKEYGQ()       

        //**********************************************************************************************************************

        //  BulkLoadCensusSupersplit() */
        //  Bulk loads ASCII to supersplit or partialsupersplit table

        //  Revision History
        //  Date       By   Description
        //  ------------------------------------------------------------------
        //  02/10/2012   tb   initial coding

        //  ------------------------------------------------------------------

        public void BulkLoadCensusSupersplit(int type)
        {
            string fo;

            if (type == 1)
                fo = networkPath + "census_supersplit";
            else
                fo = networkPath + "census_partialsupersplit";
            WriteToStatusBox("TRUNCATING Census Supersplit TABLE");
            if (type == 1)
                sqlCommand1.CommandText = String.Format(appSettings["truncate"].Value, TN.census_2010_supersplit);
            else
                sqlCommand1.CommandText = String.Format(appSettings["truncate"].Value, TN.census_2010_partialsupersplit);
            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlCnnConcep.Close();
            }

            WriteToStatusBox("BULK LOADING Census Supersplit TABLE");
            sqlCommand1.CommandTimeout = 180;
            if (type == 1)
                sqlCommand1.CommandText = String.Format(appSettings["bulkInsert"].Value, TN.census_2010_supersplit, fo);
            else
                sqlCommand1.CommandText = String.Format(appSettings["bulkInsert"].Value, TN.census_2010_partialsupersplit, fo);

            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlCnnConcep.Close();
            }
        } // end procedure BulkLoadCensusSupersplit()       

        //**********************************************************************************************************************

        //ExtractCensusData()

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/06/2012   tb   started initial coding
        //   ------------------------------------------------------------------
        public void ExtractCensusData(CT10BLOCKS[] ct10block, SUPERSPLITS[] super, SUPERSPLITS[] psuper, MGRAS[] mgra, int num_ct10blocks, bool do_SG, int MSIZE)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int i = 0, j = 0, increm = 0;
            int index = 0, ctb = 0, counter = 0;
            int dtemp = 0;

            int stemp;
            //----------------------------------------------------------------

            WriteToStatusBox("Filling MGRA landcore HS ");

            // fill mgra landcore hs data

            if (do_SG)
                sqlCommand1.CommandText = String.Format(appSettings["selectCensus1"].Value, TN.xref, TN.wilma, "SG");
            else
                sqlCommand1.CommandText = String.Format(appSettings["selectCensus1"].Value, TN.xref, TN.wilma, "supersplit");
            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    i = rdr.GetInt32(0) - 1;
                    stemp = rdr.GetInt32(1) - 1;
                    mgra[i].superID = stemp + 1;
                    mgra[i].data.sandag_hs = rdr.GetInt32(2);

                    super[stemp].data.sandag_hs += mgra[i].data.sandag_hs;

                    psuper[stemp].data.sandag_hs += mgra[i].data.sandag_hs;

                }  // end while
                rdr.Close();

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }

            // fill census block hhwc data
            if (do_SG)
                this.sqlCommand1.CommandText = String.Format(appSettings["selectCensus7"].Value, TN.census_hhwc_input, "SG");
            else
                this.sqlCommand1.CommandText = String.Format(appSettings["selectCensus7"].Value, TN.census_hhwc_input, "supersplit");

            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    if (counter % 10000 == 0)
                        WriteToStatusBox(" Processing " + counter + " records from 2010 Census HHWC records");

                    increm = 0;     // skip to ct10block

                    //ct10block
                    ctb = rdr.GetInt32(increm++);
                    index = GetCT10BlockIndex(ct10block, ctb, num_ct10blocks);

                    //supersplit
                    ct10block[index].superID = rdr.GetInt32(increm++);
                    stemp = ct10block[index].superID - 1;

                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.hhwc += dtemp;
                    super[stemp].data.hhwc += dtemp;
                    psuper[stemp].data.hhwc += dtemp;

                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.hhwoc += dtemp;
                    super[stemp].data.hhwoc += dtemp;
                    psuper[stemp].data.hhwoc += dtemp;

                    ++counter;
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }

            // fill census ct block data
            counter = 0;
            if (do_SG)
                sqlCommand1.CommandText = String.Format(appSettings["selectCensus6"].Value, TN.census_input, "SG");
            else
                this.sqlCommand1.CommandText = String.Format(appSettings["selectCensus6"].Value, TN.census_input, "supersplit");
            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    if (counter % 10000 == 0)
                        WriteToStatusBox(" Processing " + counter + " records from 2010 Census records");

                    increm = 5;     // skip to ct10block

                    //ct10block
                    ctb = rdr.GetInt32(increm++);
                    index = GetCT10BlockIndex(ct10block, ctb, num_ct10blocks);
                    if (do_SG)
                        increm = 9;  // skip to SG id
                    else
                        increm = 6;  // skip to supersplit id

                    //supersplit
                    ct10block[index].superID = rdr.GetInt32(increm++);
                    stemp = ct10block[index].superID - 1;

                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.pop += dtemp;
                    super[stemp].data.pop += dtemp;
                    psuper[stemp].data.pop += dtemp;

                    for (j = 0; j < 23; ++j)
                    {
                        dtemp = rdr.GetInt32(increm++);
                        ct10block[index].data.aget[j] += dtemp;
                        super[stemp].data.aget[j] += dtemp;
                        psuper[stemp].data.aget[j] += dtemp;
                    }   // end for j

                    //age data totals, popm, male age, popf, female age
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.popm += dtemp;
                    super[stemp].data.popm += dtemp;
                    psuper[stemp].data.popm += dtemp;

                    for (j = 0; j < 23; ++j)
                    {
                        dtemp = rdr.GetInt32(increm++);
                        ct10block[index].data.agem[j] += dtemp;
                        super[stemp].data.agem[j] += dtemp;
                        psuper[stemp].data.agem[j] += dtemp;
                    } // end for j

                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.popf += dtemp;
                    super[stemp].data.popf += dtemp;
                    psuper[stemp].data.popf += dtemp;

                    for (j = 0; j < 23; ++j)
                    {
                        dtemp = rdr.GetInt32(increm++);
                        ct10block[index].data.agef[j] += dtemp;
                        super[stemp].data.agef[j] += dtemp;
                        psuper[stemp].data.agef[j] += dtemp;
                    } // end for j

                    // ethnicity - nhisp
                    for (j = 0; j < 8; ++j)
                    {
                        dtemp = rdr.GetInt32(increm++);
                        ct10block[index].data.nhisp[j] += dtemp;
                        super[stemp].data.nhisp[j] += dtemp;
                        psuper[stemp].data.nhisp[j] += dtemp;
                    } // end for j

                    // ethnicity - hisp
                    for (j = 0; j < 8; ++j)
                    {
                        dtemp = rdr.GetInt32(increm++);
                        ct10block[index].data.hisp[j] += dtemp;
                        super[stemp].data.hisp[j] += dtemp;
                        psuper[stemp].data.hisp[j] += dtemp;
                    } // end for j

                    // hhp
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.hhp += dtemp;
                    super[stemp].data.hhp += dtemp;
                    psuper[stemp].data.hhp += dtemp;

                    // hhp_own
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.hhp_own += dtemp;
                    super[stemp].data.hhp_own += dtemp;
                    psuper[stemp].data.hhp_own += dtemp;

                    // hhp_rent
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.hhp_rent += dtemp;
                    super[stemp].data.hhp_rent += dtemp;
                    psuper[stemp].data.hhp_rent += dtemp;

                    // hhs
                    for (j = 0; j < 7; ++j)
                    {
                        dtemp = rdr.GetInt32(increm++);
                        ct10block[index].data.hhsx[j] += dtemp;
                        super[stemp].data.hhsx[j] += dtemp;
                        psuper[stemp].data.hhsx[j] += dtemp;
                    } // end for j

                    //gq
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.gq += dtemp;
                    super[stemp].data.gq += dtemp;
                    psuper[stemp].data.gq += dtemp;


                    //for (j = 0; j < 8; ++j)
                    //{
                    //    dtemp = rdr.GetInt32(increm++);
                    //    //ct10block[index].data.gqx[j] += dtemp;
                    //    super[stemp].data.gqx[j] += dtemp;
                    //    //psuper[stemp].data.gqx[j] += dtemp;
                    //} // end for j

                    // reset increment to accoutn for not reading gq data
                    increm += 8;

                    //hh
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.hh += dtemp;
                    super[stemp].data.hh += dtemp;
                    psuper[stemp].data.hh += dtemp;

                    //hh_own
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.hh_own += dtemp;
                    super[stemp].data.hh_own += dtemp;
                    psuper[stemp].data.hh_own += dtemp;

                    //hh
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.hh_rent += dtemp;
                    super[stemp].data.hh_rent += dtemp;
                    psuper[stemp].data.hh_rent += dtemp;

                    // hs
                    dtemp = rdr.GetInt32(increm++);
                    ct10block[index].data.census_hs += dtemp;
                    super[stemp].data.census_hs += dtemp;
                    psuper[stemp].data.census_hs += dtemp;

                    if (ct10block[index].data.hh > 0)
                        ct10block[index].data.hhs = (double)ct10block[index].data.hhp / (double)ct10block[index].data.hh;

                    if (ct10block[index].data.census_hs > 0)
                        ct10block[index].data.occ_rate = (double)ct10block[index].data.hh / (double)ct10block[index].data.census_hs;
                    ++counter;
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }

            for (i = 0; i < MSIZE; ++i)
            {
                if (super[i].data.sandag_hs > 0)
                    super[i].data.occ_rate = (double)super[i].data.hh / (double)super[i].data.sandag_hs;
                if (psuper[i].data.sandag_hs > 0)
                    psuper[i].data.occ_rate = (double)psuper[i].data.hh / (double)psuper[i].data.sandag_hs;
                if (super[i].data.hh > 0)
                    super[i].data.hhs = (double)super[i].data.hhp / (double)super[i].data.hh;
                else
                    super[i].data.hhs = 1;

                if (psuper[i].data.hh > 0)
                    psuper[i].data.hhs = (double)psuper[i].data.hhp / (double)psuper[i].data.hh;
                else
                    psuper[i].data.hhs = 1;

            }   // end for i

            WriteSupersplitData(super, 1, MSIZE);

            BulkLoadCensusSupersplit(1);

        } // end procedure ExtractCensusDATA

        //******************************************************************************************************

        //ExtractCTBG_GQDATA()

        // populate the census block group gq data set

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/27/12   tb   started initial coding
        //   ------------------------------------------------------------------
        public void ExtractCTBG_GQDATA(CTBG[] ctbg, MGRAS[] mgra, int whichmodel)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int counter = 0;
            int k, i, id, n, lck, m;
            int mgra_id = 0;
            int mgra_index = 0;
            int lu;
            int gq;
            int[] p = new int[3];
            double ac;
            int local_count = 0;
            int ctbg_counter;

            if (whichmodel == 1)
            {
                WriteToStatusBox("Extracting CTBG GQ Detail");
                sqlCommand1.CommandText = String.Format(appSettings["selectCensus2"].Value, TN.census_gq_ctbg, "ctbg");
                local_count = MAX_LCKEYS_IN_CTBG;
                ctbg_counter = NUM_CTBG;

            }  // end if
            else
            {
                WriteToStatusBox("Extracting CITYCTBG GQ Detail");
                this.sqlCommand1.CommandText = String.Format(appSettings["selectCensus2"].Value, TN.census_gq_ctbg, "cityctbg");
                local_count = MAX_LCKEYS_IN_CITYCTBG;
                ctbg_counter = NUM_CITYCTBG;

            }   // end else

            counter = 0;
            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    ctbg[counter] = new CTBG();
                    ctbg[counter].lck = new LCKEY_DATA[MAX_LCKEYS_IN_CITYCTBG];
                    ctbg[counter].ct10bg = rdr.GetInt32(0);  // note that we use ct10bg to hold both ct10bg and cityct10bg if we use that method
                    ctbg[counter].gq = rdr.GetInt32(1);
                    for (k = 0; k < 3; ++k)
                        ctbg[counter].gqx[k] = rdr.GetInt32(2 + k);
                    for (k = 0; k < local_count; ++k)
                    {
                        ctbg[counter].lck[k] = new LCKEY_DATA();
                        for (int l = 0; l < 3; ++l)
                            ctbg[counter].lck[k].p[l] = 999;  //initialize the priorities to 999
                    }   // end for k
                    ++counter;
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally

            // fill the ctbg ids and the census detailed gq counts
            if (whichmodel == 1)
            {
                WriteToStatusBox("Extracting lckey - ct10bg xref for gq");
                sqlCommand1.CommandText = String.Format(appSettings["selectCensus8"].Value, TN.startingGQ, TN.gq_lu_priority, "ct10bg");
            }  // end if
            else
            {
                WriteToStatusBox("Extracting lckey - cityct10bg xref for gq");
                this.sqlCommand1.CommandText = String.Format(appSettings["selectCensus8"].Value, TN.startingGQ, TN.gq_lu_priority, "cityct10bg");
            }  // end else

            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    int tstop = 0;
                    lck = rdr.GetInt32(1);

                    i = rdr.GetInt32(0);
                    id = GetCT10BGIndex(ctbg, i, ctbg_counter);
                    if (id == 9999)
                        continue;
                    // xtract data
                    lu = rdr.GetInt32(2);
                    mgra_id = rdr.GetInt32(3);
                    ac = rdr.GetDouble(4);
                    gq = rdr.GetInt32(5);
                    for (k = 0; k < 3; ++k)
                        p[k] = rdr.GetInt32(6 + k);

                    mgra_index = mgra_id - 1;
                    if (mgra_index == 1819)
                        tstop = 1;
                    m = mgra[mgra_index].num_lckeys;
                    if (mgra[mgra_index].lck[m] == null)
                    {
                        mgra[mgra_index].lck[m] = new LCKEY_DATA();
                        for (k = 0; k < 3; ++k)
                            mgra[mgra_index].lck[m].p[k] = 999;
                    }   // end if

                    n = ctbg[id].num_lckeys;

                    // populate ctbg arrays with this lckey
                    ctbg[id].lck[n].id = lck;
                    ctbg[id].lck[n].lu = lu;
                    ctbg[id].lck[n].mgra_id = mgra_id;
                    ctbg[id].lck[n].acres = ac;
                    ctbg[id].lck[n].gq = gq;
                    for (k = 0; k < 3; ++k)
                        ctbg[id].lck[n].p[k] = p[k];

                    ++ctbg[id].num_lckeys;
                    // populate mgra arrays with this lckey
                    mgra[mgra_index].lck[m].id = lck;
                    mgra[mgra_index].lck[m].lu = lu;
                    mgra[mgra_index].lck[m].acres = ac;
                    mgra[mgra_index].lck[m].gq = gq;
                    for (k = 0; k < 3; ++k)
                        mgra[mgra_index].lck[m].p[k] = p[k];

                    ++mgra[mgra_index].num_lckeys;
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally


        } // end procedure ExtractCTBG_GQDATA()

        // *********************************************************************************************************

        //ExtractCTPopDetail()

        // populate the census tract age, sex ethncity detail

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   03/6/12   tb   started initial coding
        //   ------------------------------------------------------------------

        public void ExtractCTPopDetail(CTPOP[] ctpop, int[] control_cts, ref int num_control_cts, MGRAS[] mgra)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int counter = 0;
            int increm = 0;
            int j, i, ctindex = 0;
            // ------------------------------------------------------------------------------------------------------
            // fill the ctbg ids and the census detailed gq counts

            WriteToStatusBox("Extracting CT Pop Detail");
            // get ct ids and set up arrays

            sqlCommand1.CommandText = String.Format(appSettings["selectCT10"].Value, TN.xref);
            try
            {
                counter = 0;

                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    ctpop[counter] = new CTPOP();
                    ctpop[counter].mgra_ids = new int[MAX_MGRAS_IN_CTS];
                    ctpop[counter++].ct10 = rdr.GetInt32(0);

                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally

            // get detailed ct pop data
            sqlCommand1.CommandText = String.Format(appSettings["selectAll"].Value, TN.census_detailed_pop_tab_ct);
            try
            {
                counter = 0;

                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    increm = 0;
                    i = rdr.GetInt32(increm++);
                    ctindex = GetCT10Index(ctpop, i);

                    i = rdr.GetInt32(increm++);    // ethnicity
                    ctpop[ctindex].rpopt[i] = rdr.GetInt32(increm++);

                    for (j = 0; j < 20; ++j)
                    {
                        ctpop[ctindex].raget[i, j] = rdr.GetInt32(increm++);
                    }  // end for j

                    ctpop[ctindex].rpopm[i] = rdr.GetInt32(increm++);

                    for (j = 0; j < 20; ++j)
                    {
                        ctpop[ctindex].ragem[i, j] = rdr.GetInt32(increm++);
                    }  // end for j

                    ctpop[ctindex].rpopf[i] = rdr.GetInt32(increm++);

                    for (j = 0; j < 20; ++j)
                    {
                        ctpop[ctindex].ragef[i, j] = rdr.GetInt32(increm++);
                    }  // end for j

                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally

            WriteToStatusBox("Extracting mgra,ct10 ids");
            this.sqlCommand1.CommandText = String.Format(appSettings["selectCensus4"].Value, TN.xref);

            int m, ctid;
            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    m = rdr.GetInt32(0);
                    i = rdr.GetInt32(1);      // get the ct10 id
                    ctid = GetCT10Index(ctpop, i);

                    try
                    {
                        j = ctpop[ctid].num_mgras;
                        ctpop[ctid].mgra_ids[j] = m;
                        ++ctpop[ctid].num_mgras;
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
                sqlCnnConcep.Close();
            }  // end finally

            // get mgra pop data
            WriteToStatusBox("Extracting mgra pop");
            sqlCommand1.CommandText = String.Format(appSettings["selectAll"].Value, TN.census_2010_mgra_revised_agegroups);
            try
            {
                counter = 0;

                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    increm = 0;
                    i = rdr.GetInt32(increm++) - 1;

                    mgra[i].cpopt = rdr.GetInt32(increm++);

                    for (j = 0; j < 20; ++j)
                    {
                        mgra[i].caget[j] = rdr.GetInt32(increm++);
                    }  // end for j

                    mgra[i].cpopm = rdr.GetInt32(increm++);

                    for (j = 0; j < 20; ++j)
                    {
                        mgra[i].cagem[j] = rdr.GetInt32(increm++);
                    }  // end for j

                    mgra[i].cpopf = rdr.GetInt32(increm++);

                    for (j = 0; j < 20; ++j)
                    {
                        mgra[i].cagef[j] = rdr.GetInt32(increm++);
                    }  // end for j

                    mgra[i].data.hisp[0] = rdr.GetInt32(increm++);
                    for (j = 1; j < 8; ++j)
                        mgra[i].data.nhisp[j] = rdr.GetInt32(increm++);

                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally


            sqlCommand1.CommandText = String.Format(appSettings["selectAll"].Value, TN.census_detailed_pop_control_ct);
            try
            {
                counter = 0;

                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    control_cts[counter++] = rdr.GetInt32(0);
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally

            num_control_cts = counter;


        }   // end ExtractCtPopDetail()

        //****************************************************************************************************************

        //ExtractXREFData()

        // populate xref data

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/06/12   tb   started initial coding
        //   ------------------------------------------------------------------
        public void ExtractXREFData(CT10BLOCKS[] ct10block, SUPERSPLITS[] super, SUPERSPLITS[] psuper, MGRAS[] mgra,
                                    ref int num_ct10blocks, bool do_SG, int MSIZE)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int i, j, id = 0;
            int m, stemp, ctb;

            //int ctid = 0;
            //------------------------------------------------------------------------------------------------------------------------
            // fill the ct10block ids

            WriteToStatusBox("Extracting distinct ct10block ids");
            sqlCommand1.CommandText = String.Format(appSettings["selectCensus3"].Value, TN.census_input);

            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    ct10block[num_ct10blocks] = new CT10BLOCKS();
                    ct10block[num_ct10blocks].data = new CENSUSDATA();
                    ct10block[num_ct10blocks].ct10 = rdr.GetInt32(0);
                    ct10block[num_ct10blocks].block = rdr.GetInt32(1);
                    ct10block[num_ct10blocks++].ct10block = rdr.GetInt32(2);
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally

            // fill the ct10blocks with mgra ids
            WriteToStatusBox("Extracting ct10block mgra ids for 1-1 and many - 1");
            sqlCommand1.CommandText = String.Format(appSettings["selectCensus9"].Value, TN.ctblock_mgra_manyto1);

            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {

                    ctb = rdr.GetInt32(0);
                    m = rdr.GetInt32(1);
                    id = GetCT10BlockIndex(ct10block, ctb, num_ct10blocks);
                    j = ct10block[id].num_mgras;
                    ct10block[id].mgra_id = m;
                    ct10block[id].num_mgras = 1;
                }   // end while
                rdr.Close();

            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            finally
            {
                sqlCnnConcep.Close();
            }  // end finally

            // fill the supersplits ids

            for (i = 0; i < MSIZE; ++i)
            {
                super[i] = new SUPERSPLITS();
                super[i].mgra_ids = new int[MAX_MGRAS_IN_SUPERSPLITS];
                psuper[i] = new SUPERSPLITS();
                psuper[i].mgra_ids = new int[MAX_MGRAS_IN_SUPERSPLITS];
                super[i].data = new CENSUSDATA();
                psuper[i].data = new CENSUSDATA();
            }   // end for i

            if (do_SG)
            {
                WriteToStatusBox("Extracting SG-mgra ids");
                sqlCommand1.CommandText = String.Format(appSettings["selectCensus5"].Value, TN.xref, "SG");
            }  // end if
            else
            {
                WriteToStatusBox("Extracting supersplit-mgra ids");
                this.sqlCommand1.CommandText = String.Format(appSettings["selectCensus5"].Value, TN.xref, "supersplit");
            }  // end else

            try
            {
                sqlCnnConcep.Open();
                rdr = sqlCommand1.ExecuteReader();
                while (rdr.Read())
                {
                    m = rdr.GetInt32(0);
                    stemp = rdr.GetInt32(1) - 1;

                    try
                    {
                        j = super[stemp].num_mgras;

                        super[stemp].mgra_ids[j] = m;
                        psuper[stemp].mgra_ids[j] = m;
                        ++super[stemp].num_mgras;
                        ++psuper[stemp].num_mgras;
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
                sqlCnnConcep.Close();
            }  // end finally

        } // end procedure ExtractXREFData()

        //********************************************************************************************

        //  GetCT10Index()

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   03/6/12   tb   initial coding

        //   ------------------------------------------------------------------

        public Int32 GetCT10Index(CTPOP[] ctpop, int i)
        {
            int j;
            int ret = 9999;
            for (j = 0; j < NUM_CTS; ++j)
            {
                if (ctpop[j].ct10 == i)
                {
                    ret = j;
                    break;
                }   // end if
            } // end for j
            return ret;
        } // end procedure GetCT10Index()

        //**************************************************************************************       

        //  GetCT10BlockIndex()

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/07/12   tb   initial coding

        //   ------------------------------------------------------------------

        public Int32 GetCT10BlockIndex(CT10BLOCKS[] ct, int i, int num_ct10blocks)
        {
            int j;
            int ret = 9999;
            for (j = 0; j < num_ct10blocks; ++j)
            {
                if (ct[j].ct10block == i)
                {
                    ret = j;
                    break;
                }   // end if
            } // end for j
            return ret;
        } // end procedure GetCT10BlockIndex

        //**************************************************************************************     

        //  GetCT10BGIndex()

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/27/12   tb   initial coding

        //   ------------------------------------------------------------------

        public Int32 GetCT10BGIndex(CTBG[] ctb, int i, int counter)
        {
            int j;
            int ret = 9999;
            try
            {

                for (j = 0; j < counter; ++j)
                {
                    if (ctb[j].ct10bg == i)
                    {
                        ret = j;
                        break;
                    }   // end if
                } // end for j

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }  // end catch
            return ret;
        } // end procedure GetCT10BGIndex

        //**************************************************************************************       

        //  WriteSupersplitData() 
        //  Write the supersplit sums data to ASCII for bulk loading

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/10/12   tb   initial coding

        //   ------------------------------------------------------------------
        public void WriteSupersplitData(SUPERSPLITS[] s, int type, int MSIZE)
        {
            int i, j;
            FileStream fout;		//file stream class

            // open output file
            try
            {
                if (type == 1)
                    fout = new FileStream(networkPath + "census_supersplit", FileMode.Create);
                else
                    fout = new FileStream(networkPath + "census_partialsupersplit", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);

            for (i = 0; i < MSIZE; ++i)
            {
                string str = (i + 1) + "," + s[i].data.pop + ",";
                for (j = 0; j < 23; ++j)
                    str += s[i].data.aget[j] + ",";
                str += s[i].data.popm + ",";
                for (j = 0; j < 23; ++j)
                    str += s[i].data.agem[j] + ",";
                str += s[i].data.popf + ",";
                for (j = 0; j < 23; ++j)
                    str += s[i].data.agef[j] + ",";
                for (j = 0; j < 8; ++j)
                    str += s[i].data.nhisp[j] + ",";
                for (j = 0; j < 8; ++j)
                    str += s[i].data.hisp[j] + ",";
                str += s[i].data.hhp + "," + s[i].data.hhp_own + "," + s[i].data.hhp_rent + ",";
                for (j = 0; j < 7; ++j)
                    str += s[i].data.hhsx[j] + ",";
                str += s[i].data.gq + ",";
                for (j = 0; j < 3; ++j)
                    str += s[i].data.gqx[j] + ",";
                str += s[i].data.hh + "," + s[i].data.hh_own + "," + s[i].data.hh_rent + "," + s[i].data.sandag_hs + "," + s[i].data.hhs + "," +
                    s[i].data.occ_rate + "," + s[i].data.hhwc + "," + s[i].data.hhwoc;

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
            }   // end for i
            foutw.Flush();
            foutw.Close();
        } // end procedure WriteSupersplitData

        //*******************************************************************************************

        //  WriteMGRAData() 
        // Write the mgra, supersplit,psuper data to ASCII for bulk loading

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/10/12   tb   initial coding

        //   ------------------------------------------------------------------
        public void WriteMGRAData(MGRAS[] mgra)
        {
            int i, j;
            FileStream fout;		//file stream class
            FileStream foua;        // file stream class for age output

            WriteToStatusBox("Writing MGRA data");
            // open output file
            try
            {
                fout = new FileStream(networkPath + "census_mgra_temp", FileMode.Create);

            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            try
            {
                foua = new FileStream(networkPath + "detailed_pop_temp", FileMode.Create);

            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);
            StreamWriter fouaw = new StreamWriter(foua);

            foutw.AutoFlush = true;
            fouaw.AutoFlush = true;


            for (i = 0; i < NUM_MGRAS; ++i)
            {
                if (i % 5000 == 0)
                    WriteToStatusBox("  writing record " + i);
                //int tstop = 0;
                //if (i == 16428)
                //    tstop = 1;
                string str = (i + 1) + "," + mgra[i].superID + "," + mgra[i].data.pop + ",";
                string stra = (i + 1) + "," + mgra[i].data.pop + ",";

                // age detail to temp file
                for (j = 0; j < 23; ++j)
                    stra += mgra[i].data.aget[j] + ",";
                stra += mgra[i].data.popm + ",";
                for (j = 0; j < 23; ++j)
                    stra += mgra[i].data.agem[j] + ",";
                stra += mgra[i].data.popf + ",";
                for (j = 0; j < 23; ++j)
                    stra += mgra[i].data.agef[j] + ",";
                stra += mgra[i].data.hisp[0] + ",";

                for (j = 1; j < 7; ++j)
                    stra += mgra[i].data.nhisp[j] + ",";
                stra += mgra[i].data.nhisp[7];

                for (j = 0; j < 8; ++j)
                    str += mgra[i].data.nhisp[j] + ",";
                for (j = 0; j < 8; ++j)
                    str += mgra[i].data.hisp[j] + ",";
                str += mgra[i].data.hhp + "," + mgra[i].data.hhp_own + "," + mgra[i].data.hhp_rent + ",";
                for (j = 0; j < 7; ++j)
                    str += mgra[i].data.hhsx[j] + ",";
                str += mgra[i].data.gq + ",";
                for (j = 0; j < 3; ++j)
                    str += mgra[i].data.gqx[j] + ",";
                str += mgra[i].data.hh + "," + mgra[i].data.hh_own + "," + mgra[i].data.hh_rent + "," + mgra[i].data.sandag_hs + "," + mgra[i].data.hhs + "," +
                    mgra[i].data.occ_rate + "," + mgra[i].data.hhwc + "," + mgra[i].data.hhwoc;

                try
                {
                    foutw.WriteLine(str);
                    //foutw.Flush();
                }
                catch (IOException exc)      //exceptions here
                {
                    MessageBox.Show(exc.Message + " File Write Error");
                    return;
                }
                try
                {
                    fouaw.WriteLine(stra);
                    //foutw.Flush();
                }
                catch (IOException exc)      //exceptions here
                {
                    MessageBox.Show(exc.Message + " File Write Error");
                    return;
                }

            }   // end for i
            //foutw.Flush();
            WriteToStatusBox("Closing ASCII file");
            foutw.Close();
            fouaw.Close();
            WriteToStatusBox("ASCII file closed");
            fout.Close();
            foua.Close();


        } // end procedure WriteMGRAData

        //*******************************************************************************************

        //  WriteMGRADetailedPopData() 
        // Write the mgra, detailed pop data to ASCII for bulk loading

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   03/8/12   tb   initial coding

        //   ------------------------------------------------------------------
        public void WriteMGRADetailedPopData(MGRAS[] mgra)
        {
            int i, j, k;
            FileStream fout;		//file stream class
            string str = "";
            // --------------------------------------------------------------------------

            WriteToStatusBox("Writing MGRA Detailed Pop data");
            // open output file
            try
            {
                fout = new FileStream(networkPath + "census_mgra_pop_temp", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }

            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);
            foutw.AutoFlush = true;
            int counter = 0;
            for (i = 0; i < NUM_MGRAS; ++i)
            {
                for (k = 0; k < 8; ++k)
                {

                    // build totals
                    for (j = 0; j < 20; ++j)
                    {
                        mgra[i].data.raget[k, j] = mgra[i].data.ragem[k, j] + mgra[i].data.ragef[k, j];
                        mgra[i].data.ragemt[k] += mgra[i].data.ragem[k, j];
                        mgra[i].data.rageft[k] += mgra[i].data.ragef[k, j];
                        mgra[i].data.ragett[k] += mgra[i].data.raget[k, j];
                    }  // end for j
                }  // end for k

                if (i % 5000 == 0)
                    WriteToStatusBox("  writing MGRA " + i);
                if (counter % 10000 == 0)
                    WriteToStatusBox("  writing " + counter + " records");
                //int tstop = 0;
                //if (i == 11996)
                //    tstop = 1;
                for (j = 0; j < 8; ++j)   // eth
                {
                    str = (i + 1).ToString() + "," + (j + 1).ToString() + "," + mgra[i].data.ragett[j] + ",";
                    for (k = 0; k < 20; ++k)
                        str += mgra[i].data.raget[j, k] + ",";
                    str += mgra[i].data.ragemt[j] + ",";
                    for (k = 0; k < 20; ++k)
                        str += mgra[i].data.ragem[j, k] + ",";
                    str += mgra[i].data.rageft[j] + ",";
                    for (k = 0; k < 19; ++k)
                        str += mgra[i].data.ragef[j, k] + ",";
                    str += mgra[i].data.ragef[j, 19];

                    try
                    {
                        foutw.WriteLine(str);
                        //foutw.Flush();
                    }
                    catch (IOException exc)      //exceptions here
                    {
                        MessageBox.Show(exc.Message + " File Write Error");
                        return;
                    }
                    ++counter;
                }   // end for j
            }   // end for i
            //foutw.Flush();
            WriteToStatusBox("Closing ASCII file");
            foutw.Close();
            WriteToStatusBox("ASCII file closed");
            fout.Close();
        } // end procedure WriteMGRADetailedPopData

        //********************************************************************************************

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

        
        #region Miscellaneous button handlers
       
        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
        //********************************************************************
        #endregion

        /* processParams() */

        
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/10/2012   tb   initial recoding for version 3.4 Census Allocation

        //   ------------------------------------------------------------------

        public void processParams()
        {
            TN.census_detailed_pop_tab_ct = String.Format(appSettings["census_detailed_pop_tab_ct"].Value);
            TN.census_detailed_pop_control_ct = String.Format(appSettings["census_detailed_pop_control_ct"].Value);
            TN.census_gq_ctbg = String.Format(appSettings["census_gq_ctbg"].Value);
            TN.census_gq_cityctbg = String.Format(appSettings["census_gq_cityctbg"].Value);
            TN.census_gq_lckey = String.Format(appSettings["census_gq_lckey"].Value);
            TN.census_input = String.Format(appSettings["census_input"].Value);
            TN.census_hhwc_input = String.Format(appSettings["census_hhwc_input"].Value);
            TN.census_mgra = String.Format(appSettings["census_mgra"].Value);
            TN.census_mgra_pop = String.Format(appSettings["census_mgra_pop"].Value);
            TN.census_2010_supersplit = String.Format(appSettings["census_2010_supersplit"].Value);
            TN.census_2010_partialsupersplit = String.Format(appSettings["census_2010_partialsupersplit"].Value);
            TN.census_2010_mgra_revised_agegroups = String.Format(appSettings["census_2010_mgra_revised_agegroups"].Value);
            TN.ctblock_mgra_manyto1 = String.Format(appSettings["ctblock_mgra_manyto1"].Value);

        } // end procedure processParams()

        //************************************************************************************  
                       
    } // end public class Census

   #region Census class definitions


    public class CENSUSDATA
    {
        public int pop;
        public int[] aget = new int[23];
        public int[] agem = new int[23];
        public int[] agef = new int[23];
        // revised age groups - combining some of census to cateories to estiamtes categories
        public int[,] raget = new int[8, 20];
        public int[,] ragem = new int[8, 20];
        public int[,] ragef = new int[8, 20];
        public int[] ragett = new int[8];
        public int[] ragemt = new int[8];
        public int[] rageft = new int[8];
        public int popm;
        public int popf;
        public int[] nhisp = new int[8];
        public int[] hisp = new int[8];
        public int hhp;
        public int hhp_own;
        public int hhp_rent;
        public int[] hhsx = new int[7];
        public int gq;
        // revised gq categories - college, military, other
        public int[] gqx = new int[3];
        public int hh;
        public int hh_own;
        public int hh_rent;
        public int census_hs;
        public int sandag_hs;
        public double occ_rate;
        public double hhs;
        public int hhwc;
        public int hhwoc;
    }

    // end data class

    //****************************************************************************************

    public class CT10BLOCKS
    {
        public int ct10;
        public int block;
        public int ct10block;
        public int superID;
        public CENSUSDATA data;
        public int num_mgras;
        public int mgra_id;
        public Boolean solo;
    }  // end class

    public class SUPERSPLITS
    {
        public int ct10;
        public int block;
        public int ct10block;
        public int superID;
        public CENSUSDATA data;
        public int num_mgras;
        public int[] mgra_ids;

    }  // end class

    public class MGRAS
    {
        public int superID;     // supersplit or SG id
        public CENSUSDATA data;
        public Boolean was_used = false;
        // compressed mgra pop by 20 age groups (standard categories) these are used tin the detailed age, sex eth calcs

        public int[] caget = new int[20];
        public int[] cagem = new int[20];
        public int[] cagef = new int[20];
        public int cpopt;  // totals
        public int cpopm;
        public int cpopf;
        public int num_lckeys;
        public LCKEY_DATA[] lck;
    }  // end class

    public class LCKEY_DATA
    {
        public int id;
        public int mgra_id;
        public int lu;
        public int[] p = new int[4];    // lu priority
        public int gq;
        public int revised_gq;
        public double acres;
        public int[] gqx = new int[3];
    }  // end class

    public class CTBG
    {
        public int ct10bg;
        public int gq;
        public int[] gqx = new int[3];
        public int num_lckeys;
        public LCKEY_DATA[] lck;
    }  // end class

    public class CTPOP
    {
        public int ct10;
        public int num_mgras;
        public int[] mgra_ids;
        // revised age groups - we only need 20 groups - we combine some of census categories
        public int[,] raget = new int[9, 20];
        public int[,] ragem = new int[9, 20];
        public int[,] ragef = new int[9, 20];
        public int[] rpopt = new int[9];
        public int[] rpopm = new int[9];
        public int[] rpopf = new int[9];

    }  // end class


    #endregion
    

    //*********************************************************************************************
}
