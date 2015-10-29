
/* Filename:    ControlVitals.cs
 * Program:     CONCEP
 * Version:    4.0
 * Programmers: Terry Beckhelm
 * Description:     version 4 adds standardized configuration file for global vars, table names and query content
 *                  Version 3.5 is a recode for Series 13 geographies
 * 
 *                  Version 2.5 is a recode to do sgras instead of mgras
 * 
 *					Version 2.0 - this is a major change in controlling - using the distribution
 *                  algorithm to distribute zip code data to mgras and then aggregate to CT
 *                  requires: - zip data by gender ethnicity 
 *                  not immediately changing how deaths work.
 * 
 *                  Version 1.1 features: 
 *                  some reformatting - looks like we're not going to significantly change the 
 *                  controlling 
 *              This application performs the controlling of vital rates for 
 *              characteristics estimates as of 07/10/03.  Deaths processing 
 *              is changed (use survival rates and base population to generate
 *              the deaths table).
 * Methods:     
 *              Main()
 *              runBtnProcessing_Click()
 * 
 * Database:    SQL Server Database concep
 *              Tables: 
 *                 births_ct - births for year YYYY; census tracts where YYYY = 2001, 2002 and so on                                    
 *                 births_zip - births for year YYYY by zip code, gender and ethnicity
 *                 deaths_ct - deaths for year YYYY; census tracts
 *                 controls_births_deaths - control totals for births and deaths by year
 *                 survival_rates - survival rates by eth,sex and age 
 *			       survival_rates_roc - rates of change for survival rates 2000 - 2020 by eth, sex and age
 *                 detailed_pop_ct - ct pop by eth, sex and age for year YYYY
 *                      
 * Revision History
 * Date       By    Description
 * -----------------------------------------------------------
 * 05/12/03   tb   Initial coding
 * 07/10/03   tb   Changed deaths processing
 * 05/19/04   tb   changes for version 1.1 
 * 06/21/04   tb   changes for version 2.0 - distributing births by zip to mgras 
 * 07/13/05   tb   changes for version 2.5 - distributing births to sgra instead of mgra    
 * 07/09/12   tb   changes for version 3.5 - series 13 geographies
 * 10/23/12   tb   changes for version 4
 * --------------------------------------------------------------------------
 */

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Configuration;

namespace CV
{
    // need this to use the WriteToStatusBox on different thread
     delegate void WriteDelegate(string status);

    public class ControlVitals : System.Windows.Forms.Form
    {
       
        public Configuration config;
        public KeyValueConfigurationCollection appSettings;
        public ConnectionStringSettingsCollection connectionStrings;
        public System.Data.SqlClient.SqlConnection sqlConnection;
        public System.Data.SqlClient.SqlCommand sqlCommand;

        public class zip_data
        {
            public int zip_id;
            public int mgra_count;
            public int[] mgras;
            public int[] males;
            public int[] females;
            public int[] fpop;
        } // end class

        public class TableNames
        {
            public string basePopCT;
            public string basePopMGRA;
            public string birthsCT;
            public string birthsMGRA;
            public string birthsZIP;
            public string deathsCT;
            public string survivalRates;
            public string survivalRatesROC;
            public string vitalsControl;
            public string xref;
        } // end tablenames

        public const int BASEYEAR = 2010;
        public int NUM_ETH;
        public int NUM_SEX;
        public int NUM_AGE;
        public int NUM_CTS;
        public int MAX_MGRAS_IN_ZIP;
        public int NUM_MGRAS;
        public int NUM_ZIP;
        public TableNames TN = new TableNames();
        private zip_data[] zip;

        private int[, ,] baseDataByRegion; /* Base data from detailed_pop table classified by the overall region. */
        private int[, , ,] baseDataByCT;    /* Base data from detailed_pop_table classified by eth, sex, age, and CT. */
        private int[,] basePopBymgra;
        private int[, ,] regionalDeaths;   /* Array of deaths by eth, sex, age on the regional scale.  These records get assigned to CTs by Pachinko probabilistic method. */
        private int[, , ,] deathsByCT;      /* Array of deaths that will be the final count of deaths by ethnicity, sex, age and CT.  This array will be loaded into the SQL deaths table at the end. */
        private int[, ,] birthsByCT;
        private int[,] mgra_males;
        private int[,] mgra_females;
        private bool estimateBirths;
        private bool estimateDeaths;
        private int[,] birthsControl;
        private int[,] deathsControl;
        private int[] ctList;
        private int estimatesYear;
        private int lagYear;

        public string networkPath;
        private StreamWriter sw;            /* Stream to write final deaths by ethnicity, sex, age, and CT to. */
        
        private string birthsCTFile;          // Name of ASCII births file
        private string deathsCTFile;          // Name of ASCII deaths file
        private string birthsMGRAFile;        // Name of MGRA births file
        

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnRunControlVitals;
        private System.Windows.Forms.Label label4;
        //private System.Data.SqlClient.SqlCommand sqlCommand;
        //private System.Data.SqlClient.SqlConnection sqlConnection;
        private System.Windows.Forms.ComboBox cboYear;
        private System.Windows.Forms.CheckBox doBirths;
        private System.Windows.Forms.CheckBox doDeaths;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.TextBox txtDebugZIP;
        private System.Windows.Forms.TextBox txtDebugETH;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox optDebug;
        private System.Windows.Forms.Label label6;
      
        //private IContainer components;

