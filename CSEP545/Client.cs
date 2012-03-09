using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TP;


namespace CSEP545
{
        /**
     * default command line client for the project.
     * 
     * <table border="1">
     * <tr>
     * <th>Command</th><th>Description</th>
     * </tr>
     * <tr><th colspan="2">Interpreter and Service</th></tr>
     * <tr><td>print</td><td>Print current transaction and customer information</td></tr>
     * <tr><td>exit or quit</td><td>Terminate the client.</td></tr>
     * <tr><th colspan="2">Transactions and Customer</th></tr>
     * <tr><td>begin</td><td>begin transaction</td></tr>
     * <tr><td>commit</td><td>commit transaction</td></tr>
     * <tr><td>abort</td><td>abort transaction</td></tr>
     * <tr><td>new [UUID]</td><td>Set current customer id. If UUID is not given, a random customer id is created.</td></tr>
     * <tr><th colspan="2">Managing Resources</th></tr>
     * <tr><td>add (car|seat|room) qty price</td><td>add resources</td></tr>
     * <tr><td>delete (car|seat|room) qty</td><td>delete resources</td></tr>
     * <tr><td>delete flight</td><td>cancel flight and all associate reservations.</td></tr>
     * <tr><td>query (car|seat|room) loc</td><td>query available resource at loc</td></tr>
     * <tr><td>price (car|seat|room) loc</td><td>query price of resource at loc</td></tr>
     * <tr><td>list (car|seat|room)</td><td>list registered resources.</td></tr>
     * <tr><th colspan="2">Managing Reservations</th></tr>
     * <tr><td>reserve f1 ... fN loc bookCar bookRoom</td>
     * <td>make a reservation for current customer.
     * f1 ... fN are flights.
     * bookCar and bookRoom are boolean values.</td></tr>
     * <tr><td>cancel [customer]</td><td>cancel reservation for given or current customer</td></tr>
     * <tr><td>query itinerary [customer]</td><td>query itinerary of given or current customer</td></tr>
     * <tr><td>price itinerary [customer]</td><td>query price of itinerary of given or current customer</td></tr>
     * <tr><td>list customer</td><td>list registered customers.</td></tr>
     * </table>
     */
    class CommandLineClient : TestBase
    {
        private WC myWC;
        private Customer customer;
        private Transaction tx;

        private string rmiHost;
        private int rmiPort;

        public CommandLineClient(string rmiHost, int rmiPort)
        {
            this.rmiHost = rmiHost;
            this.rmiPort = rmiPort;
        }

        public override void ExecuteAll()
        {
            // restart the whole system
            StopAll();
            StartAll();
            
            try
            {
                // connect to the WC
                this.myWC = (TP.WC)System.Activator.GetObject(typeof(RM), "http://" + this.rmiHost + ":" + this.rmiPort + "/WC.soap");
                
                Console.Clear();
                Console.WriteLine("Welcome to the interactive query client!");
                Console.WriteLine("----------------------------------------");

                String line;
                while (true)
                {                    
                    Console.Write("Enter Command: ");

                    if ((line = Console.In.ReadLine()) == null
                        || line.Equals("exit", StringComparison.InvariantCultureIgnoreCase)
                        || line.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }

                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        this.process(line);
                    }

                    Console.WriteLine("");
                }
                this.process("exit");
            }
            finally
            {
                try { this.process("exit"); }
                catch (Exception) { }

                // stop the system
                StopAll();
            }
        }

        #region Private Methods

        private bool parseBoolean(String s)
        {
            return "yYtT".IndexOf(s[0]) >= 0;
        }

