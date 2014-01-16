/*****************************************************************************/
/*
  Source file name (path):  pasee_sra.cs
  Program: concep
  Version: 4
  Programmer: tb
  Description:  regional demographic characteristics estimates
                sra estimatation utility programs
                Version 4 introduces concep.config.exe to store all global constants, queries, table names and file names

Procedures:
  SRAAgePM
  SRAAgePMCols()
  SRAAgePMResetCols()
  SRAAgePMResetRows()
  SRAAgePMRows()
  SRAPM()
  SRAPMCols()
  SRAPMResetCols()
  SRAPMResetRows()
  SRAPMRows()
*/
//******************************************************************************************
 
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Data;
using pasee;

namespace pasee
{
	public class pasee_sra
	{
		/* SRAAgePM()*/

		/// Plus-minus routine for controlling sra pop by age estimates
		
		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAAgePM(pasee P)
		{
			int g,i,j,k;
			int count_loop,row_total,col_total;
			
			bool loop,row_match,col_match,first_time = true;
			/*--------------------------------------------------------------------------*/
			  
			for (i = 1; i < P.NUM_ETH; ++i)
			{
				for (j = 1; j < P.NUM_SEX; ++j)
				{
					count_loop = 0;
					loop = false;     /* init the loop control */
					
					col_match = row_match = false;

					while (!loop)
					{
						++count_loop;
			 
						row_total = col_total = 0;

						for (g = 0; g < P.NUM_SRA; ++g)
						{
							P.row_age_vals[g].control = 0;
							P.row_age_vals[g].adj_flag = false;
							P.row_age_vals[g].nadj = 0;
							P.row_age_vals[g].padj = 0;
							P.row_age_vals[g].sumabs = 0;
							P.row_age_vals[g].summ = 0;
							row_match = true;
							/* reset the cumulative elements of structure */

							if (P.sra_list[g] == 16 || P.sra_list[g] == 43)
								++g;

							SRAAgePMRows(P,P.row_age_vals,g,i,j);
							row_total += P.row_age_vals[g].control;
							
							/* apply adjustment factors */
							SRAAgePMResetRows(P,P.row_age_vals,g,i,j);

							/* recompute the cumulative elements of row structure */
							SRAAgePMRows(P,P.row_age_vals,g,i,j);

							/* have we converged ? */
							if (!P.row_age_vals[g].adj_flag)
								row_match = false;
				        
						}     /* end for g */

						if (row_match && col_match && count_loop >= 5)
						{
							loop = true;
							continue;
						}

						col_match = true;
						for (k = 0; k < P.NUM_AGE; ++k)
						{
							/* reset the cumulative elements of col structure */
				           if (first_time)
							   P.col_age_vals[k] = new vals();
							SRAAgePMCols(P,P.col_age_vals,k,i,j);

							col_total += P.col_age_vals[k].control;
										 
							/* apply adjustment factors */
							SRAAgePMResetCols(P,P.col_age_vals,k,i,j);

							/* recompute the culmulative elements of col structure */
							SRAAgePMCols(P,P.col_age_vals,k,i,j);

							/* have we converged ? */
							if (!P.col_age_vals[k].adj_flag)
								col_match = false;
				        
						}     /* end for k */
						first_time = false;

						if (row_match && col_match && count_loop >= 5)
							loop = true;

					}     /* end while ! loop */

				}     /* end for j */
			  
			}     /* end for i */

		}     /* end SRAAgePM()*/

		/******************************************************************************/

		/*  SRAAgePMCols() */

		/* col computations for plus-minus routine for nomig controlling sra P.pop by  age estimates */

		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAAgePMCols(pasee P,vals[] col,int k,int i,int j)
		
		{
			int g;
			/*--------------------------------------------------------------------------*/

			/* reset the cumulative elements of structure */
			col[k].summ = 0;     /* init the sum */
			col[k].control = P.pop[i,j].e_nmil[k];     /* control is age grroup total */

			for (g = 0; g < P.NUM_SRA; ++g)     /* sum over sra */
				col[k].summ += P.pop_sra[g,i,j].e_nmil[k];

			/* compute the adjustment factors */        
			if (col[k].summ > 0)
				col[k].padj = (double)(col[k].control)/ (double)col[k].summ;

			col[k].adj_flag = true;     /* set the adjustment made flag */
		
		}     /* end  SRAAgePMCols()*/

