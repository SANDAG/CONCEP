/* 
 * Source File: cmain.cs
 * Program: concep
 * Version 4.0
 * Programmer: tbe
 * Description:
 *      version 4 adds standardized configuration file for global vars, table names and query content
 *      version 3.5 adds processing for allocating 2010 census to series 13 mgras
 *      version 3.4 adds processing for allocating 2010 Census to MGRAs
 *      version 3.3 adds processing to do CT-level detail for HH
 *      version 3.2 adds processing to control GQ to DOF city totals
 *      version 3.1 uses hs and gq totals for POPEST rather than change files
 *	    version 3.0 handles SGRAs - lots of name changes, but no functionality changes
 *      version 2.5 is a recode of POPEST to do all the processing at MGRAs - eliminating the split-tract
 *      processing.  Pro:  straigtforward application of mgra hs changes - we aren't controlling to DOF HS anymore
 *                         so there aren't many good reasons to od split tracts.
 *                   Con:  controlling pop, gq and hhp is less "precise" due to the large number of MGRAs
 *                         in a city.
 * 
 *		CONCEP shell - all modules are in separate files
 * Things I've learned about this code
 *    1.  trying to replicate C typedefs and arrays of structures held me up
 *        for a couple of days.  Arrays of classes have to be declared and initialized
 *        check the big initialization loop in the btnRunPMGRA_Click()
 *	  2.  I've not yet figured out the equivalent of the old #define for constants
 *        #define has a new meaning and I can't find a way to list
 *        constants like MAX_MGRAS once.  I've had to put them in a couple of places
 *        because the whole idea of scope is confusing with the class definitions
 *    3.  the sql connection, command, execution and datareader stuff is fast
 *    4.  I've made a conscious effort to open and close connections between calls
 *        that's the way ADo is supposed to work if you use the widgets\
 *    5.  Main is a dummy start which only starts the application - the processing
 *        occurs as a result of event processing for btnRunPOPEST_Click() and btnRunPMGRA_Click() -- 
 *        this is like Vb or Access works
 *    6.  My current dilema is getting more than one source file to be recognized
*/

//Revision History
 //   Date       By   Description
 //   ------------------------------------------------------------------
 //   06/05/02   tb   initial coding
 //   06/06/02   tb   got PMGRA it running for 2001 
 //   06/10/02   tb   got POPEST running for 2001  
 //   06/11/02   tb   removed most of the operational code to separate modules
 //   06/11/04   tb   recode for version 2.5 POPEST
 //   02/01/05   tb   recode for version 3.0 to handle SGRAs
 //   06/21/06   tb   add processing to control GQ
 //   10/23/12   tb   changes for version 4
 //   ------------------------------------------------------------------


