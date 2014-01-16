/*****************************************************************************/
/*
  Source file name (path): pasee_compute.cs
  Program: concep
  Version: 3.5
  Programmer: tb
  Description:  pasee module of concep 
				detailed characteristics model - initial pop estimates routines

Procedures:
  AdjustPop()
  NetMig()
  PopEstimate()
  PopEstimateCt()
  PopEstimateSRA()
  SpecialEstimateMain()
  SRAMig()
  SurvivePop()
  SurvivePopCt()
  SurvivePopSRA()
*/
/*****************************************************************************/
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Data;
using pasee;

namespace pasee
{
    public class pasee_compute
    {
        /* AdjustPop() */

        /// Adjust population for deaths and births

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void AdjustPop(pasee P)
        {
            int g, i, h, j, k;
            P.WriteToStatusBox("ADJUSTING POPULATION FOR DEATHS AND MILITARY");

            /* compute totals */
            for (g = 0; g < P.NUM_CTS; ++g)     /* use one main loop to do ct's and sra's */
            {
                P.DoMilBasepTotals(1, P.pop_mil_ct, g, P.NUM_CTS);
                if (g < P.NUM_SRA)      /* if index is in sra range */
                    P.DoMilBasepTotals(2, P.pop_mil_sra, g, P.NUM_SRA);
            }  // end for g

            /* adjust population for deaths and military*/

            for (i = 1; i < P.NUM_ETH; ++i)
            {
                for (j = 1; j < P.NUM_SEX; ++j)
                {
                    for (k = 0; k < P.NUM_AGE; ++k)
                    {
                        /* base adjusted pop is base less deaths, and mil pop */
                        if (P.pop[i, j].basep_adj == null)
                            P.pop[i, j].basep_adj = new int[P.NUM_AGE];
                        P.pop[i, j].basep_adj[k] = P.pop[i, j].basep[k] - P.pop[i, j].deaths[k] - P.pop_mil_ct[P.NUM_CTS, i, j, k].b_mil_bases - P.pop_mil_ct[P.NUM_CTS, i, j, k].b_mil_gen;

                        /* process the sras */
                        for (g = 0; g < P.NUM_SRA; ++g)
                        {
                            if (P.pop_sra[g,i, j].basep_adj == null)
                                P.pop_sra[g,i, j].basep_adj = new int[P.NUM_AGE];
                            P.pop_sra[g, i, j].basep_adj[k] = P.pop_sra[g, i, j].basep[k] -P.pop_sra[g, i, j].deaths[k] -P.pop_mil_sra[g, i, j, k].b_mil_bases - P.pop_mil_sra[g, i, j, k].b_mil_gen;

                            /* constrain to 0 or set to zero for two military bases */
                            if (P.pop_sra[g, i, j].basep_adj[k] < 0 || P.sra_list[g] == 16 || P.sra_list[g] == 43)
                                P.pop_sra[g, i, j].basep_adj[k] = 0;
                        }  // end for g

                        /* process the ct */
                        for (h = 0; h < P.NUM_CTS; ++h)
                        {
                            if (P.pop_ct[h,i, j].basep_adj == null)
                                P.pop_ct[h,i, j].basep_adj = new int[P.NUM_AGE];
                            P.pop_ct[h, i, j].basep_adj[k] = P.pop_ct[h, i, j].basep[k] - P.pop_ct[h, i, j].deaths[k] -P.pop_mil_ct[h, i, j, k].b_mil_bases - P.pop_mil_ct[h, i, j, k].b_mil_gen;

                            /* constrain to 0 or set to zero for two military bases */
                            if (P.pop_ct[h, i, j].basep_adj[k] < 0 || P.ct_list[h, 1] == 16 || P.ct_list[h, 1] == 43)
                                P.pop_ct[h, i, j].basep_adj[k] = 0;
                        }  // end for h
                    }  // end for k
                }  // end for j
            }   // end for i

            P.DoStructTotals(P.pop, 1);    /* base pop totals */
            P.DoStructTotals(P.pop, 2);    /* adjusted pop totals */

            //these are overloaded DoStructTotals calls
            for (g = 0; g < P.NUM_SRA; ++g)
            {
                P.DoStructTotals(P.pop_sra, g, 1);     /* call totals routine */
                P.DoStructTotals(P.pop_sra, g, 2);
            }  // end for g
            for (g = 0; g < P.NUM_CTS; ++g)
            {
                P.DoStructTotals(P.pop_ct, g, 1);
                P.DoStructTotals(P.pop_ct, g, 2);
            }  // end for g
        }     /* end AdjustPop()*/

        //********************************************************************************************************

