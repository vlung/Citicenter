

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
            PrintHeader("BASIC data entry demo");
            StartAll();
            
            ReadInventory(null);
            Pause();

            // insert some data in the store
            AddData();
            Pause();

            StopAll();
            Pause();
        }

        private void AddData()
        {
            Console.Clear();
            PrintHeader("Add resources (cars, flights, rooms) to the system");

            Transaction tx = GetWC().Start();

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
            ReadInventory(null);
            Pause();

            // commit
            GetWC().Commit(tx);
            Console.WriteLine("{0}: Commited", tx);

            // read the inventory after commit
            ReadInventory(null);
        }

        private void ReadInventory(Transaction context)
        {
            Console.WriteLine();
            PrintHeader("Current State of the data store");

            Transaction tx = context;
            if (null == context)
            {
                tx = GetWC().Start();
                Console.WriteLine("{0}: Started", tx);
            }

            // read customers
            string[] customers = GetWC()
                                    .ListCustomers(tx)
                                    .Select(x => x.ToString())
                                    .ToArray();
            DisplayInventory("Customers", tx, customers);

            // read cars
            string[] cars = GetWC().ListCars(tx);
            DisplayInventory("Cars", tx, cars);

            // read flights
            string[] flights = GetWC().ListFlights(tx);
            DisplayInventory("Flights", tx, flights);

            // read rooms
            string[] rooms = GetWC().ListRooms(tx);
            DisplayInventory("Rooms", tx, rooms);

            if (null == context)
            {
                GetWC().Commit(tx);
                Console.WriteLine("{0}: Commited", tx);
            }

            PrintSeparator();
        }

        private void DisplayInventory(string type, Transaction context, string[] data)
        {
            Console.WriteLine("Found {0} {1} records", data.Length, type);
            if (0 == data.Length)
            {
                return;
            }

            PrintSeparator();
            foreach (string item in data)
            {
                Console.WriteLine("{0}: {1}", context, item);
            }
            PrintSeparator();
        }
    }
}
