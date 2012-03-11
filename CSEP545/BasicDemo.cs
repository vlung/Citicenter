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

        private static string[][] carData = 
            {
                new string[]{ "Boston",         "5",    "75"},
                new string[]{ "Los Angeles",    "1",    "85"},
                new string[]{ "Kirland",        "10",   "63"},
            };

        private static string[][] flightData = 
            {
                new string[]{ "AA1234",     "10",   "500"},
                new string[]{ "DL2255",     "1",    "160"},
                new string[]{ "AK6767",     "8",    "330"},
            };

        private static string[][] roomData = 
            {
                new string[]{ "Boston",     "10",   "30"},
                new string[]{ "New York",   "1",    "110"},
                new string[]{ "Kirkland",   "8",    "45"},
            };

        private static Customer[] customerData =
        {
            new Customer(),
            new Customer(),
            new Customer(),
            new Customer(),
            new Customer(),
            new Customer(),
            new Customer()
        };

        private static string[][] itineraryFlights =
            {
                new string[] {flightData[0][0], flightData[1][0]},
                new string[] {flightData[0][0]},
                new string[] {flightData[0][0]},
                new string[] {flightData[1][0]},
                new string[] {flightData[0][0]},
                new string[] {flightData[0][0]},
                new string[] {flightData[0][0]},
            };

        private static string[][] itineraryCarAndRoom =
            {
                new string[] {roomData[0][0], "true", "true", ""},
                new string[] {roomData[1][0], "false", "true", ""},
                new string[] {carData[1][0], "true", "false", ""},
                new string[] {roomData[0][0], "true", "true", "ArgumentException"},
                new string[] {roomData[1][0], "false", "true", "ArgumentException"},
                new string[] {carData[1][0], "true", "false", "ArgumentException"},
                new string[] {"Chicago", "true", "true", "ArgumentException"},
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

            // car resource tests
            TestCarMethods();
            Pause();

            // flight resource tests
            TestFlightMethods();
            Pause();

            // test room methods
            TestRoomMethods();
            Pause();

            TestItineraryMethods();
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

        #region Test Car Methods

        private void TestCarMethods()
        {
            Console.Clear();
            PrintHeader("Manipulate car resources");

            string[] data = carData[2];
            Console.WriteLine("Modifying cars in {0}", data[0]);

            TestCarUpdate(data);
            Pause();
            TestCarQuery(data);
            Pause();
            TestCarDelete(data);

        }

        private void TestCarUpdate(string[] data)
        {
            Transaction tx = StartAndLogTransaction();
            Console.WriteLine();
            PrintCarInventory(tx);

            Console.WriteLine("Change car price:");
            PrintSeparator();
            GetWC().AddCars(tx, data[0], 0, 320);
            Console.WriteLine("{0}: Added {2} cars at ${3} in {1}", tx, data[0], 0, 320);

            Console.WriteLine();
            Console.WriteLine("Adding cars:");
            PrintSeparator();
            GetWC().AddCars(tx, data[0], 10, 320);
            Console.WriteLine("{0}: Added {2} cars at ${3} in {1}", tx, data[0], 10, 320);

            Console.WriteLine();
            Console.WriteLine("Deleting cars:");
            PrintSeparator();
            GetWC().DeleteCars(tx, data[0], 5);
            Console.WriteLine("{0}: Deleted {2} cars in {1}", tx, data[0], 5);

            CommitAndLogTransaction(tx);

            Console.WriteLine();
            PrintCarInventory(null);
        }

        private void TestCarQuery(string[] data)
        {
            Console.WriteLine();
            Transaction tx = StartAndLogTransaction();

            Console.WriteLine("Query car info:");
            PrintSeparator();
            int seats = GetWC().QueryCar(tx, data[0]);
            Console.WriteLine("{0}: {1} has {2} cars available", tx, data[0], seats);
            int price = GetWC().QueryCarPrice(tx, data[0]);
            Console.WriteLine("{0}: {1} has car price of ${2}", tx, data[0], price);

            AbortAndLogTransaction(tx);
        }

        private void TestCarDelete(string[] data)
        {
            Console.WriteLine();
            Transaction tx = StartAndLogTransaction();
            Console.WriteLine();
            PrintCarInventory(tx);

            Console.WriteLine();
            Console.WriteLine("Query/Update cars in San Diego:");
            PrintSeparator();

            try
            {
                GetWC().DeleteCars(tx, "San Diego", 3);
            }
            catch (ArgumentException)
            {
                Console.WriteLine("{0}: Could not delete {2} cars in {1}", tx, "San Diego", 3);
            }

            try
            {
                int result = GetWC().QueryCar(tx, "San Diego");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("{0}: Could not find cars in {1}", tx, "San Diego");
            }

            try
            {

                int result = GetWC().QueryCarPrice(tx, "San Diego");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("{0}: Could not get price for cars in {1}", tx, "San Diego");
            }

            // commit
            CommitAndLogTransaction(tx);

            Console.WriteLine();
            PrintCarInventory(null);
        }

        #endregion

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

        #region Test Room Methods

        private void TestRoomMethods()
        {
            Console.Clear();
            PrintHeader("Manipulate room resources");

            string[] data = roomData[2];
            Console.WriteLine("Modifying rooms in {0}", data[0]);

            TestRoomUpdate(data);
            Pause();
            TestRoomQuery(data);
            Pause();
            TestRoomDelete(data);

        }

        private void TestRoomUpdate(string[] data)
        {
            Transaction tx = StartAndLogTransaction();
            Console.WriteLine();
            PrintRoomInventory(tx);

            Console.WriteLine("Change room price:");
            PrintSeparator();
            GetWC().AddRooms(tx, data[0], 0, 320);
            Console.WriteLine("{0}: Added {2} rooms at ${3} in {1}", tx, data[0], 0, 320);

            Console.WriteLine();
            Console.WriteLine("Adding rooms:");
            PrintSeparator();
            GetWC().AddRooms(tx, data[0], 10, 320);
            Console.WriteLine("{0}: Added {2} rooms at ${3} in {1}", tx, data[0], 10, 320);

            Console.WriteLine();
            Console.WriteLine("Deleting rooms:");
            PrintSeparator();
            GetWC().DeleteRooms(tx, data[0], 5);
            Console.WriteLine("{0}: Deleted {2} rooms in {1}", tx, data[0], 5);

            CommitAndLogTransaction(tx);

            Console.WriteLine();
            PrintRoomInventory(null);
        }

        private void TestRoomQuery(string[] data)
        {
            Console.WriteLine();
            Transaction tx = StartAndLogTransaction();

            Console.WriteLine("Query car info:");
            PrintSeparator();
            int seats = GetWC().QueryRoom(tx, data[0]);
            Console.WriteLine("{0}: {1} has {2} rooms available", tx, data[0], seats);
            int price = GetWC().QueryRoomPrice(tx, data[0]);
            Console.WriteLine("{0}: {1} has room price of ${2}", tx, data[0], price);

            AbortAndLogTransaction(tx);
        }

        private void TestRoomDelete(string[] data)
        {
            Console.WriteLine();
            Transaction tx = StartAndLogTransaction();
            Console.WriteLine();
            PrintRoomInventory(tx);

            Console.WriteLine();
            Console.WriteLine("Query/Update rooms in San Diego:");
            PrintSeparator();

            try
            {
                GetWC().DeleteRooms(tx, "San Diego", 3);
            }
            catch (ArgumentException)
            {
                Console.WriteLine("{0}: Could not delete {2} rooms in {1}", tx, "San Diego", 3);
            }

            try
            {
                int result = GetWC().QueryRoom(tx, "San Diego");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("{0}: Could not find rooms in {1}", tx, "San Diego");
            }

            try
            {

                int result = GetWC().QueryRoomPrice(tx, "San Diego");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("{0}: Could not get price for rooms in {1}", tx, "San Diego");
            }

            // commit
            CommitAndLogTransaction(tx);

            Console.WriteLine();
            PrintRoomInventory(null);
        }

        #endregion

        #region Test Itinerary Methods

        private void TestItineraryMethods()
        {
            Console.Clear();
            PrintHeader("Itinerary reservation");
            
            TestItineraryReserveMethod();
            Pause();
            TestItineraryQueryMethods();
            Pause();
            TestItineraryCancelMethod();
        }        

        private void TestItineraryReserveMethod()
        {
            Console.WriteLine();
            PrintDataStore(null);

            for (int idx = 0; idx < itineraryFlights.Length; idx++)
            {
                try
                {
                    bool result = GetWC().ReserveItinerary(
                        customerData[idx],
                        itineraryFlights[idx],
                        itineraryCarAndRoom[idx][0],
                        bool.Parse(itineraryCarAndRoom[idx][1]),
                        bool.Parse(itineraryCarAndRoom[idx][2]));
                    if (!result)
                    {
                        throw new ArgumentException();
                    }
                    Console.WriteLine(
                        "Customer {0} reserved flights=[{1}] Location=[{2}] Car=[{3}] Room=[{4}]",
                        customerData[idx],
                        string.Join(",", itineraryFlights[idx]),
                        itineraryCarAndRoom[idx][0],
                        itineraryCarAndRoom[idx][1],
                        itineraryCarAndRoom[idx][2]);
                }
                catch(Exception e)
                {
                    if (e.GetType().Name != itineraryCarAndRoom[idx][3])
                    {
                        Console.WriteLine("BUG");
                    }
                    else
                    {
                        Console.WriteLine(
                            "Customer {0} faild to reserve flights=[{1}] Location=[{2}] Car=[{3}] Room=[{4}]",
                            customerData[idx],
                            string.Join(",", itineraryFlights[idx]),
                            itineraryCarAndRoom[idx][0],
                            itineraryCarAndRoom[idx][1],
                            itineraryCarAndRoom[idx][2]);
                    }
                }
            }

            Console.WriteLine();
            PrintDataStore(null);
        }

        private void TestItineraryQueryMethods()
        {
            Transaction tx = StartAndLogTransaction();
            Console.WriteLine();
            PrintCustomerInventory(tx);

            Console.WriteLine("Query itinerary info:");
            PrintSeparator();

            string itinerary = GetWC().QueryItinerary(tx, customerData[0]);
            Console.WriteLine("{0}: Customer {1} reservert {2}", tx, customerData[0], itinerary);
            int price = GetWC().QueryItineraryPrice(tx, customerData[0]);
            Console.WriteLine("{0}: Customer {1} will pay ${2}", tx, customerData[0], price);

            try
            {
                itinerary = GetWC().QueryItinerary(tx, customerData[4]);
                if (string.IsNullOrEmpty(itinerary))
                {
                    throw new ArgumentException();
                }
                Console.WriteLine("Query Itinerary BUG");
            }
            catch (Exception)
            {
                Console.WriteLine("{0}: Customer {1} does not have a reservation", tx, customerData[4]);
            }

            CommitAndLogTransaction(tx);
        }

        private void TestItineraryCancelMethod()
        {
            Console.WriteLine("Cancel reservation:");
            PrintSeparator();
            GetWC().CancelItinerary(customerData[0]);
            Console.WriteLine("Customer {0} canceled reservation.", customerData[0]);

            Console.WriteLine();
            PrintDataStore(null);
        }



        #endregion
    }
}
