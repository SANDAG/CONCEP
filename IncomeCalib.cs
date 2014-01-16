/* Filename:    IncomeCalib.cs
 * Program:     CONCEP
 * Version:     4.0
 * Programmers: Terry Beckhelm
 *             
 * Description: Version 4 introduces concep.config.exe to store all global constants, queries, table names and file names
 *              version 3.5 adds computations for using Series 13 geographies
 *                  utilizes table income_2010 
 *              Estimates income calibration using 2000 Census data for New income groups
 *				version 4.0 is a recode for Series 11 SGRAs - theoretically, nothing should change
 *				because the calibration is for CT and SRA - these don't change with shift to SGRA
 *				adopted the old names, eliminating newgroups from name
 * Methods:     
 *              Main()
 *              DoIncomeCalibrationWork()
 *              doStats()
 *              erff()
 *              MedianIncome()
 *              unerff()

 *              Tables: 
 *                input: concep : income_2010
 *                                    
 *                output: income_calibration_parms : parameter output
 *              ASCII
 *                  iout.txt - Formatted calibration results
 *                  adj.txt - formatted adjustments from calibration
 *   
 *                      
 * Revision History
 * Date       By    Description
 * --------------------------------------------------------------------------
 * 09/24/03   tb    Initial C# coding
 * 04/06/04   tb    modified for new income groups 
 * 02/02/05   tb    recode for SR11 SGRAs Version 4    
 * 06/22/11   tb    recode for SR12 MGRAs Concep 3.3
 * 07/05/12   
 * --------------------------------------------------------------------------
 */
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Configuration;