        /* NetMig() */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void NetMig(pasee P)
        {
            int i, j, k, ir, jr, kr, adj;
            int newtot_total;
            string str = "";
            Random ran = new Random(0);
            /*-------------------------------------------------------------------------*/

            // compute regional totals from popest vars
            P.WriteToStatusBox("COMPUTING NET MIGRATION");
            P.popchg = P.popest_estimate - P.popest_base;     /* total change in population */
            P.totmig = P.popchg - P.births_total + P.deaths_total;     /* total migration */
            P.milmig = P.popest_mil_est_total - P.popest_mil_basep_total;     /* military migration */
            P.nmilmig_total = P.totmig - P.milmig;     /* nonmilitary migration */

            FileStream foua;        // file stream class for age output

            // open output file
            try
            {
                foua = new FileStream(P.networkPath + "mig1", FileMode.Create);
            }
            catch (FileNotFoundException exc)
            {
                MessageBox.Show(exc.Message + " Error Opening Output File");
                return;
            }
            //assign a wrapper for writing strings to ascii
            StreamWriter fouaw = new StreamWriter(foua);
            fouaw.AutoFlush = true;
            fouaw.WriteLine("initial mig distribution by ethnicity and sex : nmilmig_total = " + P.nmilmig_total);

            // distribute implied migration to age and sex
            for (i = 1; i < P.NUM_ETH; i++)
            {
                for (j = 1; j < P.NUM_SEX; j++)
                {
                    P.pop[i, j].nmilmig = (int)((double)P.nmilmig_total * P.mig_dist[i, j] + 0.5);
                    fouaw.WriteLine("Ethnicity = " + i.ToString() + " Sex = " + j.ToString() + "factor = " + P.mig_dist[i, j].ToString() + " mig = " + P.pop[i, j].nmilmig);
                }  // end for j
            }  // end for i

            fouaw.WriteLine(" age distribution calc ");
            for (i = 1; i < P.NUM_ETH; ++i)
            {

                for (j = 1; j < P.NUM_SEX; ++j)
                {
                    P.pop[i, j].netmig_tot = 0;     /* zero the total */
                    /* compute net migration */
                    str = i + "," + i + ",";
                    for (k = 0; k < P.NUM_AGE; ++k)
                    {
                        /* */
                        P.pop[i, j].netmig[k] = (int)((double)P.pop[i, j].surv[k] * P.mig_rates[i, j, k] + .5);
                        str += P.pop[i, j].netmig[k] + ",";
                        P.pop[i, j].netmig_tot += P.pop[i, j].netmig[k];
                    }     /* end for k */
                    fouaw.WriteLine(str);
                }     /* end for j */
            }     /* end for i */

            newtot_total = 0;     /* zero the revised total */
            P.pop[0, 0].netmig_tot = 0;     /* ditto for age and sex totals */

            /* control net mig to nmilmig total */
            P.pop[0, 0].newtot = 0;
            //fouaw.WriteLine("computing regional adjustments");
            for (i = 1; i < P.NUM_ETH; ++i)
            {
                P.pop[i, 0].newtot = 0;

                for (j = 1; j < P.NUM_SEX; ++j)
                {
                    P.pop[0, j].newtot = 0;
                    P.pop[i, j].newtot = 0;

                    ///* compute regional adjustment for migration by sex and eth */
                    //if (P.pop[i, j].netmig_tot != 0)
                    //    P.pop[i, j].regadj = (double)P.pop[i, j].nmilmig / (double)P.pop[i, j].netmig_tot;
                    //str = i + "," + i + "," + P.pop[i, j].regadj + ",";
                    for (k = 0; k < P.NUM_AGE; ++k)
                    {
                        /* control age, sex and eth estimates by regional total */
                        //P.pop[i, j].netmig[k] = (int)(0.5 + (double)P.pop[i, j].netmig[k] * P.pop[i, j].regadj);
                        //str += P.pop[i, j].netmig[k] + ",";
                        /* compute the totals also */
                        P.pop[i, j].newtot += P.pop[i, j].netmig[k];
                        P.pop[0, 0].newtot += P.pop[i, j].netmig[k];
                        P.pop[0, j].newtot += P.pop[i, j].netmig[k];
                        P.pop[i, 0].newtot += P.pop[i, j].netmig[k];
                    }     /* end for k */
                    //fouaw.WriteLine(str);
                    newtot_total += P.pop[i, j].newtot;
                }     /*end for j */
            }     /* end for i */

            /* reconcile the newtot_total to nmilmig with random assignments */

            while (newtot_total != P.nmilmig_total)
            {
                // exclude controlling from all but hisp, white, black and asian and restrict ages by value and
                // by some ages
                ir = ran.Next(1, P.NUM_ETH);
                if (ir == 4 || ir > 5)
                    continue;
                jr = ran.Next(1, P.NUM_SEX);
                kr = ran.Next(1, 79);
                if (newtot_total < P.nmilmig_total)
                    adj = 1;
                else
                    adj = -1;
                P.pop[ir, jr].netmig[kr] += adj;
                P.pop[ir, jr].newtot += adj;
                P.pop[0, 0].newtot += adj;
                P.pop[0, jr].newtot += adj;
                P.pop[ir, 0].newtot += adj;

                newtot_total += adj;
            }   // end while
            fouaw.WriteLine("after controlling");
            /* recompute netmig totals */
            for (i = 0; i < P.NUM_ETH; ++i)
            {
                for (j = 0; j < P.NUM_SEX; ++j)
                {
                    str = i + "," + i + ",";
                    P.pop[i, j].netmig_tot = 0;
                    for (k = 0; k < P.NUM_AGE; ++k)
                        str += P.pop[i, j].netmig[k] + ",";
                    fouaw.WriteLine(str);
                }     /* end for j */
            }     /* end for i */
            fouaw.Close();
            foua.Close();
            P.DoStructTotals(P.pop, 6);     /* call the totaling proc with parm = 6 for netmig totals */

        }     /* end  net_mig() */

