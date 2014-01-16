/*****************************************************************************/
/*
  Source file name (path):  pasee_ct.sc
  Program: concep
  Version: 4
  Programmer: tb
  Description:  regional demographic characteristics estimates
                ct estimatation utility programs
                Version 4 introduces concep.config.exe to store all global constants, queries, table names and file names

Procedures:
  CTPM()
  CTPMAge()
  CTPMCols
  CTPMColsAge()
  CTPMResetCols()
  CTPMResetColsAge()
  CTPMResetRows()
  CTPMResetRowsAge()
  CTPMRows()
  CTPMRowsAge()
  
*/
/*****************************************************************************/
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Data;

namespace pasee
{
	public class pasee_ct
	{
		
		/*  CTPM() */

		/// Plus-minus routine for controlling ct surv pop estimates
		
		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

        public void CTPM(pasee P, int[, ,] sv, int nc, int sra_index, int[] ct_id)
        {
            vals[] ct_row_vals = new vals[60];
            vals[,] ct_col_vals = new vals[P.NUM_ETH, P.NUM_SEX];
            bool loop, row_match, col_match;
            int g, i, j;
            int count_loop, row_total, col_total;

            /*--------------------------------------------------------------------------*/

            count_loop = 0;
            loop = false;     /* init the loop control */

            col_match = row_match = false;

            for (g = 0; g < nc; ++g)
                ct_row_vals[g] = new vals();
            for (i = 0; i < P.NUM_ETH; ++i)
                for (j = 0; j < P.NUM_SEX; ++j)
                    ct_col_vals[i, j] = new vals();

            while (!loop)
            {
                ++count_loop;

                row_total = col_total = 0;
                for (g = 0; g < nc; ++g)
                {

                    row_match = true;
                    /* reset the cumulative elements of structure */
                    /* store the popest total for this ct in the control field */
                    ct_row_vals[g].control = P.pop_ct[ct_id[g], 0, 0].est_totals;

                    CTPMRows(sv, ct_row_vals, g, P.NUM_ETH, P.NUM_SEX);
                    row_total += ct_row_vals[g].control;

                    /* apply adjustment factors */
                    CTPMResetRows(sv, ct_row_vals, g, P.NUM_ETH, P.NUM_SEX);

                    /* recompute the cumulative elements of row structure */
                    CTPMRows(sv, ct_row_vals, g, P.NUM_ETH, P.NUM_SEX);

                    /* have we converged ? */
                    if (!ct_row_vals[g].adj_flag)
                        row_match = false;
                }     /* end for g */

                if (row_match && col_match && count_loop >= 5)
                {
                    loop = true;
                    continue;
                }  // end if

                col_match = true;
                for (i = 1; i < P.NUM_ETH; ++i)
                {
                    for (j = 1; j < P.NUM_SEX; ++j)
                    {
                        /* reset the cumulative elements of col structure */

                        ct_col_vals[i, j].control = P.pop_sra[sra_index, i, j].e_nmil_totals;
                        CTPMCols(sv, nc, ct_col_vals, i, j);

                        col_total += ct_col_vals[i, j].control;

                        /* apply adjustment factors */
                        CTPMResetCols(sv, nc, ct_col_vals, i, j);

                        /* recompute the culmulative elements of col structure */
                        CTPMCols(sv, nc, ct_col_vals, i, j);

                        /* have we converged ? */
                        if (!ct_col_vals[i, j].adj_flag)
                            col_match = false;
                    }  /* end for j */

                }     /* end for i */

                if (row_match && col_match && count_loop >= 5)
                    loop = true;
            }     /* end while ! loop */

        }     /* end CTPM()*/

        /******************************************************************************/

        /*  CTPMAge() */

        /// Plus-minus routine for controlling ct surv pop age estimates

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMAge(pasee P, int[,] sv, int nc, int sra_index, int[] ct_id, int i, int j)
        {
            vals[] ct_row_vals = new vals[60];
            vals[] ct_col_vals = new vals[101];
            bool loop, row_match, col_match;

            int ii, g;
            int count_loop, row_total, col_total;
            /*--------------------------------------------------------------------------*/

            count_loop = 0;
            loop = false;     /* init the loop control */
            col_match = row_match = false;

            for (g = 0; g < nc; ++g)
                ct_row_vals[g] = new vals();
            for (ii = 0; ii < P.NUM_AGE; ++ii)
                ct_col_vals[ii] = new vals();

            while (!loop)
            {
                ++count_loop;
                row_total = col_total = 0;
                for (g = 0; g < nc; ++g)
                {
                    row_match = true;
                    /* reset the cumulative elements of structure */

                    /* store the popest total for this ct in the control field */
                    ct_row_vals[g].control = P.pop_ct[ct_id[g], i, j].surv_totals;

                    CTPMRowsAge(sv, ct_row_vals, g, P.NUM_AGE);
                    row_total += ct_row_vals[g].control;

                    /* apply adjustment factors */
                    CTPMResetRowsAge(sv, ct_row_vals, g, P.NUM_AGE);

                    /* recompute the cumulative elements of row structure */
                    CTPMRowsAge(sv, ct_row_vals, g, P.NUM_AGE);

                    /* have we converged ? */
                    if (!ct_row_vals[g].adj_flag)
                        row_match = false;

                }     /* end for g */

                if (row_match && col_match && count_loop >= 5)
                {
                    loop = true;
                    continue;
                }  // end if

                col_match = true;
                for (ii = 0; ii < P.NUM_AGE; ++ii)
                {
                    /* reset the cumulative elements of col structure */

                    ct_col_vals[ii].control = P.pop_sra[sra_index, i, j].e_nmil[ii];
                    CTPMColsAge(sv, nc, ct_col_vals, ii);

                    col_total += ct_col_vals[ii].control;

                    /* apply adjustment factors */
                    CTPMResetColsAge(sv, nc, ct_col_vals, ii);

                    /* recompute the culmulative elements of col structure */
                    CTPMColsAge(sv, nc, ct_col_vals, ii);

                    /* have we converged ? */
                    if (!ct_col_vals[ii].adj_flag)
                        col_match = false;

                }     /* end for ii */

                if (row_match && col_match && count_loop >= 5)
                    loop = true;

            }     /* end while ! loop */

        }     /* end CTPMAge()*/

