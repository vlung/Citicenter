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
            //TPTest tptest = new TPTest();
            //tptest.ExecuteAll();

            // bring up the interactive client            
            CommandLineClient client = new CommandLineClient("localhost", 8086);
            client.ExecuteAll();
        }
    }
}