        //**************************************************************************************************

        // PopEstimate()
        /* compute total pop estimates */
        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void PopEstimate(pasee P)
        {
            int i, j, k;
            P.WriteToStatusBox("COMPUTING REGIONAL TOTAL POPULATION ESTIMATE");
            for (i = 1; i < P.NUM_ETH; ++i)
            {
                for (j = 1; j < P.NUM_SEX; ++j)
                {
                    for (k = 0; k < P.NUM_AGE; ++k)
                    {
                        P.pop[i, j].e_nmil[k] = P.pop[i, j].surv[k] + P.pop[i, j].netmig[k];
                        if (P.pop[i, j].e_nmil[k] < 0)
                        {
                            P.pop[i, j].e_nmil[k] = 0;
                        }  // end if
                        P.pop[i, j].est[k] = P.pop[i, j].e_nmil[k] +
                            P.pop_mil_ct[P.NUM_CTS, i, j, k].est;
                    }  // end for k
                }  // end for j
            }  // end for i
            P.DoStructTotals(P.pop, 3);     /* e_nmil totals */
            P.DoStructTotals(P.pop, 4);     /* est totals */
        }  // end procedure PopEstimate()

        //********************************************************************************************************
        // PopEstimateCt()

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void PopEstimateCt(pasee P, int lyear)
        {
            int g, i, j, k;

            /*--------------------------------------------------------------------------*/
            P.WriteToStatusBox("COMPUTING CT POPULATION ESTIMATES");
            SurvivePopCt(P, lyear);

            /* store the ct popest estimates and zero the accums */
            for (g = 0; g < P.NUM_CTS; ++g)
            {
                for (i = 0; i < P.NUM_ETH; ++i)
                {
                    for (j = 0; j < P.NUM_SEX; ++j)
                    {
                        P.pop_ct[g, i, j].est_totals = 0;
                        for (k = 0; k < P.NUM_AGE; ++k)
                            P.pop_ct[g, i, j].e_nmil_totals = 0;
                    }  // end for j
                }  // end for i
            }  // end for g

            for (i = 1; i < P.NUM_ETH; ++i)
            {
                for (j = 1; j < P.NUM_SEX; ++j)
                {
                    for (k = 0; k < P.NUM_AGE; ++k)
                    {
                        for (g = 0; g < P.NUM_CTS; ++g)
                        {
                            P.pop_ct[g, i, j].est[k] = P.pop_ct[g, i, j].e_nmil[k] + P.pop_mil_ct[g, i, j, k].est;
                            P.pop_ct[g, i, j].est[0] = P.pop_ct[g, i, j].births;
                            if (P.pop_ct[g, i, j].est[k] < 0)
                                P.pop_ct[g, i, j].est[k] = 0;
                            P.pop_ct[g, i, j].netmig[k] = P.pop_ct[g, i, j].e_nmil[k] - P.pop_ct[g, i, j].surv[k];

                        }  // end for g
                    }  // end for k
                }  // end for j
            }  // end for i

            for (g = 0; g < P.NUM_CTS; ++g)
            {
                P.DoStructTotals(P.pop_ct, g, 3);
                P.DoStructTotals(P.pop_ct, g, 4);
                P.DoStructTotals(P.pop_ct, g, 6);
            }  // end for g
        }  // end procedure PopEstimateCt()

