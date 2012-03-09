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

            // start WC, TM, and RoomRM
            StartAll();
            Pause();

            // add some data
            AddRoomData();
            ReadAllRoomData(null);
            Pause();

            // add data and abort
            AddRoomDataAndAbort();
            ReadAllRoomData(null);
            Pause();

            // concurrent read
            ReadConcurrently();
            ReadAllRoomData(null);
            Pause();

            // dealock
            Deadlock();
            ReadAllRoomData(null);
            Pause();

            // rm dies
            //CrashDuringCommit();
            //ReadAllRoomData(null);
            //Pause();

            // shut down
            StopAll();
            Pause();
        }

        #endregion

        #region Private Methods

        private void AddRoomData()
        {
            Console.WriteLine("Populate RM with some data:");
            Console.WriteLine("---------------------------");

            Transaction tx = GetWC().Start();
            Console.WriteLine("{0}: Started", tx);
            
            foreach (string[] data in roomData1)
            {
                GetWC().AddRooms(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} rooms for {3} in {1}", tx, data[0], data[1], data[2]);
            }
            GetWC().Commit(tx);
            Console.WriteLine("{0}: Commited", tx);
        }

        private void AddRoomDataAndAbort()
        {
            Console.WriteLine("Try to add more data to RM:");
            Console.WriteLine("---------------------------");

            Transaction tx = GetWC().Start();
            Console.WriteLine("{0}: Started", tx);
            
            foreach (string[] data in roomData2)
            {
                GetWC().AddRooms(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} rooms for {3} in {1}", tx, data[0], data[1], data[2]);
            }

            ReadAllRoomData(tx);

            GetWC().Abort(tx);
            Console.WriteLine("{0}: Aborted", tx);
        }

        private void CrashDuringCommit()
        {
            Console.WriteLine("Failure Handling:");
            Console.WriteLine("---------------------------");

            Transaction tx1 = GetWC().Start();
            Console.WriteLine("{0}: Started", tx1);

            bool result = GetWC().AddRooms(tx1, roomData1[0][0], 5, int.Parse(roomData1[0][2]));
            if (result)
            {
                Console.WriteLine("{0}: Adding {1} rooms in {2} for {3}", tx1, 5, roomData1[0][0], roomData1[0][2]);
            }

            Console.WriteLine("{0}: Call RM.SelfDestruct(2)", tx1);
            GetRoomsRM().SelfDestruct(2);
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

            Pause();
            StartRoomsRM();
            ReadAllRoomData(null);

        }

        private void Deadlock()
        {
            Console.WriteLine("Concurrent read and write:");
            Console.WriteLine("---------------------------");

            Transaction tx1 = GetWC().Start();
            Console.WriteLine("{0}: Started", tx1);

            bool result = GetWC().DeleteRooms(tx1, roomData1[0][0], 5);
            if (result)
            {
                Console.WriteLine("{0}: Deleting {1} rooms in {2}", tx1, 5, roomData1[0][0]);
            }
            Pause();

            Transaction tx2 = GetWC().Start();
            Console.WriteLine("{0}: Started", tx2);
            while (true)
            {
                try
                {
                    int roomCount = GetWC().QueryRoom(tx2, roomData1[0][0]);
                    Console.WriteLine("{0}: There are {1} rooms in {2}", tx1, roomCount, roomData1[0][0]);
                    
                    GetWC().Commit(tx2);
                    Console.WriteLine("{0}: Commited", tx2);

                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0}: {1}", tx2, e.Message);
                    Console.Write("Type 'unblock' commit the write transaction or enter: ");
                    string line = Console.ReadLine();
                    if (line == "unblock")
                    {
                        GetWC().Commit(tx1);
                        Console.WriteLine("{0}: Commited", tx1);
                    }
                }
            }
        }

        private void ReadConcurrently()
        {
            Console.WriteLine("Concurrent read of a resource:");
            Console.WriteLine("---------------------------");

            Transaction tx1 = GetWC().Start();
            Console.WriteLine("{0}: Started", tx1);

            Transaction tx2 = GetWC().Start();
            Console.WriteLine("{0}: Started", tx2);

            int roomCount = GetWC().QueryRoom(tx1, roomData1[0][0]);
            Console.WriteLine("{0}: There are {1} rooms in {2}", tx1, roomCount, roomData1[0][0]);

            int roomPrice = GetWC().QueryRoomPrice(tx2, roomData1[0][0]);
            Console.WriteLine("{0}: The price if {1} for rooms in {2}", tx2, roomPrice, roomData1[0][0]);

            Pause();

            GetWC().Abort(tx1);
            Console.WriteLine("{0}: Aborted", tx1);
            GetWC().Abort(tx2);
            Console.WriteLine("{0}: Aborted", tx2);
        }

        private void ReadAllRoomData(Transaction context)
        {
            Console.WriteLine("Rooms Inventory:");

            Transaction tx = context;
            if (null == context)
            {
                tx = GetWC().Start();
                Console.WriteLine("{0}: Started", tx);
            }

            string[] rooms = GetWC().ListRooms(tx);
            foreach (string room in rooms)
            {
                Console.WriteLine("{0}: {1}", tx, room);
            }

            if (null == context)
            {
                GetWC().Commit(tx);
                Console.WriteLine("{0}: Commited", tx);
            }
        }


        #endregion
    }
}