        /******************************************************************************/

        /* CTPMCols */

        /// Column computations for plus-minus routine for controlling ct surv pop age estimates

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMCols(int[, ,] sv, int nc, vals[,] col, int i, int j)
        {
            int g;

            /*--------------------------------------------------------------------------*/

            col[i, j].summ = 0;

            /* reset the sum variables */
            for (g = 0; g < nc; ++g)
                col[i, j].summ += sv[g, i, j];

            /* compute the adjustment factors */
            if (col[i, j].summ > 0)
                col[i, j].padj = (double)(col[i, j].control) / (double)col[i, j].summ;

            col[i, j].adj_flag = true;

        }     /* end CTPMCols*/

        /******************************************************************************/

        /*  CTPMColsAge() */

        /// col computations for plus-minus routine for controlling ct surv pop by age estimates

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMColsAge(int[,] sv, int nc, vals[] col, int i)
        {
            int g;
            /*--------------------------------------------------------------------------*/

            /* reset the cumulative elements of structure */
            col[i].summ = 0;

            /* reset the sum variables */
            for (g = 0; g < nc; ++g)
                col[i].summ += sv[g, i];

            /* compute the adjustment factors */
            if (col[i].summ > 0)
                col[i].padj = (double)(col[i].control) / (double)col[i].summ;

            col[i].adj_flag = true;

        }     /* end CTPMColsAge()*/

        /******************************************************************************/

        /*  CTPMResetCols() */

        /// recompute col elelemts for plus-minus routine for controlling ct surv pop estimates

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMResetCols(int[, ,] sv, int nc, vals[,] col, int i, int j)
        {
            int g;

            /*--------------------------------------------------------------------------*/
            for (g = 0; g < nc; ++g)
            {
                /* apply positive adjustment factor */
                sv[g, i, j] = (int)((double)sv[g, i, j] * col[i, j].padj + 0.5);
            }     /* end for g */

        }     /* end CTPMResetCols()*/

        /******************************************************************************/

        /*  CTPMResetColsAge() */

        /// recompute col elelemts for plus-minus routine for controlling ct surv pop by age estimates

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMResetColsAge(int[,] sv, int nc, vals[] col, int i)
        {
            int g;

            /*--------------------------------------------------------------------------*/
            for (g = 0; g < nc; ++g)
                sv[g, i] = (int)((double)sv[g, i] * col[i].padj + 0.5);

        }     /* end CTPMResetColsAge()*/

        /******************************************************************************/

        /*  CTPMResetRows() */

        /// recompute row elements for plus-minus routine for controlling ct surv pop estimates


        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMResetRows(int[, ,] sv, vals[] row, int g, int num_ethnic, int num_sex)
        {

            int i, j;

            /*--------------------------------------------------------------------------*/
            for (i = 1; i < num_ethnic; ++i)
            {
                for (j = 1; j < num_sex; ++j)
                    sv[g, i, j] = (int)((double)sv[g, i, j] * row[g].padj + 0.5);
            }     /* end for i */

        }     /* end CTPMResetRows()*/

        /******************************************************************************/

        /*  CTPMResetRowsAge() */
        /// recompute row elements for plus-minus routine for controlling ct surv pop estimates

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMResetRowsAge(int[,] sv, vals[] row, int g, int num_age)
        {
            int i;

            /*--------------------------------------------------------------------------*/
            for (i = 0; i < num_age; ++i)
            {
                sv[g, i] = (int)((double)sv[g, i] * row[g].padj + 0.5);
            }     /* end for i */

        }     /* end CTPMResetRowsAge()*/

        /******************************************************************************/

        /*  CTPMRows() */

        /// row computations for plus-minus routine for controlling ct migration estimates

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMRows(int[, ,] sv, vals[] row, int g, int num_ethnic, int num_sex)
        {
            int i, j;
            /*--------------------------------------------------------------------------*/
            row[g].summ = 0;

            /* reset the cumulative elements of structure */

            for (i = 1; i < num_ethnic; ++i)
            {
                for (j = 1; j < num_sex; ++j)
                {
                    row[g].summ += System.Math.Abs(sv[g, i, j]);
                }     /* end for j */
            }     /* end for i */

            /* compute the adjustment factors */
            if (row[g].summ > 0)
                row[g].padj = (double)row[g].control / (double)row[g].summ;

            row[g].adj_flag = true;

        }     /* end CTPMRows()*/

        /******************************************************************************/

        /* CTPMRowsAge() */

        /// row computations for plus-minus routine for controlling ct surv pop by age estimates

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void CTPMRowsAge(int[,] sv, vals[] row, int g, int num_age)
        {
            int i;
            /*--------------------------------------------------------------------------*/
            row[g].summ = 0;

            /* reset the cumulative elements of structure */

            for (i = 0; i < num_age; ++i)
                row[g].summ += System.Math.Abs(sv[g, i]);

            /* compute the adjustment factors */
            if (row[g].summ > 0)
                row[g].padj = (double)row[g].control / (double)row[g].summ;

            row[g].adj_flag = true;

        }     /* end CTPMRowsAge()*/

        /******************************************************************************/
		
	}     // end class
}     // end namespace
/* end source file pasee_ct.cs */