        /******************************************************************************/
        /*  PopEstimateSra() */

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------
        public void PopEstimateSRA(pasee P)
        {
            int fillid, g, i, j, k, ret = 0;
            int[] row_passer = new int[P.NUM_CTS];
            int[] col_passer = new int[P.NUM_AGE];
            int[,] matrx_passer = new int[P.NUM_CTS, P.NUM_AGE];

            P.WriteToStatusBox("Computing SRA Population Estimates");

            // store the sra popest estimates and zero the accums
            for (g = 0; g < P.NUM_SRA; g++)
            {
                P.row_vals[g] = new vals();
                P.row_age_vals[g] = new vals();
                for (i = 0; i < P.NUM_ETH; i++)
                {
                    for (j = 0; j < P.NUM_SEX; j++)
                    {
                        P.pop_sra[g, i, j].popest_est_totals = P.pop_sra[g, i, j].est_totals;
                        P.pop_sra[g, i, j].est_totals = 0;
                    }  // end for j
                }  // end for i
            }  // end for g
            for (i = 1; i < P.NUM_ETH; i++)
            {
                for (j = 1; j < P.NUM_SEX; j++)
                {
                    for (k = 0; k < P.NUM_AGE; k++)
                    {
                        for (g = 0; g < P.NUM_SRA; g++)
                        {
                            P.pop_sra[g, i, j].popest_est[k] = P.pop_sra[g, i, j].est[k];
                            P.pop_sra[g, i, j].e_nmil[k] = P.pop_sra[g, i, j].surv[k] +
                                P.pop_sra[g, i, j].netmig[k];
                            if (P.pop_sra[g, i, j].e_nmil[k] < 0)
                                P.pop_sra[g, i, j].e_nmil[k] = 0;
                            P.pop_sra[g, i, j].est[k] = P.pop_sra[g, i, j].e_nmil[k];
                        }  // end for g
                    }  // end for k
                }  // end for j
            }  // end for i

            // get totals before controlling
            for (g = 0; g < P.NUM_SRA; g++)
            {
                P.DoStructTotals(P.pop_sra, g, 3);
                P.DoStructTotals(P.pop_sra, g, 4);
            }  // end for g

            // trying to use update for controlling instead of PM
            // build the matrix with sra survived pop using regional controls by eth and sex for
            // col controls and sra e_nmil_total for row controls
            Array.Clear(row_passer, 0, row_passer.Length);
            Array.Clear(col_passer, 0, col_passer.Length);
            Array.Clear(matrx_passer, 0, matrx_passer.Length);
            fillid = 0;
            for (g = 0; g < P.NUM_SRA; g++)
            {
                row_passer[g] = P.pop_sra[g, 0, 0].popest_est_totals -
                    P.pop_sra[g, 0, 0].popest_mil_est_totals;
                fillid = 0;
                for (i = 1; i < P.NUM_ETH; i++)
                {
                    for (j = 1; j < P.NUM_SEX; j++)
                    {
                        col_passer[fillid] = P.pop[i, j].e_nmil_totals;
                        matrx_passer[g, fillid++] = P.pop_sra[g, i, j].surv_totals;
                    }  // end for j
                }  // end for i
            }  // end for g
            ret = CU.cUtil.update(P.NUM_SRA, 16, matrx_passer, row_passer, col_passer);

            //restore the updated info in the surv array
            for (g = 0; g < P.NUM_SRA; ++g)
            {
                fillid = 0;
                for (i = 1; i < P.NUM_ETH; ++i)
                {
                    for (j = 1; j < P.NUM_SEX; ++j)
                    {
                        P.pop_sra[g, i, j].e_nmil_totals = matrx_passer[g, fillid++];
                    }     // end for j
                }     //end for i
            }     // end for g

            // control the age groups with an sra by ethnic and sex with col controls being the
            // regional age totals
            for (i = 1; i < P.NUM_ETH; ++i)
            {
                for (j = 1; j < P.NUM_SEX; ++j)
                {
                    Array.Clear(row_passer, 0, row_passer.Length);
                    Array.Clear(col_passer, 0, col_passer.Length);
                    Array.Clear(matrx_passer, 0, matrx_passer.Length);
                    for (g = 0; g < P.NUM_SRA; ++g)
                    {
                        row_passer[g] = P.pop_sra[g, i, j].e_nmil_totals;
                        for (k = 0; k < P.NUM_AGE; ++k)
                        {
                            matrx_passer[g, k] = P.pop_sra[g, i, j].surv[k];
                        }     // end for 
                    }    // end for g

                    for (k = 0; k < P.NUM_AGE; ++k)
                        col_passer[k] = P.pop[i, j].e_nmil[k];

                    ret = CU.cUtil.update(P.NUM_SRA, 101, matrx_passer, row_passer, col_passer);

                    // restore for this i,j combo
                    for (g = 0; g < P.NUM_SRA; ++g)
                        for (k = 0; k < P.NUM_AGE; ++k)
                            P.pop_sra[g, i, j].e_nmil[k] = matrx_passer[g, k];
                }     // end for j
            }     // end for i

            for (g = 0; g < P.NUM_SRA; ++g)
            {
                for (i = 1; i < P.NUM_ETH; ++i)
                    for (j = 1; j < P.NUM_SEX; ++j)
                        for (k = 0; k < P.NUM_AGE; ++k)
                        {
                            P.pop_sra[g, i, j].est[k] = P.pop_sra[g, i, j].e_nmil[k] + P.pop_mil_sra[g, i, j, k].est;
                            P.pop_sra[g, i, j].netmig[k] = P.pop_sra[g, i, j].e_nmil[k] - P.pop_sra[g, i, j].surv[k];
                        }  // end for k

                P.pop_sra[g, 0, 0].e_nmil_totals = 0;
                P.pop_sra[g, 0, 0].est_totals = 0;
                for (k = 0; k < P.NUM_AGE; ++k)
                {
                    P.pop_sra[g, 0, 1].est[k] = 0;
                    P.pop_sra[g, 0, 2].est[k] = 0;
                }  // end for k
                for (i = 0; i < P.NUM_ETH; ++i)
                {

                    P.pop_sra[g, i, 0].e_nmil_totals = 0;
                    P.pop_sra[g, i, 0].est_totals = 0;
                    for (j = 0; j < P.NUM_SEX; ++j)
                    {
                        P.pop_sra[g, i, j].e_nmil_totals = 0;
                        P.pop_sra[g, i, j].est_totals = 0;
                    }     /* end for j */
                }     /* end for i */

                for (i = 0; i < P.NUM_ETH; ++i)
                    for (k = 0; k < P.NUM_AGE; ++k)
                    {
                        P.pop_sra[g, i, 0].e_nmil[k] = 0;
                        P.pop_sra[g, i, 0].est[k] = 0;
                    }  // end for k

                for (j = 0; j < P.NUM_SEX; ++j)
                {
                    P.pop_sra[g, 0, j].e_nmil_totals = 0;
                    P.pop_sra[g, 0, j].est_totals = 0;
                    for (k = 0; k < P.NUM_AGE; ++k)
                    {
                        P.pop_sra[g, 0, j].e_nmil[k] = 0;
                    }  // end for k
                }  // end for j

                P.DoStructTotals(P.pop_sra, g, 3);
                P.DoStructTotals(P.pop_sra, g, 4);
                P.DoStructTotals(P.pop_sra, g, 6);
            }  // end for g
        } // end procedure PopEstimatesSRA()