using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace Sandag.TechSvcs.RegionalModels
{
    public class concep : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Button btnExit;
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Button btnRunPOPEST;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnRunEstInc;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnRunControlVitals;
        private Button btnRunIncomeCalib;
        private Button btnRunDHH;
        private Button btnRunCensus;
        private Button btnRunControlGQ;
        private System.Windows.Forms.Button btnRunPASEE;

        public concep()
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
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.btnExit = new System.Windows.Forms.Button();
            this.btnRunPOPEST = new System.Windows.Forms.Button();
            this.btnRunPASEE = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnRunEstInc = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnRunControlGQ = new System.Windows.Forms.Button();
            this.btnRunCensus = new System.Windows.Forms.Button();
            this.btnRunDHH = new System.Windows.Forms.Button();
            this.btnRunIncomeCalib = new System.Windows.Forms.Button();
            this.btnRunControlVitals = new System.Windows.Forms.Button();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Red;
            this.btnExit.Font = new System.Drawing.Font("Book Antiqua", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.Location = new System.Drawing.Point(230, 628);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(96, 40);
            this.btnExit.TabIndex = 4;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnRunPOPEST
            // 
            this.btnRunPOPEST.BackColor = System.Drawing.Color.Teal;
            this.btnRunPOPEST.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunPOPEST.ForeColor = System.Drawing.Color.White;
            this.btnRunPOPEST.Location = new System.Drawing.Point(16, 194);
            this.btnRunPOPEST.Name = "btnRunPOPEST";
            this.btnRunPOPEST.Size = new System.Drawing.Size(192, 47);
            this.btnRunPOPEST.TabIndex = 9;
            this.btnRunPOPEST.Text = "POPEST";
            this.btnRunPOPEST.UseVisualStyleBackColor = false;
            this.btnRunPOPEST.Click += new System.EventHandler(this.btnRunPOPEST_Click);
            // 
            // btnRunPASEE
            // 
            this.btnRunPASEE.BackColor = System.Drawing.Color.Teal;
            this.btnRunPASEE.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunPASEE.ForeColor = System.Drawing.Color.White;
            this.btnRunPASEE.Location = new System.Drawing.Point(16, 247);
            this.btnRunPASEE.Name = "btnRunPASEE";
            this.btnRunPASEE.Size = new System.Drawing.Size(192, 48);
            this.btnRunPASEE.TabIndex = 10;
            this.btnRunPASEE.Text = "PASEE";
            this.btnRunPASEE.UseVisualStyleBackColor = false;
            this.btnRunPASEE.Click += new System.EventHandler(this.btnRunPASEE_Click);
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.label2.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.label2.Location = new System.Drawing.Point(8, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(426, 32);
            this.label2.TabIndex = 11;
            this.label2.Text = "Consolidated Characteristics Estimates Program";
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.label1.Font = new System.Drawing.Font("Book Antiqua", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(16, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(137, 32);
            this.label1.TabIndex = 1;
            this.label1.Text = "CONCEP";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Location = new System.Drawing.Point(8, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(150, 48);
            this.panel1.TabIndex = 12;
            // 
            // btnRunEstInc
            // 
            this.btnRunEstInc.BackColor = System.Drawing.Color.Teal;
            this.btnRunEstInc.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunEstInc.ForeColor = System.Drawing.Color.White;
            this.btnRunEstInc.Location = new System.Drawing.Point(16, 301);
            this.btnRunEstInc.Name = "btnRunEstInc";
            this.btnRunEstInc.Size = new System.Drawing.Size(192, 48);
            this.btnRunEstInc.TabIndex = 13;
            this.btnRunEstInc.Text = " HH Income";
            this.btnRunEstInc.UseVisualStyleBackColor = false;
            this.btnRunEstInc.Click += new System.EventHandler(this.btnRunEstInc_Click);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.btnRunControlGQ);
            this.panel2.Controls.Add(this.btnRunCensus);
            this.panel2.Controls.Add(this.btnRunDHH);
            this.panel2.Controls.Add(this.btnRunIncomeCalib);
            this.panel2.Controls.Add(this.btnRunPASEE);
            this.panel2.Controls.Add(this.btnRunEstInc);
            this.panel2.Controls.Add(this.btnRunPOPEST);
            this.panel2.Controls.Add(this.btnRunControlVitals);
            this.panel2.Location = new System.Drawing.Point(56, 104);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(224, 487);
            this.panel2.TabIndex = 14;
            // 
            // btnRunControlGQ
            // 
            this.btnRunControlGQ.BackColor = System.Drawing.Color.Teal;
            this.btnRunControlGQ.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunControlGQ.ForeColor = System.Drawing.Color.White;
            this.btnRunControlGQ.Location = new System.Drawing.Point(16, 14);
            this.btnRunControlGQ.Name = "btnRunControlGQ";
            this.btnRunControlGQ.Size = new System.Drawing.Size(192, 50);
            this.btnRunControlGQ.TabIndex = 15;
            this.btnRunControlGQ.Text = "Control GQ";
            this.btnRunControlGQ.UseVisualStyleBackColor = false;
            this.btnRunControlGQ.Click += new System.EventHandler(this.btnRunControlGQ_Click);
            // 
            // btnRunCensus
            // 
            this.btnRunCensus.BackColor = System.Drawing.Color.Teal;
            this.btnRunCensus.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunCensus.ForeColor = System.Drawing.Color.White;
            this.btnRunCensus.Location = new System.Drawing.Point(16, 408);
            this.btnRunCensus.Name = "btnRunCensus";
            this.btnRunCensus.Size = new System.Drawing.Size(192, 56);
            this.btnRunCensus.TabIndex = 16;
            this.btnRunCensus.Text = global::concep.Properties.Settings.Default.controlGqMainButtonText;
            this.btnRunCensus.UseVisualStyleBackColor = false;
            this.btnRunCensus.Click += new System.EventHandler(this.btnRunCensus_Click);
            // 
            // btnRunDHH
            // 
            this.btnRunDHH.BackColor = System.Drawing.Color.Teal;
            this.btnRunDHH.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunDHH.ForeColor = System.Drawing.Color.White;
            this.btnRunDHH.Location = new System.Drawing.Point(16, 355);
            this.btnRunDHH.Name = "btnRunDHH";
            this.btnRunDHH.Size = new System.Drawing.Size(192, 47);
            this.btnRunDHH.TabIndex = 15;
            this.btnRunDHH.Text = "Detailed HH Data";
            this.btnRunDHH.UseVisualStyleBackColor = false;
            this.btnRunDHH.Click += new System.EventHandler(this.btnRunDHH_Click);
            // 
            // btnRunIncomeCalib
            // 
            this.btnRunIncomeCalib.BackColor = System.Drawing.Color.Teal;
            this.btnRunIncomeCalib.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunIncomeCalib.ForeColor = System.Drawing.Color.White;
            this.btnRunIncomeCalib.Location = new System.Drawing.Point(16, 132);
            this.btnRunIncomeCalib.Name = "btnRunIncomeCalib";
            this.btnRunIncomeCalib.Size = new System.Drawing.Size(192, 56);
            this.btnRunIncomeCalib.TabIndex = 15;
            this.btnRunIncomeCalib.Text = "Income Calibration";
            this.btnRunIncomeCalib.UseVisualStyleBackColor = false;
            this.btnRunIncomeCalib.Click += new System.EventHandler(this.btnRunIncomeCalib_Click);
            // 
            // btnRunControlVitals
            // 
            this.btnRunControlVitals.BackColor = System.Drawing.Color.Teal;
            this.btnRunControlVitals.Font = new System.Drawing.Font("Book Antiqua", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunControlVitals.ForeColor = System.Drawing.Color.White;
            this.btnRunControlVitals.Location = new System.Drawing.Point(16, 70);
            this.btnRunControlVitals.Name = "btnRunControlVitals";
            this.btnRunControlVitals.Size = new System.Drawing.Size(192, 56);
            this.btnRunControlVitals.TabIndex = 14;
            this.btnRunControlVitals.Text = "Control Vitals";
            this.btnRunControlVitals.UseVisualStyleBackColor = false;
            this.btnRunControlVitals.Click += new System.EventHandler(this.btnControlVitals_Click);
            // 
            // concep
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.ClientSize = new System.Drawing.Size(430, 680);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel2);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "concep";
            this.Text = global::concep.Properties.Settings.Default.concepMainFormText;
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion


        [STAThread]
        static void Main()
        {
            Application.Run(new concep());
        }

        // ************************************************************************

        /*  btnRunPOPEST() */
      
        /// POPEST  Main
     
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------
        private void btnRunPOPEST_Click(object sender, System.EventArgs e)
        {
            PMGRA.pmgra pmgra = new PMGRA.pmgra();
            pmgra.Show();
        }

        //******************************************************************************

        private void btnExit_Click(object sender, System.EventArgs e)
        {
            Application.Exit();
        }

        private void btnRunPASEE_Click(object sender, System.EventArgs e)
        {
            pasee.pasee pasee = new pasee.pasee();
            pasee.Show();
        }

        private void btnRunEstInc_Click(object sender, System.EventArgs e)
        {
            estinc.estinc estinc = new estinc.estinc();
            estinc.Show();
        }

        private void btnControlVitals_Click(object sender, System.EventArgs e)
        {
            CV.ControlVitals controlVitals = new CV.ControlVitals();
            controlVitals.Show();
        }

        private void btnRunControlGQ_Click(object sender, System.EventArgs e)
        {
            CGQ.controlGQ controlGQ = new CGQ.controlGQ();
            controlGQ.Show();
        }

        private void btnRunIncomeCalib_Click(object sender, System.EventArgs e)
        {
            IncomeCalib.IncCalib IncCalib = new IncomeCalib.IncCalib();
            IncCalib.Show();
        }

        private void btnRunDHH_Click(object sender, System.EventArgs e)
        {
            popestDHH.pdhh pdhh = new popestDHH.pdhh();
            pdhh.Show();
        }

        private void btnRunCensus_Click(object sender, System.EventArgs e)
        {
           CENSUS.census census = new CENSUS.census();
            census.Show();
        }

      
    }
}