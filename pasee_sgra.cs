/*****************************************************************************/
/*
  Source file name (path):  pasee_mgra.cs
  Program: concep
  Version: 3.0
  Programmer: tb
  Description:  pasee_mgra module of concep
  
           pasee_mgra uses Census aggregate age groups.  Essentially 5-year groups with
              15-17, 18-19, 60-61, and 62-64 differentiated
           How this works:
           1.  Store data by eth,sex, age for CT into single dimension array
               with 320 elements (20x2x8)
           2.  Compute the cumulative prob for this master vector
           3.  Run pachinko on this master vector - control loop is pop of 
               1st mgra.  Select random number
               and get index into the 320. Inc that element of the mgra
               and decrement that element of the master.  Keep this up until
               the target pop for the mgra is met.
           4.  Restore the mgra vector to the mgra age, sex , eth array.
           5.  Recompute the cumulative prob on remaining master elements.
           6.  Repeat the process with the next mgra.
           7.  Continue until n-1 mgra is complete.  The nth mgra is what is
               left in the master.

		concep database
		   input tables
           detailed_pop_ct_tab_YYYY : detailed demographic charactersitics by CT year YYYY
           popest_YYYY : popest to ct used for ct ids
           popest_YYYY_mgra : control mgra populations

           output tables
           detailed_pop_mgra_tab_YYYY : detailed demographic characteristics by mgra year YYYY
          		

  PaseemgraMain()
  ExtractCtPop()
  LoadTable()
  LoadVector()
  Pachinkomgra()
  WriteAscii()
*/
 
 //Revision History
 //   Date       By   Description
 //   ------------------------------------------------------------------
 //   07/09/02   tb   started initial coding for this module
 //   07/28/03   tb   changed code to do 5-year groups
 //   ------------------------------------------------------------------
 //******************************************************************************************

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Data;

namespace pasee
{
    public class pasee_mgra
    {
        /* PaseemgraMain() */
        /// <summary>
        /// PASEE to mgra main
        /// </summary>
        /// <param name="P"><value>PASEE master class</value></param>
        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------
        private string filePath;

        public void PaseeMGRAMain(pasee P)
        {
            int[,] popByMgraInCt = new int[GD.NCT, GD.MAX_ELEM];
            int[,] mgrasByCt = new int[GD.NCT, GD.MAX_ELEM];
            int[, , ,] masterEsaByCT = new int[GD.NCT, GD.NETHNIC, GD.NSEX, GD.NAGE5];
            int[, , ,] mgraEsa;
            int[] mgraDetailedEsa;   // 320 element array for MGRA-specific eth,sex,SYA

            int[] numMgrasByCt = new int[GD.NCT];
            int[] esaForSingleCt = new int[GD.MAX_CHAR5];
            int[] total = new int[GD.NAGE5];

            filePath = "\\\\hana\\shared\\RES\\estimates & forecast\\CONCEP\\temp\\psat";
            
            //"\\\\hana\\shared\\temp\\concep\\psat";

            StreamWriter mat = new StreamWriter(filePath, false);
            mat.AutoFlush = true;
            /* get the detailed characteristics */
            ExtractCtPop(P, masterEsaByCT, numMgrasByCt, mgrasByCt, popByMgraInCt);

            /* build a temporary table with control totals - then select from the temp sorting the
                totals in ascending order */

            for (int ct = 0; ct < GD.NCT; ct++)
            {
                P.writeToStatusBox("Processing CT # " + ct + " ID = " + P.ct_list[ct, 0]);
                
                /* distribute the detailed charasteristic to the master vector */
                if (numMgrasByCt[ct] > 0)
                {
                    mgraEsa = new int[GD.MAX_ELEM, GD.NETHNIC, GD.NSEX, GD.NAGE5]; //Array.Clear(mgraEsa, 0, mgraEsa.Length);
                    LoadVector(masterEsaByCT, ct, esaForSingleCt, 1);
                    
                    for (int mgraCounter = 0; mgraCounter < numMgrasByCt[ct]; mgraCounter++)
                    {
                        if (popByMgraInCt[ct, mgraCounter] > 0)
                        {
                            mgraDetailedEsa = new int[GD.MAX_CHAR5];// Array.Clear(mgraDetailedEsa, 0, mgraDetailedEsa.Length);
                            Pachinkomgra(P, ct, mgraCounter, esaForSingleCt,
                                popByMgraInCt[ct, mgraCounter], mgraDetailedEsa, 320);
                            LoadVector(mgraEsa, mgraCounter, mgraDetailedEsa, 2);
                        }
                    }
                    /* write the output to ASCII */
                    WriteASCII(mat, GD.NETHNIC, GD.NSEX, GD.NAGE5, ct, mgraEsa, numMgrasByCt,
                        mgrasByCt, P.fyear);
                }
            }
            mat.Close();

            LoadTable(P);
        }
        
