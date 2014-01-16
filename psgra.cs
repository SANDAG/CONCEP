/* 
 * Source File: psgra.cs
 * Program: concep
 * Version 3.1
 * Programmer: tbe
 * Description:
 *		This is the POPEST SGRAS component of CONCEP
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
 *			popest_YYYY_sgra	: reduced data set POPEST SGRAs for year YYYY
 *			popest_L_sgra	    : previous year POPEST SGRA where L = YYYY-1
 *			gq_sgra			      : gq by sgra - this is a change as of 10/06/05 using totals rather than changes
 *                          to handle gq's by parcel aggregated to sgra
 *			hs_YYYY_sgra_from_landcore: hs  by sgra derived from landcore
 *      sandag_popest_controls_YYYY : adjusted dof city controls for year YYYY
 
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
 //   10/07/05   tb   changes for Version 3.1 using hs derived from landcore aggregated to sgra
 //   ------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;


namespace psgra
{
    // need this to use the writeToStatusBox on different thread
    delegate void WriteDelegate(string status);
    /// Summary description for psgra.
    /// </summary>
    public class psgra : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label label2;
        private System.Data.SqlClient.SqlCommand sqlCommand1;
        private System.Data.SqlClient.SqlConnection sqlCnnConcep;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.MenuItem menuItem4;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnValidate;
        private System.Windows.Forms.CheckBox chkOverrides;
        private System.Windows.Forms.Button btnRunPSGRA;
        private System.Windows.Forms.ComboBox txtYear;
        private IContainer components;
        private int fyear;
        private int lyear;
        private bool useOverrides;

        private TN TableNames = new TN();

        public psgra()
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
                }
            }
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
            this.btnRunPSGRA = new System.Windows.Forms.Button();
            this.sqlCommand1 = new System.Data.SqlClient.SqlCommand();
            this.sqlCnnConcep = new System.Data.SqlClient.SqlConnection();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.btnValidate = new System.Windows.Forms.Button();
            this.chkOverrides = new System.Windows.Forms.CheckBox();
            this.txtYear = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(32, 256);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 16);
            this.label3.TabIndex = 12;
            this.label3.Text = "Status";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(24, 168);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(448, 80);
            this.txtStatus.TabIndex = 11;
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(216, 104);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(96, 48);
            this.btnExit.TabIndex = 10;
            this.btnExit.Text = "Return";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(112, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(128, 24);
            this.label2.TabIndex = 9;
            this.label2.Text = "Estimates Year";
            // 
            // btnRunPSGRA
            // 
            this.btnRunPSGRA.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRunPSGRA.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunPSGRA.Location = new System.Drawing.Point(24, 104);
            this.btnRunPSGRA.Name = "btnRunPSGRA";
            this.btnRunPSGRA.Size = new System.Drawing.Size(96, 48);
            this.btnRunPSGRA.TabIndex = 15;
            this.btnRunPSGRA.Text = "Run ";
            this.btnRunPSGRA.UseVisualStyleBackColor = false;
            this.btnRunPSGRA.Click += new System.EventHandler(this.btnRunPSGRA_Click);
            // 
            // sqlCnnConcep
            // 
            this.sqlCnnConcep.ConnectionString = "Data Source=HILO;Initial Catalog=concep_sr12;User ID=forecast;Password=forecast";
            this.sqlCnnConcep.FireInfoMessageEventOnUserErrors = false;
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem3});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem2});
            this.menuItem1.Text = "File";
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 0;
            this.menuItem2.Text = "Exit";
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 1;
            this.menuItem3.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem4});
            this.menuItem3.Text = "Help";
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 0;
            this.menuItem4.Text = "About POPEST SGRAS";
            this.menuItem4.Click += new System.EventHandler(this.menuItem4_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(24, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(248, 40);
            this.panel1.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Blue;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(270, 40);
            this.label1.TabIndex = 0;
            this.label1.Text = "POPEST MGRA12s";
            // 
            // btnValidate
            // 
            this.btnValidate.BackColor = System.Drawing.Color.Yellow;
            this.btnValidate.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnValidate.Location = new System.Drawing.Point(120, 104);
            this.btnValidate.Name = "btnValidate";
            this.btnValidate.Size = new System.Drawing.Size(96, 48);
            this.btnValidate.TabIndex = 17;
            this.btnValidate.Text = "Validate Results";
            this.btnValidate.UseVisualStyleBackColor = false;
            this.btnValidate.Click += new System.EventHandler(this.btnValidate_Click);
            // 
            // chkOverrides
            // 
            this.chkOverrides.Checked = true;
            this.chkOverrides.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOverrides.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkOverrides.Location = new System.Drawing.Point(256, 64);
            this.chkOverrides.Name = "chkOverrides";
            this.chkOverrides.Size = new System.Drawing.Size(216, 24);
            this.chkOverrides.TabIndex = 18;
            this.chkOverrides.Text = "Use Vac and HHS  Overrides";
            // 
            // txtYear
            // 
            this.txtYear.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtYear.Items.AddRange(new object[] {
            "2001",
            "2002",
            "2003",
            "2004",
            "2005",
            "2006",
            "2007",
            "2008",
            "2009"});
            this.txtYear.Location = new System.Drawing.Point(24, 64);
            this.txtYear.Name = "txtYear";
            this.txtYear.Size = new System.Drawing.Size(72, 27);
            this.txtYear.TabIndex = 19;
            // 
            // psgra
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(488, 289);
            this.Controls.Add(this.txtYear);
            this.Controls.Add(this.chkOverrides);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.btnValidate);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnRunPSGRA);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.label2);
            this.Menu = this.mainMenu1;
            this.Name = "psgra";
            this.Text = "POPEST MGRA12s";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region PSGRA Run button processing

        /*  btnRunPSGRA_Click() */
        /// <summary>
        /// method invoker for run button - starts another thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   04/08/04   tb   added new thread code

        //   ------------------------------------------------------------------
        private void btnRunPSGRA_Click(object sender, System.EventArgs e)
        {
            //build the table names from runtime args
            ProcessParms(txtYear.SelectedItem.ToString(), TableNames, ref fyear, ref lyear);
            MethodInvoker mi = new MethodInvoker(beginPSGRAWork);
            mi.BeginInvoke(null, null);
        }

        /*  beginPSGRAwork() */
        /// <summary>
        /// POPEST SGRA Main
        /// </summary>

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding
        //   04/08/04   tb   added code for separate threads and try/catch for all sql statements
        //   07/01/04   tb   additional checks for inconsistencies in change data
        //   ------------------------------------------------------------------
        private void beginPSGRAWork()
        {
            int i, j;
            CityData[] city = new CityData[GD.MAX_CITIES];
            MASTER[] md = new MASTER[GD.MAX_CITIES];
            try
            {
                sqlCommand1 = new System.Data.SqlClient.SqlCommand();
                sqlCommand1.CommandTimeout = 180;
                sqlCommand1.Connection = sqlCnnConcep;
                for (i = 0; i < GD.MAX_CITIES; ++i)
                {
                    city[i] = new CityData();
                    md[i] = new MASTER();
                    for (j = 0; j < GD.MAX_MGRAS; ++j)
                    {
                        city[i].mu[j] = new address_data();
                        md[i].d[j] = new PSGRA();
                    }
                }
                //fill the city arrays
                FillCityArrays(city, TableNames);
                ExecuteEstimatesGeoUpdates(TableNames, fyear, lyear);
                DoHS(fyear, TableNames, city);
                ControlSGRAAll(TableNames, city, md);
                BulkLoadPOPEST(TableNames);
                writeToStatusBox("COMPLETED POPEST TO SGRA RUN");

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                return;
            }
        }
        #endregion


        #region PSGRA processing

        // procedures
        //    ControlSGRAAll() - Control hs, hh, gq_civ and hhp
        //    DoHS() - Process hs_chg_sgra data and control to city changes 

        /*  ControlSGRAAll() */
        /// <summary>
        /// Control hs, hh, gq_civ and hhp
        /// </summary>
        /// <param name="tn"><value>TableNames</value></param>
        /// <param name="s"><value>city array structure</value></param>
        /// <param name="md"><value>SGRA structure</value></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/10/02   tb   initial coding

        //   ------------------------------------------------------------------
        public void ControlSGRAAll(TN tn, CityData[] s, MASTER[] md)
        {
            int i, j, k, target, ii, kk, real_index;
            int status, mgra1, city;
            FileStream fout;		//file stream class

            System.Data.SqlClient.SqlDataReader rdr;
            int[] mgra_list = new int[GD.NUM_MGRAS];
            int[] limit = new int[GD.MAX_MGRAS];     //lower or upper bound used to restrict +/- controlling
            int[] passer = new int[GD.MAX_MGRAS];	  //temp array with actual sorted data tobe controlled
            int[] t_index = new int[GD.MAX_MGRAS];	  //array storing real index of data after sorting
            int[] mdata = new int[20];			  //bound array to query result
            double[] mdataf = new double[6];
            //-------------------------------------------------------------------------
            // open output file
            try
            {
                fout = new FileStream("\\\\sandag.org\\home\\shared\\res\\estimates & forecast\\concep\\psgra_out", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);

            /* cycle through each city, sum mgras and control to totals */
            writeToStatusBox("CONTROLLING HS, HH AND GQ DATA");

            this.sqlCommand1.CommandText = "select p.mgra, p.cityct, p.city, p.ct, " +
                "p.hs, p.hs_sf, p.hs_sfmu, p.hs_mf, p.hs_mh, p.gq, p.gq_civ, p.gq_mil, " +
                "p.hhp, p.hh, p.hh_sf, p.hh_sfmu, p.hh_mf, p.hh_mh, po.hs, " +
                "p.vac_sf, p.vac_sfmu, p.vac_mf, p.vac_mh, p.vac, p.hhs from " +
                tn.pm_table + " p, " + tn.pm_table + " po where p.popest_year = " + fyear +
                " AND po.popest_year = " + lyear + " AND p.mgra = po.mgra";

            try
            {
                sqlCnnConcep.Open();     //open the connection
                rdr = this.sqlCommand1.ExecuteReader();     //open the data reader

                //order on mdata array 0:mgra; 1:cityct; 2:city; 3:ct; 4:hs; 5:sf; 6:sfmu;
                  // 7:mf; 8:mh; 9:gq;
                //  10:gq_civ; 11:gq_mil; 12:hhp; 13:hh; 14:hh_sf; 15:hh_sfmu; 16:hh_mf; 17:hh_mh; 
                //   18:old hs (previous year); mdataf 0:vac_sf; 1:vac_mf; 2:vac_mh;3:vac; 4:hhs
                while (rdr.Read())
                {
                    mdata[0] = rdr.GetInt32(0);
                    mdata[1] = rdr.GetInt32(1);
                    mdata[2] = rdr.GetByte(2);

                    for (i = 3; i < 19; ++i)
                        mdata[i] = rdr.GetInt32(i);
                    for (i = 0; i < mdataf.Length; i++)
                        mdataf[i] = rdr.GetDouble(i + 19);
                    mgra1 = mdata[0] - 1;
                    city = mdata[2];   // assign the city id here
                    if (city < GD.MAX_CITIES)
                    {
                        j = md[city].counter;
                        md[city].d[j].mgra = mdata[0];
                        md[city].d[j].cityct = mdata[1];
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
                        md[city].d[j].hhp = mdata[12];
                        md[city].d[j].hh = mdata[13];
                        md[city].d[j].hh_sf = mdata[14];
                        md[city].d[j].hh_sfmu = mdata[15];
                        md[city].d[j].hh_mf = mdata[16];
                        md[city].d[j].hh_mh = mdata[17];
                        md[city].d[j].old_hs = mdata[18];
                        md[city].d[j].occ_sf = 1 - mdataf[0];
                        md[city].d[j].occ_sfmu = 1 - mdataf[1];
                        md[city].d[j].occ_mf = 1 - mdataf[2];
                        md[city].d[j].occ_mh = 1 - mdataf[3];
                        md[city].d[j].occ = 1 - mdataf[4];
                        md[city].d[j].hhs = mdataf[5];

                        // compute the hhs ; load the regional value if beyond bounds
                        if (md[city].d[j].hh > 0)
                            md[city].d[j].hhs = (double)md[city].d[j].hhp / (double)md[city].d[j].hh;
                        if (md[city].d[j].hhs < 1 || md[city].d[j].hhs > 7.0)
                            md[city].d[j].hhs = s[city].hhs;
                        // compute the structure occupancy rates and control
                        // load regional values if beyond bounds
                        md[city].counter++;
                    }
                }
                rdr.Close();
                sqlCnnConcep.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }

            for (ii = 1; ii < GD.MAX_CITIES; ++ii)
            {
                writeToStatusBox("Controlling city # " + ii.ToString());

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
                }
                //sort the data in ascending order - this
                AscendingSort(t_index, passer, limit, j);

                //call the controlling
                status = Roundit(passer, limit, target, j, 1);
                if (status > 0)
                {
                    MessageBox.Show("hs_sf roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());
                }

                /* restore the rounded data to the actual data arrays*/
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hs_sf = passer[kk];
                }

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
                }
                //sort the data in ascending order - this
                AscendingSort(t_index, passer, limit, j);

                //call the controlling
                status = Roundit(passer, limit, target, j, 1);
                if (status > 0)
                {
                    MessageBox.Show("hs_sfmu roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());
                }

                /* restore the rounded data to the actual data arrays*/
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hs_sfmu = passer[kk];
                }

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
                AscendingSort(t_index, passer, limit, j);

                //call the controlling
                status = Roundit(passer, limit, target, j, 1);
                if (status > 0)
                    MessageBox.Show("hs_mf roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());

                /* restore the rounded data to the actual data arrays*/
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hs_mf = passer[kk];
                }

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
                AscendingSort(t_index, passer, limit, j);

                //call the controlling
                status = Roundit(passer, limit, target, j, 1);
                if (status > 0)
                    MessageBox.Show("hs_mh roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());

                /* restore the rounded data to the actual data arrays*/
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hs_mh = passer[kk];
                    md[ii].d[real_index].hs = md[ii].d[real_index].hs_sf +
                        md[ii].d[real_index].hs_mf + md[ii].d[real_index].hs_mh;
                }     /* end for kk */

                //GQ_civ
                j = md[ii].counter;
                target = s[ii].gq_civ;
                Array.Clear(limit, 0, limit.Length);
                Array.Clear(passer, 0, passer.Length);
                Array.Clear(t_index, 0, t_index.Length);

                //write the data to temporary arrays for sorting before controlling
                for (kk = 0; kk < j; ++kk)
                {
                    passer[kk] = md[ii].d[kk].gq_civ;
                    t_index[kk] = kk;
                }
                //sort data in ascending order
                AscendingSort(t_index, passer, limit, j);

                //call controlling routine
                status = Roundit(passer, limit, target, j, 1);
                if (status > 0)
                    MessageBox.Show("gq_civ roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());

                //restore controlled data to actual data arrays
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].gq_civ = passer[kk];
                }

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
                AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = Roundit(passer, limit, target, j, 2);
                if (status > 0)
                    MessageBox.Show("hh_sf roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());

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
                }

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
                }

                //sort in ascending order
                AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = Roundit(passer, limit, target, j, 2);
                if (status > 0)
                {
                    MessageBox.Show("hh_sfmu roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());
                }

                /* restore the rounded data */
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hh_sfmu = passer[kk];
                }




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
                AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = Roundit(passer, limit, target, j, 2);
                if (status > 0)
                    MessageBox.Show("hh roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());

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
                AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = Roundit(passer, limit, target, j, 2);
                if (status > 0)
                    MessageBox.Show("hh roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());

                /* restore the rounded data */
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hh_mh = passer[kk];
                    md[ii].d[real_index].hh = md[ii].d[real_index].hh_sf +
                        md[ii].d[real_index].hh_sfmu + md[ii].d[real_index].hh_mf +
                        md[ii].d[real_index].hh_mh;
                }

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
                }

                //sort in ascending order
                AscendingSort(t_index, passer, limit, j);

                //call controlling
                status = Roundit(passer, limit, target, j, 3);
                if (status > 0)
                    MessageBox.Show("hhp roundit city # " + ii.ToString() + " diff = " +
                        status.ToString());

                /* restore the rounded data */
                for (kk = 0; kk < j; ++kk)
                {
                    real_index = t_index[kk];
                    md[ii].d[real_index].hhp = passer[kk];
                }
                //write this sgra to ascii for bulk-loading
                WriteSGRAData(foutw, md, ii, j);
            }
            foutw.Close();     //close the output ascii file
        }


        /// <summary>
        /// Process hs data 
        /// </summary>
        /// <param name="fyear"><value>Forecast year</value></param>
        /// <param name="tn"><value>TableNames</value></param>
        /// <param name="s"><value>Split-tract array structure</value></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding
        //   10/07/05   tb   changes for Version 3.1 using landcore HS totals
        //   ------------------------------------------------------------------

        public void DoHS(int fyear, TN tn, CityData[] s)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int city;
            int cnt;
            int i, j;
            int sf, sfmu, mf, mh, hs;
            int mgra;

            /* sgra HS from hs table */
            writeToStatusBox("EXTRACTING HS DATA");

            this.sqlCommand1.CommandText = "select t.mgra,x.city,sf,sfmu,mf,mh from " + tn.hs_table +
                " t, xref_mgra_sr12 x where t.ludu_year = " + fyear + " AND t.mgra = x.mgra";
            try
            {
                sqlCnnConcep.Open();
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
                    }
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
                }
                rdr.Close();
                sqlCnnConcep.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }

            try
            {
                sqlCnnConcep.Open();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }   // end catch

            for (i = 1; i < GD.MAX_CITIES; ++i)
            {
                writeToStatusBox("Updating City # " + i.ToString());

                if (s[i].abschg_hs != 0)
                {
                    for (j = 0; j < s[i].num_mgras; ++j)
                    {
                        //build a sql query and update for this sgra
                        this.sqlCommand1.CommandText = "UPDATE " + tn.pm_table + " SET hs = "
                            + s[i].mu[j].hs
                            + ",hs_sf = " + s[i].mu[j].sf
                            + ",hs_sfmu = " + s[i].mu[j].sfmu
                            + ",hs_mf = " + s[i].mu[j].mf
                            + ",hs_mh = " + s[i].mu[j].mh
                            + " WHERE popest_year = " + fyear + " AND mgra = " + s[i].mu[j].mgra;
                        try
                        {
                            sqlCommand1.ExecuteNonQuery();
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                            this.Close();
                        }
                    }
                }
            }
            sqlCnnConcep.Close();
        }
        #endregion

        #region Miscellaneous utilities

        // procedures
        //   Ascending Sort() - Sort a small list in ascending order
        //   ProcessParms() - Build the table names from runtime parms
        //   VerifyInputs() - verify hs, gq input consistency
        //   Roundit() - +/- 1 rounding - type determines whether or not to use the limit array to constrain +/- rounding
        //   WritesgraData() - Write the controlled data to ASCII for bulk loading 
        //   writeToStatusBox - display status text

        /*  AscendingSort() */
        /// <summary>
        /// Sort a small list in ascending order
        /// </summary>
        /// <param name="v"><value>Array with original index</value></param>
        /// <param name="p"><value>Array with data</value></param>
        /// <param name="l"><value>Array with limits (constraints, if applicable></value></param>
        /// <param name="n"><value>Number of elements</value></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------

        public void AscendingSort(int[] v, int[] p, int[] l, int n)
        {
            int i, j, temp0, temp1, temp2;
            for (i = 0; i < n; ++i)
            {
                temp1 = p[i];
                temp0 = v[i];
                temp2 = l[i];
                for (j = i - 1; j >= 0 && p[j] > temp1; j--)
                {
                    p[j + 1] = p[j];
                    v[j + 1] = v[j];
                    l[j + 1] = l[j];
                }
                p[j + 1] = temp1;
                v[j + 1] = temp0;
                l[j + 1] = temp2;
            }
        }

        /* ProcessParms() */
        /// <summary>
        /// Build the table names from runtime parms
        /// </summary>
        /// <param name="year"><value>Selected year from GUI</value></param>
        /// <param name="TableNames"><value>TableNames</value></param>
        /// <param name="fyear"><value>Pointer to Forecast year (return value)</value></param>
        /// <param name="lyear"><value>Pointer to Previous year (return value)</value></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/02/04   tb   initial recoding - moved verification steps to separate routine

        //   ------------------------------------------------------------------

        public void ProcessParms(string year, TN TableNames, ref int fyear, ref int lyear)
        {
            useOverrides = chkOverrides.Checked;
            fyear = int.Parse(year);
            lyear = fyear - 1;
            TableNames.pm_table = "popest_mgra";
            TableNames.control_table = "popest_controls";
            TableNames.hs_table = "hs_from_landcore";
        }

        //************************************************************************************

        /* Roundit() */
        /// <summary>
        /// +/- 1 rounding - type determines whether or not to use the limit array to constrain +/- rounding
        /// hh lte hs, hhp gte hh and so on
        /// </summary>
        /// <param name="local"><value>Array to be controlled</value></param>
        /// <param name="limit"><value>Constraints, if applicable</value></param>
        /// <param name="target"><value>Control value</value></param>
        /// <param name="counter"><value>Number of items</value></param>
        /// <param name="type"><value>Determines use of constraints</value></param>
        /// <returns> success or failure as difference</returns>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/10/02   tb   initial coding

        //   ------------------------------------------------------------------
        public int Roundit(int[] local, int[] limit, int target, int counter, byte type)
        {
            int i, max_set, max_id;
            int summer, iter_count;
            int diff;
            double factor;

            //-------------------------------------------------------------------------
            summer = 0;

            /* sum local data elements */
            for (i = 0; i < counter; ++i)
                summer += local[i];

            if (summer > 0)
                factor = (double)target / (double)summer;
            else
                factor = 1.0;

            summer = 0;     /* reset sum */
            max_set = -1;
            max_id = 0;
            /* apply factor */
            for (i = 0; i < counter; ++i)
            {
                if (type == 1)
                    local[i] = (int)((double)local[i] * factor + .5);
                else if (type == 2 && (double)local[i] * factor + .5 <= limit[i])
                    local[i] = (int)((double)local[i] * factor + .5);
                else if (type == 3 && (double)local[i] * factor + .5 >= limit[i])
                    local[i] = (int)((double)local[i] * factor + .5);

                summer += local[i];     /* recompute sum */
                /* find the largest member of this set to get any residual that can't get allocated */
                if (local[i] > max_set)
                {
                    max_set = local[i];
                    max_id = i;
                }
            }     /* end for */

            /* +\- rounding */
            iter_count = 0;
            diff = target - summer;

            while ((diff != 0) && iter_count < 1000)
            {
                ++iter_count;
                for (i = counter - 1; i >= 0; --i)
                {
                    if (diff > 0)
                    {
                        if ((type == 1 || type == 3) || (type == 2 && local[i] < limit[i]))
                        {
                            local[i] += 1;
                            diff -= 1;
                        }     /* end if */
                    }
                    else if (local[i] > 0)
                    {
                        if (type != 3 || (type == 3 && local[i] > limit[i]))
                        {
                            local[i] -= 1;
                            diff += 1;
                        }     /* end if */
                    }     /* end else */

                    if (diff == 0)
                        break;

                }     /* end for */

            }     /* end while */

            if (diff > 0)     /* after 1000 iterations, store remainder in largest member */
            {
                if (type == 1 ||
                    (type == 2 && (local[max_id] + diff < limit[max_id])))
                    local[max_id] += diff;
                diff = 0;
            }     /* end if */

            return (diff);

        }     /* end roundit() */

        //******************************************************************************

        /*  WritesgraData() */
        /// <summary>
        /// Write the controlled data to ASCII for bulk loading
        /// </summary>
        /// <param name="foutw"><value>StreamWriter handle</value></param>
        /// <param name="md"><value>sgra array structure</value></param>
        /// <param name="ii"><value>Index in md structure</value></param>
        /// <param name="j"><value>Number of items</value></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/10/02   tb   initial coding

        //   ------------------------------------------------------------------
        public void WriteSGRAData(StreamWriter foutw, MASTER[] md, int ii, int j)
        {
            string str;
            for (int k = 0; k < j; k++)
            {
                md[ii].d[k].pop = md[ii].d[k].hhp + md[ii].d[k].gq;
                str = md[ii].d[k].mgra + ",";
                str += md[ii].d[k].cityct + ",";
                str += md[ii].d[k].city + ",";
                str += md[ii].d[k].ct + ",";
                str += md[ii].d[k].pop + ",";
                str += md[ii].d[k].hhp + ",";
                str += md[ii].d[k].gq + ",";
                str += md[ii].d[k].gq_civ + ",";
                str += md[ii].d[k].gq_mil + ",";
                str += md[ii].d[k].hs + ",";
                str += md[ii].d[k].hs_sf + ",";
                str += md[ii].d[k].hs_sfmu + ",";
                str += md[ii].d[k].hs_mf + ",";
                str += md[ii].d[k].hs_mh + ",";
                str += md[ii].d[k].hh + ",";
                str += md[ii].d[k].hh_sf + ",";
                str += md[ii].d[k].hh_sfmu + ",";
                str += md[ii].d[k].hh_mf + ",";
                str += md[ii].d[k].hh_mh + "\r\n";
                try
                {
                    foutw.Write(str);
                    foutw.Flush();
                }
                catch (IOException exc)      //exceptions here
                {
                    MessageBox.Show(exc.Message + " File Write Error");
                    return;
                }
            }
        }

        /// <summary>
        /// Display the current processing status to the form
        /// </summary>
        /// <param name="str"><value>Processing status</value></param>

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void writeToStatusBox(string status)
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
            }
            // Invoked from another thread.  Show progress asynchronously.
            else
            {
                WriteDelegate write = new WriteDelegate(writeToStatusBox);
                Invoke(write, new object[] { status });
            }
        }     //end writeToStatusBox

        //*****************************************************************************
        #endregion

        #region Psgra extraction utilities

        /* FillCityArrays() */
        /// <summary>
        /// Populate city popest arrays
        /// </summary>
        /// <param name="o"><value>Previous city array structure</value></param>
        /// <param name="split"><value>city array structure</value></param>
        /// <param name="tn">TableNames</param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------

        public void FillCityArrays(CityData[] s, TN tn)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            byte city;     //city keeps local index

            writeToStatusBox("Filling City Arrays");

            this.sqlCommand1.CommandText = "Select city,hs,hs_sf,hs_sfmu,hs_mf,hs_mh,hh,hh_sf,hh_sfmu,hh_mf,hh_mh,"
                + "hhp,gq,gq_civ,gq_mil,vac_sf,vac_sfmu,vac_mf,vac_mh,hhs from "
                + tn.control_table + " where popest_year = " + fyear + " order by city";
            try
            {
                sqlCnnConcep.Open();
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
                    s[city].occ_sf = 1 - rdr.GetDouble(15);
                    s[city].occ_sfmu = 1 - rdr.GetDouble(16);
                    s[city].occ_mf = 1 - rdr.GetDouble(17);
                    s[city].occ_mh = 1 - rdr.GetDouble(18);
                    s[city].hhs = rdr.GetDouble(19);
                }
                rdr.Close();
                sqlCnnConcep.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }
        }

        //*********************************************************************
        #endregion

        #region SQL command procedures

        // procedures
        //    BulkLoadPOPEST() - Bulk loads ASCII to POPEST SGRA
        //	  ExecuteSGRAUpdates() - Run SQL commands to populate new SGRA table and execute updates

        /*  BulkLoadPOPEST() */
        /// <summary>
        /// Bulk loads ASCII to POPEST SGRA
        /// </summary>
        /// <param name="tn"><value>TableNames</value></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------
        public void BulkLoadPOPEST(TN tn)
        {
            string fo;
            fo = "'\\\\sandag.org\\home\\shared\\res\\estimates & forecast\\concep\\psgra_out'";
            writeToStatusBox("TRUNCATING POPEST UPDATE TABLE");
            try
            {
                sqlCnnConcep.Open();
                sqlCommand1.CommandText = "truncate table popest_update";
                sqlCommand1.ExecuteNonQuery();	

                writeToStatusBox("BULK LOADING POPEST SGRA TABLE");
                sqlCommand1.CommandTimeout = 180;
                sqlCommand1.CommandText = "bulk insert popest_update from " + fo
                    + " with (fieldterminator = ',', firstrow = 1)";
                sqlCommand1.ExecuteNonQuery();

                writeToStatusBox("UPDATE POPEST SGRA FROM POPEST SGRA UPDATE TABLE");
                sqlCommand1.CommandText = "update " + tn.pm_table
                    + " set pop = p2.pop, hhp = p2.hhp, gq = p2.gq, gq_civ = p2.gq_civ,"
                    + "hs = p2.hs, hs_sf = p2.hs_sf, hs_sfmu = p2.hs_sfmu, hs_mf = p2.hs_mf, hs_mh = p2.hs_mh,"
                    + "hh = p2.hh, hh_sf = p2.hh_sf, hh_sfmu = p2.hh_sfmu, hh_mf = p2.hh_mf, hh_mh = p2.hh_mh from " + tn.pm_table + " p, "
                    + "popest_update p2 where p.popest_year = " + fyear + " and p.mgra = p2.mgra";
                sqlCommand1.ExecuteNonQuery();
                sqlCommand1.CommandText = "update " + tn.pm_table
                    + " set hs = hs_sf+hs_sfmu+hs_mf+hs_mh, gq = gq_civ+gq_mil, pop = gq_civ+gq_mil+hhp where popest_year = " + fyear;
                sqlCommand1.ExecuteNonQuery();

                sqlCommand1.CommandText = "update " + tn.pm_table +
                    " SET vac = 0,vac_sf = 0, vac_mf = 0, vac_mh = 0 " +
                    "WHERE popest_year = " + fyear;
                sqlCommand1.ExecuteNonQuery();

                //update the vacancy rates and check for legit values
                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac = round(1 - cast(hh as float)/cast(hs as float),3) " +
                    "WHERE popest_year = " + fyear + " AND hs > 0";
                sqlCommand1.ExecuteNonQuery();
                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_sf = round(1 - cast(hh_sf as float)/cast(hs_sf as float),3) " +
                    "WHERE popest_year = " + fyear + " AND hs_sf > 0";
                sqlCommand1.ExecuteNonQuery();

                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_sfmu = round(1 - cast(hh_sfmu as float)/cast(hs_sfmu as float),3) " +
                    "WHERE popest_year = " + fyear + " AND hs_sfmu > 0";
                sqlCommand1.ExecuteNonQuery();

                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_mf = round(1 - cast(hh_mf as float)/cast(hs_mf as float),3) " +
                    "WHERE popest_year = " + fyear + " AND hs_mf > 0";
                sqlCommand1.ExecuteNonQuery();
                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_mh = round(1 - cast(hh_mh as float)/cast(hs_mh as float),3) " +
                    "WHERE popest_year = " + fyear + " AND hs_mh > 0";
                sqlCommand1.ExecuteNonQuery();
                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_mh = round(1 - cast(hh_mh as float)/cast(hs_mh as float),3) " +
                    "WHERE popest_year = " + fyear + " AND hs_mh > 0";
                sqlCommand1.ExecuteNonQuery();
                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_sf = .05 WHERE popest_year = " + fyear + " AND vac_sf = 1";
                sqlCommand1.ExecuteNonQuery();

                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_sfmu = .05 WHERE popest_year = " + fyear + " AND vac_sfmu = 1";
                sqlCommand1.ExecuteNonQuery();

                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_mf = .05 WHERE popest_year = " + fyear + " AND vac_mf = 1";
                sqlCommand1.ExecuteNonQuery();
                sqlCommand1.CommandText = "UPDATE " + tn.pm_table
                    + " SET vac_mh = .05 WHERE popest_year = " + fyear + " AND vac_mh = 1";
                sqlCommand1.ExecuteNonQuery();
                sqlCnnConcep.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }
        }



        /*  ExecuteSGRAUpdates() */
        /// <summary>
        /// Run SQL commands to populate new SGRA table and execute updates
        /// </summary>
        /// <param name="tn"><value>TableNames</value></param>
        /// <param name="fyear"><value>Forecast year</value></param>
        /// <param name="lyear"><value>Previous year</value></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------

        public void ExecuteEstimatesGeoUpdates(TN tn, int fyear, int lyear)
        {
            writeToStatusBox("BUILDING INITIAL MGRA TABLE");
            try
            {
                sqlCommand1.Connection = sqlCnnConcep;
                sqlCnnConcep.Open();

                sqlCommand1.CommandText = "delete from  " + tn.pm_table +
                    " WHERE popest_year = " + fyear;
                sqlCommand1.ExecuteNonQuery();

                //copy the old mGRA to the new
                sqlCommand1.CommandText = "insert into " + tn.pm_table +
                    " select " + fyear + ", mgra, cityct, city, ct, pop, gq, gq_civ, gq_mil, " +
                    "hs, hs_sf, hs_sfmu, hs_mf, hs_mh, hh, hh_sf, hh_sfmu, hh_mf, " +
                    "hh_mh, hhp, vac, vac_sf, vac_sfmu, vac_mf, vac_mh, hhs from " + tn.pm_table +
                    " WHERE popest_year = " + lyear;
                sqlCommand1.ExecuteNonQuery();

                //update with gq_civ
                // truncate vals first
                sqlCommand1.CommandText = "update " + tn.pm_table +
                    " set gq_civ = 0, gq_mil = 0 WHERE popest_year = " + fyear;
                sqlCommand1.ExecuteNonQuery();

                sqlCommand1.CommandText = "WITH g as (select mgra, sum(civ) as civ, sum(mil) as mil " +
                    "FROM dbo.gq_by_lckey_controlled where popest_year = " + fyear + " group by mgra) " +
                    "UPDATE " + tn.pm_table + " SET gq_civ = g.civ, " +
                    " gq_mil = g.mil FROM " + tn.pm_table + " p, g WHERE p.mgra = g.mgra AND " +
                    "p.popest_year = " + fyear;
                sqlCommand1.ExecuteNonQuery();

                //update with gq total
                sqlCommand1.CommandText = "update " + tn.pm_table + " set gq = gq_mil + gq_civ where " +
                    "popest_year = " + fyear;
                sqlCommand1.ExecuteNonQuery();

                //update sgra vacancy rates if applicable
                if (useOverrides)
                {
                    sqlCommand1.CommandText = "update " + tn.pm_table +
                        " set vac_sf = o.vac_sf_override" +
                        " from " + tn.pm_table + " p, popest_vacancy_overrides_mgra o where o.vac_sf_override > 0 and " +
                        "p.mgra = o.mgra and p.popest_year = " + fyear;
                    sqlCommand1.ExecuteNonQuery();

                    sqlCommand1.CommandText = "update " + tn.pm_table + " set vac_mf = o.vac_mf_override" +
                      " from " + tn.pm_table + " p, popest_vacancy_overrides_mgra o where o.vac_mf_override > 0 and " +
                      "p.mgra = o.mgra and p.popest_year = " + fyear;
                    sqlCommand1.ExecuteNonQuery();

                    sqlCommand1.CommandText = "update " + tn.pm_table + " set hhs = o.hhs_override" +
                        " from " + tn.pm_table + " p, popest_hhs_overrides_mgra o where " +
                        "p.mgra = o.mgra and p.popest_year = " + fyear;
                    sqlCommand1.ExecuteNonQuery();
                }
                sqlCnnConcep.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }
        }

        //***********************************************************************************************
        #endregion
        #region Miscellaneous button handlers
        private void menuItem4_Click(object sender, System.EventArgs e)
        {
            aboutpsgra.AboutPSGRA about = new aboutpsgra.AboutPSGRA();
            about.ShowDialog();

        }
        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
        //********************************************************************
        #endregion



        #region Validation Button Handler

        /*  btnValidate_Click() */
        /// <summary>
        /// POPEST Split-Tracts Validation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   04/07/04   tb   initial coding

        //   ------------------------------------------------------------------
        private void btnValidate_Click(object sender, System.EventArgs e)
        {
            int fyear = 0, lyear = 0;
            int i, num_vtext = 0;
            string sql1 = "";
            TN TableNames = new TN();
            string[] vtext = new string[GD.MAX_POPEST_EXCEPTIONS];
            string[] description = new string[GD.MAX_POPEST_EXCEPTIONS];
            byte[] vcheck = new byte[GD.MAX_POPEST_EXCEPTIONS];
            System.Data.SqlClient.SqlDataReader rdr;

            //-----------------------------------------------------------------

            //build the table names

            this.sqlCommand1 = new System.Data.SqlClient.SqlCommand();
            this.sqlCommand1.Connection = sqlCnnConcep;
            ProcessParms(txtYear.SelectedItem.ToString(), TableNames, ref fyear, ref lyear);
            // load the popest exceptions string array
            this.sqlCommand1.CommandText = "Select code,vcheck,rtrim(vtext),rtrim(description) from popest_validation_lookup";

            try
            {
                sqlCnnConcep.Open();
                rdr = this.sqlCommand1.ExecuteReader();
                num_vtext = 0;
                while (rdr.Read())
                {
                    i = rdr.GetByte(0);
                    if (i < GD.MAX_POPEST_EXCEPTIONS)
                        vtext[i] = rdr.GetString(2);
                    vcheck[i] = rdr.GetByte(1);
                    description[i] = rdr.GetString(3);
                    ++num_vtext;
                }   // end while
                rdr.Close();
                sqlCnnConcep.Close();

            }   // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }   // end catch

            // truncate the exceptions table for this year
            this.sqlCommand1.CommandText = "delete from popest_sgra_validation_exceptions where vyear = " + fyear.ToString();

            try
            {
                sqlCnnConcep.Open();
                this.sqlCommand1.ExecuteNonQuery();
                sqlCnnConcep.Close();
            }   // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }   // end catch

            // some of the checks are performed on just the estimates table, others need to compute changes
            // these are the single table checks
            for (i = 1; i < num_vtext; ++i)
            {
                sql1 = "insert into popest_sgra_validation_exceptions select ";
                if (vcheck[i] == 0)
                    continue;

                sql1 += fyear.ToString() + ",sgra," + i.ToString() + ",'" +
                    description[i] + "' from " + TableNames.pm_table + " where ";

                sql1 += vtext[i];

                this.sqlCommand1.CommandText = sql1;
                try
                {
                    sqlCnnConcep.Open();
                    this.sqlCommand1.ExecuteNonQuery();
                    sqlCnnConcep.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                    this.Close();
                }
            }   // end for i

            // now do those check that require a process - for popest sgras there is one class
            // compare the % change in pop and hs between two years and report any that
            // are out of range

            // do the % change in pop
            this.sqlCommand1.CommandText = "insert into popest_sgra_validation_exceptions select " + fyear.ToString() + "," +
                "p1.sgra,17,'" + description[17] + "' from " + TableNames.pm_table + " p1," +
                TableNames.pm_table + " p0 where p0.sgra = p1.sgra and " +
                "abs((cast (p1.pop as float) - cast(p0.pop as float)) / cast(p0.pop as float) * 100 ) > " + GD.V_POP_RATE.ToString() + " and p0.pop > 500";
            try
            {
                sqlCnnConcep.Open();
                this.sqlCommand1.ExecuteNonQuery();
                sqlCnnConcep.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }   // end catch

            // now do the % change in hs
            this.sqlCommand1.CommandText = "insert into popest_sgra_validation_exceptions select " + fyear.ToString() + "," +
                "p1.sgra,18,'" + description[18] + "'from " + TableNames.pm_table + " p1," +
                TableNames.pm_table + " p0 where p0.sgra = p1.sgra and " +
                "abs((cast (p1.hs as float) - cast(p0.hs as float)) / cast(p0.hs as float) * 100 ) > " + GD.V_HS_RATE.ToString() + " and p0.hs > 100";
            try
            {
                sqlCnnConcep.Open();
                this.sqlCommand1.ExecuteNonQuery();
                sqlCnnConcep.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                this.Close();
            }   // end catch

            MessageBox.Show("COMPLETED POPEST sgra VALIDATION " + fyear.ToString());
            writeToStatusBox("COMPLETED POPEST sgra VALIDATION " + fyear.ToString());

        }
        #endregion
    }
    #region PSGRA class definitions

    public class GD //global data
    {
        public static int MAX_MGRAS = 10000;  // max number of SGRAs in any city
        public static int MAX_CITIES = 20;
        public static int NUM_MGRAS = 22633;
        public static int MAX_POPEST_EXCEPTIONS = 25;
        public static int V_HS_RATE = 25;
        public static int V_POP_RATE = 25;
    }
    /// <summary>
    /// class TN - table name class
    /// </summary>
    //   Revision History
    //   Date       By   Description
    //   ------------------------------------------------------------------
    //   06/05/02   tb   initial coding

    //   ------------------------------------------------------------------
    public class TN
    {
        public string pm_table;      //mgra table name
        public string control_table;     //dof controls for split tracts
        public string hs_table;     // housing stock table
    }

    public class address_data
    {
        public int mgra;
        public int sf;
        public int sfmu;
        public int mf;
        public int mh;
        public int hs;
    }

    /// <summary>
    /// Class CityData - main popest data class
    /// </summary>
    //   Revision History
    //   Date       By   Description
    //   ------------------------------------------------------------------
    //   06/05/02   tb   initial coding

    //   ------------------------------------------------------------------

    public class CityData
    {
        public int city;
        public int gq;
        public int gq_civ;
        public int gq_mil;
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
        public address_data[] mu = new address_data[GD.MAX_MGRAS];
        public int num_mgras;
        public int unit_flag;
    }

    /// <summary>
    /// Class PMGRA - main popest data class
    /// </summary>
    //   Revision History
    //   Date       By   Description
    //   ------------------------------------------------------------------
    //   06/05/02   tb   initial coding

    //   ------------------------------------------------------------------

    public class PSGRA
    {
        public int mgra;
        public int city;
        public int ct;
        public int cityct;
        public int gq;
        public int gq_civ;
        public int gq_mil;
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

    }
    /// <summary>
    /// public class MASTER
    /// PMGRA and counter for each mgra in split
    /// </summary>
    //   Revision History
    //   Date       By   Description
    //   ------------------------------------------------------------------
    //   06/05/02   tb   initial coding

    //   ------------------------------------------------------------------

    public class MASTER
    {
        public int counter;
        public PSGRA[] d = new PSGRA[GD.MAX_MGRAS];
    }
    #endregion
}
