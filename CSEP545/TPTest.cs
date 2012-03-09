using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSEP545
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.Text;
    using System.Threading;
    using System.Diagnostics;
    using System.IO;
    using TP;

    class TPTest : TestBase
    {
        public override void ExecuteAll()
        {
            // delete old data files
            var dbFiles = Directory.EnumerateFiles(".", "*.tpdb");
            foreach (string file in dbFiles)
            {
                File.Delete(file);
                Console.WriteLine("Deleting RM data file: {0}", file);
            }
            
            // delete TM data file
            if (File.Exists(MyTM.OutstandingTransactions.GetFilename()))
            {
                File.Delete(MyTM.OutstandingTransactions.GetFilename());
                Console.WriteLine("Deleting {0}", MyTM.OutstandingTransactions.GetFilename());
            }

            StartAll();

            TP.WC wc = (TP.WC)System.Activator.GetObject(typeof(RM), "http://localhost:8086/WC.soap");
            RM rmcars = (RM)System.Activator.GetObject(typeof(RM), "http://localhost:8082/RM.soap");
            RM rmrooms = (RM)System.Activator.GetObject(typeof(RM), "http://localhost:8083/RM.soap");

            PauseHeading("Basic commit scenario");

            Console.WriteLine("T1 - Adding 1 car, 1 room and 1 seat to location SEA");

            Transaction t1 = wc.Start();

            wc.AddCars(t1, "SEA", 1, 50);
            wc.AddRooms(t1, "SEA", 1, 100);
            wc.AddSeats(t1, "SEA", 1, 200);
            wc.Commit(t1);

            Console.WriteLine("T1 committed");

            Console.WriteLine("T2 started");
            Transaction t2 = wc.Start();

            Console.WriteLine("T2 queries inventory...");
            InventoryAtLocation(t2, wc, "SEA");
            wc.Abort(t2);

            Console.WriteLine("Another transaction reserving an itinerary: 1 car, 1 room and 1 seat at location SEA");
            Customer c = new Customer();
            wc.ReserveItinerary(c, new string[] {"SEA"}, "SEA", true, true);

            Console.WriteLine("t3 started");
            Transaction t3 = wc.Start();

            Console.WriteLine("T3 queries inventory...");
            InventoryAtLocation(t3, wc, "SEA");
            wc.Abort(t3);
            
            Pause("Press Enter to Exit");
            StopAll();
        }

        public void InventoryAtLocation(Transaction t, WC wc, string loc)
        {
            Console.WriteLine(string.Format("Cars: {0} at ${1} each", wc.QueryCar(t, loc), wc.QueryCarPrice(t, loc)));
            Console.WriteLine(string.Format("Flights: {0} at ${1} each", wc.QueryFlight(t, loc), wc.QueryFlightPrice(t, loc)));
            Console.WriteLine(string.Format("Rooms: {0} at ${1} each", wc.QueryRoom(t, loc), wc.QueryRoomPrice(t, loc)));
        }

        public void PauseHeading(string message)
        {
            Pause(string.Format("==========\n{0}\n==========\nPress any key to begin", message));
        }
    }
}


