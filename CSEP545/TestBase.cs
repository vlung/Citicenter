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
    using TP;

    class MasterTest
    {
        public void ExecuteAll()
        {
            StartProcesses();
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
            
            rmcars.SelfDestruct(2);
            rmrooms.SelfDestruct(2);
            
            Thread.Sleep(1000);

            StopTM();

            Pause("Press Enter To Start TM");
            StartTM();

            Pause("Press Enter to Start Cars RM");
            StartCarsRM();

            Pause("Press Enter to Start Rooms RM");
            StartRoomsRM();

            Pause("Press Enter to Exit");
            StopProcesses();
        }

        public void Pause(string message)
        {
            Console.WriteLine(message);
            Console.ReadLine();
        }

        public void Pause()
        {
            Pause("Press Enter to Continue");
        }

        static void StartTM()
        {
            Process.Start("MyTM.exe", "");
        }

        static void StartWC()
        {
            Process.Start("MyWC.exe", "");
        }

        static void StartCarsRM()
        {
            Process.Start("MyRM.exe", "-n car -p 8082");
        }

        static void StartRoomsRM()
        {
            Process.Start("MyRM.exe", "-n room -p 8083");
        }

        static void StartFlightsRM()
        {
            Process.Start("MyRM.exe", "-n flight -p 8081");
        }

        static void StartRMs()
        {
            StartRoomsRM();
            StartCarsRM();
            StartFlightsRM();
        }

        static void StartProcesses()
        {
            StartTM();
            StartWC();
            StartRMs();
           
        }

        static void StopTM()
        {
            StopProcess("MyTM");
        }

        static void StopWC()
        {
            StopProcess("MyWC");
        }

        static void StopRMs()
        {
            StopProcess("MyRM");
        }

        static void StopProcesses()
        {
            StopWC();
            StopRMs();
            StopTM();
        }

        static void StopProcess(string name)
        {
            foreach (Process p in Process.GetProcessesByName(name))
            {
                p.Kill();
            }
        }
    }
}


