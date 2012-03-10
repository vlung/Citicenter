namespace CSEP545
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TP;

    class TPDemo : TestBase
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

            PreparedWithNoResponse();
            Pause();

            PreparedWithTimeout();
            Pause();

            CommitTimesOut();
            Pause();

            AbortTimesOut();
            Pause();

            // shut down
            StopAll();
            Pause();
        }

        #endregion

        #region Private Methods

        private void PreparedWithNoResponse()
        {
            Console.WriteLine("RM response NO to Request to Prepare:");
            Console.WriteLine("-------------------------------------");

            Transaction tx = GetWC().Start();
            Console.WriteLine("{0}: Started", tx);

            foreach (string[] data in roomData1)
            {
                GetWC().AddRooms(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} rooms for {3} in {1}", tx, data[0], data[1], data[2]);
            }

            GetRoomsRM().SetPrepareFailure(PrepareFailure.PrepareReturnsNo);
            GetRoomsRM().SetCommitFailure(false);
            GetRoomsRM().SetAbortFailure(false);

            GetWC().Commit(tx);
            Console.WriteLine("{0}: Aborted", tx);
        }

        private void PreparedWithTimeout()
        {
            Console.WriteLine("RM times out to Request to Prepare:");
            Console.WriteLine("-----------------------------------");

            Transaction tx = GetWC().Start();
            Console.WriteLine("{0}: Started", tx);

            foreach (string[] data in roomData1)
            {
                GetWC().AddRooms(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} rooms for {3} in {1}", tx, data[0], data[1], data[2]);
            }

            GetRoomsRM().SetPrepareFailure(PrepareFailure.PrepareTimesOut);
            GetRoomsRM().SetCommitFailure(false);
            GetRoomsRM().SetAbortFailure(false);

            GetWC().Commit(tx);
            Console.WriteLine("{0}: Aborted", tx);
        }

        private void CommitTimesOut()
        {
            Console.WriteLine("RM times out on Commit:");
            Console.WriteLine("-----------------------");

            Transaction tx = GetWC().Start();
            Console.WriteLine("{0}: Started", tx);

            foreach (string[] data in roomData1)
            {
                GetWC().AddRooms(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} rooms for {3} in {1}", tx, data[0], data[1], data[2]);
            }

            GetRoomsRM().SetPrepareFailure(PrepareFailure.NoFailure);
            GetRoomsRM().SetCommitFailure(true);
            GetRoomsRM().SetAbortFailure(true);

            GetWC().Commit(tx);
            Console.WriteLine("{0}: Failed to commit and abort since RM is not responding. Notice in TP window that TP will attempt to re-commit in recovery.", tx);

            Pause("Press Enter to make the RM operational again");
            GetRoomsRM().SetCommitFailure(false);
            GetRoomsRM().SetAbortFailure(false);

            Pause("Wait for TM to do the re-commit. Press Enter when done");

            Console.WriteLine("{0}: Committed", tx);
        }

        private void AbortTimesOut()
        {
            Console.WriteLine("RM times out on Abort:");
            Console.WriteLine("----------------------");

            Transaction tx = GetWC().Start();
            Console.WriteLine("{0}: Started", tx);

            foreach (string[] data in roomData1)
            {
                GetWC().AddRooms(tx, data[0], int.Parse(data[1]), int.Parse(data[2]));
                Console.WriteLine("{0}: Added {2} rooms for {3} in {1}", tx, data[0], data[1], data[2]);
            }

            GetRoomsRM().SetPrepareFailure(PrepareFailure.NoFailure);
            GetRoomsRM().SetCommitFailure(false);
            GetRoomsRM().SetAbortFailure(true);

            GetWC().Abort(tx);
            Console.WriteLine("{0}: Failed to abort since RM is not responding. Notice in TP window that TP will not re-abort in recovery since the TP has implemented Presumed Abort.", tx);

            Console.WriteLine("{0}: Aborted", tx);
        }
        #endregion
    }
}