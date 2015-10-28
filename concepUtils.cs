/* 
 * Source File: concepUtils.cs
 * Program: concep
 * Version 3.3
 * Programmer: tbe
 * Description:
 *		collection of common utilities that are used in more than 1 model component
 *		these procedures were moved from and removed from various modules where they were redundant code

*/
//Revision History
//   Date       By   Description
//   ------------------------------------------------------------------
//   06/30/11   tb   initial coding 

//   ------------------------------------------------------------------

using System;
namespace CU
{
    public class cUtil
    {
        // utility methods for all models
        // procedures
        //  AscendingSort()
        //  AscendingSortDouble()
        //  DescendingSortMulti()
        //  chkBaseTot()
        //  chkGrandTot()
        //  countMatches()
        //  finish1()
        //  finish2()
        //  GetArrayStats()
        //  GetArraysum()
        //  InsertSort()
        //  PachinkoNoMaster()
        //  PachinkoWithMasterDecrement()
        //  PachinkoWithMasterNoDecrement()

        //  Roundit()
        //  Update()

        //---------------------------------------------------------------------------------------------------------

        /*  AscendingSort() */

        /// Sort a small list in ascending order

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------

        public static void AscendingSort(int[] v, int[] p, int[] l, int n)
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
                } // end for
                p[j + 1] = temp1;
                v[j + 1] = temp0;
                l[j + 1] = temp2;
            }  // end for i

        } // end procedure AscendingSort

        //***************************************************************************************************************

        /*  AscendingSortDouble() */

        /// Sort a small list of doubles in ascending order

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   02/28/12   tb   initial coding

        //   ------------------------------------------------------------------

        public static void AscendingSortDouble(double[] v, double[] p, int n)
        {
            int i, j;
            double temp0, temp1;
            for (i = 0; i < n; ++i)
            {
                temp1 = p[i];
                temp0 = v[i];

                for (j = i - 1; j >= 0 && p[j] > temp1; j--)
                {
                    p[j + 1] = p[j];
                    v[j + 1] = v[j];
                } // end for
                p[j + 1] = temp1;
                v[j + 1] = temp0;

            }  // end for i

        } // end procedure AscendingSort

        // *****************************************************************************************************

        /*  DesscendingSortMulti() */

        /// Sort a small list of several vars (2+) in ascending order 

        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/05/02   tb   initial coding

        //   ------------------------------------------------------------------

        public static void DescendingSortMulti(int[] indexer, int[] datacontrol, int[,] dataother, int num_rows, int num_cols)
        {
            int i, j, k;
            int tempindexer;
            int tempdatacontrol;
            int[] tempdataother = new int[num_cols];
            for (i = 0; i < num_rows; ++i)
            {
                tempdatacontrol = datacontrol[i];
                tempindexer = indexer[i];

                for (k = 0; k < num_cols; ++k)
                    tempdataother[k] = dataother[i,k];

                for (j = i - 1; j >= 0 && datacontrol[j] < tempdatacontrol; j--)
                {
                    datacontrol[j + 1] = datacontrol[j];
                    indexer[j + 1] = indexer[j];
                    for (k = 0; k < num_cols; ++k)
                        dataother[j + 1,k] = dataother[j,k];
                } // end for

                datacontrol[j + 1] = tempdatacontrol;
                indexer[j + 1] = tempindexer;
                for (k = 0; k < num_cols; ++k)
                    dataother[j + 1,k] = tempdataother[k];
            }  // end for i

        } // end procedure DescendingSortMulti

        //***************************************************************************************************************

        // procedure chkBaseTot()

        /* estimate values for rows with control totals and no base data */

        /* Revision History
            SCR            Date       By   Description
            -------------------------------------------------------------------------
                            8/08/94    tb   initial coding
            -------------------------------------------------------------------------
        */

        public static void chkBaseTot(int num_rows, int num_cols, int[,] matrx, int[] nurowt, int[] nucolt, int coltot)
        {
            int[] rowsum = new int[num_rows];
            int[] colsum = new int[num_cols];

            /* compute row and column totals */
            int total = GetRowColTots(num_rows, num_cols, matrx, rowsum, colsum);
            for (int j = 0; j < num_cols; j++)
            {
                for (int i = 0; i < num_rows; i++)
                {
                    if (rowsum[i] == 0 && nurowt[i] > 0)
                    {
                        if (coltot > 0)
                        {
                            matrx[i, j] = (int)(((double)(nurowt[i] * nucolt[j]) / (double)coltot));
                        }  // end if
                        else
                        {
                            matrx[i, j] = 0;
                        }  // end else
                    }  // end if
                }  // end for i
            }  // end for j
        }  // end procedure chkBaseTot()

        // **********************************************************************************************

        // procedure chkGrandTot()

        /*  new row and column totals and redistribute if unequal return coltot */
        /* Revision History
            SCR            Date       By   Description
            -------------------------------------------------------------------------
                            8/08/94    tb   initial coding
            -------------------------------------------------------------------------
        */

        public static int chkGrandTot(int num_rows, int num_cols, int[,] matrx, int[] nurowt, int[] nucolt)
        {

            int rowtot, coltot;
            int rsum, total, rt, rtot;
            int ii, i, j;
            int[] rowsum = new int[num_rows];
            int[] colsum = new int[num_cols];

            /* initialize some data */
            total = rowtot = coltot = 0;
            rsum = 0;

            /* compute total of row control vector */
            for (i = 0; i < num_rows; ++i)
                rowtot += nurowt[i];

            total = GetRowColTots(num_rows, num_cols, matrx, rowsum, colsum);

            /*total col vectors */
            for (j = 0; j < num_cols; ++j)
                coltot += nucolt[j];

            /* check new row and col control totals.  if not equal adjust, 
                otherwise, continue with allocation */
            if (coltot != rowtot)
            {
                /* the grand totals are different, adjust the rows */
                for (i = 0; i < num_rows; ++i)
                {
                    if (rowsum[i] == nurowt[i])     /* keep track of cases where
                                                        old row tot = new row tot 
                                                        don't want to messs with these */
                        rsum += nurowt[i];
                }  // end for i
                total = 0;         /*reset total */
                rt = 0;
                ii = 0;
                for (i = 0; i < num_rows; ++i)
                {
                    rtot = rowtot - rsum;
                    if (rtot == 0 || (nurowt[i] == rowsum[i]))
                        continue;
                    else
                    {
                        /* redistribute based on column grand total */
                        if (rtot > 0)
                            nurowt[i] = (int)(.5 + (double)nurowt[i] * (double)(coltot - rsum) /
                                (double)rtot);
                        else
                            nurowt[i] = 0;
                        total += nurowt[i];
                        if (nurowt[i] <= rt)
                            continue;
                        else
                        {
                            rt = nurowt[i];
                            ii = i;
                        }  // end else
                    }  // end else
                }  // end for i
                /* force row control totals to grand control total*/
                nurowt[ii] += coltot - total - rsum;
            }  // end if
            return coltot;

        } // end procedure chkGrandTot()

        //*********************************************************************************************

        /* count the matches in row and col totals */
        /* Revision History
            SCR            Date       By   Description
            -------------------------------------------------------------------------
                            8/08/94    tb   initial coding
            -------------------------------------------------------------------------
        */
        public static void countMatches(int rowsame, int colsame,
                                  int num_rows, int num_cols, int[] rowsum, int[] colsum,
                                  int[] nurowt, int[] nucolt)
        {
            rowsame = 0;
            colsame = 0;

            for (int i = 0; i < num_rows; i++)
            {
                if ((double)rowsum[i] / (double)nurowt[i] >= .999999 && (double)rowsum[i] / (double)nurowt[i] <= 1.000003)
                {
                    rowsame++;
                }  // end if
            }  // end for i

            for (int j = 0; j < num_cols; j++)
            {
                if ((double)colsum[j] / (double)nucolt[j] >= .999999 && (double)colsum[j] / (double)nucolt[j] <= 1.000003)
                {
                    colsame++;
                }  // end if
            }  // end for j

        }  // end procedure countMatches()

        //**************************************************************************************************

        /* finish1()*/
        /* final rounding for distribution estimates */
        /* this one for sets with row totals not matching controls */

        /* Revision History
           Date       By   Description
           -------------------------------------------------------------------------
           07/27/98    tb   initial coding
           -------------------------------------------------------------------------
        */
        public static void finish1(int[,] matrix, int[] row_controls, int[] col_controls, int nrows, int ncols)
        {
            int row_total, col_total;
            int[] col_diff = new int[ncols];
            int[] col_sum = new int[ncols];
            int[] row_sum = new int[nrows];
            int[] row_suma = new int[nrows];
            int[] row_diff = new int[nrows];

            int i, j;
            Random ran = new Random(0);

            row_total = col_total = 0;
            /* compute differences in column (sector) sums and regional controls */
            col_diff.Initialize();
            row_diff.Initialize();
            col_sum.Initialize();
            row_sum.Initialize();

            for (j = 0; j < ncols; ++j)
            {
                for (i = 0; i < nrows; ++i)
                {
                    col_sum[j] += matrix[i, j];
                }  // end for i

                col_diff[j] = col_controls[j] - col_sum[j];
                col_total += col_diff[j];
            }  // end for j

            for (i = 0; i < nrows; ++i)
            {
                row_sum[i] = 0;
                for (j = 0; j < ncols; ++j)
                {
                    row_sum[i] += matrix[i, j];
                }  // end for j

                row_diff[i] = row_controls[i] - row_sum[i];
                row_total += row_diff[i];
            }  // end for i

            // adjust rows by factor
            for (i = 0; i < nrows; ++i)
            {
                row_suma[i] = 0;
                for (j = 0; j < ncols; ++j)
                {
                    if (row_sum[i] > 0)
                    {
                        matrix[i, j] = (int)((double)matrix[i, j] * (double)row_controls[i] / (double)row_sum[i]);
                    }  // end if

                    row_suma[i] += matrix[i, j];
                    row_diff[i] = row_controls[i] - row_suma[i];
                }  // end for j
            }  // end for i

            /* now run through rows, get negatives (subtract to meet control) and make adjustment in first col with neg regional control difference */
            for (i = 0; i < nrows; ++i)
            {
                while (row_diff[i] != 0)
                {
                    j = ran.Next(0, ncols);
                    if (row_diff[i] > 0)
                    {
                        ++matrix[i, j];
                        ++col_diff[j];
                        --row_diff[i];
                        if (row_diff[i] == 0)
                            break;
                    }  // end if
                    else
                    {
                        if (matrix[i, j] > 0)
                        {
                            --matrix[i, j];
                            --col_diff[j];
                            ++row_diff[i];
                            if (row_diff[i] == 0)
                                break;
                        }  // end if
                    }  // end else
                }  // end while
            }  // end for i

            /* recompute sums and compare to totals */
            row_total = col_total = 0;
            /* compute differences in column (sector) sums and regional controls */
            col_diff.Initialize();
            row_diff.Initialize();

            for (j = 0; j < ncols; ++j)
            {
                col_sum[j] = 0;
                for (i = 0; i < nrows; ++i)
                {
                    col_sum[j] += matrix[i, j];

                }  // end for i
                col_diff[j] = col_controls[j] - col_sum[j];
                col_total += col_diff[j];
            }
            for (i = 0; i < nrows; ++i)
            {
                row_sum[i] = 0;
                for (j = 0; j < ncols; ++j)
                {
                    row_sum[i] += matrix[i, j];
                }  // end for j
                row_diff[i] = row_controls[i] - row_sum[i];
                row_total += row_diff[i];
            }  // end for i
        }  // end procedure finish1()

        //***********************************************************************************

        // finish2()

        /* final rounding for distribution estimates */
        /* this one for sets with col totals not matching controls */
        /* this is the normal case - all the row dists add up, but the regional
           column totals are slightly off from regional totals */
        /* Revision History
           Date       By   Description
           -------------------------------------------------------------------------
           07/27/98    tb   initial coding
           -------------------------------------------------------------------------
        */
        public static void finish2(int[,] matrix, int[] col_controls, int nrows, int ncols)
        {
            int i, j, k, countk;
            bool found_another_col, found_col;
            int[] col_tot = new int[ncols];
            int[] col_diff = new int[ncols];
            Random ran = new Random(0);

            col_tot.Initialize();
            col_diff.Initialize();

            for (j = 0; j < ncols; ++j)
            {
                for (i = 0; i < nrows; ++i)
                {
                    col_tot[j] += matrix[i, j];
                }  // end for i

                col_diff[j] = col_controls[j] - col_tot[j];
            }  // end for j

            for (j = 0; j < ncols; ++j)
            {
                while (col_diff[j] != 0)
                {
                    if (col_diff[j] < 0)
                    {
                        for (i = 0; i < nrows; ++i)
                        {
                            if (matrix[i, j] > 0)
                            {
                                --matrix[i, j];
                                ++col_diff[j];

                                /* now find the pos col in this row i to do the offset addition */
                                found_col = false;
                                while (!found_col)
                                {
                                    k = ran.Next(0, ncols);
                                    if (k == j)
                                        continue;
                                    if (col_diff[k] > 0)
                                    {
                                        ++matrix[i, k];
                                        --col_diff[k];
                                        found_col = true;
                                    } // end if
                                }  // end while
                            }  // end if
                            if (col_diff[j] == 0) break;
                        }  // end for i
                    }  // end if
                    else
                    {
                        /* col diff are > 0 */
                        for (i = 0; i < nrows; ++i)
                        {
                            ++matrix[i, j];
                            --col_diff[j];
                            found_another_col = false;
                            /* now find the neg col in this row i to do the offset subtraction */
                            /* there may not be any - so we have to set a flag and check it, 
                                if the flag is cleared ok, otherwise we have to reset the matrix and
                                col_diff done above and go to the next row */
                            found_col = false;
                            countk = 0;
                            while (!found_col && countk < ncols)
                            {
                                k = ran.Next(0, ncols);

                                if (k == j) continue;
                                ++countk;
                                if (col_diff[k] < 0)
                                {
                                    if (matrix[i, k] > 0)
                                    {
                                        found_another_col = true;
                                        --matrix[i, k];
                                        ++col_diff[k];
                                        found_col = true;
                                        break;
                                    }  // end if
                                }  // end if
                            }  // end while

                            if (found_another_col && col_diff[j] == 0)
                                break;
                            else if (!found_another_col)
                            {
                                /* no additional column was found in this row to make an adjustment -
                                    restore the matrix and col_diff values and go to next row */
                                --matrix[i, j];
                                ++col_diff[j];
                            }  // ens else if
                        }  // end for i
                    } // end else
                }  // end while
            } // end for j
            
        } // end procedure finish2()

        //**********************************************************************************************************************

        /* GetArrayStats() */
        // Array distribution and cumulative probability used in Pachinko

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public static void GetArrayStats(int[] arr, int lcl_sum, int ncount, double[] arr_dist, double[] arr_prob)
        {
            int i;
            double summer;
            /*-------------------------------------------------------------------------*/
            summer = 0;
            for (i = 0; i < ncount; ++i)
            {
                arr_dist[i] = (double)arr[i] / (double)lcl_sum * 100;
                summer += arr_dist[i];
                arr_prob[i] = summer;
            }     /* end for */

        }     /* end GetArrayStats()*/

        /*****************************************************************************/

        /* GetArraySum() */
        /// Get sum of array in Pachinko processing

        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   ------------------------------------------------------------------

        public static int GetArraySum(int[] arr, int ncount)
        {
            int i;
            int summer;

            /*-------------------------------------------------------------------------*/
            summer = 0;
            for (i = 0; i < ncount; ++i)
                summer += arr[i];
            return (summer);

        }     /* end GetArraySum()*/

        /*****************************************************************************/

        // GetRowColTots()
        /* compute row and column sums and total */
        /* Revision History
            SCR            Date       By   Description
            -------------------------------------------------------------------------
                            8/08/94    tb   initial coding
            -------------------------------------------------------------------------
        */
        public static int GetRowColTots(int num_rows, int num_cols, int[,] matrx, int[] rowsum, int[] colsum)
        {
            int i, j, total;
            for (i = 0; i < num_rows; ++i)
                rowsum[i] = 0;
            total = 0;

            for (j = 0; j < num_cols; ++j)
            {
                colsum[j] = 0;
                for (i = 0; i < num_rows; ++i)
                {
                    rowsum[i] += matrx[i, j];
                    colsum[j] += matrx[i, j];
                    total += matrx[i, j];
                }     /* end for i*/
            }     /* end for j */
            return (total);
        }  // end procedure GetRowColTots()

        //***************************************************************************************

        public static void insertsort(int[,] v, int n)
        {
            int i, j, temp0v, temp1v;
            for (i = 1; i < n; i++)
            {
                temp0v = v[i, 0];
                temp1v = v[i, 1];
                for (j = i - 1; j >= 0 && v[j, 1] > temp1v; j--)
                {
                    v[j + 1, 0] = v[j, 0];
                    v[j + 1, 1] = v[j, 1];
                }
                v[j + 1, 1] = temp1v;
                v[j + 1, 0] = temp0v;
            }  // end for i

        } // end procedure insertsort()

        //*************************************************************************************

        /* PachinkoNoMaster() */
        // Cumulative probability distribution method of +/- controlling/allocation
        // This Pachinko uses the distribution of the passed array (slave) to determine the cumulative distribution
        // Hence, there is no Master distribution being allocated and therefore no decrement
        // this is essentially a rounding algorithm to completely fill an array to match a given total


        //	Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/12/02   tb   started initial coding
        //   07/06/11   tb   stndardized for moving to concepUtils.cs
        //   ------------------------------------------------------------------

        public static int PachinkoNoMaster(int target, int[] slave, int ncount)
        {
            int i, lcl_sum, i1 = 0;
            int loop_count;
            int where;

            double[] local_dist = new double[ncount];     /* computed distribution */
            double[] cum_prob = new double[ncount];       /* cumulative probability */
            Random ran = new Random(0);
            /*-------------------------------------------------------------------------*/
            loop_count = 0;

            //initialize the objects
            Array.Clear(local_dist, 0, local_dist.Length);
            Array.Clear(cum_prob, 0, cum_prob.Length);
            lcl_sum = CU.cUtil.GetArraySum(slave, ncount);


            while (lcl_sum != target && loop_count < 40000)
            {
                /* reset the computed arrays */

                Array.Clear(local_dist, 0, local_dist.Length);
                Array.Clear(cum_prob, 0, cum_prob.Length);

                CU.cUtil.GetArrayStats(slave, lcl_sum, ncount, local_dist, cum_prob);
                /* get the random number between 1 and 100 */
                where = ran.Next(1, 100);

                /* look for the index of the cum_prob <= the random number */
                for (i = 0; i < ncount; ++i)
                {
                    if (where <= cum_prob[i])     /* is the random number greater than this cum_prob */
                    {
                        i1 = i;     /* save the index in the cum_prob*/
                        break;
                    }  // end else
                }     /* end for i */

                if (target > lcl_sum)
                    ++slave[i1];
                else if (slave[i1] > 0)
                    --slave[i1];

                lcl_sum = CU.cUtil.GetArraySum(slave, ncount);
                ++loop_count;

            }     /* end while */
            return loop_count;

        }     /* end PachinkoNoMaster() */

        /*****************************************************************************/

        /* PachinkoWithMasterDecrement() */

        // Cumulative probability distribution method of +/- controlling/allocation
        // This Pachinko uses the distribution of a controlling (master) to determine the cumulative distribution of a slave
        // this algorithm uses the master distribution to fill an empty array that will sum to a control total
        // the master is decremented;  this is a common 2-way controlling to fill a 2X array with row and col controls
        // this version uses the row control to throttle the evental distribution as long as the master (column control) > 0
     

        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/22/11   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        public static int PachinkoWithMasterDecrement(int target, int[] master, int[] slave, int ncount)
        {
            int i1, k;
            int loop_count;
            int where;
            int lcl_sum;

            double[] local_dist = new double[ncount];     /* computed distribution , max 20 elements */
            double[] cum_prob = new double[ncount];       /* cumulative probability */
            Random ran = new Random(0);

            /*-------------------------------------------------------------------------*/

            /* reset the computed arrays */
            Array.Clear(local_dist, 0, local_dist.Length);
            Array.Clear(cum_prob, 0, cum_prob.Length);
            i1 = 0;
            loop_count = 0;

            /* keep doing this until the target pop is met */
            lcl_sum = 999;
           
            while (target != 0 && loop_count < 40000 && lcl_sum > 0)
            {
                lcl_sum = CU.cUtil.GetArraySum(master, ncount);
                CU.cUtil.GetArrayStats(master, lcl_sum, ncount, local_dist, cum_prob);
                /* get the random number between 1 and 100*/
                where = ran.Next(1, 100);

                //int tt = Array.BinarySearch(cum_prob, where);
                /* look for the index of the cum_prob <= the random number */
                for (k = 0; k < ncount; ++k)
                {
                    if (where <= cum_prob[k])     /* is the random number greater than this cum_prob */
                    {
                        i1 = k;     /* save the index in the cum_prob*/
                        break;
                    }  // end else
                }     /* end for i */

                if (master[i1] > 0)
                {
                    slave[i1] += 1;
                    master[i1] -= 1;
                    target -= 1;
                }  // end if
                ++loop_count;
            }     /* end while */

            return loop_count;
        }     /* end procedure PachinkoWithMasterDecrement */

        /*****************************************************************************/

        /* method PachinkoWithMasterNoDecrement() */
        // Cumulative probability distribution method of +/- controlling/allocation
        // This Pachinko uses the distribution of a controlling (master) to determine the cumulative distribution of a slave
        // this algorithm uses the master distribution to fill an empty array that will sum to a control total
        // the master is not decremented; 

        /* Revision History
            * 
            * STR             Date       By    Description
            * --------------------------------------------------------------------------
            *                 08/26/97   tb    Initial coding
            *                 08/06/03   df    C# revision
            * --------------------------------------------------------------------------
            */
        public static int PachinkoWithMasterNoDecrement(int target, int[] master, int[] slave, int ncount)
        {
            int i1, i;
            int loopCount = 0;
            int where;
            int localSum = 0;
            int controlSum = 0;
            int diff;

            double[] lcl_dist = new double[ncount];// Computed distribution,
            double[] cum_prob = new double[ncount]; // Cumulative probability

            Random rand = new Random(0);
            // -------------------------------------------------------------------------
            // Reset the computed arrays

            // Keep doing this until the target pop is met
            localSum = 0;
            for (i = 0; i < ncount; i++)
                localSum += slave[i];
            diff = target - localSum;

            while (diff != 0 && loopCount < 40000)
            {
                controlSum = CU.cUtil.GetArraySum(master, ncount);
                CU.cUtil.GetArrayStats(master, controlSum, ncount, lcl_dist, cum_prob);

                /* Get the random number between 1 and 10000 and convert to xx.xx * decimal*/
                where = (int)(rand.NextDouble() * 100);
                i1 = 9999;
                // Look for the index of the cumProb <= the random number
                for (i = 0; i < ncount; i++)
                {
                    if (where <= cum_prob[i])
                    {
                        i1 = i;      // Save the index in the cumProb
                        break;
                    }   // end if
                }   // end for 
                if (i1 > ncount)
                {
                    loopCount++;
                    continue;
                }   // end if

                if (diff > 0)
                    slave[i1]++;
                else if (slave[i1] > 0)
                    slave[i1]--;

                // Reset localSum, and recompute the sum
                localSum = 0;
                for (i = 0; i < ncount; i++)
                    localSum += slave[i];

                diff = target - localSum;
                loopCount++;
            }     // End while
            return diff;
        }     // End method PachinkoWithMasterNoDecrement()

        /*****************************************************************************/

        /* Roundit() */
       
        // +/- 1 rounding - type determines whether or not to use the limit array to constrain +/- rounding
        // hh lte hs, hhp gte hh and so on
       
        //   Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   06/10/02   tb   initial coding

        //   ------------------------------------------------------------------
        public static int Roundit(int[] local, int[] limit, int target, int counter, byte type)
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
                } // end if
            }     /* end for */

            /* +\- rounding */
            iter_count = 0;
            diff = target - summer;

            while ((diff != 0) && iter_count < 100000)
            {
                ++iter_count;
                for (i = counter - 1; i >= 0; --i)
                {
                    if (diff > 0)
                    {
                        if (type == 1)
                            
                        {
                            local[i] += 1;
                            diff -= 1;
                        }     /* end if */
                        else if (type == 3 && limit[i] > 0)
                        {
                            local[i] += 1;
                            diff -= 1;
                        }     /* end if */
                        else if (type == 2 && local[i] < limit[i])
                        {
                            local[i] += 1;
                            diff -= 1;
                        }     /* end if */
                    } // end if
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

        public static int update(int num_rows, int num_cols, int[,] matrx, int[] nurowt, int[] nucolt)
        {

            int coltot;             /* grand total of control rows and cols */
            int rowsame = 0, colsame = 0;
            int i, j;
            int total, loop_count;
            bool loop = true, call_finish1 = false, call_finish2 = false;

            /* incoming individual row and column totals*/
            int[] rowsum = new int[num_rows];
            int[] colsum = new int[num_cols];
            int[] csum = new int[num_cols];
            int[] tempa = new int[num_cols];
            /*------------------------------------------------------------------------*/
            /* check for negative cells */
            for (i = 0; i < num_rows; ++i)
            {
                for (j = 0; j < num_cols; ++j)
                {
                    tempa[j] = matrx[i, j];
                    if (matrx[i, j] < 0)
                        return (0);
                }     /* end for j */

            }     // end for

            /* check sum of new control row and cols and redistribute if necessary */
            coltot = chkGrandTot(num_rows, num_cols, matrx, nurowt, nucolt);

            chkBaseTot(num_rows, num_cols, matrx, nurowt, nucolt, coltot);

            total = GetRowColTots(num_rows, num_cols, matrx, rowsum, colsum);

            countMatches(rowsame, colsame, num_rows, num_cols, rowsum, colsum, nurowt, nucolt);

            loop_count = 0;
            while (loop && loop_count < 2)
            {
                /*factor matrix to control totals by iteration*/
                /* initialize summation arrays*/
                for (j = 0; j < num_cols; ++j)
                {
                    if (colsum[j] != nucolt[j])
                    {
                        for (i = 0; i < num_rows; ++i)
                        {
                            if (nucolt[j] > 0 && colsum[j] > 0)
                                matrx[i, j] = (int)(0.5 + (double)matrx[i, j] * ((double)nucolt[j] / (double)colsum[j]));
                        }     /* end for i*/
                    }     /* end if */
                }     /* end for j */

                chkBaseTot(num_rows, num_cols, matrx, nurowt, nucolt, coltot);

                total = GetRowColTots(num_rows, num_cols, matrx, rowsum, colsum);
                countMatches(rowsame, colsame, num_rows, num_cols, rowsum, colsum, nurowt, nucolt);

                /* check difference between intermediate row totals and projected row totals */

                if (rowsame == num_rows && colsame == num_cols)     /* if the rows match - bail out */
                {
                    loop = false;
                    continue;
                }  // end if

                ++loop_count;

            }     /* end while loop*/

            call_finish1 = false;
            total = GetRowColTots(num_rows, num_cols, matrx, rowsum, colsum);
            for (i = 0; i < num_rows; ++i)
            {
                if (rowsum[i] != nurowt[i])
                {
                    call_finish1 = true;
                    break;
                }     /* end if */
            }     /*end for */

            if (call_finish1)
                CU.cUtil.finish1(matrx, nurowt, nucolt, num_rows, num_cols);

            total = GetRowColTots(num_rows, num_cols, matrx, rowsum, colsum);
            call_finish2 = false;
            for (j = 0; j < num_cols; ++j)
            {
                if (colsum[j] != nucolt[j])
                {
                    call_finish2 = true;
                    break;
                }     /* end for j */
            }     /* end for j */

            if (call_finish2)
                CU.cUtil.finish2(matrx, nucolt, num_rows, num_cols);
            return 1;
        }  // end procedure update()

        //*****************************************************************************************        

    }  // end class cUtil
}  // end namespace CU