        //***************************************************************************************************

        public void SpecialEstimateMain(pasee P)
        {
            int g, h, i, j, k, adj, jr, kr, lr;
            int umilchg, hmilchg, new_mil_pop_est;
            int nsra;
            int lcounter;
            double factor;
            Random ran = new Random(0);

            try
            {
                for (h = 0; h < P.NUM_SRA; h++)
                {
                    nsra = P.sra_list[h];
                    // loop control is max cts, but processing will do sra's if index is in range
                    for (g = 0; g < P.NUM_CTS; g++)
                    {
                        if (P.ct_list[g, 1] != nsra)
                            continue;
                        factor = 0f;
                        // compute factor for base - est

                        // base year mil or special pop > 0
                        if (P.popestCtSpecialPop[g].baseYearSpecialPop > 0)
                        {
                            new_mil_pop_est = 0;
                            factor = (double)P.popestCtSpecialPop[g].e_mil / (double)P.popestCtSpecialPop[g].baseYearSpecialPop;
                            for (i = 1; i < P.NUM_ETH; ++i)
                            {
                                for (j = 1; j < P.NUM_SEX; ++j)
                                {
                                    for (k = 1; k < P.NUM_AGE; ++k)
                                    {
                                        if (P.pop_mil_ct[g, i, j, k].b_mil_gen > 0)
                                            P.pop_mil_ct[g, i, j, k].e_mil_gen =
                                                (int)((double)P.pop_mil_ct[g, i, j, k].b_mil_gen * factor + .5);

                                        if (P.pop_mil_ct[g, i, j, k].e_mil_gen < 0)
                                            P.pop_mil_ct[g, i, j, k].e_mil_gen = 0;

                                        umilchg = (int)((P.popestCtSpecialPop[g].e_umil -
                                            P.popestCtSpecialPop[g].b_umil) * P.mil_pct[i, j, k] + .5);
                                        hmilchg = (int)((P.popestCtSpecialPop[g].e_hmil - P.popestCtSpecialPop[g].b_hmil) *
                                            (0.75 * P.mil_dep_pct[i, j, k] + 0.25 * P.mil_pct[i, j, k]) + .5);

                                        P.pop_mil_ct[g, i, j, k].e_mil_bases = P.pop_mil_ct[g, i, j, k].b_mil_bases +
                                            umilchg + hmilchg;

                                        if (P.pop_mil_ct[g, i, j, k].e_mil_bases < 0)
                                            P.pop_mil_ct[g, i, j, k].e_mil_bases = 0;

                                        P.pop_mil_ct[g, i, j, k].est = P.pop_mil_ct[g, i, j, k].e_mil_bases +
                                            P.pop_mil_ct[g, i, j, k].e_mil_gen;

                                        /* accumulate new total mil pop at ct level for controlling */
                                        if (P.pop_mil_ct[g, i, j, k].est > 0)
                                            new_mil_pop_est += P.pop_mil_ct[g, i, j, k].est;
                                    } // end for k
                                }  // end for j
                            }  // end for i

                            //set up controlling
                            if (new_mil_pop_est < P.popestCtSpecialPop[g].e_mil)
                                adj = 1;
                            else
                                adj = -1;
                            lcounter = 0;
                            while (new_mil_pop_est != P.popestCtSpecialPop[g].e_mil && lcounter < 100000)
                            {
                                jr = ran.Next(1, P.NUM_ETH);
                                kr = ran.Next(1, P.NUM_SEX);
                                lr = ran.Next(1, P.NUM_AGE - 1);     // skip adjusting births

                                if (P.pop_mil_ct[g, jr, kr, lr].est > 0)
                                {
                                    P.pop_mil_ct[g, jr, kr, lr].est += adj;
                                    if (P.pop_mil_ct[g, jr, kr, lr].e_mil_bases > 0)
                                    {
                                        P.pop_mil_ct[g, jr, kr, lr].e_mil_bases += adj;
                                        new_mil_pop_est += adj;
                                    }  // end if

                                    else if ((P.pop_mil_ct[g, jr, kr, lr].e_mil_gen > 0 && adj < 0) || adj > 0)
                                    {
                                        P.pop_mil_ct[g, jr, kr, lr].e_mil_gen += adj;
                                        new_mil_pop_est += adj;
                                    }  // end else if
                                }  // end if
                                ++lcounter;
                            }
                            if (lcounter == 1000000 && new_mil_pop_est != P.popestCtSpecialPop[g].e_mil)
                            {
                                MessageBox.Show("MIL POP random control failed to converge");
                            }  // end fi

                            // determine what sra contains this ct
                            for (i = 1; i < P.NUM_ETH; ++i)
                            {
                                for (j = 1; j < P.NUM_SEX; ++j)
                                {
                                    for (k = 0; k < P.NUM_AGE; ++k)
                                    {
                                        /* aggregate estimates to sra's */
                                        P.pop_mil_sra[h, i, j, k].e_mil_gen += P.pop_mil_ct[g, i, j, k].e_mil_gen;
                                        P.pop_mil_sra[h, i, j, k].e_mil_bases += P.pop_mil_ct[g, i, j, k].e_mil_bases;
                                        P.pop_mil_sra[h, i, j, k].est += P.pop_mil_ct[g, i, j, k].est;
                                        P.pop_sra[h, 0, 0].popest_mil_est_totals += P.pop_mil_ct[g, i, j, k].est;
                                    }  // end for k
                                }  // end for j
                            }  // end for i
                        }  // end if
                    }  // end for g
                }  // end for h
            } // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }   // end catch

            for (g = 0; g < P.NUM_CTS; ++g)
                P.DoMilEstTotals(1, P.pop_mil_ct, g, P.NUM_CTS);

            for (g = 0; g < P.NUM_SRA; ++g)
                P.DoMilEstTotals(2, P.pop_mil_sra, g, P.NUM_SRA);
        }  // end SpecialEstimateMain()

