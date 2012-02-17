using System;
using TP;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters;
using System.Collections;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting;
using System.Collections.Generic;

namespace MyTM
{
	/// <summary>
	/*  Transaction Manager */
	/// </summary>
    public class MyTM : System.MarshalByRefObject, TP.TM
    {
        #region Private Properties

        private HashSet<RM> resourceManagers;
        private Dictionary<Transaction, List<string>> activeTransactions;

        #endregion

        #region Public Methods

        public MyTM()
        {
            System.Console.WriteLine("Transaction Manager instantiated");
            resourceManagers = new HashSet<RM>();
            activeTransactions = new Dictionary<Transaction, List<string>>();
        }

        #region Methods called from WC

        /// <summary>
        /// Call from W to start a new transaction.
        /// </summary>
        /// <returns>new transaction object</returns>
        public Transaction Start()
        {
            Transaction context = new Transaction();
            System.Console.WriteLine(string.Format("TM: Transaction {0} started", context.Id));

            lock (this.activeTransactions)
            {
                this.activeTransactions.Add(context, new List<string>());
            }
            return context;
        }

        /// <summary>
        //	 Call from WC in response to a client's commit.
        /// </summary>
        /// <param name="context"></param>
        public void Commit(TP.Transaction context)
        {
            List<string> rmList = GetRMListForTransaction(context);
            if (null == rmList)
            {
                System.Console.WriteLine(string.Format("Transaction {0} unknown", context.Id));
                throw new AbortTransationException();
            }

            // send commit to all managers involved in the transaction
            lock (this.resourceManagers)
            {
                foreach (RM mgr in this.resourceManagers)
                {
                    if (!rmList.Contains(mgr.GetName()))
                    {
                        continue;
                    }

                    mgr.Commit(context);
                }
            }

            System.Console.WriteLine(string.Format("Transaction {0} commited", context.Id));
        }

        /// <summary>
        // Call from WC in response to a client's abort
        /// </summary>
        /// <param name="context"></param>
        public void Abort(TP.Transaction context)
        {
            List<string> rmList = GetRMListForTransaction(context);
            if (null == rmList)
            {
                System.Console.WriteLine(string.Format("Transaction {0} unknown", context.Id));
                return;
            }

            // send abort to all managers involved in the transaction
            lock (this.resourceManagers)
            {
                foreach (RM mgr in this.resourceManagers)
                {
                    if (!rmList.Contains(mgr.GetName()))
                    {
                        continue;
                    }

                    mgr.Abort(context);
                }
            }

            System.Console.WriteLine(string.Format("Transaction {0} aborted", context.Id));
        }

        /// <summary>
        /// Call from WC to retreive an RM by name.
        /// </summary>
        /// <param name="name">name of RM to get</param>
        /// <returns>RM object</returns>
        public RM GetResourceMananger(string name)
        {
            lock (resourceManagers)
            {
                foreach (RM rm in resourceManagers)
                {
                    if (rm.GetName().Contains(name.ToLower()))
                        return rm;
                }
            }
            return null;
        }

        #endregion

        #region Methods called from RM

        /// <summary>
        /// Called by RM to register it's URL with the TM.
        /// </summary>
        /// <param name="msg"></param>
        public void Register(string msg)
        {
            string[] URL = msg.Split('$');
            Console.WriteLine("Register " + URL[0]);

            TP.RM newRM = (TP.RM)System.Activator.GetObject(typeof(TP.RM), URL[0]);
            try
            {
                newRM.SetName(URL[1]);
            }
            catch (RemotingException e)
            {
                Console.WriteLine(e.ToString());
            }

            // check and see if this RM is currently involved in any active 
            // transactions and remove all those from the active list next 
            // operation on that transaction will cause an abort
            lock (this.activeTransactions)
            {
                foreach (Transaction context in this.activeTransactions.Keys)
                {
                    if (!this.activeTransactions[context].Contains(newRM.GetName()))
                    {
                        continue;
                    }

                    // remove the transaction from the active list
                    this.activeTransactions.Remove(context);
                }
            }

            // add the new RM to the list
            lock (this.resourceManagers)
            {
                resourceManagers.Add(newRM);
            }
        }

        /// <summary>
        /// Called by RM.
        /// This method notifies TM that it is involved in a given transaction
        /// TM keeps track of which RM is enlisted with which transaction to do distributed transactions */
        /// </summary>
        /// <param name="context"></param>
        public bool Enlist(TP.Transaction context, string enlistingRM)
        {
            lock (this.activeTransactions)
            {
                if (!this.activeTransactions.ContainsKey(context))
                {
                    // an RM is trying to enlist in a transaction 
                    // the TM knows nothing about - return false
                    return false;
                }

                this.activeTransactions[context].Add(enlistingRM);
            }

            System.Console.WriteLine(string.Format("Transaction {0} enlisted", context.Id));
            return true;
        }

        

        #endregion

        #endregion

        #region Protected Methods

        public void shutdown()
        {
            // TODO DO PROPER SHUTDOWN HERE
        }


        protected void init(String[] args)
        {
        }


        protected void initStorage()
        {
            // TODO create commit log
        }


        protected void recovery()
        {
            // TODO Abort/commit/garbage collect
        }


        protected void startUp()
        {
            // TODO start garbage collector?
        }


        protected void readyToServe()
        {
        }

        #endregion

        #region Private Methods

        private List<string> GetRMListForTransaction(TP.Transaction context)
        {
            List<string> output = null;

            lock (this.activeTransactions)
            {
                if (!this.activeTransactions.TryGetValue(context, out output))
                {
                    // nothing to do transaction must already have been commited                    
                    return null;
                }

                // remove from list
                this.activeTransactions.Remove(context);
            }

            return output;
        }

        #endregion

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

        class TMParser : CommandLineParser
        {
            public TMParser()
            {
                Add("p", "Port", "The port this transaction manager listens on", "8089");
            }
        }

        static void Main(string[] args)
        {
            TMParser parser = new TMParser();
            if (!parser.Parse(args))
            {
                return;
            }

            SoapServerFormatterSinkProvider serverProv = new SoapServerFormatterSinkProvider();
            serverProv.TypeFilterLevel = TypeFilterLevel.Full;

            SoapClientFormatterSinkProvider clientProv = new SoapClientFormatterSinkProvider();

            IDictionary props = new Hashtable();
            props["port"] = Int32.Parse(parser["p"]);

            HttpChannel channel = new HttpChannel(props, clientProv, serverProv);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType
                (Type.GetType("MyTM.MyTM")								// full type name
                        , "TM.soap"												// URI
                        , System.Runtime.Remoting.WellKnownObjectMode.Singleton	// instancing mode
                );

            while (true)
            {
                System.Threading.Thread.Sleep(100000);
            }
        }

        #endregion
    }
}
