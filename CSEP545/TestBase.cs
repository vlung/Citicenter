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

    abstract class TestBase
    {
        #region Private Members

        private static WC s_wc;
        private static TM s_tm;
        
        private static RM s_carsRM;
        private static RM s_flightsRM;
        private static RM s_roomsRM;

        #endregion

        public abstract void ExecuteAll();

        /*
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
            Pause();

            TP.WC wc = (TP.WC)System.Activator.GetObject(typeof(RM), "http://localhost:8086/WC.soap");
            RM rmcars = (RM)System.Activator.GetObject(typeof(RM), "http://localhost:8082/RM.soap");
            RM rmrooms = (RM)System.Activator.GetObject(typeof(RM), "http://localhost:8083/RM.soap");
            Transaction t = wc.Start();
            Customer c = new Customer();
            wc.AddCars(t,"Car1", 1, 1);
            wc.AddRooms(t, "Room1", 2, 1);
            wc.AddSeats(t, "flt231", 2, 1);
            wc.Commit(t);
            string[] flights = new string[0];
            wc.ReserveItinerary(c,flights,"Room1",false,true);

            t = wc.Start();
            Console.WriteLine(wc.QueryItinerary(t,c));
            string [] rooms = wc.ListRooms(t);
            foreach (string r in rooms)
                Console.WriteLine(r);
            wc.Commit(t);
            
            rmcars.Shutdown();
            rmrooms.Shutdown();
            
            Thread.Sleep(1000);

            StopTM();

            Pause("Press Enter To Start TM");
            StartTM();

            Pause("Press Enter to Start Cars RM");
            StartCarsRM();

            Pause("Press Enter to Start Rooms RM");
            StartRoomsRM();

            t = wc.Start();
            wc.AddCars(t, "Car2", 1, 1);
            wc.AddSeats(t, "flt345", 2, 1);
            wc.Commit(t);

            t = wc.Start();
            string[] cars = wc.ListCars(t);
            foreach (string car in cars)
            {
                Console.WriteLine(car);
            }

            flights = wc.ListFlights(t);
            foreach (string f in flights)
            {
                Console.WriteLine(f);
            }
            wc.Commit(t);

            Pause("Press Enter to Exit");
            StopAll();
        }
         */
        protected WC GetWC()
        {
            bool done = false;
            while (!done)
            {
                try
                {
                    if (null == s_wc)
                    {
                        s_wc = (WC)System.Activator.GetObject(typeof(WC), "http://localhost:8086/WC.soap");
                    }
                    s_wc.ToString();

                    done = true;
                }
                catch (Exception)
                {
                    s_wc = null;
                }
            }

            return s_wc;
        }

        protected TM GetTM()
        {
            bool done = false;
            while (!done)
            {
                try
                {
                    if (null == s_tm)
                    {
                        s_tm = (TM)System.Activator.GetObject(typeof(TM), "http://localhost:8089/TM.soap");
                    }
                    s_tm.ToString();

                    done = true;
                }
                catch (Exception)
                {
                    s_tm = null;
                }
            }

            return s_tm;
        }

        protected RM GetCarsRM()
        {
            s_carsRM = GetRM(s_carsRM, "http://localhost:8081/RM.soap");
            return s_carsRM;
        }

        protected RM GetFlightsRM()
        {
            s_flightsRM = GetRM(s_flightsRM, "http://localhost:8082/RM.soap");
            return s_flightsRM;
        }

        protected RM GetRoomsRM()
        {
            s_roomsRM = GetRM(s_roomsRM, "http://localhost:8083/RM.soap");
            return s_roomsRM;
        }

        protected RM GetRM(RM rm, string url)
        {
            bool done = false;
            while (!done)
            {
                try
                {
                    if (null == rm)
                    {
                        rm = (RM)System.Activator.GetObject(typeof(RM), url);
                    }
                    rm.GetName();
                    done = true;
                }
                catch (Exception)
                {
                    rm = null;
                }
            }

            return rm;
        }

        protected void DeleteDataFiles()
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
        }

        protected void PrintHeader(string message)
        {
            PrintSeparator();
            Console.WriteLine(message);
            PrintSeparator();
        }

        protected void PrintSeparator()
        {
            Console.WriteLine("------------------------------------------------------------");
        }
        
        protected void Pause(string message)
        {
            Console.WriteLine();
            Console.WriteLine(message);
            Console.ReadLine();
        }

        protected void Pause()
        {
            Pause("Press Enter to Continue");
        }

        protected void StartTM()
        {
            Process.Start("MyTM.exe", "");
        }

        protected void StartWC()
        {
            Process.Start("MyWC.exe", "");
        }

        protected void StartCarsRM()
        {
            Process.Start("MyRM.exe", "-n car -p 8081");
        }

        protected void StartFlightsRM()
        {
            Process.Start("MyRM.exe", "-n flight -p 8082");
        }

        protected void StartRoomsRM()
        {
            Process.Start("MyRM.exe", "-n room -p 8083");
        }

        protected void StartRMs()
        {
            StartRoomsRM();
            StartCarsRM();
            StartFlightsRM();
        }

        protected void StartAll()
        {
            StartTM();
            StartWC();
            StartRMs();
        }

        protected void StopTM()
        {
            StopProcess("MyTM");
        }

        protected void StopWC()
        {
            StopProcess("MyWC");
        }

        protected void StopRMs()
        {
            StopProcess("MyRM");
        }

        protected void StopAll()
        {
            StopWC();
            StopRMs();
            StopTM();
        }

        protected void StopProcess(string name)
        {
            foreach (Process p in Process.GetProcessesByName(name))
            {
                p.Kill();
            }
        }
    }
}