        //*******************************************************************************************************************

        /* SurvivePop() */
        /// <summary>
        /// Compute survived population and net migration
        /// </summary>
        /// <param name="P"><value>class pasee - master variable class</value></param>
        /// <param name="lyear"<value>base year - used to decide survival portion to account for census year</value>

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void SurvivePop(pasee P, int lyear)
        {
            int i, j, k;
            int[] mover = new int[101];
            int[] remainder = new int[101];
            /*-------------------------------------------------------------------------*/

            P.WriteToStatusBox("COMPUTING SURVIVED POP");

            // got each of these groups, the adjusted pop already includes deaths;  therefore
            // these cohorts are moved forward.
            // for census base year we only move 3/4 of base pop forward
            for (i = 1; i < P.NUM_ETH; ++i)
            {
                for (j = 1; j < P.NUM_SEX; ++j)
                {
                    /* regional total */
                    P.pop[i, j].netmig_tot = 0;     /* zero net migration total by age and sex */

                    remainder[0] = 0;
                    mover[0] = P.pop[i, j].basep_adj[0];

                    /* first age group uses adjusted births */
                    P.pop[i, j].surv[0] = P.pop[i, j].births + remainder[0];

                    for (k = 1; k < P.NUM_AGE - 1; ++k)     // goes to age group 99
                    {
                        remainder[k] = 0;
                        mover[k] = P.pop[i, j].basep_adj[k];

                        P.pop[i, j].surv[k] = remainder[k] + mover[k - 1];

                    }     /* end for k */
                    // now accumulate age group 100
                    P.pop[i, j].surv[100] = P.pop[i, j].basep_adj[100] + mover[99];
                }     /* end for j */
            }     /* end for i */

            P.DoStructTotals(P.pop, 5);

        }     // end SurvivePop()

        /******************************************************************************/

        /*  SurvivePopCt() */
        /// <summary>
        /// Compute Ct survived population and net migration
        /// </summary>
        /// <param name="P"><value>class pasee - master variable class</value></param>

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   07/23/03   tb   changed controlling to 2-way update
        //   ------------------------------------------------------------------

