using System;
using System.Collections.Generic;
using System.Text;


namespace CSEP545
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            // THERE IS ALSO A MAIN MODULE IN Client.cs ... COMMENT THIS MAIN MODULE IF YOU ARE WORKING WITH CLIENT 
            MasterTest mt = new MasterTest();
            mt.ExecuteAll();

            //TPTest tptest = new TPTest();
            //tptest.ExecuteAll();
        }
    }
}
