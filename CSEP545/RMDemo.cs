namespace CSEP545
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TP;

    class RMDemo : TestBase
    {
        #region Test Data

        private string[][] roomData1 = 
            {
                new string[]{ "Boston",     "10",   "20"},
                new string[]{ "New York",   "3",    "45"},
                new string[]{ "Kirkland",   "8",    "35"},
            };

        private string[][] roomData2 = 
            {
                new string[]{ "Montreal",   "15",   "75"},
            };

        #endregion

        #region TestBase Methods
        public override void ExecuteAll()
        {
            // clean up
            DeleteDataFiles();
            Console.Clear();

            // start WC, TM, and RoomRM
            PrintHeader("RM FAILURE HANDLING DEMO");
            StartAll();

            AddRoomData();
            PrintRoomInventory(null);
            Pause();

            // concurrent read
            ReadConcurrently();
            Pause();

            // dealock
            Deadlock();
            Pause();

            // rm dies
            CrashDuringCommit();
            Pause();

            // shut down
            StopAll();

            PrintHeader("DONE RM FAILURE DEMO");
            Pause();
        }

        #endregion

        #region Private Methods

        private void AddRoomData()
        {
            Transaction tx = GetWC().Start();
            foreach (string[] data in roomData1)
            {
                GetWC().AddRooms(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
            }
            GetWC().Commit(tx);
        }

        private void CrashDuringCommit()
        {
            Console.Clear();
            PrintHeader("Failure Handling on RM crash");
            PrintRoomInventory(null);

            Transaction tx1 = StartAndLogTransaction();
            bool result = GetWC().AddRooms(tx1, roomData1[0][0], 5, int.Parse(roomData1[0][2]));
            if (result)
            {
                Console.WriteLine("{0}: Adding {1} rooms in {2} for {3}", tx1, 5, roomData1[0][0], roomData1[0][2]);
                PrintRoomInventory(tx1);
            }
            
            GetRoomsRM().SelfDestruct(2);
            Console.WriteLine("{0}: Called RM.SelfDestruct(2)", tx1);
            Pause();

            try
            {
                Console.WriteLine("{0}: Try commit", tx1);
                GetWC().Commit(tx1);
                Console.WriteLine("{0}: UNEXPECTED ERROR", tx1);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}", tx1, e.Message);
            }

            Pause("Restart Rooms RM");
            StartRoomsRM();
            System.Threading.Thread.Sleep(2000);
            PrintRoomInventory(null);
            Pause();

            tx1 = StartAndLogTransaction();

            result = GetWC().AddRooms(tx1, roomData1[0][0], 5, int.Parse(roomData1[0][2]));
            if (result)
            {
                Console.WriteLine("{0}: Adding {1} rooms in {2} for {3}", tx1, 5, roomData1[0][0], roomData1[0][2]);
                PrintRoomInventory(tx1);
            }

            GetRoomsRM().SelfDestruct(10);
            Console.WriteLine("{0}: Called RM.SelfDestruct(10)", tx1);
            Pause();

            try
            {
                Console.WriteLine("{0}: Try commit", tx1);
                GetWC().Commit(tx1);
                Console.WriteLine("{0}: UNEXPECTED ERROR", tx1);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}", tx1, e.Message);
            }

            Pause("Start Rooms RM");
            StartRoomsRM();
            System.Threading.Thread.Sleep(2000);
            PrintRoomInventory(null);
        }

        private void Deadlock()
        {
            Console.Clear();
            PrintHeader("Concurrent read and write");
            PrintRoomInventory(null);

            Transaction tx1 = StartAndLogTransaction();
            bool result = GetWC().DeleteRooms(tx1, roomData1[0][0], 5);
            if (result)
            {
                Console.WriteLine("{0}: Deleted {1} rooms in {2}", tx1, 5, roomData1[0][0]);
            }
            Pause();

            Transaction tx2 = StartAndLogTransaction();
            while (true)
            {
                try
                {
                    Console.WriteLine("{0}: Trying to query number of rooms in {1}", tx2, roomData1[0][0]);
                    int roomCount = GetWC().QueryRoom(tx2, roomData1[0][0]);
                    Console.WriteLine("{0}: There are {1} rooms in {2}", tx2, roomCount, roomData1[0][0]);

                    CommitAndLogTransaction(tx2);

                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0}: {1}", tx2, e.Message);
                    Console.Write("Type 'unblock' commit the write transaction or enter: ");
                    string line = Console.ReadLine();
                    if (line == "unblock")
                    {
                        CommitAndLogTransaction(tx1);
                    }
                }
            }

            PrintRoomInventory(null);
        }

        private void ReadConcurrently()
        {
            Console.Clear();
            PrintHeader("Concurrent read of a resource:");
            PrintRoomInventory(null);

            Transaction tx1 = StartAndLogTransaction();
            Console.WriteLine("{0}: Started", tx1);

            Transaction tx2 = GetWC().Start();
            Console.WriteLine("{0}: Started", tx2);

            int roomCount = GetWC().QueryRoom(tx1, roomData1[0][0]);
            Console.WriteLine("{0}: There are {1} rooms in {2}", tx1, roomCount, roomData1[0][0]);

            int roomPrice = GetWC().QueryRoomPrice(tx2, roomData1[0][0]);
            Console.WriteLine("{0}: The price if {1} for rooms in {2}", tx2, roomPrice, roomData1[0][0]);

            Pause();

            AbortAndLogTransaction(tx1);
            AbortAndLogTransaction(tx2);
            PrintRoomInventory(null);
        }


        #endregion
    }
}
