namespace CSEP545
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TP;

    class DutchAuctionDemo : TestBase
    {
        public const int MIN_UNITS = 1;
        public const int MAX_UNITS = 5;

        public const int MIN_PARTICIPANTS = 5;
        public const int MAX_PARTICIPANTS = 10;

        public const int MIN_PRICE = 200;
        public const int MAX_PRICE = 1000;

        public static string FLIGHT = "DL3122";
        public static string LOCATION = "Seattle, WA";

        public static Random RAND_GEN = new Random(DateTime.Now.Millisecond);

        public static List<int[]> DEMAND_TABLE = new List<int[]>();

        public override void ExecuteAll()
        {
            // clean up
            DeleteDataFiles();
            Console.Clear();

            // start WC, TM, and RoomRM
            PrintHeader("WELCOME TO THE DUTCH AUCTION DEMO");
            StartAll();

            SetupTheAuction();
            PrintDataStore(null);
            Pause("Press Enter To Start the auction");

            RunAuction();
            Pause();

            StopAll();
            PrintHeader("DONE DUTCH AUCTION DEMO");
            Pause();
        }

        #region Setup Methods

        private void SetupTheAuction()
        {
            // generate the number of participants
            int participants = RAND_GEN.Next(MIN_PARTICIPANTS, MAX_PARTICIPANTS);
            int units = 0;
            for (int count = 0; count < participants; count++)
            {
                int[] data = 
                {
                    RAND_GEN.Next(MIN_UNITS, MAX_UNITS),
                    RAND_GEN.Next(MIN_PRICE, MAX_PRICE)
                };

                DEMAND_TABLE.Add(data);
                units += data[0];
            }

            Console.WriteLine("Today we are selling a package comprise of:");
            Console.WriteLine("\tSeats on flight: {0}", FLIGHT);
            Console.WriteLine("\tRental Cars and Rooms in: {0}", LOCATION);
            PrintSeparator();
            Console.WriteLine("Participants: {0}", participants);
            Console.WriteLine("Units Desired: {0}", units);

            // add the units to the store
            Transaction tx = GetWC().Start();
            GetWC().AddCars(tx, LOCATION, units, MAX_PRICE / 4);
            GetWC().AddSeats(tx, FLIGHT, units, MAX_PRICE / 2);
            GetWC().AddRooms(tx, LOCATION, units, MAX_PRICE / 4);
            GetWC().Commit(tx);
        }

        private void RunAuction()
        {
            Action<object> manager = (object obj) =>
            {
                bool done = false;
                int lastUnitCount = int.MaxValue;
                int priceDropCount = 0;
                while (!done)
                {
                    Transaction tx = GetWC().Start();;
                    try
                    {
                        int currentUnitCount = GetWC().QueryCar(tx, LOCATION);
                        if (0 == currentUnitCount)
                        {
                            // sold all the units
                            GetWC().Abort(tx);
                            done = true;
                            continue;
                        }

                        if (lastUnitCount != currentUnitCount)
                        {
                            // no proce drop needed
                            lastUnitCount = currentUnitCount;
                            GetWC().Abort(tx);
                            System.Threading.Thread.Sleep(RAND_GEN.Next(400, 500));
                            continue;
                        }

                        int priceDrop = RAND_GEN.Next(MIN_PRICE / 20, MIN_PRICE / 10);
                        switch (priceDropCount % 3)
                        {
                            case 0:
                                {
                                    int currentPrice = GetWC().QueryCarPrice(tx, LOCATION);
                                    if (currentPrice < MIN_PRICE / 4)
                                    {
                                        // not dropping price any longer
                                        break;
                                    }

                                    GetWC().AddCars(tx, LOCATION, 0, currentPrice - priceDrop);
                                    Console.WriteLine("Dropping car price by ${0} to ${1}", priceDrop, currentPrice - priceDrop);
                                }
                                break;

                            case 1:
                                {
                                    int currentPrice = GetWC().QueryFlightPrice(tx, FLIGHT);
                                    if (currentPrice < MIN_PRICE / 2)
                                    {
                                        // not dropping price any longer
                                        break;
                                    }

                                    GetWC().AddSeats(tx, FLIGHT, 0, currentPrice - priceDrop);
                                    Console.WriteLine("Dropping seat price by ${0} to ${1}", priceDrop, currentPrice - priceDrop);
                                }
                                break;

                            case 2:
                                {
                                    int currentPrice = GetWC().QueryRoomPrice(tx, LOCATION);
                                    if (currentPrice < MIN_PRICE / 4)
                                    {
                                        // not dropping price any longer
                                        break;
                                    }

                                    GetWC().AddRooms(tx, LOCATION, 0, currentPrice - priceDrop);
                                    Console.WriteLine("Dropping room price by ${0} to ${1}", priceDrop, currentPrice - priceDrop);
                                }
                                break;
                        }

                        priceDropCount++;
                        GetWC().Commit(tx);
                        System.Threading.Thread.Sleep(RAND_GEN.Next(1500, 2000));
                    }
                    catch (DeadLockDetected)
                    {
                        GetWC().Abort(tx);
                    }
                    catch (AbortTransationException)
                    {
                        GetWC().Abort(tx);
                    }
                }

                Console.WriteLine("Sold all the units!");
            };

            Action<object> agent = (object index) =>
            {
                int id = int.Parse((string)index);

                int bidUnits = DEMAND_TABLE[id][0];
                int bidPrice = DEMAND_TABLE[id][1];

                while (true)
                {
                    Transaction tx = GetWC().Start();
                    int carPrice = GetWC().QueryCarPrice(tx, LOCATION);
                    int flighPrice = GetWC().QueryFlightPrice(tx, FLIGHT);
                    int roomPrice = GetWC().QueryRoomPrice(tx, LOCATION);
                    if (0 == id % 2)
                    {
                        GetWC().Commit(tx);
                    }
                    else
                    {
                        GetWC().Abort(tx);
                    }

                    // compute the price
                    int currentPrice = carPrice + flighPrice + roomPrice;
                    if (currentPrice > bidPrice)
                    {
                        System.Threading.Thread.Sleep(RAND_GEN.Next(4000, 5000));
                        continue;
                    }
                    bidPrice = currentPrice;

                    // reserve the units
                    for (int count = 0; count < bidUnits; count++)
                    {
                        Customer customer = new Customer();
                        bool result = GetWC().ReserveItinerary(customer, new string[] { FLIGHT }, LOCATION, true, true);
                        if (!result)
                        {
                            count--;
                            System.Threading.Thread.Sleep(RAND_GEN.Next(400, 500));
                        }
                    }

                    break;
                }

                Console.WriteLine("Bidder {0} filled order of {1} units at ${2}. Desired Price ${3}", id, bidUnits, bidPrice, DEMAND_TABLE[id][1]);
            };

            // start the agent tasks
            for (int count = 0; count < DEMAND_TABLE.Count; count++)
            {
                Task.Factory.StartNew(agent, count.ToString());
            }

            // start the manager
            Task managerTask = Task.Factory.StartNew(manager, "hellow");
            managerTask.Wait();

            PrintDataStore(null);
        }

        #endregion
    }
}