        /// <summary>
        /// Extract the controlling populations from ct and POPEST to mgra
        /// </summary>
        /// <param name="P"><value>PASEE master class</value></param>
        /// <param name="me"><value>master_esa struct array</value></param>
        /// <param name="mind"><value>mgra_index array (list of mgras in this ct</value></param>
        /// <param name="mids"><value>mgra identities array</value></param>
        /// <param name="mp"><value>mgra population array</value></param>

        // Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        public void ExtractCtPop(pasee P, int[, , ,] me, int[] mind, int[,] mids, int[,] mp)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int ct, i, j, k, l;

            P.writeToStatusBox("EXTRACTING DETAILED CT POPULATION");
            // open the connection
            P.sqlCommand.CommandText = "select ct,ethnicity,sex,age5,pop from age_age5_lookup a5," +
                P.TableNames.estimate_pop_ct + " a where estimates_year = " + P.fyear +
                " AND a.age = a5.age";
            try
            {
                P.sqlCnnConcep.Open();
                rdr = P.sqlCommand.ExecuteReader();
                while (rdr.Read())
                {
                    i = rdr.GetInt32(0);
                    ct = P.GetCtIndex(i);
                    j = (int)rdr.GetByte(1);
                    k = (int)rdr.GetByte(2);
                    l = (int)rdr.GetByte(3);
                    me[ct, j, k, l] += rdr.GetInt32(4);

                }     // end while
                rdr.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                P.Close();
            }   // end catch

            P.writeToStatusBox("EXTRACTING DETAILED POPEST mgra POPULATION");

            // open the connection
            Array.Clear(mind, 0, mind.Length);
            Array.Clear(mids, 0, mids.Length);
            Array.Clear(mp, 0, mp.Length);
            P.sqlCommand.CommandText = "select ct,mgra,pop from " + P.TableNames.popest_estimate_mgra +
                " WHERE popest_year = " + P.fyear;
            rdr = P.sqlCommand.ExecuteReader();
            while (rdr.Read())
            {
                i = rdr.GetInt32(0);
                ct = P.GetCtIndex(i);
                j = mind[ct];
                mids[ct, j] = rdr.GetInt32(1);
                mp[ct, j] = rdr.GetInt32(2);
                ++mind[ct];
            }     // end while

            rdr.Close();

            P.sqlCnnConcep.Close();
        }


        /* load the database table from ascii */

        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        public void LoadTable(pasee P)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int i = 0;
            //-------------------------------------------------------------------------

            P.writeToStatusBox("BULK LOADING TABULAR TABLE");
            P.sqlCommand.CommandTimeout = 600;
            try
            {
                P.sqlCnnConcep.Open();
                P.sqlCommand.CommandText = "select count(*) from " + P.TableNames.estimate_pop_mgra_tab + " where estimates_year = " + P.fyear;
                i = (int)P.sqlCommand.ExecuteScalar();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                P.Close();
            }

            // disable the indexes
            //P.sqlCommand.CommandText = "EXECUTE dbo.disable_detailed_pop_mgra_tab_indexes";
            //P.sqlCommand.ExecuteNonQuery();
            if (i > 0)
            {
                
                P.sqlCommand.CommandText = "delete from " + P.TableNames.estimate_pop_mgra_tab + " where estimates_year = " + P.fyear;
                try
                {
                    P.sqlCommand.ExecuteNonQuery();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                    P.Close();
                }
            }
            P.sqlCommand.CommandText = "select * into #temp from " + P.TableNames.estimate_pop_mgra_tab + " where 1 = 2";
            
