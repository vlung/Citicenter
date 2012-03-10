namespace CSEP545
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TP;

    class BasicDemo :TestBase
    {
        #region Test Data

        private string[][] carData = 
            {
                new string[]{ "Boston",         "5",    "75"},
                new string[]{ "Los Angeles",    "8",    "85"},
                new string[]{ "Kirland",        "10",   "63"},
            };

        private string[][] flightData = 
            {
                new string[]{ "AA1234",     "10",   "500"},
                new string[]{ "DL2255",     "3",    "160"},
                new string[]{ "AK6767",     "8",    "330"},
            };

        private string[][] roomData = 
            {
                new string[]{ "Boston",     "10",   "30"},
                new string[]{ "New York",   "3",    "110"},
                new string[]{ "Kirkland",   "8",    "45"},
            };

        #endregion

        public override void ExecuteAll()
        {
            // clean up
            DeleteDataFiles();
            Console.Clear();

            // start WC, TM, and RoomRM
            PrintHeader("BASIC FUNCTIONALITY DEMO");
            StartAll();
            
            PrintDataStore(null);
            Pause();

            // insert some data in the store
            TestAddData();
            Pause();

            // flight resource tests
            TestFlightMethods();
            Pause();

            StopAll();
            PrintHeader("DONE BASIC FUNCTIONALITY DEMO");
            Pause();
        }

        private void TestAddData()
        {
            Console.Clear();
            PrintHeader("Add resources (cars, flights, rooms) to the system");

            Transaction tx = GetWC().Start();

            PrintDataStore(tx);

            Console.WriteLine("Add flights:");
            PrintSeparator();
            foreach (string[] data in flightData)
            {
                GetWC().AddSeats(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} seats at ${3} on flight {1}", tx, data[0], data[1], data[2]);
            }
            PrintSeparator();

            Console.WriteLine("Add cars:");
            PrintSeparator();
            foreach (string[] data in carData)
            {
                GetWC().AddCars(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} cars at ${3} in {1}", tx, data[0], data[1], data[2]);
            }
            PrintSeparator();

            Console.WriteLine("Adding rooms:");
            PrintSeparator();
            foreach (string[] data in roomData)
            {
                GetWC().AddRooms(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} rooms at ${3} in {1}", tx, data[0], data[1], data[2]);
            }
            PrintSeparator();

            // read the inventory before commit
            PrintDataStore(null);
            Pause();

            // commit
            GetWC().Commit(tx);
            Console.WriteLine("{0}: Commited", tx);

            // read the inventory after commit
            PrintDataStore(null);
        }

        #region Test Flight Methods

        private void TestFlightMethods()
        {
            Console.Clear();
            PrintHeader("Manipulate flight resources");

            string[] data = flightData[2];
            Console.WriteLine("Modifying flight {0}", data[0]);

            TestFlightUpdate(data);
            Pause();
            TestFlightQuery(data);
            Pause();
            TestFlightDelete(data);
        }

        private void TestFlightUpdate(string[] data)
        {
            Transaction tx = StartAndLogTransaction();
            Console.WriteLine();
            PrintFlightInventory(tx);

            Console.WriteLine("Change flight price:");
            PrintSeparator();
            GetWC().AddSeats(tx, data[0], 0, 320);
            Console.WriteLine("{0}: Added {2} seats at ${3} on flight {1}", tx, data[0], 0, 320);

            Console.WriteLine();
            Console.WriteLine("Adding seats:");
            PrintSeparator();
            GetWC().AddSeats(tx, data[0], 10, 320);
            Console.WriteLine("{0}: Added {2} seats at ${3} on flight {1}", tx, data[0], 10, 320);

            Console.WriteLine();
            Console.WriteLine("Deleting seats:");
            PrintSeparator();
            GetWC().DeleteSeats(tx, data[0], 5);
            Console.WriteLine("{0}: Deleted {2} seats on flight {1}", tx, data[0], 5);

            CommitAndLogTransaction(tx);

            Console.WriteLine();
            PrintFlightInventory(null);
        }

        private void TestFlightQuery(string[] data)
        {
            Console.WriteLine();
            Transaction tx = StartAndLogTransaction();

            Console.WriteLine("Query flight info:");
            PrintSeparator();
            int seats = GetWC().QueryFlight(tx, data[0]);
            Console.WriteLine("{0}: Flight {1} has {2} available seats", tx, data[0], seats);
            int price = GetWC().QueryFlightPrice(tx, data[0]);
            Console.WriteLine("{0}: Flight {1} has a seat price of ${2}", tx, data[0], price);

            AbortAndLogTransaction(tx);
        }

        private void TestFlightDelete(string[] data)
        {
            Console.WriteLine();
            Transaction tx = StartAndLogTransaction();
            Console.WriteLine();
            PrintFlightInventory(tx);

            Console.WriteLine();
            Console.WriteLine("Deleting flight:");
            PrintSeparator();
            GetWC().DeleteFlight(tx, data[0]);
            Console.WriteLine("{0}: Deleted flight {1}", tx, data[0]);

            try
            {
                int result = GetWC().QueryFlight(tx, data[0]);
                Console.WriteLine("BUG");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("{0}: Could not query seats on flight {1}", tx, data[0]);
            }

            try
            {
                int result = GetWC().QueryFlightPrice(tx, data[0]);
                Console.WriteLine("BUG");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("{0}: Could not query seat price on flight {1}", tx, data[0]);
            }

            // commit
            CommitAndLogTransaction(tx);

            Console.WriteLine();
            PrintFlightInventory(null);
        }
        
        #endregion

    }
}
