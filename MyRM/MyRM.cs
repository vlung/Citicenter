
namespace MyRM
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Channels.Http;
    using System.Text;
    using System.Linq;
    using TP;

    /// <summary>
    /// class MyRM implements TP.RM
    /// </summary>
    public class MyRM : System.MarshalByRefObject, TP.RM
    {
        #region Member Variables

        private string name = null;
        private StorageManager dataStore = null;

        private TP.TM transactionManager = null;

        #endregion

        public MyRM()
        {
            this.name = GlobalState.Name;
            this.dataStore = null;
        }

        public void SetName(string _name)
        {
            this.name = _name;
        }

        public string GetName()
        {
            return this.name;
        }

        /// <summary>
        /// Called to enlist with transaction manager if we are not in shutdown mode
        /// </summary>
        /// <param name="context"></param>
        public void Enlist(Transaction context)
        {
            if (GlobalState.Mode != GlobalState.RunMode.Loop)
            {
                // we are not intialized yet so just abort
                throw new AbortTransationException();
            }

            // enlist with TM
            if (!this.transactionManager.Enlist(context, this.GetName()))
            {
                throw new AbortTransationException();
            }
        }

        public enum PrepareToCommitFailure { NoFailure, PrepareReturnsNo, PrepareTimesOut };
        public PrepareToCommitFailure prepareToCommitFailure = PrepareToCommitFailure.NoFailure;

        public bool RequestToPrepare(TP.Transaction context)
        {
            if (prepareToCommitFailure == PrepareToCommitFailure.PrepareReturnsNo)
            {
                return false;
            }
            else if (prepareToCommitFailure == PrepareToCommitFailure.PrepareTimesOut)
            {
                while (true)
                {
                }
            }
            return true;
        }

        public void Commit(TP.Transaction context)
        {
            // commit transaction
            this.dataStore.Commit(context);
        }

        public void Abort(TP.Transaction context)
        {
            // abort transaction
            this.dataStore.Abort(context);
        }

        /// <summary>
        /// Adds a resource to the available ones
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rId"></param>
        /// <param name="count"></param>
        /// <param name="price"></param>
        /// <returns></returns>
        public bool Add(Transaction context, RID rId, int count, int price)
        {
            // enlist with TM
            this.Enlist(context);

            // read the resource
            Resource data = null;
            bool result = this.dataStore.Read(context, rId, out data);
            if (!result)
            {
                data = new Resource(rId);
            }

            // update the item
            data.incrCount(count);
            data.setPrice(price);

            // write the resource
            return this.dataStore.Write(context, rId, data);
        }

        /// <summary>
        /// Deletes a resource.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rId"></param>
        /// <returns></returns>
        public bool Delete(Transaction context, RID rId)
        {
            // enlist with TM
            this.Enlist(context);

            // remove the resource
            bool removed = this.dataStore.Write(context, rId, null);
            if (!removed)
            {
                return removed;
            }

            // drop all reservations on removed resource
            List<Customer> reservationIdList = null;
            removed = this.dataStore.Read(context, out reservationIdList);
            if (!removed)
            {
                return removed;
            }

            foreach (Customer cId in reservationIdList)
            {
                // read the reservation data
                Reservation data = null;
                removed = this.dataStore.Read(context, cId, out data);
                if (!removed || null == data)
                {
                    continue;
                }

                // update the reservation if needed
                if (!data.Resources.Contains(rId))
                {
                    continue;
                }
                data.Resources.Remove(rId);

                // write the data back
                removed = this.dataStore.Write(context, cId, data);
                if (!removed)
                {
                    // something went teribbly wrong
                    return removed;
                }
            }

            return removed;
        }

        public bool Delete(Transaction context, RID rId, int count)
        {
            // enlist with TM
            this.Enlist(context);

            // read in the resource
            Resource data = null;
            bool removed = this.dataStore.Read(context, rId, out data);
            if (!removed
                || null == data)
            {
                // silently discard
                return false;
            }

            // update the resource
            if (data.getCount() > count)
            {
                data.decrCount(count);
            }
            else
            {
                data.setCount(0);
            }

            // write the resource back
            return this.dataStore.Write(context, rId, data);
        }

        public bool Reserve(Transaction context, Customer customer, RID resource)
        {
            // enlist with TM
            this.Enlist(context);

            // get the resource info
            Resource item = null;
            bool result = this.dataStore.Read(context, resource, out item);
            if (!result)
            {
                throw new InvalidOperationException(resource + " does not exist!");
            }
            if (item.getCount() <= 0)
            {
                return false;
            }

            // get the reservation record
            Reservation data = null;
            result = this.dataStore.Read(context, customer, out data);
            if (!result)
            {
                data = new Reservation(customer);
            }

            // only allow one reservation per item
            if (data.Resources.Contains(resource))
            {
                return false;
            }

            // update the data
            data.Resources.Add(resource);
            item.decrCount();

            // write back the records
            result = this.dataStore.Write(context, customer, data);
            if (!result)
            {
                return result;
            }

            result = this.dataStore.Write(context, resource, item);
            if (!result)
            {
                return result;
            }

            return true;
        }

        public void UnReserve(Transaction context, Customer customer)
        {
            // enlist with TM
            this.Enlist(context);

            Reservation data = null;
            bool result = this.dataStore.Read(context, customer, out data);
            if (!result
                || null == data)
            {
                // silently discard
                return;
            }

            // update the reservations
            foreach (RID rId in data.Resources)
            {
                Resource item = null;

                // read in the resource
                result = this.dataStore.Read(context, rId, out item);
                if (!result)
                {
                    throw new InvalidOperationException(rId + " does not exist!");
                }

                // update the resource
                item.incrCount();

                // write back the resource
                result = this.dataStore.Write(context, rId, item);
                if (!result)
                {
                    throw new InvalidOperationException(rId + " could not be updated!");
                }
            }

            // delete the reservation
            result = this.dataStore.Write(context, customer, null);
            if (!result)
            {
                throw new InvalidOperationException(customer + " could not be un-reserved!");
            }
        }

        /// <summary>
        /// Need to add code here
        /// returns the amount available for the specified item type
        /// </summary>
        public int Query(Transaction context, RID rId)
        {
            // enlist with TM
            this.Enlist(context);

            Resource data = null;
            bool result = this.dataStore.Read(context, rId, out data);
            if (!result)
            {
                throw new ArgumentException(rId + " does not exist");
            }

            return data.getCount();
        }

        /// <summary>
        /// Need to add code here
        /// returns the price for the specified item type
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rId"></param>
        /// <returns></returns>
        public int QueryPrice(Transaction context, RID rId)
        {
            // enlist with TM
            this.Enlist(context);

            Resource data = null;
            bool result = this.dataStore.Read(context, rId, out data);
            if (!result)
            {
                throw new ArgumentException(rId + " does not exist");
            }

            return data.getPrice();
        }

        public string QueryReserved(Transaction context, Customer customer)
        {
            StringBuilder buffer = new StringBuilder(512);

            // enlist with TM
            this.Enlist(context);

            Reservation data = null;
            bool result = this.dataStore.Read(context, customer, out data);
            if (!result)
            {
                return buffer.ToString();
            }

            // build the output string
            foreach (RID rId in data.Resources)
            {
                if (buffer.Length > 0)
                {
                    buffer.Append(',');
                }
                buffer.Append(rId);
            }

            return buffer.ToString();
        }

        public int QueryReservedPrice(Transaction context, Customer customer)
        {
            int bill = 0;

            // enlist with TM
            this.Enlist(context);

            // read the reservation data
            Reservation data = null;
            bool result = this.dataStore.Read(context, customer, out data);
            if (!result)
            {
                return bill;
            }

            // tally up the resource prices
            foreach (RID rId in data.Resources)
            {
                Resource resource = null;

                // read the resource
                result = this.dataStore.Read(context, rId, out resource);
                if (!result)
                {
                    throw new InvalidOperationException(rId + " does not exist in RM");
                }

                bill += resource.getPrice();
            }

            return bill;
        }

        public Customer[] ListCustomers(Transaction context)
        {
            // enlist with TM
            this.Enlist(context);

            List<Customer> customerList = null;
            bool result = this.dataStore.Read(context, out customerList);
            if (!result)
            {
                throw new InvalidOperationException("Could not retrieve customer list!");
            }

            return customerList.ToArray();
        }

        public string[] ListResources(Transaction context, RID.Type type)
        {
            // enlist with TM
            this.Enlist(context);

            List<Resource> resourceList = null;
            bool result = this.dataStore.Read(context, type, out resourceList);
            if (!result)
            {
                throw new InvalidOperationException("Could not retrieve resource list!");
            }

            // convert to string
            return resourceList
                    .Select(c => c.ToString())
                        .ToArray();
        }

        /// <summary>
        /// NEED TO ADD CODE For STEP 2
        /// Calling shutdown causes RM to exit gracefully.
        /// This means, it waits for all the existing transactions 
        /// to end and enlist requests for new transactions are refused. 
        /// If any of the existing transactions blocks forever, 
        /// a retry/timeout mechanism is used to exit.
        /// No recovery is done on startup
        /// </summary>
        public void Shutdown()
        {
            // indicate that we are in shutdown
            lock (this.name)
            {
                GlobalState.Mode = GlobalState.RunMode.Wait;
            }
        }

        /// <summary>
        /*    Exit after the specified number of disk writes.
              Support for this method requires a wrapper around _write_ system
              call that decrements the counter set by this method.

              This counter should be set by default to 0, which implies that the wrapper
              will do nothing.  If it is non-zero, the wrapper should decrement
              the counter, see if it becomes zero, and if so, call exit(), otherwise
              continue to write. */
        /// </summary>
        public void SelfDestruct(int diskWritesToWait)
        {
        }

        /// <summary>
        /// Initializes the RM.
        /// </summary>
        public void Init(string name, string url, string tmUrl)
        {
            // set the name of the RM
            this.SetName(name);

            // init storage
            this.InitStorage();

            // recover the lost state
            this.Recovery();

            // startup 
            this.StartUp(url, tmUrl);

            // set the state
            GlobalState.Mode = GlobalState.RunMode.Loop;
        }

        /// <summary>
        /// Runs the execution loop
        /// </summary>
        public void Run()
        {
            // execution loop
            while (GlobalState.Mode == GlobalState.RunMode.Loop)
            {
                System.Threading.Thread.Sleep(2000);
            }

            // wait for active transactions loop
            int loopCount = 0;
            while (GlobalState.Mode == GlobalState.RunMode.Wait && loopCount < 15)
            {
                if (!this.dataStore.HasActiveTransactions())
                {
                    break;
                }

                Console.WriteLine("{0}: Waiting for transaction complete ({1} second(s))", GlobalState.Name, loopCount);
                System.Threading.Thread.Sleep(1000);
                loopCount++;
            }

            // close the stream files
            this.dataStore.Dispose();
        }


        protected void InitStorage()
        {
            // create the data store
            this.dataStore = StorageManager.CreateObject(string.Format("{0}.tpdb", this.name));
            if (null == dataStore)
            {
                throw new InvalidOperationException();
            }
            Console.WriteLine("{0}: Data store initialized", GlobalState.Name);
        }

        protected void Recovery()
        {
            // TODO recover state from database file
        }


        protected void StartUp(string url, string tmUrl)
        {
            if (string.Compare(tmUrl, "none", true) == 0)
            {
                return;
            }

            // Register with the TM
            while (transactionManager == null)
            {
                try
                {
                    this.transactionManager = (TP.TM)System.Activator.GetObject(typeof(TP.TM), tmUrl);

                    Transaction tid = this.transactionManager.Start();
                    this.transactionManager.Register(url + "$" + this.GetName());
                    this.transactionManager.Abort(tid);
                }
                catch (ArgumentException e)
                {
                    this.transactionManager = null;
                    Console.WriteLine(e.ToString());
                    Console.WriteLine("Waiting 1 second for Transaction Manager \"{0}\"", tmUrl);
                    System.Threading.Thread.Sleep(1000);
                }
            }
            Console.WriteLine("{0} RM: Transaction Manager retrieved at {1}", this.GetName(), tmUrl);

            // TODO: figure out what to do about the prepared transactions
        }

        #region Exception Classes

        [Serializable]
        public class AbortTransationException : System.Exception
        {
            public AbortTransationException()
                : base("Unable resolve logical address.")
            {
            }

            public AbortTransationException(string message)
                : base(message)
            {
            }

            public AbortTransationException(string message, System.Exception e)
                : base(message, e)
            {
            }
        }

        #endregion

        #region Process Startup

        internal class GlobalState
        {
            public enum RunMode
            {
                Loop,
                Wait,
                Kill
            }

            public static RunMode Mode = RunMode.Kill;
            public const string DefaultName = "MyRM";

            private const int MaxNameLength = 21;
            private static string name = null;

            public static string Name
            {
                get
                {
                    if (name == null)
                    {
                        name = DefaultName;
                    }

                    return name;
                }
                set
                {
                    if (name == null)
                    {
                        string temp = value.Trim();
                        if (temp.IndexOfAny(System.IO.Path.GetInvalidPathChars()) < 0 && temp.Length <= MaxNameLength)
                        {
                            name = temp;
                        }
                        else
                        {
                            throw new ArgumentException(String.Format("\"{0}\" is not a valid MyRM Name", temp), "Name");
                        }
                    }
                    else
                    {
                        throw new ArgumentException(String.Format("\"{0}\" is not valid at this time, MyRM Name is already set to \"{1}\"", value, name), "Name");
                    }
                }
            }
        }

        [Serializable]
        protected class RMParser : CommandLineParser
        {
            public RMParser()
            {
                Add("p", "Port", "The port this Resource Manager listens on", "8081");
                Add("n", "Name", "The name of this Resource Manager", GlobalState.DefaultName);
                Add("tm", "TM", "The URL of the Transaction Manager.  Specify \"NONE\" to run this RM in stand alone mode", "http://localhost:8089/TM.soap");
            }
        }

        static void Main(string[] args)
        {
            RMParser parser = new RMParser();
            if (!parser.Parse(args))
            {
                return;
            }

            GlobalState.Name = parser["n"].ToLower();
            string port_num = parser["p"];

            System.Console.WriteLine(string.Format("Starting resource manager for {0} on port {1}", GlobalState.Name, port_num));

            System.Collections.Specialized.ListDictionary channelProperties = new System.Collections.Specialized.ListDictionary();
            channelProperties.Add("port", port_num);
            channelProperties.Add("name", GlobalState.Name);
            
            HttpChannel channel = new HttpChannel(channelProperties, new SoapClientFormatterSinkProvider(), new SoapServerFormatterSinkProvider());
            System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(channel, false);
            System.Runtime.Remoting.RemotingConfiguration.RegisterWellKnownServiceType
            (Type.GetType("MyRM.MyRM")									    // Assembly name
                  , "RM.soap"												// URI
                  , System.Runtime.Remoting.WellKnownObjectMode.Singleton	// Instancing mode
            );

            // activate the object
            string[] urls = channel.GetUrlsForUri("RM.soap");
            if (1 != urls.Length)
            {
                throw new InvalidOperationException();
            }

            MyRM resourceManager = (MyRM)System.Activator.GetObject(typeof(TP.RM), urls[0]);
            if (null == resourceManager)
            {
                throw new InvalidProgramException();
            }

            // initialize and start RM
            Console.WriteLine("{0}: Initializing", GlobalState.Name);
            resourceManager.Init(parser["n"], urls[0], parser["tm"]);

            Console.WriteLine("{0}: Running", GlobalState.Name);
            resourceManager.Run();

            Console.WriteLine("{0}: Exitting", GlobalState.Name);
        }

        #endregion
    }
}