            P.sqlCommand.ExecuteNonQuery();

            P.sqlCommand.CommandText = "bulk insert #temp " + //P.TableNames.estimate_pop_mgra_tab +
                " from '" + filePath + "' with (fieldterminator = ',', firstrow = 1)";

            try
            {
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                P.Close();
            }

            P.sqlCommand.CommandText = "INSERT INTO " + P.TableNames.estimate_pop_mgra_tab + " SELECT * FROM #temp";
            P.sqlCommand.ExecuteNonQuery();

            // update the totals
            P.sqlCommand.CommandText = "update " + P.TableNames.estimate_pop_mgra_tab +
                " set pop_0to4 = popm_0to4 + popf_0to4, " +
                " pop_5to9 = popm_5to9 + popf_5to9," +
                " pop_10to14 = popm_10to14 + popf_10to14," +
                " pop_15to17 = popm_15to17 + popf_15to17," +
                " pop_18to19 = popm_18to19 + popf_18to19," +
                " pop_20to24 = popm_20to24 + popf_20to24," +
                " pop_25to29 = popm_25to29 + popf_25to29," +
                " pop_30to34 = popm_30to34 + popf_30to34," +
                " pop_35to39 = popm_35to39 + popf_35to39," +
                " pop_40to44 = popm_40to44 + popf_40to44," +
                " pop_45to49 = popm_45to49 + popf_45to49," +
                " pop_50to54 = popm_50to54 + popf_50to54," +
                " pop_55to59 = popm_55to59 + popf_55to59," +
                " pop_60to61 = popm_60to61 + popf_60to61," +
                " pop_62to64 = popm_62to64 + popf_62to64," +
                " pop_65to69 = popm_65to69 + popf_65to69," +
                " pop_70to74 = popm_70to74 + popf_70to74," +
                " pop_75to79 = popm_75to79 + popf_75to79," +
                " pop_80to84 = popm_80to84 + popf_80to84," +
                " pop_85plus = popm_85plus + popf_85plus where estimates_year = " + P.fyear;
            try
            {
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                P.Close();
            }

            P.sqlCommand.CommandText = "update " + P.TableNames.estimate_pop_mgra_tab +
                          " set popm = (popm_0to4 + popm_5to9 + popm_10to14 + popm_15to17 + popm_18to19 + " +
                          "popm_20to24 + popm_25to29 + popm_30to34 + popm_35to39 + popm_40to44 + " +
                          "popm_45to49 + popm_50to54 + popm_55to59 + popm_60to61 + popm_62to64 + " +
                          "popm_65to69 + popm_70to74 + popm_75to79 + popm_80to84 + popm_85plus), " +
                          "popf = (popf_0to4 + popf_5to9 + popf_10to14 + popf_15to17 + popf_18to19 +" +
                        "popf_20to24 + popf_25to29 + popf_30to34 + popf_35to39 + popf_40to44 +" +
                        "popf_45to49 + popf_50to54 + popf_55to59 + popf_60to61 + popf_62to64 +" +
                        "popf_65to69 + popf_70to74 + popf_75to79 + popf_80to84 + popf_85plus) where estimates_year = " + P.fyear;;
            try
            {
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                throw exc;
            }