namespace IncomeCalib
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class IncCalib : System.Windows.Forms.Form
	{
        public class TableNames
        {
            public string incomeCalibrationParms;
            public string baseYearIncome;
            
        } // end tablenames

        public Configuration config;
        public KeyValueConfigurationCollection appSettings;
        public ConnectionStringSettingsCollection connectionStrings;

        public TableNames TN = new TableNames();
        public string networkPath;
        private StreamWriter iout,aout;
        public int NUM_INCOME_GROUPS;   // number of income groups
        public int NUM_CTS;             // number of CTS
        public int [] bounds = new int[] {0,15000,30000,45000,60000,75000,100000,125000,150000,200000,350000};
        public double [] income_log;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnExit;
        private System.Data.SqlClient.SqlConnection sqlConnection;
        private System.Data.SqlClient.SqlCommand sqlCommand;
        private System.Windows.Forms.TextBox txtStatus;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public IncCalib()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

        private delegate void WriteDelegate( string str );

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.label1 = new System.Windows.Forms.Label();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.sqlConnection = new System.Data.SqlClient.SqlConnection();
            this.sqlCommand = new System.Data.SqlClient.SqlCommand();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label1.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(253, 36);
            this.label1.TabIndex = 0;
            this.label1.Text = "Income Calibration";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Book Antiqua", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(16, 72);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(375, 56);
            this.txtStatus.TabIndex = 1;
            // 
            // btnRun
            // 
            this.btnRun.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnRun.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRun.Location = new System.Drawing.Point(24, 152);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(111, 52);
            this.btnRun.TabIndex = 2;
            this.btnRun.Text = "Run";
            this.btnRun.UseVisualStyleBackColor = false;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(134, 152);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(113, 52);
            this.btnExit.TabIndex = 3;
            this.btnExit.Text = "Return";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // sqlConnection
            // 
            this.sqlConnection.ConnectionString = "data source=pila\\sdgintdb;initial catalog=concep_test;integrated security=SSPI;pe" +
    "rsist security info=False;workstation id=TBE;packet size=4096";
            this.sqlConnection.FireInfoMessageEventOnUserErrors = false;
            // 
            // IncCalib
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(436, 266);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.label1);
            this.Name = "IncCalib";
            this.Text = "CONCEP Version 4 - Income Calibration";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
		#endregion
        
		
		[STAThread]

       
        /* method btnRun_Click() */
        /// <summary>
        /// Method to begin processing form entry.
        /// </summary>
  
        /* Revision History
        * 
        * STR             Date       By    Description
        * --------------------------------------------------------------------------
        *                 09/24/03   tb    Initial coding
        * --------------------------------------------------------------------------
        */
        private void btnRun_Click(object sender, System.EventArgs e)
        {
            processParams();
            MethodInvoker mi = new MethodInvoker( DoIncomeCalibrationWork );
            mi.BeginInvoke( null, null );
        }  // end btnRun_Click()

        // ************************************************************************

        /* method DoIncomeCalibrationWork() */
        /// <summary>
        /// Method to begin processing form entry.
        /// </summary>
      
        /* Revision History
        * 
        * Date       By    Description
        * --------------------------------------------------------------------------
        * 09/24/03   tb    C# revision
        * 04/06/04   tb    modified for new groups
        * --------------------------------------------------------------------------
        */
        private void DoIncomeCalibrationWork()
        {
            double asd, diss, nla;
            double[] asd_alt = new Double[NUM_CTS];
            double[] nla_alt = new Double[NUM_CTS];
            double[] diss_alt = new Double[NUM_CTS];

            int[] hh_alt = new Int32[NUM_CTS];
            int[,] inc = new Int32[NUM_CTS, NUM_INCOME_GROUPS];
            int[] id_alt = new Int32[NUM_CTS];
            int[] median_alt = new Int32[NUM_CTS];
            int[] hh = new Int32[NUM_CTS];
            int[] inc_pass = new Int32[NUM_INCOME_GROUPS];
            int[] id = new Int32[NUM_CTS];

            int geo_type, geo_typea, hh_pass, i, loop_count, median, j, id_pass;
            string val = "";
            System.Data.SqlClient.SqlDataReader rdr;

            // -----------------------------------------------------------------------

            sqlCommand.Connection = sqlConnection;
            // open files
            try
            {
                iout = new StreamWriter(new FileStream(networkPath + String.Format(appSettings["iout"].Value), FileMode.Create));
                aout = new StreamWriter(new FileStream(networkPath + String.Format(appSettings["aout"].Value), FileMode.Create));
                income_log = new double[NUM_INCOME_GROUPS];
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());
            }

            for (i = 0; i < NUM_INCOME_GROUPS - 1; ++i)
            {
                //income_log[i] = new double();
                income_log[i] = System.Math.Log((double)bounds[i + 1]);
            }   // end for i

            for (geo_typea = 1; geo_typea <= 4; ++geo_typea)
            {
                if (geo_typea == 1)
                    geo_type = 11;  //series 13 ct = type 11
                else
                    geo_type = geo_typea;

                writeToStatusBox("PROCESSING GEO_TYPE " + geo_type.ToString());

                /* purge the income_parms table before reloading */
                sqlCommand.CommandText = String.Format(appSettings["deleteIncomeCalib1"].Value, TN.incomeCalibrationParms, geo_type);
                writeToStatusBox("...Deleting from calibration_parameters_income_model table before reloading");

                try
                {
                    sqlConnection.Open();
                    sqlCommand.ExecuteNonQuery();
                }  // end try
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), e.GetType().ToString());
                    Close();
                }  // end catch
                finally
                {
                    sqlConnection.Close();
                }

                /* get income distribution and geography */
                writeToStatusBox("...extracting income data");
                sqlCommand.CommandText = String.Format(appSettings["selectIncomeCalib1"].Value, TN.baseYearIncome, geo_type);

                try
                {
                    this.sqlConnection.Open();
                    rdr = this.sqlCommand.ExecuteReader();
                    loop_count = 0;
                    while (rdr.Read())
                    {
                        writeToStatusBox("...Processing record # " + loop_count.ToString());

                        id[loop_count] = rdr.GetInt32(1);
                        for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                            inc[loop_count, i] = rdr.GetInt32(i + 2);
                        hh[loop_count] = rdr.GetInt32(12);
                        ++loop_count;
                    }   // end while
                    rdr.Close();

                    for (i = 0; i < loop_count; ++i)
                    {
                        asd = 0;
                        nla = 0;
                        median = 0;
                        diss = 0;
                        for (j = 0; j < NUM_INCOME_GROUPS; ++j)
                            inc_pass[j] = inc[i, j];
                        hh_pass = hh[i];
                        id_pass = id[i];

                        doStats(id_pass, loop_count, geo_type, inc_pass, hh_pass, ref asd, ref nla, ref median, ref diss);

                        diss_alt[i] = diss;
                        nla_alt[i] = nla;
                        asd_alt[i] = asd;
                        median_alt[i] = median;
                        id_alt[i] = id_pass;
                        hh_alt[i] = hh_pass;

                        writeToStatusBox("...Loading record # " + i.ToString());
                        val = " values(" + geo_type + "," + id_alt[i] + "," + diss_alt[i] + "," + asd_alt[i] + "," + nla_alt[i] + "," + hh_alt[i] + "," + median_alt[i] + ")";
                        sqlCommand.CommandText = String.Format(appSettings["insertInto"].Value, TN.incomeCalibrationParms, val);

                        try
                        {
                            sqlCommand.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.ToString(), e.GetType().ToString());
                        }  // end catch
                    }     // end for i

                }  // end try
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), e.GetType().ToString());

                }  // end catch
                finally
                {
                    sqlConnection.Close();
                }
            }   // end for geo_type
            iout.Close();
            aout.Close();
            writeToStatusBox("Completed IncomeCalibration processing!");

        }   // end DoIncomeCalibrationWork()

        // ********************************************************************************

        /* procedure doStats() */

        /* perfform computations */

        /* Revision History
            STR            Date       By   Description
            -------------------------------------------------------------------------
                            08/09/95   tb   moved from main to facilitate 1980 processing
                            08/14/95   tb   added processing for ftb data 
                            10/26/98   tb   changed code for 1995 ftb data
                            09/24/03   tb   C# version
            -------------------------------------------------------------------------

        */
        /*---------------------------------------------------------------------------*/

        public void doStats(int id, int loop_count,int geo_type, int [] income, int hh, ref double asd, ref double nla, ref int median, ref double diss)
  
        {    
            int hh_tot;                      /* total households */
            int i;
            int iasd, iex;                  /* local indexes */
            int iasd1,iasd2,iex1,iex2;     /* calibration increments */
              
            double cuml;                    /* cumulative total for z values */
            double diff;
            double dissl;
            
            double log_med;                 /* log of median income */
            double x1= 0,x2= 0;

            double [] hh_pct_act = new double[NUM_INCOME_GROUPS];         /* distribution of household income */
            double [] hh_pct_est = new double[NUM_INCOME_GROUPS];         /* estimated distribution */
            
            double [] adj = new double[NUM_INCOME_GROUPS];
            double [,] p = new double[2,NUM_INCOME_GROUPS];
            double [] z_actual = new double[NUM_INCOME_GROUPS];       /* actual z values */
            double [] z_est = new double[NUM_INCOME_GROUPS];          /* estimated z values */
            double [] z1 = new double[NUM_INCOME_GROUPS];             /* difference between bound and median */

            double sqrt2;                   /* square root of 2 */

            string str,stra;
            /*--------------------------------------------------------------------------*/
            /* set calibration limits */
            iex1 = 125;
            iex2 = 310;
            iasd1 = 1;
            iasd2 = 50;
            sqrt2 = System.Math.Sqrt((double)2);
  
            hh_tot = 0;     /* init some totals */
            asd = nla = 0;

            /* total households from sum */
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
            {
                hh_tot += income[i];
            }  // end for

            median = 0;
            Array.Clear(hh_pct_act,0,hh_pct_act.Length);
            Array.Clear(hh_pct_est,0,hh_pct_est.Length);
            Array.Clear(z_actual,0,z_actual.Length);
            Array.Clear(z_est,0,z_est.Length);
            Array.Clear(adj,0,adj.Length);

            
            if (hh_tot > 0)
            {
                median = MedianIncome(income);

                /* calculate % of hh in each income class */
                for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                {
                    if (hh_tot > 0)
                        hh_pct_act[i] = (double)income[i]/(double)hh_tot * 100;
                }  // end for

                if (median > 0)
                    log_med = System.Math.Log((double)median);
                else 
                    log_med = 1;
                cuml = 0;
                
                /* retrieve z value for cumulative fraction */
                for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                {
                    cuml += hh_pct_act[i]/100;
                    z_actual[i] = unerff(cuml);
                }  // end for
                
                dissl = 1.0e10;
                Array.Clear(p,0,p.Length);
                for (iex = iex1; iex < iex2; ++iex)
                {
                    nla = (double)iex/100;
                    for (iasd = iasd1; iasd < iasd2; ++iasd)
                    {
                        Array.Clear(p,0,p.Length);
                        Array.Clear(adj,0,adj.Length);
                        asd = (double)iasd/1000;
                
                        for (i = 0; i < NUM_INCOME_GROUPS-1; ++i)
                        {
                            z1[i] = income_log[i] - log_med;
                            adj[i] = asd * (System.Math.Pow(income_log[i],nla));
                            z_est[i] = z1[i] * adj[i];
                            p[0,i] = (1 + erff((double)z_est[i]/sqrt2))/2;
                            if (i > 0)
                                p[1,i] = p[0,i] - p[0,i-1];
                        }     /* end for i */
                
                        p[1,0] = p[0,0];
                        p[1,NUM_INCOME_GROUPS-1] = 1 - p[0,NUM_INCOME_GROUPS-2];
                    
                        diss = 0;     /* init index of dissimilarity */     
                        for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                        {
                            hh_pct_est[i] = p[1,i] * 100;
                            diss += System.Math.Abs(hh_pct_act[i] - hh_pct_est[i]) / 2;
                        }  // end for i
                
                        if (diss < dissl)
                        {
                            x1 = nla;
                            x2 = asd;
                            dissl = diss;
                        }     /* end if */
                    }     /* end for iasd */
                }     /* end for iex*/
                
                nla = x1;
                asd = x2;
                
                /* recalculate z & p for each class */
                for (i = 0; i < NUM_INCOME_GROUPS -1; ++i)
                {
                    z1[i] = income_log[i] - log_med;
                    adj[i] = asd * (System.Math.Pow(income_log[i],nla));
                    z_est[i] = z1[i] * adj[i];
                    p[0,i] = (1 + erff((double)z_est[i]/sqrt2))/2;
                    if (i > 0)
                        p[1,i] = p[0,i] - p[0,i-1];
                }     /* end for i */
                
                p[1,0] = p[0,0];
                p[1,NUM_INCOME_GROUPS -1] = 1 - p[0,NUM_INCOME_GROUPS-2];
                diss = 0;
                for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                {
                    hh_pct_est[i] = p[1,i] * 100;
                    diff = System.Math.Abs(hh_pct_act[i] - hh_pct_est[i]) / 2;
                    diss += diff;
                }  // end for i
            }   /* end if */

            else
            {
                diss = 0;
                asd = 0;
                nla = 0;
                median = 0;
            }  // end else        

            str = String.Format("ID:{0,5:D}  DISS:{1,9:F5}   PARM: {2,9:F5}   EXP:{3,9:F5}   HH:{4,7:D}  MEDIAN:{5,7:D}", id,diss,asd,nla,hh_tot,median);
            stra = id.ToString() + ",";
            iout.WriteLine(str);
            iout.WriteLine();
            iout.Flush();

            str = ("ACT : ");
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += String.Format("{0,9:F3}",hh_pct_act[i]);
            iout.WriteLine(str);
            iout.Flush();

            str = "EST : ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += String.Format("{0,9:F3}",hh_pct_est[i]);
            iout.WriteLine(str);
            iout.Flush();
            
            str = "ZACT: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += String.Format("{0,9:F3}",z_actual[i]);
            iout.WriteLine(str);
            iout.Flush();

            str = "ZEST: ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                str += String.Format("{0,9:F3}",z_est[i]);
            iout.WriteLine(str);
            iout.Flush();
           
            str = "ADJ : ";
            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
            {
                str += String.Format("{0,9:F3}",adj[i]);
                if (i < NUM_INCOME_GROUPS -1)
                    stra += (hh_pct_act[i]-hh_pct_est[i]).ToString() + ",";
            }  // end for i

            iout.WriteLine(str);
            stra += (hh_pct_act[NUM_INCOME_GROUPS-1]-hh_pct_est[NUM_INCOME_GROUPS-1]).ToString();
            iout.WriteLine();
            aout.WriteLine(stra);
            aout.Flush();
            iout.Flush();
            
        }     /* end procedure do_stats()*/

        /******************************************************************************/

        /* procedure erff() */

        /* error function */

        /* Revision History
            STR            Date       By   Description
            -------------------------------------------------------------------------
                    07/04/95    tb   Initial coding
            -------------------------------------------------------------------------

        */
        /*---------------------------------------------------------------------------*/

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
            double x,res,xsq,xnum,xden,xi;

            /*     COEFFICIENTS FOR 0.0 <= Y < .477 */
            double [] p = new double[] {113.8641541510502, 377.4852376853020,3209.377589138469, .1857777061846032,3.181123743870566};

            double [] q = new double[] {244.0246379344442, 1282.616526077372,2844.236833439171, 23.60129095234412};

            double [] p1 = new double[] {8.883149794388376, 66.11919063714163,298.6351381974001, 881.9522212417691,
                                            1712.047612634071, 2051.078377826071,1230.339354797997, 2.153115354744038E-8,.5641884969886701};
            double [] q1 = new double[] {117.6939508913125, 537.1811018620099,
                                            1621.389574566690, 3290.799235733460,4362.619090143247, 3439.367674143722,1230.339354803749, 15.74492611070983};

            /*     COEFFICIENTS FOR 4.0 < Y */

            double [] p2 = new double[] {-3.603448999498044E-01, -1.257817261112292E-01,-1.608378514874228E-02, -6.587491615298378E-04,
                                            -1.631538713730210E-02, -3.053266349612323E-01};
            double [] q2 = new double[] {1.872952849923460,    5.279051029514284E-01, 6.051834131244132E-02,  2.335204976268692E-03, 2.568520192289822};
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
			        
                    res = x * (xnum/xden);
                }     /* end if */
			     
                else
                    res = x * (p[2]/q[2]);
                iskip = 1;
            }     /* end if x */

            else if (x <= 4.0)
            {
                xsq = x * x;
				
                xnum = p1[7]*x + p1[8];
                xden = x + q1[7];
                for (i = 0; i < 7; ++i)
                {
                    xnum = xnum * x + p1[i];
                    xden = xden * x + q1[i];
                }  // end for i

                res = xnum/xden;
				
            }     /* end else if */

            else if (x < xlarge)
            {
                xsq = x * x;
                xi = 1.0 / xsq;
                xnum = p2[4]*xi + p2[5];
                xden = xi + q2[4];
                for (i = 0; i < 4; ++i)
                {
                    xnum = xnum * xi + p2[i];
                    xden = xden * xi + q2[i];
                }  // end for i
			   
                res = (sqrpi + xi * (xnum/xden)) / x;
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

            return(res);
        }     /* end procedure erff()*/

        /******************************************************************************/

        /* procedure MedianIncome() */

        /* perform median income calculations */

        // Revision History
        // Date			By	Description
        // ----------------------------------------------------------------------------
        // 07/11/02     tb  started this version
        // ----------------------------------------------------------------------------

        public int MedianIncome(int [] data)
        { 
            int sx,i;
            
            float xp,sx1,med_val,total;
            /*--------------------------------------------------------------------------*/
            sx = 0;
            sx1 = 0;
            med_val = 0;
            total = 0;

            for (i = 0; i < NUM_INCOME_GROUPS; ++i)
                total += data[i];

            if (total == 0)     /* if the total of the array is zero - return 0 for med*/
                return((int)med_val);

            xp = (float)(total * 50) /100;

            for (i = 1; i < 9;++i)
            {
                sx += data[i-1];
                sx1 = data[i-1] - sx + xp;
                if (xp - sx < 0)
                {
                    med_val = sx1/(float)data[i-1] * (float)(bounds[i]-bounds[i-1]) + (float)bounds[i-1] ;
                    break;
                }  // end if
                else if (xp - sx == 0)
                {
                    med_val = (float)bounds[i];
                    break;
                }  // end else if
            }     /* end for i*/
			  
            return((int)med_val);
        }     /* end MedianIncome() */

        //*********************************************************************************

        /* processParams() */

        // Build the table names from runtime parms

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/02/04   tb   initial recoding - moved verification steps to separate routine

        //   ------------------------------------------------------------------

        public void processParams()
        {
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                appSettings = config.AppSettings.Settings;
                connectionStrings = config.ConnectionStrings.ConnectionStrings;

                networkPath = String.Format(appSettings["networkPath"].Value);

                NUM_CTS = int.Parse(appSettings["NUM_CTS"].Value);
                NUM_INCOME_GROUPS = int.Parse(appSettings["NUM_INCOME_GROUPS"].Value);

                sqlConnection.ConnectionString = connectionStrings["ConcepDBConnectionString"].ConnectionString;
                this.sqlCommand.Connection = this.sqlConnection;

                TN.incomeCalibrationParms = String.Format(appSettings["incomeCalibrationParms"].Value);
                TN.baseYearIncome = String.Format(appSettings["baseYearIncome"].Value);
                

            }  // end try

            catch (ConfigurationErrorsException c)
            {
                throw c;
            }
        } // end procedure processParams()

        //************************************************************************************

        /* subroutine unerff() */

        /* error function  */

        /* Revision History
            STR            Date       By   Description
            -------------------------------------------------------------------------
                            07/02/95    tb   Initial coding
            -------------------------------------------------------------------------

        */
        /*---------------------------------------------------------------------------*/

        public double unerff(double pct)
        {

            int i;
            double p,z;
            double sq2;
              
            /*--------------------------------------------------------------------------*/
            z = 0;
            sq2 = System.Math.Sqrt((double)2);
            for (i = 1; i < 1000; ++i)
            {
                z = ((double)i - 500)/100;
                p = (double)((1 + erff((double)z/sq2))/2);
               
                if (p >= pct)
                    break;
            }     /* end for i */

            return(z);
        }     /* end procedure unerff()*/

        /*****************************************************************************/

        /* method writeToStatusBox() */
        /// <summary>
        /// Method to append a string on a new line to the status box.
        /// </summary>
    
        /* Revision History
        * 
        * STR             Date       By    Description
        * --------------------------------------------------------------------------
        *                07/28/03   df     Initial coding
        * --------------------------------------------------------------------------
        */
        public void writeToStatusBox( string status )
        {
            /* If we are running this method from primary thread, no marshalling is
                * needed. */
            if( !txtStatus.InvokeRequired )
            {
                // Append to the string (whose last character is already newLine)
                txtStatus.Text += status + Environment.NewLine;
                // Move the caret to the end of the string, and then scroll to it.
                txtStatus.Select( txtStatus.Text.Length, 0 );
                txtStatus.ScrollToCaret();
                Refresh();
            }   // end if
                /* Invoked from another thread.  Show progress asynchronously via a new
                    * delegate. */
            else
            {
                WriteDelegate write = new WriteDelegate( writeToStatusBox );
                Invoke( write, new object[] {status} );
            }   // end else
        }

        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }     // End method writeToStatusBox()

        // ****************************************************************************
	}
}