		/******************************************************************************/

		/* SRAAgePMResetCols() */

		/* recompute col elements for plus-minus routine for controlling sra 
		   P.pop by age estimates */

		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAAgePMResetCols(pasee P,vals [] col,int k,int i,int j)
		{
			int g;

			/*--------------------------------------------------------------------------*/
			for (g = 0; g < P.NUM_SRA; ++g)
				P.pop_sra[g,i,j].e_nmil[k] = (int)((double)P.pop_sra[g,i,j].e_nmil[k] * col[k].padj + 0.5);

		}     /* end SRAAgePMResetCols()*/

		/******************************************************************************/

		/*  SRAAgePMResetRows() */

		/* recompute row elements for plus-minus routine for controlling sra 
		P.pop by age estimates */

		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAAgePMResetRows(pasee P, vals [] row,int g,int i,int j)
		{
			int k;

			/*--------------------------------------------------------------------------*/
			for (k = 0; k < P.NUM_AGE; ++k)
				P.pop_sra[g,i,j].e_nmil[k] = (int)((double)P.pop_sra[g,i,j].e_nmil[k] * row[g].padj + 0.5);

		}     /* end  SRAAgePMResetRows()*/

		/******************************************************************************/

		/* SRAAgePMRows() */

		/* row computations for plus-minus routine for nomig controlling sra P.pop by estimates */

		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAAgePMRows(pasee P, vals [] row,int g,int i,int j)
		{
			int k;
			/*--------------------------------------------------------------------------*/

			row[g].summ = 0;
			row[g].control = P.pop_sra[g,i,j].e_nmil_totals;
			  
			/* reset the cumulative elements of structure */

			for (k = 0; k < P.NUM_AGE; ++k)
				row[g].summ += P.pop_sra[g,i,j].e_nmil[k];

			/* compute the adjustment factors */
			if (row[g].summ > 0)
				row[g].padj = (double)(row[g].control)/ (double)row[g].summ;

			row[g].adj_flag = true;
		 
		}     /* end SRAAgePMRows()*/

		/******************************************************************************/

		/* SRAPM */

		/* plus-minus routine for controlling sra survived P.pop estimates */

		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAPM(pasee P)
		{
			int g,i,j;
			bool loop,row_match,col_match;
			int count_loop,row_total,col_total;
			bool first_time = true;
			/*--------------------------------------------------------------------------*/
		  
			count_loop = 0;
			loop = false;     /* init the loop control */
			col_match = false;

			while (!loop)
			{
				++count_loop;
			
				row_total = col_total = 0;
				row_match = true;
				for (g = 0; g < P.NUM_SRA; ++g)
				{
		       
					P.row_vals[g].control = 0;
					P.row_vals[g].adj_flag = false;
					P.row_vals[g].nadj = 0;
					P.row_vals[g].padj = 0;
					P.row_vals[g].sumabs = 0;
					P.row_vals[g].summ = 0;
				
					/* skip military special sras */
					if (P.sra_list[g] == 16 || P.sra_list[g] == 43)
						++g;

					SRAPMRows(P,P.row_vals,g);
					row_total += P.row_vals[g].control;
				
					/* apply adjustment factors */
					SRAPMResetRows(P,P.row_vals,g);

					/* recompute the cumulative elements of row structure */
					SRAPMRows(P,P.row_vals,g);

					/* have we converged ? */
					if (!P.row_vals[g].adj_flag)
					row_match = false;
		        
				}     /* end for g */

				if (row_match && col_match)
				{
					loop = true;
					continue;
				}

				col_match = true;
				for (i = 1; i < P.NUM_ETH; ++i)
				{
					for (j = 1; j < P.NUM_SEX; ++j)
					{
						if (first_time)     //init this class only one time
                            P.col_vals[i,j] = new vals();
						/* reset the cumulative elements of col structure */
			           
						SRAPMCols(P,P.col_vals,i,j);

						col_total += P.col_vals[i,j].control;
					
						/* apply adjustment factors */
						SRAPMResetCols(P,P.col_vals,i,j);

						/* recompute the culmulative elements of col structure */
						SRAPMCols(P,P.col_vals,i,j);

						/* have we converged ? */
						if (!P.col_vals[i,j].adj_flag)
							col_match = false;
		        
					}  /* end for j */

				}     /* end for i */
				first_time = false;     //reset the class init flag for col_vals

				if (row_match && col_match && count_loop >= 5)
				   loop = true;

			
			}     /* end while ! loop */

		}     /* end SRAPM()*/