            P.sqlCommand.CommandText = "update " + P.TableNames.estimate_pop_mgra_tab + " set pop = popm + popf where estimates_year = " + P.fyear;
            try
            {
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                P.Close();
            }
            P.sqlCommand.CommandText = "execute " + P.TableNames.pasee_update_0_proc + " " +
                P.fyear + ", '" + P.TableNames.estimate_pop_mgra_tab + "', 'mgra'";
            try
            {
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());
                P.Close();
            }

            //P.sqlCommand.CommandText = "execute dbo.enable_detailed_pop_mgra_tab_indexes";
            //try
            //{
            //    P.sqlCommand.BeginExecuteNonQuery();
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.ToString(), e.GetType().ToString());
            //    P.Close();
            //}

            P.sqlCnnConcep.Close();
        }

        /// <summary>
        /// Distributes the ethnicity,sex,age array to the master (1-dimension) array
        /// </summary>
        /// <param name="esa"></param>
        /// <param name="g"></param>
        /// <param name="v"></param>
        /// <param name="control"></param>
        public void LoadVector(int[, , ,] esa, int indexToLoad,
            int[] targetArray, int control)
        {
            int index = 0;

            /* load the vector */
            if (control == 1)
            {
                for (int i = 1; i < GD.NETHNIC; ++i)
                {
                    for (int j = 1; j < GD.NSEX; ++j)
                    {
                        for (int k = 0; k < GD.NAGE5; ++k)
                        {
                            targetArray[index++] = esa[indexToLoad, i, j, k];
                        }
                    }
                }
            }
            else
            {
                for (int i = 1; i < GD.NETHNIC; ++i)
                {
                    for (int j = 1; j < GD.NSEX; ++j)
                    {
                        for (int k = 0; k < GD.NAGE5; ++k)
                        {
                            esa[indexToLoad, i, j, k] = targetArray[index++];
                        }
                    }
                }
            }
        }



        /* Pachinkomgra() */

        /* +1 distribution scheme using random number and cumulative distribution to assign
        values to elements in an array*/

        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        public void Pachinkomgra(pasee P, int whichct, int whichmgra, int[] master, int target, int[] mgra, int ncount)
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

                lcl_sum = P.GetArraySum(master, ncount);
                P.GetArrayStats(master, lcl_sum, ncount, local_dist, cum_prob);
                /* get the random number between 1 and 100*/
                where = ran.Next(1, 100);

                int tt = Array.BinarySearch<double>(cum_prob, (double)where);
                /* look for the index of the cum_prob <= the random number */
                for (k = 0; k < ncount; ++k)
                {
                    if (where > cum_prob[k])     /* is the random number greater than this cum_prob */
                        continue;       /* then continue looking */
                    else     /* otherwise, use this index to increment */
                    {
                        i1 = k;     /* save the index in the cum_prob*/
                        break;
                    }
                }     /* end for i */

                if (master[i1] > 0)
                {
                    mgra[i1] += 1;
                    master[i1] -= 1;
                    target -= 1;
                }
                ++loop_count;
            }     /* end while */

            if (loop_count >= 40000)
            {
                MessageBox.Show("Pachinko did not resolve in 40000 iterations for CT " + whichct.ToString() +
                    " mgra " + whichmgra.ToString());
            }     /* end if */

        }     /* end procedure Pachinkomgra */

        /*****************************************************************************/

        /* WriteASCII() */

        /* output estimates to ascii table for loading into Ingres */

        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        public void WriteASCII(StreamWriter mat, int ne, int ns, int na,
                               int ct_index, int[, , ,] data, int[] mind, int[,] mids, int popestYear)
        {
            int age, eth, pop, mgra;
            int i, j;
            string str1;

            for (i = 0; i < mind[ct_index]; ++i)
            {
                mgra = mids[ct_index, i];
                for (eth = 1; eth < ne; ++eth)
                {
                    // start the output string with mgra, ethnicity and 22 totals with 0 
                    // (total, 20 age group totals and 1 male total
                    str1 = popestYear + "," + mgra + "," + eth + ",";
                    for (j = 0; j < 21; ++j)
                        str1 += "0,";
                    //write the male total place holder, then 20 male age groups
                    str1 += "0,";

                    for (age = 0; age < na; ++age)
                    {
                        pop = data[i, eth, 1, age];
                        str1 += pop.ToString() + ",";
                    }
                    // add the female total
                    str1 += "0,";
                    // now he first 19 female age groups
                    for (age = 0; age < na - 1; ++age)
                    {
                        pop = data[i, eth, 2, age];
                        str1 += data[i, eth, 2, age] + ",";
                    }
                    str1 += data[i, eth, 2, age];

                    // final female age group and return
                   // str1 += data[i, eth, 2, na - 1];
                    try
                    {
                        mat.WriteLine(str1);
                    }
                    catch (IOException e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                }
            }
        }
    }
}