        private void process(String line)
        {
            String[] cmds = Regex.Split(line, "\\s+");
            if (cmds.Length == 0) return;

            String command = cmds[0];

            try
            {
                if (string.Compare("begin", command, true) == 0)
                {
                    if (tx != null)
                    {
                        throw new System.InvalidOperationException("transaction " + tx + " is active");
                    }
                    tx = myWC.Start();
                }
                else if (string.Compare("commit", command, true) == 0)
                {
                    if (tx == null)
                    {
                        throw new System.InvalidOperationException("no transaction is active");
                    }
                    myWC.Commit(tx);
                    tx = null;
                }
                else if (string.Compare("abort", command, true) == 0)
                {
                    if (tx != null)
                    {
                        myWC.Abort(tx);
                    }
                    tx = null;
                }
                else if (string.Compare("cancel", command, true) == 0)
                {
                    Customer c = cmds.Length == 2 ? new Customer(cmds[1]) : customer;
                    if (myWC.CancelItinerary(c))
                    {
                        Console.WriteLine("Itinierary for customer " + c + " has canceled");
                    }
                    else
                    {
                        Console.WriteLine("Failed to cancel itinerary for customer " + c);
                    }
                }
                else if (string.Compare("new", command, true) == 0)
                {
                    if (customer != null)
                    {
                        Console.WriteLine("Old customer = " + customer);
                    }
                    if (cmds.Length == 2)
                    {
                        customer = new Customer(cmds[1]);
                    }
                    else
                    {
                        customer = new Customer();
                    }
                    Console.WriteLine("current customer = " + customer);
                }
                else if (string.Compare("print", command, true) == 0)
                {
                    if (tx != null)
                    {
                        Console.WriteLine("Current transaction = " + tx);
                    }
                    if (customer != null)
                    {
                        Console.WriteLine("Current customer = " + customer);
                    }
                }
                else if (string.Compare("add", command, true) == 0)
                {
                    if (tx == null)
                    {
                        throw new System.InvalidOperationException("no active transaction");
                    }
                    String target = cmds[1];
                    String loc = cmds[2];
                    int num = Int32.Parse(cmds[3]);
                    int price = Int32.Parse(cmds[4]);
                    if (string.Compare("car", target, true) == 0)
                    {
                        myWC.AddCars(tx, loc, num, price);
                    }
                    else if (string.Compare("seat", target, true) == 0)
                    {
                        myWC.AddSeats(tx, loc, num, price);
                    }
                    else if (string.Compare("room", target, true) == 0)
                    {
                        myWC.AddRooms(tx, loc, num, price);
                    }
                    else
                    {
                        throw new System.InvalidOperationException("usage: add (seat|car|room) loc qty price");
                    }
                }
                else if (string.Compare("del", command, true) == 0 || string.Compare("delete", command, true) == 0)
                {
                    if (tx == null)
                    {
                        throw new System.InvalidOperationException("no active transaction");
                    }
                    String target = cmds[1];
                    String loc = cmds[2];
                    if (string.Compare("flight", target, true) == 0)
                    {
                        myWC.DeleteFlight(tx, loc);
                    }
                    else
                    {
                        int num = Int32.Parse(cmds[3]);
                        if (string.Compare("car", target, true) == 0)
                        {
                            myWC.DeleteCars(tx, loc, num);
                        }
                        else if (string.Compare("seat", target, true) == 0)
                        {
                            myWC.DeleteSeats(tx, loc, num);
                        }
                        else if (string.Compare("room", target, true) == 0)
                        {
                            myWC.DeleteRooms(tx, loc, num);
                        }
                        else
                        {
                            throw new System.InvalidOperationException("usage: " + command + " (seat|car|room) qty | flight");
                        }
                    }
                }
                else if (string.Compare("query", command, true) == 0)
                {
                    if (tx == null)
                    {
                        throw new System.InvalidOperationException("no active transaction");
                    }
                    String target = cmds[1];
                    if (string.Compare("itinerary", (target), true) == 0 || string.Compare("i", (target), true) == 0)
                    {
                        Customer c = customer;
                        if (cmds.Length == 3)
                        {
                            c = new Customer(cmds[2]);
                        }
                        else if (c == null)
                        {
                            throw new System.InvalidOperationException("usage: query (itinerary|i) [customer]");
                        }
                        String result = myWC.QueryItinerary(tx, c);
                        Console.WriteLine("Itinerary for customer " + c);
                        Console.WriteLine(result);
                    }
                    else
                    {
                        String loc = cmds[2];
                        int avail = 0;
                        if (string.Compare("car", (target), true) == 0 || string.Compare("c", (target), true) == 0)
                        {
                            avail = myWC.QueryCar(tx, loc);
                        }
                        else if (string.Compare("flight", (target), true) == 0 || string.Compare("f", (target), true) == 0)
                        {
                            avail = myWC.QueryFlight(tx, loc);
                        }
                        else if (string.Compare("room", (target), true) == 0 || string.Compare("r", (target), true) == 0)
                        {
                            avail = myWC.QueryRoom(tx, loc);
                        }
                        else
                        {
                            throw new System.InvalidOperationException("usage: query (car|flight|room) loc");
                        }
                        Console.WriteLine(avail + " " + target + " are available at " + loc);
                    }
                }
                else if (string.Compare("price", (command), true) == 0)
                {
                    if (tx == null)
                    {
                        throw new System.InvalidOperationException("no active transaction");
                    }
                    String target = cmds[1];

                    if (string.Compare("itinerary", (target), true) == 0 || string.Compare("i", (target), true) == 0)
                    {
                        Customer c = customer;
                        if (cmds.Length == 3)
                        {
                            c = new Customer(cmds[2]);
                        }
                        else if (c == null)
                        {
                            throw new System.InvalidOperationException("usage: price (itinerary|i) [customer]");
                        }
                        int total = myWC.QueryItineraryPrice(tx, c);
                        Console.WriteLine("Total price of itinerary for customer " + c + " = " + total);
                    }
                    else
                    {
                        String loc = cmds[2];
                        int avail = 0;
                        if (string.Compare("car", (target), true) == 0 || string.Compare("c", (target), true) == 0)
                        {
                            avail = myWC.QueryCarPrice(tx, loc);
                        }
                        else if (string.Compare("flight", (target), true) == 0 || string.Compare("f", (target), true) == 0)
                        {
                            avail = myWC.QueryFlightPrice(tx, loc);
                        }
                        else if (string.Compare("room", (target), true) == 0 || string.Compare("r", (target), true) == 0)
                        {
                            avail = myWC.QueryRoomPrice(tx, loc);
                        }
                        else
                        {
                            throw new System.InvalidOperationException("usage: price (car|flight|room) loc");
                        }
                        Console.WriteLine("price to reserve " + target + " at " + loc + " = " + avail);
                    }
                }
                else if (string.Compare("list", (command), true) == 0)
                {
                    if (tx == null)
                    {
                        throw new System.InvalidOperationException("no active transaction");
                    }
                    String target = cmds[1];
                    if (string.Compare("customer", (target), true) == 0)
                    {
                        Customer[] customers = myWC.ListCustomers(tx);
                        Console.WriteLine(customers.Length + " customers exist");
                        foreach (Customer c in customers)
                        {
                            Console.WriteLine(c);
                        }
                    }
                    else
                    {
                        String[] result = new String[0];
                        if (string.Compare("car", (target), true) == 0)
                        {
                            result = myWC.ListCars(tx);
                        }
                        else if (string.Compare("flight", (target), true) == 0)
                        {
                            result = myWC.ListFlights(tx);
                        }
                        else if (string.Compare("room", (target), true) == 0)
                        {
                            result = myWC.ListRooms(tx);
                        }
                        else
                        {
                            throw new System.InvalidOperationException("usage: list (car|flight|room|customer)");
                        }

                        Console.WriteLine(result.Length + " items exist");
                        foreach (String e in result)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
                else if (string.Compare("reserve", (command), true) == 0)
                {
                    if (cmds.Length < 5)
                    {
                        throw new System.InvalidOperationException("usage: reserve flight1 ... flightN loc bookCar bookRoom");
                    }
                    if (customer == null)
                    {
                        throw new System.InvalidOperationException("no customer was set.");
                    }

                    String[] flights = new String[cmds.Length - 3 - 1];
                    Array.Copy(cmds, 1, flights, 0, cmds.Length - 3 - 1);
                    String loc = cmds[cmds.Length - 3];
                    bool bcar = parseBoolean(cmds[cmds.Length - 2]);
                    bool broom = parseBoolean(cmds[cmds.Length - 1]);

                    if (myWC.ReserveItinerary(customer, flights, loc, bcar, broom))
                    {
                        Console.WriteLine("reserved the itinerary for customer " + customer);
                    }
                    else
                    {
                        Console.WriteLine("Failed to reserve the itinerary for customer " + customer);
                    }
                }
                else if (string.Compare("exit", (command), true) == 0 || string.Compare("quit", (command), true) == 0)
                {
                    try
                    {
                        if (tx != null) myWC.Abort(tx);
                    }
                    catch (Exception) { }
                }
                else
                {
                    throw new System.InvalidOperationException("unknown command: " + command);
                }
            }
            catch (InvalidOperationException x)
            {
                Console.WriteLine(x.ToString());
            }
            catch (Exception x)
            {
                Console.WriteLine(x.ToString());
            }
        }

        #endregion

        /**
         * @param args
         */

        // UNCOMMENT THE MAIN MODULE IF YOU WANT TO WORK WITH CLIENT 
        /* 
        private static void printUsage()
        {
            Console.Error.WriteLine("Usage: CommandLineClient " +
                    " [--rmiHost host]" +
                    " [--rmiPort port]" +
                    " ...");
            Environment.Exit(-1);
        }
         static void Main(String[] args) {    
             int argc = 0;
             // default RMI registry connection information
             String rmiHost = "127.0.0.1";
             int rmiPort = 8086;
        
             for ( ; argc < args.Length; ++argc ) {
                 String arg = args[argc];
                 if ( arg[0] != '-' ) break;
            
                 if ( string.Compare("--rmiHost",arg,true) == 0 ) {
                     rmiHost = args[++argc];
                 } else if (string.Compare("--rmiPort",arg,true) == 0 ) {
                     rmiPort = Int32.Parse(args[++argc]);
                 } else {
                     printUsage();
                 }
             }
        
             CommandLineClient client = new CommandLineClient();
             client.run(rmiHost, rmiPort);
         }*/
    }
}