        public void SurvivePopCt(pasee P, int lyear)
        {
            int g, k, h, i, j, num_cts_in_sra, fillid, ret;
            int[] t1 = new int[P.NUM_CTS];
            int[] ct_id = new int[P.MAX_CTS_IN_SRA];
            int[] mover = new int[101];
            int[] remainder = new int[101];
            int[] row_passer = new int[P.MAX_CTS_IN_SRA];
            int[] col_passer = new int[P.NUM_AGE];
            int[,] matrx_passer = new int[P.MAX_CTS_IN_SRA, P.NUM_AGE];

            for (i = 1; i < P.NUM_ETH; i++)
            {
                for (j = 1; j < P.NUM_SEX; j++)
                {
                    // ct pops
                    for (g = 0; g < P.NUM_CTS; g++)
                    {
                        P.pop_ct[g, i, j].netmig_tot = 0;     // zero net migration total by age and sex
                        remainder[0] = 0;
                        mover[0] = P.pop_ct[g, i, j].basep_adj[0];

                        // first age group uses adjusted births
                        if (P.ct_list[g, 1] == 16 || P.ct_list[g, 1] == 43)
                        {
                            P.pop_ct[g, i, j].surv[0] = 0;
                        }  // end if
                        else
                        {
                            P.pop_ct[g, i, j].surv[0] = P.pop_ct[g, i, j].births + remainder[0];
                        }  // end else

                        for (k = 1; k < P.NUM_AGE - 1; k++)
                        {
                            remainder[k] = 0;
                            mover[k] = P.pop_ct[g, i, j].basep_adj[k];

                            if (P.ct_list[g, 1] == 16 || P.ct_list[g, 1] == 43)
                            {
                                P.pop_ct[g, i, j].surv[k] = 0;
                            }  // end if
                            else
                            {
                                P.pop_ct[g, i, j].surv[k] = remainder[k] + mover[k - 1];
                            }   // end else
                        }  // end for k
                        // now accumulate age group 100
                        P.pop_ct[g, i, j].surv[100] = P.pop_ct[g, i, j].basep_adj[100] + mover[99];
                    }  // end for g
                }  // end for j
            }  // end for i

            // zero births and popest estimates for military ct
            for (g = 0; g < P.specialPops.Length; g++)
            {
                for (i = 0; i < P.NUM_ETH; i++)
                {
                    for (j = 0; j < P.NUM_SEX; ++j)
                    {
                        k = P.GetCtIndex(P.specialPops[g].ct);
                        if (k == 999)
                        {
                            MessageBox.Show("Bad index on ct = " + P.specialPops[g].ct);
                        } // end if

                        /* save the popest estimate before zeroing  -- these are zeroed because
                         * we are treating the military tract survived pop as zero for computations
                         */
                        P.pop_ct[k, i, j].popest_est_totals = P.pop_ct[k, i, j].est_totals;
                        P.pop_ct[k, i, j].est_totals = 0;
                        P.pop_ct[k, i, j].surv[0] = 0;
                    }  // end for j
                }     // end for i           
            }   // end for g

            // fill the totals
            for (g = 0; g < P.NUM_CTS; g++)
            {
                P.DoStructTotals(P.pop_ct, g, 5);
            }  // end for g

            /* adjust the ct survived pop to sra total by eth and sex */
            /* this uses the update method.  Move the survived ct pop by eth and sex to temp array before calling controlling routine replace after plus/minus */

            /* for each sra, build a temp array of ct's */
            for (g = 0; g < P.NUM_SRA; ++g)
            {
                Array.Clear(row_passer, 0, row_passer.Length);
                Array.Clear(col_passer, 0, col_passer.Length);
                num_cts_in_sra = 0;
                Array.Clear(ct_id, 0, ct_id.Length);

                for (h = 0; h < P.NUM_CTS; ++h)
                {
                    if (P.ct_list[h, 1] == P.sra_list[g])
                    {
                        ct_id[num_cts_in_sra] = h;
                        row_passer[num_cts_in_sra] = P.pop_ct[h, 0, 0].popest_est_totals - P.pop_ct[h, 0, 0].popest_mil_est_totals;
                        fillid = 0;
                        for (i = 1; i < P.NUM_ETH; i++)
                        {
                            for (j = 1; j < P.NUM_SEX; j++)
                            {
                                col_passer[fillid] = P.pop_sra[g, i, j].e_nmil_totals;
                                matrx_passer[num_cts_in_sra, fillid++] = P.pop_ct[h, i, j].surv_totals;
                            }  // end for j
                        }  // end for i

                        ++num_cts_in_sra;
                    }  // end if
                }  // end for h

                ret = CU.cUtil.update(num_cts_in_sra, 16, matrx_passer, row_passer, col_passer);

                // restore the controlled data in P.pop_ct structure
                for (h = 0; h < num_cts_in_sra; ++h)
                {
                    fillid = 0;
                    for (i = 1; i < P.NUM_ETH; ++i)
                    {
                        for (j = 1; j < P.NUM_SEX; ++j)
                        {
                            P.pop_ct[ct_id[h], i, j].e_nmil_totals = matrx_passer[h, fillid++];
                        }  // end for j
                    }  // end for i
                }  // end for h
            }  // end for g

            /* control the age groups within a ct by ethnic and sex with col controls
             * being the sra age controls
             */
            for (i = 1; i < P.NUM_ETH; i++)
            {
                for (j = 1; j < P.NUM_SEX; j++)
                {
                    // for each sra, build a temp array of CTs
                    for (g = 0; g < P.NUM_SRA; g++)
                    {
                        row_passer = new int[row_passer.Length];
                        col_passer = new int[col_passer.Length];
                        Array.Clear(matrx_passer, 0, matrx_passer.Length);
                        //Array.Clear(row_passer, 0, row_passer.Length);
                        //Array.Clear(col_passer, 0, col_passer.Length);
                        //Array.Clear(matrx_passer, 0, matrx_passer.Length);
                        num_cts_in_sra = 0;
                        ct_id = new int[ct_id.Length];
                        for (h = 0; h < P.NUM_CTS; h++)
                        {
                            if (P.ct_list[h, 1] == P.sra_list[g])
                            {
                                ct_id[num_cts_in_sra] = h;
                                row_passer[num_cts_in_sra] = P.pop_ct[h, i, j].e_nmil_totals;
                                for (k = 0; k < P.NUM_AGE; k++)
                                {
                                    matrx_passer[num_cts_in_sra, k] = P.pop_ct[h, i, j].surv[k];
                                }  // end for k
                                num_cts_in_sra++;
                            }  // end if
                        }
                        for (k = 0; k < P.NUM_AGE; ++k)
                        {
                            col_passer[k] = P.pop_sra[g, i, j].e_nmil[k];
                        }  // end for k
                        ret = CU.cUtil.update(num_cts_in_sra, 101, matrx_passer, row_passer, col_passer);

                        // P.PCT.CTPMAge(P,,num_cts_in_sra,g,ct_id,i,j);     /*plus/minus control */

                        /* replace the controlled data in P.pop_ct structure */
                        for (h = 0; h < num_cts_in_sra; ++h)
                        {
                            for (k = 0; k < P.NUM_AGE; ++k)
                            {
                                P.pop_ct[ct_id[h], i, j].e_nmil[k] = matrx_passer[h, k];
                            }  // end for k
                        }  // end for h
                    }  // end for g
                }  // end for j
            }  // end for i
        } // end procedure SurvivePopC