        public ControlVitals()
        {
            InitializeComponent();
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
       

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ControlVitals));
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnExit = new System.Windows.Forms.Button();
            this.doDeaths = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.doBirths = new System.Windows.Forms.CheckBox();
            this.cboYear = new System.Windows.Forms.ComboBox();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.optDebug = new System.Windows.Forms.CheckBox();
            this.txtDebugZIP = new System.Windows.Forms.TextBox();
            this.txtDebugETH = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.btnRunControlVitals = new System.Windows.Forms.Button();
            this.sqlConnection = new System.Data.SqlClient.SqlConnection();
            this.sqlCommand = new System.Data.SqlClient.SqlCommand();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(76, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(174, 24);
            this.label2.TabIndex = 26;
            this.label2.Text = "Estimates  Year";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Book Antiqua", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label1.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.label1.Location = new System.Drawing.Point(16, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(206, 32);
            this.label1.TabIndex = 32;
            this.label1.Text = "Control Vitals";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Location = new System.Drawing.Point(8, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(256, 48);
            this.panel1.TabIndex = 33;
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(112, 192);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(96, 48);
            this.btnExit.TabIndex = 27;
            this.btnExit.Text = "Return";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // doDeaths
            // 
            this.doDeaths.Font = new System.Drawing.Font("Book Antiqua", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.doDeaths.Location = new System.Drawing.Point(80, 144);
            this.doDeaths.Name = "doDeaths";
            this.doDeaths.Size = new System.Drawing.Size(64, 24);
            this.doDeaths.TabIndex = 37;
            this.doDeaths.Text = "Deaths";
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(8, 120);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 16);
            this.label4.TabIndex = 0;
            this.label4.Text = "Controls";
            // 
            // doBirths
            // 
            this.doBirths.Font = new System.Drawing.Font("Book Antiqua", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.doBirths.Location = new System.Drawing.Point(16, 144);
            this.doBirths.Name = "doBirths";
            this.doBirths.Size = new System.Drawing.Size(56, 24);
            this.doBirths.TabIndex = 36;
            this.doBirths.Text = "Births";
            // 
            // cboYear
            // 
            this.cboYear.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboYear.Items.AddRange(new object[] {
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
            this.cboYear.Location = new System.Drawing.Point(8, 88);
            this.cboYear.Name = "cboYear";
            this.cboYear.Size = new System.Drawing.Size(64, 31);
            this.cboYear.TabIndex = 35;
            // 
            // txtStatus
            // 
            this.txtStatus.BackColor = System.Drawing.Color.White;
            this.txtStatus.Location = new System.Drawing.Point(16, 248);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ReadOnly = true;
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(368, 80);
            this.txtStatus.TabIndex = 36;
            // 
            // optDebug
            // 
            this.optDebug.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.optDebug.Location = new System.Drawing.Point(176, 128);
            this.optDebug.Name = "optDebug";
            this.optDebug.Size = new System.Drawing.Size(96, 28);
            this.optDebug.TabIndex = 38;
            this.optDebug.Text = "Debug Stop";
            // 
            // txtDebugZIP
            // 
            this.txtDebugZIP.Location = new System.Drawing.Point(38, 37);
            this.txtDebugZIP.Name = "txtDebugZIP";
            this.txtDebugZIP.Size = new System.Drawing.Size(56, 20);
            this.txtDebugZIP.TabIndex = 39;
            // 
            // txtDebugETH
            // 
            this.txtDebugETH.Location = new System.Drawing.Point(142, 37);
            this.txtDebugETH.Name = "txtDebugETH";
            this.txtDebugETH.Size = new System.Drawing.Size(40, 20);
            this.txtDebugETH.TabIndex = 40;
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Book Antiqua", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(6, 37);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 16);
            this.label3.TabIndex = 41;
            this.label3.Text = "ZIP";
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Book Antiqua", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(110, 37);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(32, 16);
            this.label5.TabIndex = 42;
            this.label5.Text = "Eth";
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.txtDebugETH);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.txtDebugZIP);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Location = new System.Drawing.Point(168, 120);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(208, 64);
            this.panel2.TabIndex = 43;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(270, 16);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(240, 24);
            this.label6.TabIndex = 44;
            this.label6.Text = "Control Births and Deaths Totals";
            // 
            // btnRunControlVitals
            // 
            this.btnRunControlVitals.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRunControlVitals.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunControlVitals.Location = new System.Drawing.Point(16, 192);
            this.btnRunControlVitals.Name = "btnRunControlVitals";
            this.btnRunControlVitals.Size = new System.Drawing.Size(96, 48);
            this.btnRunControlVitals.TabIndex = 31;
            this.btnRunControlVitals.Text = global::concep.Properties.Settings.Default.controlVitalsRunButtonText;
            this.btnRunControlVitals.UseVisualStyleBackColor = false;
            this.btnRunControlVitals.Click += new System.EventHandler(this.btnRunControlVitals_Click);
            // 
            // sqlConnection
            // 
            this.sqlConnection.FireInfoMessageEventOnUserErrors = false;
            // 
            // ControlVitals
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(588, 345);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.optDebug);
            this.Controls.Add(this.cboYear);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnRunControlVitals);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.doDeaths);
            this.Controls.Add(this.doBirths);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.panel2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "ControlVitals";
            this.Text = "CONCEP Version 4 - Control Vitals";
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]

        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        #region Run button processing

        private void btnRunControlVitals_Click(object sender, System.EventArgs e)
        {
            processParams(cboYear.SelectedItem.ToString());
            MethodInvoker mi = new MethodInvoker(beginControlVitalsWork);
            mi.BeginInvoke(null, null);
        }

        private void beginControlVitalsWork()
        {
            /* Control arrays for number of births and deaths total by ethnicity and sex. */     
            fillControls();
            if (doBirths.Checked)
            {
                WriteToStatusBox("Controlling births...");
                controlBirths();
            }  // end if

            if (doDeaths.Checked)
            {
                WriteToStatusBox("Controlling deaths...");
                controlDeaths();
            } // end if

            WriteToStatusBox(Environment.NewLine + "Completed vital records controlling!");
            sqlConnection.Close();
        }
        #endregion
        #region main utilities
        
        //*******************************************************************************

        /* method computeDifference() */

        /// Method to determine the difference in actual deaths total versus 
        /// calculated deaths total.  This difference will be sprinkled throughout
        /// the eth, sex, and age by CT in the Pachinko method.

        //  True to compute difference in actual 
        /// versus calculated deaths.  False to compute difference in actual versus 
        /// calculated births


        /* Revision History
        * 
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private int computeDifference(bool computeDeathsDiff, int eth, int sex)
        {
            int actualSum, calculatedSum = 0;
            if (computeDeathsDiff)
                for (int i = 0; i <= 100; i++)
                    calculatedSum += regionalDeaths[eth, sex, i];
            else
                for (int i = 0; i <= 100; i++)
                    calculatedSum += regionalDeaths[eth, sex, i];
            actualSum = getVitalSum(2, eth, sex);
            return (actualSum - calculatedSum);
        }  // end procedure computeDifference

        //******************************************************************************

        // controlBirths()
        /* Revision History
       * 
       * Date       By    Description
       * --------------------------------------------------------------------------
       * 05/12/03   tb    Initial coding
       * 07/10/03   df    C# revision
       * --------------------------------------------------------------------------
       */
        public void controlBirths()
        {
            int sumBirths = 0;
            bool debugStop = false;
            int eth, counter, realIndex, target;
            int zi, j, mi, mgraId, total, debugZIP = 0, debugETH = 0;
            int[] mgrabpop = new int[MAX_MGRAS_IN_ZIP];
            int[,] dIndex1 = new int[MAX_MGRAS_IN_ZIP, 2];
            int[] sortedData = new int[MAX_MGRAS_IN_ZIP];
            int[] passer = new int[MAX_MGRAS_IN_ZIP];
            double[] cumProb = new double[MAX_MGRAS_IN_ZIP];
            string fo = "";
            FileStream fout;		//file stream class

            //-----------------------------------------------------------------------------------------
            // open output file
            fo = networkPath + String.Format(appSettings["badZIPBD"].Value);
            try
            {
                fout = new FileStream(fo, FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter foutw = new StreamWriter(fout);

            if (optDebug.Checked)
            {
                debugZIP = Int32.Parse(txtDebugZIP.Text);
                debugETH = Int32.Parse(txtDebugETH.Text);
                debugStop = optDebug.Checked;
            }   // end if

            // Distribute ZIP births to mgras
            for (eth = 1; eth < NUM_ETH; eth++)
            {

                for (zi = 0; zi < zip.Length; ++zi)
                {
                    WriteToStatusBox("Controlling ethnicity " + eth + " zip " + zip[zi].zip_id);
                    counter = zip[zi].mgra_count;

                    if (debugStop && zip[zi].zip_id == debugZIP && eth == debugETH)
                        MessageBox.Show("DEBUG STOP - ZIP = " + zip[zi].zip_id + " ETH = " + eth);

                    /* For each ethnicity, get the female birth age pop by mgras.
                     * Then, sort the array and create a distribution array. */
                    Array.Clear(dIndex1, 0, dIndex1.Length);
                    for (j = 0; j < counter; j++)
                    {
                        mi = zip[zi].mgras[j] - 1;
                        dIndex1[j, 0] = j;
                        dIndex1[j, 1] = basePopBymgra[mi, eth];
                        zip[zi].fpop[eth] += basePopBymgra[mi, eth];
                    }   // end for i

                    // quickSort(dIndex1, 0, counter);
                    CU.cUtil.insertsort(dIndex1, counter);

                    Array.Clear(sortedData, 0, sortedData.Length);
                    total = 0;
                    for (j = 0; j < counter; j++)
                    {
                        sortedData[j] = dIndex1[j, 1];
                        total += sortedData[j];
                    }   // end for j

                    string str;
                    if (total == 0 && zip[zi].males[eth] > 0)
                    {
                        //MessageBox.Show("WARNING CONTROLLING BIRTHS - BASE POP = 0 males = " + zip[zi].males[eth] +
                        //  " ZIP = " + zip[zi].zip_id +  " Eth = " + eth);
                        str = "WARNING CONTROLLING BIRTHS - BASE POP = 0 males = " + zip[zi].males[eth] + " ZIP = " + zip[zi].zip_id + " Eth = " + eth;
                        foutw.WriteLine(str);

                    }  // end if
                    else
                    {
                        Array.Clear(passer, 0, passer.Length);
                        //WriteToStatusBox("...Controlling Males");
                        target = zip[zi].males[eth];
                        int ret = CU.cUtil.PachinkoWithMasterNoDecrement(target, sortedData, passer, counter);

                        // Restore the rounded totals
                        for (j = 0; j < counter; j++)
                        {
                            realIndex = dIndex1[j, 0];
                            mgraId = zip[zi].mgras[realIndex] - 1;
                            if (mgraId < 0)
                            {
                                MessageBox.Show("FATAL ERROR - MGRA INDEX < 0");
                            }  // end if
                            mgra_males[mgraId, eth] = passer[j];
                            sumBirths += passer[j];
                        }  // end for j
                    }  // end else

                    if (total == 0 && zip[zi].females[eth] > 0)
                    {
                        //MessageBox.Show("WARNING CONTROLLING BIRTHS - BASE POP = 0 females = " + zip[zi].females[eth].ToString() +
                        //  " ZIP = " + zip[zi].zip_id.ToString() +
                        //  " Eth = " + eth.ToString());
                        str = "WARNING CONTROLLING BIRTHS - BASE POP = 0 females = " + zip[zi].females[eth] + " ZIP = " + zip[zi].zip_id + " Eth = " + eth;
                        foutw.WriteLine(str);
                    }  // end if
                    else
                    {
                        Array.Clear(passer, 0, passer.Length);
                        //WriteToStatusBox("...Controlling Females");
                        target = zip[zi].females[eth];
                        int ret = CU.cUtil.PachinkoWithMasterNoDecrement(target, sortedData, passer, counter);
                        // Restore the rounded totals
                        for (j = 0; j < counter; j++)
                        {
                            realIndex = dIndex1[j, 0];
                            mgraId = zip[zi].mgras[realIndex] - 1;
                            mgra_females[mgraId, eth] = passer[j];
                            sumBirths += passer[j];
                        }  // end for j
                    }  // end else
                }  // end for zi
            }  // end for eth

            WriteMGRABirths(birthsMGRAFile, TN.birthsMGRA);

            // populate the ct births table
            sqlCommand.CommandText = String.Format(appSettings["deleteControlVitals1"].Value, TN.birthsCT, "birth_year", lagYear);

            try
            {
                sqlConnection.Open();
                this.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

            sqlCommand.CommandText = String.Format(appSettings["insertControlVitals1"].Value, TN.birthsCT, TN.xref, TN.birthsMGRA, lagYear);

            try
            {
                sqlConnection.Open();
                this.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }
            foutw.Close();
        }     // End method controlBirths()

        /*****************************************************************************/

        /* method controlDeaths() */
        /// <summary>
        /// Method to control the deaths.
        /// </summary>

        /* Revision History
            * 
            * Date       By    Description
            * --------------------------------------------------------------------------
            * 05/12/03   tb    Initial coding
            * 07/10/03   df    C# revision
            * --------------------------------------------------------------------------
            */
        public void controlDeaths()
        {
            int age, eth, sex, pop;
            double roc, survivalRate, xxx;
            System.Data.SqlClient.SqlDataReader rdr;
            sqlCommand.CommandText = String.Format(appSettings["selectControlVitals1"].Value, TN.survivalRates, TN.survivalRatesROC, TN.basePopCT, lagYear);
            try
            {
                sqlConnection.Open();

                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    eth = rdr.GetByte(0);
                    sex = rdr.GetByte(1);
                    age = rdr.GetByte(2);
                    pop = rdr.GetInt32(3);
                    survivalRate = rdr.GetDouble(4);
                    roc = rdr.GetDouble(5);

                    //build a temp array of distributions > 1 because small cohorts * surv rates don't yield
                    // enough deaths to distribute in Pachinko.  Use the distribution to distribute all deaths
                    // rather than just the difference
                    xxx = 0.5 + pop * (1 - survivalRate * Math.Pow(roc, lagYear - BASEYEAR));
                    xxx = 10 * xxx;   // mult by 10 to get a distribution for small cohorts where values < 1
                    regionalDeaths[eth, sex, age] = (int)xxx;
                }   // end while
                rdr.Close();
            }   // end try

            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }

            finally
            {
                sqlConnection.Close();
            }
            deathByPachinko();
            writeToFileAndBulkLoad(true, deathsCTFile, TN.deathsCT);

        }     // End method controlDeaths()

        /*****************************************************************************/

        /* method deathByPachinko() */
        /// Method to scatter the difference in deaths (difference between actual 
        /// deaths and calculated deaths) amongst age groups.

        /* Revision History
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private void deathByPachinko()
        {
            int cumSum, sum, difference;
            int i, eth, sex, age, m;
            double index;
            int[,] deathsByAge = new int[NUM_AGE, 2];  /* Array of population of a distinct eth and sex by age (1-100). */
            double[] cumProb = new double[NUM_AGE];
            Random rand = new Random(0);

            // First distribute calculated deaths across ages array by eth, sex.
            for (eth = 1; eth < NUM_ETH; eth++)
            {
                for (sex = 1; sex < NUM_SEX; sex++)
                {
                    sum = 0;
                    for (age = 0; age < NUM_AGE; age++)
                    {
                        deathsByAge[age, 0] = age;
                        deathsByAge[age, 1] = 0;
                        sum += regionalDeaths[eth, sex, age];
                    }   // end for age
                    //quickSort(deathsByAge, 0, NUM_AGE - 1);

                    cumSum = 0;
                    // Now compute cumulative probabilities and place into cumProb
                    for (m = 0; m < cumProb.Length; m++)
                    {
                        cumSum += regionalDeaths[eth, sex, m];
                        if (sum > 0)
                            cumProb[m] = ((double)cumSum / (double)sum) * 100;
                    }   // end for m

                    difference = deathsControl[eth, sex];

                    /* In this case too many deaths were computed using survival rates and rate of change.  Must revive some of the dead. */
                    if (difference < 0)
                    {
                        while (difference < 0)
                        {
                            index = rand.NextDouble() * 100;

                            for (i = 0; i < NUM_AGE; i++)
                            {
                                if (cumProb[i] > (double)index)
                                {
                                    if (deathsByAge[i, 1] > 0)
                                    {
                                        deathsByAge[i, 1]--;
                                        difference++;
                                    }   // end if
                                    break;
                                }   // end if
                            }     // End for
                        }     // End while
                    }     // End if

                    // Not enough deaths were computed.  Create some over age range.
                    else
                    {
                        while (difference > 0)
                        {
                            index = rand.Next(0, 100);
                            for (i = 0; i < cumProb.Length; i++)
                            {
                                if (cumProb[i] > index)
                                {
                                    if (deathsByAge[i, 1] < baseDataByRegion[eth, sex, deathsByAge[i, 0]])
                                    {
                                        deathsByAge[i, 1]++;
                                        difference--;
                                    }   // end if
                                    break;
                                }   // end if
                            }     // End for
                        }     // End while
                    }   // end else
                    quickSort(deathsByAge, 0, NUM_AGE - 1);
                    distributeDeathsByCT(eth, sex, deathsByAge);

                }     // End for sex
            }    // End for eth
        }     // End method deathByPachinko()

        /*****************************************************************************/

        /* method distributeDeathsByCT() */
        /// Method to distribute the controlled deaths totals throughout the CTs.
       
        /* Revision History
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private void distributeDeathsByCT(int eth, int sex, int[,] deathsAcrossAges)
        {
            int cumSum, sum, age;
            double index;
            int loopCount, ct, k;
            int[,] pop = new int[NUM_CTS, 2];  /* First index of second dimension holds CT number, the second index the population of that CT. */
            double[] cumProb = new double[NUM_CTS];
            Random rand = new Random(0);

            // Fill pop with populations.
            for (loopCount = 0; loopCount < NUM_AGE; loopCount++)
            {
                age = deathsAcrossAges[loopCount, 0];
                sum = 0;
                for (ct = 0; ct < NUM_CTS; ct++)
                {
                    pop[ct, 0] = ct;
                    pop[ct, 1] = baseDataByCT[eth, sex, age, ct];
                    sum += pop[ct, 1];
                }   // end for
                quickSort(pop, 0, NUM_CTS - 1);
                cumSum = 0;
                for (k = 0; k < NUM_CTS; k++)
                {
                    cumSum += pop[k, 1];
                    if (sum > 0)
                        cumProb[k] = ((double)cumSum / (double)sum) * 100;
                }   // end for


                // Distribute deaths of this eth, sex, and age across CTs.
                while (deathsAcrossAges[loopCount, 1] > 0)
                {
                    index = rand.NextDouble() * 100;
                    for (int i = 0; i < cumProb.Length; i++)
                    {
                        if (cumProb[i] > index)
                        {
                            if (pop[i, 1] > deathsByCT[eth, sex, age, pop[i, 0]])
                            {
                                if (age == 92) Console.WriteLine(pop[i,0]);
                                deathsByCT[eth, sex, age, pop[i, 0]]++;
                                deathsAcrossAges[loopCount, 1]--;
                            }   // end if
                            break;
                        }   // end if
                    }     // End for i
                }     // End while
            }     // End for loopCount
        }     // End method distributeDeathsByCT()

        /*****************************************************************************/
        #endregion main utilities
        #region fillControls

        /* method fillControls() */
        /// <summary>
        /// Method to fill the births and deaths control arrays from database table, 
        /// and fill the base population array.  In addition, the list of CTs gets
        /// populated.
        /// </summary>

        /* Revision History
            * 
            * STR             Date       By    Description
            * --------------------------------------------------------------------------
            *                 05/12/03   tb    Initial coding
            *                 06/21/04   tb    added code for zip code controls
            * --------------------------------------------------------------------------
            */
        public void fillControls()
        {
            int ct, eth, i, sex, age, pop, zipid, zi, mgra13;

            zip = new zip_data[NUM_ZIP];

            WriteToStatusBox("Populating controls...");
            SqlDataReader rdr;

            WriteToStatusBox("...Distinct ZIP List");
            sqlCommand.CommandText = String.Format(appSettings["selectControlVitals2"].Value, TN.xref);    

            // populate the zip_code list
            try
            {
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                zi = 0;
                while (rdr.Read())
                {
                    zip[zi] = new zip_data();
                    zipid = rdr.GetInt32(0);  // get the zip code
                    zip[zi].mgras = new int[MAX_MGRAS_IN_ZIP];
                    zip[zi].males = new int[NUM_ETH];
                    zip[zi].females = new int[NUM_ETH];
                    zip[zi].fpop = new int[NUM_ETH];
                    zip[zi++].zip_id = zipid;
                }
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

            // get the mgra list for zip codes - populate the zip code list 
            WriteToStatusBox("...MGRA by ZIP");
            sqlCommand.CommandText = String.Format(appSettings["selectControlVitals4"].Value, TN.xref);

            try
            {

                sqlConnection.Open();

                rdr = sqlCommand.ExecuteReader();

                while (rdr.Read())
                {
                    mgra13 = rdr.GetInt32(0);  // get the zip code
                    zipid = rdr.GetInt32(1);
                    zi = getZIPIndex(zipid);
                    zip[zi].mgras[zip[zi].mgra_count++] = mgra13;
                }
                rdr.Close();
               
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

            // populate the zip code births conrol table 
            WriteToStatusBox("...ZIP Births");
            sqlCommand.CommandText = String.Format(appSettings["selectControlVitals3"].Value, TN.birthsZIP, lagYear);
            //sqlCommand.CommandText = "select zip, births, hm, nhwm, nhbm, nhim, nham, nhhm, nhom, " +
            //        "nh2m, tm, hf, nhwf, nhbf, nhif, nhaf, nhhf, nhof, nh2f, tf from " + zipBirthsTable +
            //        " where birth_year = " + lagYear + " order by zip";
            try
            {
                
                sqlConnection.Open();
                
                rdr = sqlCommand.ExecuteReader();
               
                while (rdr.Read())
                {
                    zipid = rdr.GetInt32(0);  // get the zip code
                    zi = getZIPIndex(zipid);
                    if (zi >= zip.Length)
                    {
                        MessageBox.Show("Incorrect Zip Code entry for Zip = " + zipid);
                    }  // end if
                    else
                    {
                        zip[zi].zip_id = zipid;   // store the id

                        //skip total births and get the male by eth (standard order)
                        //skip total male; get the female by eth; skip total female
                        for (i = 0; i < 8; ++i)
                        {
                            zip[zi].males[i + 1] = rdr.GetInt32(i + 2);
                            zip[zi].females[i + 1] = rdr.GetInt32(i + 11);
                        }  // end for
                    }  // end else
                }
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

            /* Populate births and deaths control arrays (which store total deaths by eth and sex). */
            WriteToStatusBox("...Death Controls");
            sqlCommand.CommandText = String.Format(appSettings["selectControlVitals8"].Value, TN.vitalsControl, lagYear);
            //sqlCommand.CommandText = "SELECT eth, dcm, dcf FROM " + controlsTable + " WHERE estimates_year = " + lagYear;
            try
            {
                sqlConnection.Open();
               
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    i = rdr.GetByte(0);
                    deathsControl[i, 1] = rdr.GetInt32(1);
                    deathsControl[i, 2] = rdr.GetInt32(2);
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

            WriteToStatusBox("...Base pop for region deaths dist");
            sqlCommand.CommandText = String.Format(appSettings["selectControlVitals7"].Value, TN.basePopCT, lagYear);
            //sqlCommand.CommandText = "SELECT ethnicity, sex, age, SUM(pop) " +
             //     "FROM " + basePopTable + " WHERE estimates_year = " + lagYear + " and pop > 0 GROUP BY ethnicity, sex, age ORDER BY ethnicity, sex, age";
            try
            {
                sqlConnection.Open();
                // Fill the baseDataByRegion array from basePopTable.
                
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    eth = rdr.GetByte(0);
                    sex = rdr.GetByte(1);
                    age = rdr.GetByte(2);
                    pop = rdr.GetInt32(3);
                    try
                    {
                        if (age > 100)
                        {
                            age = 100;
                            baseDataByRegion[eth, sex, age] += pop;
                        }  // end if
                        else
                            baseDataByRegion[eth, sex, age] = pop;
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                    }

                }  // end while
                rdr.Close();

            }   // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

            WriteToStatusBox("...Distinct CTs for Deaths");
            sqlCommand.CommandText = String.Format(appSettings["selectCT"].Value, TN.xref);

            try
            {
                sqlConnection.Open();
                // Populate list of CTs from xref
               
                rdr = this.sqlCommand.ExecuteReader();
                ct = 0;     // Use ct as an index into ctList array
                while (rdr.Read())
                {
                    ctList[ct] = rdr.GetInt32(0);
                    ct++;
                }  // end while
                rdr.Close();

            }   // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

            WriteToStatusBox("...Base pop for CTs"); 
            // Populate baseDataByCT
            sqlCommand.CommandText = String.Format(appSettings["selectControlVitals6"].Value, TN.basePopCT, lagYear);
            //sqlCommand.CommandText = "SELECT ct10, ethnicity, sex, age, pop FROM " + basePopTable + " WHERE estimates_year = " + lagYear;
            try
            {
                sqlConnection.Open();
               
                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    ct = rdr.GetInt32(0);
                    eth = rdr.GetByte(1);
                    sex = rdr.GetByte(2);
                    age = rdr.GetByte(3);
                    pop = rdr.GetInt32(4);
                    if (age > 100)
                    {
                        age = 100;
                        baseDataByCT[eth, sex, age, getCTIndex(ct)] += pop;
                    }  // end if
                    else
                        baseDataByCT[eth, sex, age, getCTIndex(ct)] = pop;
                }  
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

            WriteToStatusBox("...Base pop for MGRAs");  
            // Populate baseDataByMGRA
            sqlCommand.CommandText = String.Format(appSettings["selectControlVitals5"].Value, TN.basePopMGRA, lagYear);
            //sqlCommand.CommandText = "SELECT mgra, ethnicity, popf_18to19+popf_20to24+popf_25to29+" +
            //      "popf_30to34+popf_35to39 FROM " + basePopTablemgra + " WHERE estimates_year = " + lagYear + " and ethnicity > 0";
            try
            {
                sqlConnection.Open();
               
                rdr = this.sqlCommand.ExecuteReader();
                basePopBymgra.Initialize();
                while (rdr.Read())
                {
                    mgra13 = rdr.GetInt32(0);
                    eth = rdr.GetByte(1);
                    pop = rdr.GetInt32(2);
                    basePopBymgra[mgra13 - 1, eth] = pop;
                } // end while
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
        }     // End method fillControls()

        /*****************************************************************************/
        #endregion fillControls

        #region miscellaneous utilities
        // includes procedures
      
        //  gtCTIndex()
        //  getVitalSum()
        //  getZIPIndex()
        //  processParms()
        //  quickSort()
        //*****************************************************************************

        /* method getCTIndex() */

        /// Method to get the index in list of CTs, of a given CT number.  A simple,
        /// yet highly efficient searching algorithm (binary search) with a 
        /// worst-case complexity of log(n) (n = NUM_CTS).


        /* Revision History
        * 
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private int getCTIndex(int ctNumber)
        {
            bool found = false;
            int index = ctList.Length / 2, hi = ctList.Length, lo = 0;

            while (!found && hi >= lo)
            {
                if (ctList[index] == ctNumber)
                    found = true;
                else if (ctList[index] < ctNumber)
                {
                    lo = index;
                    index = lo + (hi - lo) / 2;
                }   //ens else if
                else      // ctList[index] > ctNumber
                {
                    hi = index;
                    index = lo + (hi - lo) / 2;
                }   // end else
            }     // End while

            return index;
        }     // End method getCTIndex()

        /*****************************************************************************/

        /* method getVitalSum() */

        /// Method to fill the births or deaths total.
        
        /* Revision History
        * 
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private int getVitalSum(byte switcher, int ethnicity, int sex)
        {

            System.Data.SqlClient.SqlDataReader rdr;
            int total = 0;
            string tableName = "";
            string sName;

            if (switcher == 1)
            {
                tableName = TN.birthsCT;
                sName = "births";
            }  // end if
            else
            {
                tableName = TN.deathsCT;
                sName = "deaths";
            }  // end else

            sqlCommand.CommandText = "SELECT sum(" + sName + ") FROM " + tableName + " WHERE ethnicity = " + ethnicity + " AND sex = " + sex;
            try
            {
                sqlConnection.Open();

                rdr = this.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    if (rdr.IsDBNull(0))
                        total = 0;
                    else
                        total = rdr.GetInt32(0);
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


            return total;
        }  // end procedure getVitalSum()

        /*****************************************************************************/


        /* method getZipIndex() */

        /// Method to get the index in list of Zip, of a given zip code.  


        /* Revision History
            * 
            * Date       By    Description
            * --------------------------------------------------------------------------
            * 05/12/03   tb    Initial coding
            * 07/10/03   df    C# revision
            * 06/21/04   tb    Initial coding for version 2
            * --------------------------------------------------------------------------
            */
        private int getZIPIndex(int zip_id)
        {
            int i, index = 999;
            for (i = 0; i < zip.Length; ++i)
            {
                if (zip[i].zip_id == zip_id)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }  // end procedure getZIPIndex()

        /*****************************************************************************/

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
        private void processParams(string year)
        {
            try
            {

                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                appSettings = config.AppSettings.Settings;
                connectionStrings = config.ConnectionStrings.ConnectionStrings;

                networkPath = String.Format(appSettings["networkPath"].Value);

                MAX_MGRAS_IN_ZIP = int.Parse(appSettings["MAX_MGRAS_IN_ZIP"].Value);
                NUM_ETH = int.Parse(appSettings["NUM_ETH"].Value);
                NUM_SEX = int.Parse(appSettings["NUM_SEX"].Value);
                NUM_AGE = int.Parse(appSettings["NUM_AGE"].Value);
                NUM_CTS = int.Parse(appSettings["NUM_CTS"].Value);
                NUM_MGRAS = int.Parse(appSettings["NUM_MGRAS"].Value);
                NUM_ZIP = int.Parse(appSettings["NUM_ZIP"].Value);

                sqlConnection.ConnectionString = connectionStrings["ConcepDBConnectionString"].ConnectionString;
                this.sqlCommand.Connection = this.sqlConnection;

                TN.basePopCT = String.Format(appSettings["basePopCT"].Value);
                TN.basePopMGRA = String.Format(appSettings["basePopMGRA"].Value);
                TN.birthsCT = String.Format(appSettings["birthsCT"].Value);
                TN.birthsMGRA = String.Format(appSettings["birthsMGRA"].Value);
                TN.birthsZIP = String.Format(appSettings["birthsZIP"].Value);
                TN.deathsCT = String.Format(appSettings["deathsCT"].Value);
                TN.survivalRates = String.Format(appSettings["survivalRates"].Value);
                TN.survivalRatesROC = String.Format(appSettings["survivalRatesROC"].Value);
                TN.vitalsControl = String.Format(appSettings["vitalsControl"].Value);
                TN.xref = String.Format(appSettings["xref"].Value);

                birthsCTFile = networkPath + String.Format(appSettings["birthsCTFile"].Value);
                deathsCTFile = networkPath + String.Format(appSettings["deathsCTFile"].Value);
                birthsMGRAFile = networkPath + String.Format(appSettings["birthsMGRAFile"].Value);
            }  // end try

            catch (ConfigurationErrorsException c)
            {
                throw c;
            }

            birthsControl = new int[NUM_ETH, NUM_SEX];
            deathsControl = new int[NUM_ETH, NUM_SEX];
            baseDataByCT = new int[NUM_ETH, NUM_SEX, NUM_AGE, NUM_CTS];
            baseDataByRegion = new int[NUM_ETH, NUM_SEX, NUM_AGE];
            basePopBymgra = new int[NUM_MGRAS, NUM_ETH];
            birthsByCT = new int[NUM_ETH, NUM_SEX, NUM_CTS];
            deathsByCT = new int[NUM_ETH, NUM_SEX, NUM_AGE, NUM_CTS];
            ctList = new int[NUM_CTS];
            regionalDeaths = new int[NUM_ETH, NUM_SEX, NUM_AGE];
            WriteToStatusBox("Processing input and building table names...");
            estimatesYear = int.Parse(year);
            lagYear = estimatesYear - 1;   // data year is estimates year - 1

            mgra_females = new int[NUM_MGRAS, NUM_ETH];
            mgra_males = new int[NUM_MGRAS, NUM_ETH];
            

            estimateBirths = doBirths.Checked;
            estimateDeaths = doDeaths.Checked;
                       
            estimateBirths = doBirths.Checked;
            estimateDeaths = doDeaths.Checked;


        }  // end procedure processParams()

        /*****************************************************************************/

        /* method quickSort() */

        /// Method to sort a two-dimensional array in ascending order.  Average 
        /// complexity is O(nlogn).  
      
        /* Revision History
        * 
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private void quickSort(int[,] a, int lo, int hi)
        {
            int i = lo, j = hi, temp1, temp2;
            int x = a[(lo + hi) / 2, 1];

            do
            {
                while (a[i, 1] < x)
                    i++;
                while (a[j, 1] > x)
                    j--;
                if (i <= j)
                {
                    temp1 = a[i, 0];
                    temp2 = a[i, 1];
                    a[i, 0] = a[j, 0];
                    a[i, 1] = a[j, 1];
                    a[j, 0] = temp1;
                    a[j, 1] = temp2;
                    i++;
                    j--;
                }   // end if
            } while (i <= j);  // end do

            // Recursion
            if (lo < j)
                quickSort(a, lo, j);
            if (i < hi)
                quickSort(a, i, hi);
        }  // end procedure quickSort()

        //***********************************************************************************************

        /* method writeMGRABirths() */
        /// <summary>
        /// Method to write final mgra births arrays to file and bulk load into  
        /// table.
        /// </summary>
        private void WriteMGRABirths(string fileName, string tableName)
        {
            int eth, i;

            //-------------------------------------------------------------------------

            sw = new StreamWriter(new FileStream(fileName, FileMode.Create));

            // Write births by mgra and eth where births > 0.
            for (i = 0; i < NUM_MGRAS; ++i)
            {
                for (eth = 1; eth < NUM_ETH; eth++)
                {
                    if (mgra_males[i, eth] > 0)
                        sw.WriteLine(lagYear + "," + (i + 1) + "," + eth + ",1," + mgra_males[i, eth]);
                    if (mgra_females[i, eth] > 0)
                        sw.WriteLine(lagYear + "," + (i + 1) + "," + eth + ",2," + mgra_females[i, eth]);
                    sw.Flush();
                }  // end for eth
            }  // end for i
            sw.Close();

            WriteToStatusBox("Truncating table " + tableName);
            sqlCommand.CommandText = String.Format(appSettings["deleteControlVitals1"].Value, tableName, "birth_year", lagYear);

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

            // Finally, bulk load into table.
            WriteToStatusBox("Writing data to SQL Server table " + tableName);
            sqlCommand.CommandText = String.Format(appSettings["bulkInsert"].Value, tableName, fileName);

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
        }  // end procedure WriteMGRABirths()

        //****************************************************************************

        /* method writeToFileAndBulkLoad() */

        /// Method to write final deaths array to file and bulk load into deaths 
        /// table.
      
        /* Revision History
        * 
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private void writeToFileAndBulkLoad(bool writeDeaths, string fileName, string tableName)
        {
            int eth, sex, age, ct;
            sw = new StreamWriter(new FileStream(fileName, FileMode.Create));

            // Write births by mgra and eth where births > 0.
            if (!writeDeaths)
            {
                for (ct = 0; ct < NUM_CTS; ct++)
                {
                    for (eth = 1; eth < NUM_ETH; eth++)
                    {
                        for (sex = 1; sex < NUM_SEX; sex++)
                        {
                            sw.WriteLine(ctList[ct] + ", " + eth + ", " + sex + ", " + birthsByCT[eth, sex, ct]);
                        }  // end for sex
                    }  // end for eth
                }  // end for ct
            }  // end if
            // Write deaths by CT, eth, sex, and age where deaths > 0.
            else
            {
                for (ct = 0; ct < NUM_CTS; ct++)
                {
                    for (eth = 1; eth < NUM_ETH; eth++)
                    {
                        for (sex = 1; sex < NUM_SEX; sex++)
                        {
                            for (age = 0; age < NUM_AGE; age++)
                            {
                                if (deathsByCT[eth, sex, age, ct] > 0)
                                {
                                    sw.WriteLine(lagYear + "," + ctList[ct] + "," + eth + "," + sex + "," + age + "," + deathsByCT[eth, sex, age, ct]);
                                } // end if
                            }  // end for age
                        }  // end for sex
                    }  // end for eth
                }  // end for ct
            }  // end else
            sw.Close();

            WriteToStatusBox("Truncating table " + tableName);
            sqlCommand.CommandText = String.Format(appSettings["deleteControlVitals1"].Value, tableName, "death_year", lagYear);
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

            // Finally, bulk load into table.
            WriteToStatusBox("Writing data to SQL Server table " + tableName);

            sqlCommand.CommandText = String.Format(appSettings["bulkInsert"].Value, tableName, fileName);
            try
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                sqlConnection.Close();
            }

        }     // End method writeToFileAndBulkLoad()

        /*****************************************************************************/

        /* method WriteToStatusBox() */
        /// display status

        /* Revision History
            * 
            * Date       By    Description
            * --------------------------------------------------------------------------
            * 05/12/03   tb    Initial coding
            * 07/10/03   df    C# revision
            * --------------------------------------------------------------------------
            */
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
        }  // end procedure WriteToStatusBox


        //*************************************************************************************

        #endregion Miscellaneous utilities


    } // end class controlVitals
} // end namespace