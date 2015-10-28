
/* Filename:   controlGQ.cs
 * Program:    concep
 * Version:     4.0
 * Programmers: Terry Beckhelm
 * Description:     
 *              This application performs the controlling of GQ
 *              version 4 adds standardized configuration file for global vars, table names and query content
 *              version 3.5 adds computations for using Series 13 geographies
 *              Version 3.3 adds detail to GQ_CIV
 * Methods:     
 *              Main()
 *              runBtnProcessing_Click()
 * 
 * Database:    SQL Server Database concep
 *              Tables: 
 *                     gq_by_lckey_raw
 *                     gq_by_lckey_controlled
 *                     controls_popest_city
 *                      
 * Revision History
 * Date       By    Description
 * -----------------------------------------------------------
 * 06/21/2006 tb    Initial coding
   10/23/2012 tb    Mods for configuration file data     
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

namespace CGQ
{
    

    public class controlGQ : System.Windows.Forms.Form
    {
        delegate void WriteDelegate(string status);
        public class TableNames
        {
            public string GQRaw;
            public string GQControlled;
            public string popestControls;
            public string xref;
        } // end tablenames

        public Configuration config;
        public KeyValueConfigurationCollection appSettings;
        public ConnectionStringSettingsCollection connectionStrings;
        public System.Data.SqlClient.SqlConnection sqlConnection;
        public System.Data.SqlClient.SqlCommand sqlCommand;
        public System.Windows.Forms.TextBox txtStatus;

        public int MAXGQRECORDS;  //max number of lckey records by city in GQ lckey file
        public int NUM_CITIES;

        public string networkPath;
        public int selectedYear;
        public string controlTable;
        public TableNames TN = new TableNames();
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnRuncontrolGQ;
        
        private System.Windows.Forms.ComboBox cboYear;
        
        private System.ComponentModel.Container components = null;

        public controlGQ()
        {
            InitializeComponent();
        }

        #region Windows Form Designer generated code

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

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnExit = new System.Windows.Forms.Button();
            this.sqlCommand = new System.Data.SqlClient.SqlCommand();
            this.sqlConnection = new System.Data.SqlClient.SqlConnection();
            this.btnRuncontrolGQ = new System.Windows.Forms.Button();
            this.cboYear = new System.Windows.Forms.ComboBox();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(80, 88);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(167, 24);
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
            this.label1.Size = new System.Drawing.Size(184, 32);
            this.label1.TabIndex = 32;
            this.label1.Text = "Control GQ";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Location = new System.Drawing.Point(8, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 48);
            this.panel1.TabIndex = 33;
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(104, 128);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(96, 48);
            this.btnExit.TabIndex = 27;
            this.btnExit.Text = "Return";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // sqlConnection
            // 
            this.sqlConnection.FireInfoMessageEventOnUserErrors = false;
            // 
            // btnRuncontrolGQ
            // 
            this.btnRuncontrolGQ.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRuncontrolGQ.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRuncontrolGQ.Location = new System.Drawing.Point(8, 128);
            this.btnRuncontrolGQ.Name = "btnRuncontrolGQ";
            this.btnRuncontrolGQ.Size = new System.Drawing.Size(96, 48);
            this.btnRuncontrolGQ.TabIndex = 31;
            this.btnRuncontrolGQ.Text = "Run ";
            this.btnRuncontrolGQ.UseVisualStyleBackColor = false;
            this.btnRuncontrolGQ.Click += new System.EventHandler(this.btnRuncontrolGQ_Click);
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
            this.txtStatus.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(8, 192);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ReadOnly = true;
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(368, 80);
            this.txtStatus.TabIndex = 36;
            // 
            // controlGQ
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(418, 299);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.cboYear);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnRuncontrolGQ);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "controlGQ";
            this.Text = "CONCEP Version 4 - Control GQ";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        [STAThread]

        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        #region Run button processing

        /*****************************************************************************/

        /*  btnRuncontrolGQ_Click() */
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
        private void btnRuncontrolGQ_Click(object sender, System.EventArgs e)
        {
            selectedYear = Int32.Parse(cboYear.SelectedItem.ToString());
            MethodInvoker mi = new MethodInvoker(begincontrolGQWork);
            mi.BeginInvoke(null, null);
        }  // end btnRunControlGQ_Click()

        //********************************************************************************

        /* method begincontrolGQWork() */

        /* Revision History
        * 
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 05/12/03   tb    Initial coding
        * 07/10/03   df    C# revision
        * --------------------------------------------------------------------------
        */
        private void begincontrolGQWork()
        {
            int i, j, diff, lcount;
            SqlDataReader rdr = null;
            // ----------------------------------------------------------------------

            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                appSettings = config.AppSettings.Settings;
                connectionStrings = config.ConnectionStrings.ConnectionStrings;

                networkPath = String.Format(appSettings["networkPath"].Value);
                MAXGQRECORDS = int.Parse(appSettings["MAXGQRECORDS"].Value);
                NUM_CITIES = int.Parse(appSettings["NUM_CITIES"].Value);
               
               
                sqlConnection.ConnectionString = connectionStrings["ConcepDBConnectionString"].ConnectionString; 
                this.sqlCommand.Connection = this.sqlConnection;
                TN.GQControlled = String.Format(appSettings["GQControlled"].Value);
                TN.GQRaw = String.Format(appSettings["GQRaw"].Value);
                TN.popestControls = String.Format(appSettings["popestControls"].Value);
                TN.xref = String.Format(appSettings["xref"].Value);
            }  // end try
            catch (ConfigurationErrorsException c)
            {
                throw c;
            }

            GQDATA[] g = new GQDATA[NUM_CITIES];

            for (i = 0; i < NUM_CITIES; ++i)
            {
                g[i] = new GQDATA();
                g[i].lckey = new int[MAXGQRECORDS];
                g[i].lu = new int[MAXGQRECORDS];
                g[i].mgra = new int[MAXGQRECORDS];
                g[i].civ = new int[MAXGQRECORDS];
                g[i].civ_college = new int[MAXGQRECORDS];
                g[i].civ_other = new int[MAXGQRECORDS];
                g[i].mil = new int[MAXGQRECORDS];
                g[i].gq = new int[MAXGQRECORDS];

                for (j = 1; j < MAXGQRECORDS; ++j)
                {
                    g[i].civ[j] = 0;
                    g[i].mil[j] = 0;
                    g[i].lckey[j] = 0;
                }
                g[i].control_inst = 0;
                g[i].control_college = 0;
                g[i].control_other = 0;
                g[i].citycount = 0;
                g[i].cityID = 0;
                g[i].civ_college_total = 0;
                g[i].civ_other_total = 0;

            }   // end for i

            //get city GQ controls
            sqlCommand.CommandText = String.Format(appSettings["selectControlGQ1"].Value, TN.popestControls, selectedYear);
            //sqlCommand.CommandText = "Select city, gq_civ_college,gq_civ_other from dbo.controls_popest_city where estimates_year = " + selectedYear;

            try
            {
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        j = rdr.GetByte(0) - 1;
                        g[j].control_college = rdr.GetInt32(1);
                        g[j].control_other = rdr.GetInt32(2);

                    }  // end while
                    rdr.Close();
                }  // end if
                else
                {
                    throw new Exception("No GQ Popest Controls for " + selectedYear + " have been loaded.");
                }  // end else
            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                Application.Exit();
            }
            finally
            {
                
                sqlConnection.Close();
            }

            // fill the lckey array
            sqlCommand.CommandText = String.Format(appSettings["selectControlGQ2"].Value, TN.GQRaw, TN.xref, selectedYear);
            //sqlCommand.CommandText = "Select x.city,lckey,lu,g.mgra,gq,gq_civ,gq_mil,gq_civ_college,gq_civ_other from gq_by_lckey_raw g, xref_mgra_sr13 x" +
            //                        " where x.mgra = g.mgra and estimates_year = " + selectedYear + " order by x.city,gq desc";

            try
            {
                sqlConnection.Open();
                rdr = sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    j = rdr.GetByte(0) - 1;
                    lcount = g[j].citycount;
                    g[j].lckey[lcount] = rdr.GetInt32(1); 
                    g[j].lu[lcount] = rdr.GetInt32(2);
                    g[j].mgra[lcount] = rdr.GetInt32(3);
                    g[j].gq[lcount] = rdr.GetInt32(4);              
                    g[j].civ[lcount] = rdr.GetInt32(5);
                    g[j].mil[lcount] = rdr.GetInt32(6);
                    g[j].civ_college[lcount] = rdr.GetInt32(7);
                    g[j].civ_other[lcount] = rdr.GetInt32(8); 

                    g[j].civ_college_total += g[j].civ_college[lcount];
                    g[j].civ_other_total += g[j].civ_other[lcount];
                    g[j].citycount++;
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
            }
           
            for (i = 0; i < NUM_CITIES; ++i)
            {
                WriteToStatusBox("processing city " + (i + 1).ToString());
                //sum the gq_civ_college and control
                diff = g[i].control_college - g[i].civ_college_total;
                while (diff != 0)
                {
                    for (j = 0; j < g[i].citycount; ++j)
                    {
                        if (g[i].civ_college[j] > 0)
                        {
                            if (diff < 0)
                            {
                                ++diff;
                                --g[i].civ_college[j];
                            }  // end if
                            else
                            {
                                ++g[i].civ_college[j];
                                --diff;
                            }  // end else
                            if (diff == 0)
                                break;
                        }  // end if
                    }  // end for j
                }  // end while

                //sum the gq_civ_other and control
                try
                {
                    if (g[i].control_other > 0 && g[i].civ_other_total == 0)
                    {
                        throw new Exception("City " + (i + 1) + " has control of " + g[i].control_other +
                            " gq_civ_oth, but 0 gq_civ_oth lckey sites to control to! Check your gq_by_lckey_raw table.");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    Application.Exit();
                }

                diff = g[i].control_other - g[i].civ_other_total;
                

                while (diff != 0)
                {
                    for (j = 0; j < g[i].citycount; ++j)
                    {
                        if (g[i].civ_other[j] > 0)
                        {
                            if (diff < 0)
                            {
                                ++diff;
                                --g[i].civ_other[j];
                            }  // end if
                            else
                            {
                                ++g[i].civ_other[j];
                                --diff;
                            }  // end else
                            if (diff == 0)
                                break;
                        }  // end if
                    }  // end for j
                }  // end while
            }  // end for i

            // truncate the controlled table for this selectedYear and reload with inserts
            sqlCommand.CommandText = String.Format(appSettings["deleteFrom"].Value, TN.GQControlled, selectedYear);
            //this.sqlCommand.CommandText = "delete from gq_by_lckey_controlled where estimates_year = " + selectedYear;
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
            // insert the new data
            string tex = "";
            for (i = 0; i < NUM_CITIES; ++i)
            {
                for (j = 0; j < g[i].citycount; ++j)
                {
                    g[i].civ[j] = g[i].civ_college[j] + g[i].civ_other[j];
                    g[i].gq[j] = g[i].civ[j] + g[i].mil[j];
                    tex = " values( " + selectedYear + "," + g[i].lckey[j] + "," + g[i].lu[j] + "," + g[i].mgra[j] + "," + g[i].gq[j] + "," + g[i].civ[j] + "," +
                                    g[i].mil[j] + "," + g[i].civ_college[j] + "," + g[i].civ_other[j] + ")";
                    sqlCommand.CommandText = String.Format(appSettings["insertInto"].Value, TN.GQControlled, tex);
                    //sqlCommand.CommandText = "insert into gq_by_lckey_controlled values( " + selectedYear + "," +
                    //  g[i].lckey[j] + "," + g[i].lu[j] + "," +  g[i].mgra[j] + "," + g[i].gq[j] + "," + g[i].civ[j] + "," +  g[i].mil[j] + "," +
                    //  g[i].civ_college[j] + "," + g[i].civ_other[j] + ")";
                    try
                    {
                        this.sqlConnection.Open();
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
                }  // end for j
            }  // end for i

            WriteToStatusBox(Environment.NewLine + "Completed GQ controlling!");
        }  // end procedure begincontrolGEWork()

        //*********************************************************************
        #endregion

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
        }  // end WriteToStatusBox()

        //***************************************************************************
        
        public class GQDATA
        {
            public int cityID;
            public int citycount;
            public int civ_college_total;
            public int civ_other_total;
            public int control_inst;
            public int control_college;
            public int control_other;
            public int[] lckey;
            public int[] lu;
            public int[] mgra;
            public int[] civ;
            public int[] civ_college;
            public int[] civ_other;
            public int[] mil;
            public int[] gq;

        }     ///end class GQData
    }
}