		/******************************************************************************/

		/* SRAPMCols() */

		/* col computations for plus-minus routine for nomig option controlling sra surv P.pop estimates */

		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAPMCols(pasee P, vals [,] col,int i,int j)
		
		{
			int g;
			/*--------------------------------------------------------------------------*/

			/* reset the cumulative elements of structure */
			col[i,j].summ = 0;
			col[i,j].control = P.pop[i,j].e_nmil_totals;

			/* reset the sum variables */
			for (g = 0; g < P.NUM_SRA; ++g)
				col[i,j].summ += P.pop_sra[g,i,j].e_nmil_totals;

			/* compute the adjustment factors */        
			if (col[i,j].summ > 0)
				col[i,j].padj = (double)(col[i,j].control)/ (double)col[i,j].summ;

			col[i,j].adj_flag = true;

		}     /* end SRAPMCols()*/

		/******************************************************************************/

		/* SRAPMResetCols() */

		///	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAPMResetCols(pasee P,vals [,] col,int i,int j)
		{
			int g;

			/*--------------------------------------------------------------------------*/
			for (g = 0; g < P.NUM_SRA; ++g)
			{
				/* apply  positive adjustment factor */
				P.pop_sra[g,i,j].e_nmil_totals = (int)((double)P.pop_sra[g,i,j].e_nmil_totals * col[i,j].padj + 0.5);
			}     /* end for g */

		}     /* end SRAPMResetCols()*/

		/******************************************************************************/

		/* SRAPMResetRows() */

		/* recompute row elelemts for plus-minus routine for nomig option for controlling sra P.pop estimates */

		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAPMResetRows(pasee P,vals[] row,int g)
		{
			int i,j;

			/*--------------------------------------------------------------------------*/
			for (i = 1; i < P.NUM_ETH; ++i)
			{
				for (j = 1; j < P.NUM_SEX; ++j)
					P.pop_sra[g,i,j].e_nmil_totals = (int)((double)P.pop_sra[g,i,j].e_nmil_totals * row[g].padj + 0.5);
			}     /* end for i */

		}     /* end SRAPMResetRows()*/

		/******************************************************************************/

		/* SRAPMRows() */

		/* row computations for plus-minus routine for nomig option controlling sra P.pop estimates */

		//	Revision History
		//   Date       By   Description
		//   ------------------------------------------------------------------
		//   06/12/02   tb   started initial coding
		//   ------------------------------------------------------------------

		public void SRAPMRows(pasee P,vals[] row,int g)
		{
			int i,j;

			/*--------------------------------------------------------------------------*/
			row[g].summ = 0;
			row[g].control = P.pop_sra[g,0,0].popest_est_totals - P.pop_sra[g,0,0].popest_mil_est_totals;
		  
			/* reset the cumulative elements of structure */

			for (i = 1; i < P.NUM_ETH; ++i)
			{
				for (j = 1; j < P.NUM_SEX; ++j)
				{
					row[g].summ += P.pop_sra[g,i,j].e_nmil_totals;
				}     /* end for j */
			}     /* end for i */

			/* compute the adjustment factors */
			if (row[g].summ > 0)
				row[g].padj = (double)(row[g].control)/ (double)row[g].summ;

			row[g].adj_flag = true;

		}     /* end SRAPMRows()*/

		/******************************************************************************/


	}     //end class pasee_sra

}     //end namespace

/* end soure file pasee_sra.cs */