        /*****************************************************************************/

        /* SurvivePopSRA() */
        /// <summary>
        /// Compute SRA survived population and net migration
        /// </summary>
        /* compute sra survived population and net migration*/
        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public void SurvivePopSRA(pasee P, int lyear)
        {
            int g, i, j, k;
            int[] t1 = new int[P.NUM_SRA];
            int[] mover = new int[101];
            int[] remainder = new int[101];

            for (i = 1; i < P.NUM_ETH; i++)
            {
                for (j = 1; j < P.NUM_SEX; j++)
                {
                    // compute sums of survived pop by each single year of age for each SRA
                    for (g = 0; g < P.NUM_SRA; g++)
                    {
                        P.pop_sra[g, i, j].netmig_tot = 0;     /* zero net migration total by age and sex */
                        remainder[0] = 0;
                        mover[0] = P.pop_sra[g, i, j].basep_adj[0];

                        /* first age group uses adjusted births */

                        if (P.sra_list[g] == 16 || P.sra_list[g] == 43) // 16= miramar, 43=pendleton
                            P.pop_sra[g, i, j].surv[0] = 0;
                        else
                        {
                            P.pop_sra[g, i, j].surv[0] = P.pop_sra[g, i, j].births + remainder[0];
                        }  // end else
                        for (k = 1; k < P.NUM_AGE - 1; k++)
                        {
                            remainder[k] = 0;
                            mover[k] = P.pop_sra[g, i, j].basep_adj[k];

                            if (P.sra_list[g] == 16 || P.sra_list[g] == 43)
                                P.pop_sra[g, i, j].surv[k] = 0;
                            else
                            {
                                P.pop_sra[g, i, j].surv[k] = remainder[k] + mover[k - 1];
                            }  // end else
                        }  // end for k
                        // now accumulate age group 100
                        if (P.sra_list[g] == 16 || P.sra_list[g] == 43)
                            P.pop_sra[g, i, j].surv[100] = 0;
                        else
                            P.pop_sra[g, i, j].surv[100] = P.pop_sra[g, i, j].basep_adj[100] + mover[99];
                    }  // end for g
                }  // end for j
            }  // end for i

            for (g = 0; g < P.NUM_SRA; g++)
                P.DoStructTotals(P.pop_sra, g, 5);
        }  // end procedure SurvivePopSRA
    }  // end class 	
}  // end namespace

