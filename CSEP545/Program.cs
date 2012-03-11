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
            bool done = false;

            while (!done)
            {
                Console.Clear();
                Console.WriteLine("=========\nMAIN MENU\n========\n1. Basic demo\n2. RM Demo\n3. TP Demo\n4. Dutch auction\n5: Console client\n6: Exit\nSelect:");
                string input = Console.ReadLine().Trim();

                switch (input)
                {
                    case "1":
                        {
                            BasicDemo basicDemo = new BasicDemo();
                            basicDemo.ExecuteAll();
                            break;
                        }
                    case "2":
                        {
                            RMDemo rmDemo = new RMDemo();
                            rmDemo.ExecuteAll();
                            break;
                        }
                    case "3":
                        {
                            TPDemo tpDemo = new TPDemo();
                            tpDemo.ExecuteAll();
                            break;
                        }
                    case "4":
                        {
                            DutchAuctionDemo daDemo = new DutchAuctionDemo();
                            daDemo.ExecuteAll();
                            break;
                        }
                    case "5":
                        {
                            // bring up the interactive client            
                            CommandLineClient client = new CommandLineClient("localhost", 8086);
                            client.ExecuteAll();
                            break;
                        }
                    case "6":
                        {
                            done = true;
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("Invalid option!");
                            break;
                        }
                }
            }
        }
    }
}
