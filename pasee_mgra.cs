/*****************************************************************************/
/*
  Source file name (path):  pasee_mgra.cs
  Program: concep
  Version: 4
  Programmer: tb
  Description:  pasee_mgra module of concep
 *          Version 4 introduces concep.config.exe to store all global constants, queries, table names and file names
   *		version 3.5 adds computations for using Series 13 geographies
           pasee_mgra uses Census aggregate age groups.  Essentially 5-year groups with
              15-17, 18-19, 60-61, and 62-64 differentiated
           How this works:
           1.  Store data by eth,sex, age for CT into single dimension array
               with 320 elements (20x2x8)
           2.  Compute the cumulative prob for this master vector
           3.  Run Pachinko on this master vector - control loop is pop of 
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
           detailed_pop_tab_ct : detailed demographic charactersitics by CT year YYYY
          
           popest_mgra : control mgra populations

           output tables
           detailed_pop_tab_mgra : detailed demographic characteristics by mgra year YYYY
          		

  PaseemgraMain()
  ExtractCtPop()
  LoadTable()
  LoadVector()
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

        /// PASEE to mgra main

        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------
        private string filePath;

        public void PaseeMGRAMain(pasee P)
        {
            int[,] popByMgraInCt = new int[P.NUM_CTS, P.MAX_ELEM];
            int[,] mgrasByCt = new int[P.NUM_CTS, P.MAX_ELEM];
            int[, , ,] masterEsaByCT = new int[P.NUM_CTS, P.NUM_ETH, P.NUM_SEX, P.NUM_AGE5];
            int[, , ,] mgraEsa;
            int[] mgraDetailedEsa;   // 320 element array for MGRA-specific eth,sex,SYA

            int[] numMgrasByCt = new int[P.NUM_CTS];
            int[] esaForSingleCt = new int[P.MAX_ASE5];
            int[] total = new int[P.NUM_AGE5];

            filePath = P.networkPath + "psat";

            StreamWriter mat = new StreamWriter(filePath, false);
            mat.AutoFlush = true;
            /* get the detailed characteristics */
            ExtractCtPop(P, masterEsaByCT, numMgrasByCt, mgrasByCt, popByMgraInCt);

            /* build a temporary table with control totals - then select from the temp sorting the totals in ascending order */

            for (int ct = 0; ct < P.NUM_CTS; ct++)
            {
                P.WriteToStatusBox("Processing CT # " + ct + " ID = " + P.ct_list[ct, 0]);

                /* distribute the detailed charasteristic to the master vector */
                if (numMgrasByCt[ct] > 0)
                {
                    mgraEsa = new int[P.MAX_ELEM, P.NUM_ETH, P.NUM_SEX, P.NUM_AGE5]; //Array.Clear(mgraEsa, 0, mgraEsa.Length);
                    LoadVector(P.NUM_ETH, P.NUM_SEX, P.NUM_AGE5, masterEsaByCT, ct, esaForSingleCt, 1);

                    for (int mgraCounter = 0; mgraCounter < numMgrasByCt[ct]; mgraCounter++)
                    {
                        if (popByMgraInCt[ct, mgraCounter] > 0)
                        {
                            mgraDetailedEsa = new int[P.MAX_ASE5];// Array.Clear(mgraDetailedEsa, 0, mgraDetailedEsa.Length);
                            int ret = CU.cUtil.PachinkoWithMasterDecrement(popByMgraInCt[ct, mgraCounter], esaForSingleCt, mgraDetailedEsa, 320);

                            LoadVector(P.NUM_ETH, P.NUM_SEX, P.NUM_AGE5, mgraEsa, mgraCounter, mgraDetailedEsa, 2);
                        } // end if
                    }  // end for mgraCounter

                    /* write the output to ASCII */
                    WriteASCII(mat, P.NUM_ETH, P.NUM_SEX, P.NUM_AGE5, ct, mgraEsa, numMgrasByCt, mgrasByCt, P.fyear);
                }  // end if
            }  // end for ct

            mat.Close();

            LoadTable(P);
        }  // end procedure PaseeMGRAMain()

        //***************************************************************************************************
        
        // ExtractCtPop()
        /// Extract the controlling populations from ct and POPEST to mgra

        // Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        public void ExtractCtPop(pasee P, int[, , ,] me, int[] mind, int[,] mids, int[,] mp)
        {
            System.Data.SqlClient.SqlDataReader rdr;
            int ct, i, j, k, l;

            P.WriteToStatusBox("EXTRACTING DETAILED CT POPULATION");
            // open the connection
            P.sqlCommand.CommandText = String.Format(P.appSettings["selectPASEE5"].Value, P.TN.popEstimatesCT, P.TN.age5Lookup, P.fyear);
            try
            {
                P.sqlConnection.Open();
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
            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }   // end catch
            finally
            {
                P.sqlConnection.Close();
            }

            P.WriteToStatusBox("EXTRACTING DETAILED POPEST mgra POPULATION");

            // open the connection
            Array.Clear(mind, 0, mind.Length);
            Array.Clear(mids, 0, mids.Length);
            Array.Clear(mp, 0, mp.Length);
            P.sqlCommand.CommandText = String.Format(P.appSettings["selectPASEE6"].Value, P.TN.popestMGRA, P.fyear);
            try
            {
                P.sqlConnection.Open();
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
            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }   // end catch
            finally
            {
                P.sqlConnection.Close();
            }
        }  // end procedure ExtractCtPop()

        //*********************************************************************

        // LoadTable()

        /* load the database table from ascii */

        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        public void LoadTable(pasee P)
        {

           
            //-------------------------------------------------------------------------

            P.WriteToStatusBox("BULK LOADING TABULAR TABLE");
            P.sqlCommand.CommandTimeout = 600;
            P.sqlCommand.CommandText = String.Format(P.appSettings["deleteFrom"].Value, P.TN.popEstimatesTabMGRA, P.fyear);
            try
            {
                P.sqlConnection.Open();
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                P.Close();
            }
            finally
            {
                P.sqlConnection.Close();
            }
            // have to leave the connection open to preserve the temp file

            P.sqlCommand.CommandText = "select * into #temp from " + P.TN.popEstimatesTabMGRA + " where 1 = 2";
            try
            {
                P.sqlConnection.Open();  // don't close this connection because we have data going into temp tables         

                P.sqlCommand.ExecuteNonQuery();

                P.sqlCommand.CommandText = string.Format(P.appSettings["bulkInsert"].Value, "#temp", filePath);
                //P.sqlCommand.CommandText = "bulk insert #temp from '" + filePath + "' with (fieldterminator = ',', firstrow = 1)";

                P.sqlCommand.ExecuteNonQuery();

                P.sqlCommand.CommandText = string.Format(P.appSettings["insertInto"].Value, P.TN.popEstimatesTabMGRA, " SELECT * FROM #temp");
                //P.sqlCommand.CommandText = "INSERT INTO " + P.TN.popEstimatesTabMGRA + " SELECT * FROM #temp";
                P.sqlCommand.ExecuteNonQuery();
            }  // end try
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                P.sqlConnection.Close();
            }

            P.WriteToStatusBox("UPDATING TOTALS ON TABULAR TABLE");
            // update the totals
            P.sqlCommand.CommandText = string.Format(P.appSettings["updatePASEE1"].Value, P.TN.popEstimatesTabMGRA, P.fyear);
            
            try
            {
                P.sqlConnection.Open();
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
            }
            finally
            {
                P.sqlConnection.Close();
            }
            P.sqlCommand.CommandText = string.Format(P.appSettings["updatePASEE2"].Value, P.TN.popEstimatesTabMGRA, P.fyear);
            
            try
            {
                P.sqlConnection.Open();
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());
                throw exc;
            }
            finally
            {
                P.sqlConnection.Close();
            }
            P.sqlCommand.CommandText = string.Format(P.appSettings["updatePASEE3"].Value, P.TN.popEstimatesTabMGRA, P.fyear);
            
            try
            {
                P.sqlConnection.Open();
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), exc.GetType().ToString());

            }
            finally
            {
                P.sqlConnection.Close();
            }

            P.sqlCommand.CommandText = "execute " + P.pasee_update_0_proc + " " + P.fyear + ", '" + P.TN.popEstimatesTabMGRA + "', 'mgra'";
            try
            {
                P.sqlConnection.Open();
                P.sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), e.GetType().ToString());

            }
            finally
            {
                P.sqlConnection.Close();
            }

        }  // end procedure LOadTAble()

        //*******************************************************************************************************

        // LoadVector()
        /* load the database table from ascii */

        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        /// Distributes the ethnicity,sex,age array to the master (1-dimension) array

        public void LoadVector(int NUM_ETH, int NUM_SEX, int NUM_AGE5, int[, , ,] esa, int indexToLoad, int[] targetArray, int control)
        {
            int index = 0;

            /* load the vector */
            if (control == 1)
            {
                for (int i = 1; i < NUM_ETH; ++i)
                {
                    for (int j = 1; j < NUM_SEX; ++j)
                    {
                        for (int k = 0; k < NUM_AGE5; ++k)
                        {
                            targetArray[index++] = esa[indexToLoad, i, j, k];
                        }  // end for k
                    }  // end for j
                }  // end for i
            }  // end if
            else
            {
                for (int i = 1; i < NUM_ETH; ++i)
                {
                    for (int j = 1; j < NUM_SEX; ++j)
                    {
                        for (int k = 0; k < NUM_AGE5; ++k)
                        {
                            esa[indexToLoad, i, j, k] = targetArray[index++];
                        }  // end for k
                    }  // end for j
                }  // end for i
            }  // end else
        }  // end procedure LoadVector()

        //***************************************************************************************************************      

        /* WriteASCII() */

        /* output estimates to ascii table for loading into Ingres */

        //Revision History
        //   Date       By   Description
        //   ------------------------------------------------------------------
        //   07/09/02   tb   started initial coding for this module
        //   ------------------------------------------------------------------

        public void WriteASCII(StreamWriter mat, int ne, int ns, int na, int ct_index, int[, , ,] data, int[] mind, int[,] mids, int estimatesYear)
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
                    str1 = estimatesYear + "," + mgra + "," + eth + ",";
                    for (j = 0; j < 21; ++j)
                        str1 += "0,";
                    //write the male total place holder, then 20 male age groups
                    str1 += "0,";

                    for (age = 0; age < na; ++age)
                    {
                        pop = data[i, eth, 1, age];
                        str1 += pop.ToString() + ",";
                    }  // end for age
                    // add the female total
                    str1 += "0,";
                    // now he first 19 female age groups
                    for (age = 0; age < na - 1; ++age)
                    {
                        pop = data[i, eth, 2, age];
                        str1 += data[i, eth, 2, age] + ",";
                    }  // end for age
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
                    }  // end catch
                }  // end for eth
            }  // end for i
        }  // end procedure WriteASCII()

        //************************************************************************************
    }  // end public class pasee_mgra
}  // end namespace pasee