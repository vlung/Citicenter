using System;
using TP;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters;
using System.Collections;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        private static int RM_TIMEOUT = 5000;

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
            // Get the list of names of the resource manager involved in this transaction
            List<string> rmNameList = GetRMListForTransaction(context);
            if (null == rmNameList)
            {
                System.Console.WriteLine(string.Format("Transaction {0} unknown", context.Id));
                throw new AbortTransationException();
            }

            // Get the list of resource managers involved in this transaction
            List<RM> rmList = new List<RM>();
            lock (this.resourceManagers)
            {
                foreach (RM mgr in this.resourceManagers)
                {
                    if (rmNameList.Contains(mgr.GetName()))
                    {
                        rmList.Add(mgr);
                    }
                }

                // Request to prepare for all resource managers involved in this transaction
                bool allPrepared = true;
                Task<bool>[] prepareTasks = new Task<bool>[rmList.Count];
                for (int i = 0; i < rmList.Count; ++i)
                {
                    prepareTasks[i] = Task.Factory.StartNew<bool>((obj) =>
                        {
                            return ((RM)obj).RequestToPrepare(context);
                        },
                        rmList[i]);
                }

                // Wait for all resource managers to respond to the request to prepare
                if (!Task.WaitAll(prepareTasks, RM_TIMEOUT))
                {
                    // If any of the resource managers timed out, cancel active tasks and abort the transaction
                    System.Console.WriteLine(string.Format("Transaction {0} timed out while waiting for the RequestToPrepare response. Aborting transaction...", context.Id));
                    allPrepared = false;
                }
                else
                {
                    // If all resource managers are ready responded to the request to prepare, check if
                    // any of them responded NO to the request.
                    foreach (Task<bool> t in prepareTasks)
                    {
                        if (t.Result == false) // Since the task did not time out, getting the result is non-blocking.
                        {
                            allPrepared = false;
                            System.Console.WriteLine(string.Format("Transaction {0} received No for RequestToPrepare. Aborting transaction...", context.Id));
                            break;
                        }
                    }
                }

                // If all resource managers responded Prepared to the Request to prepare, commit the transaction.
                if (allPrepared)
                {
                    System.Console.WriteLine(string.Format("Transaction {0} received Prepared from all resource managers. Committing transaction...", context.Id));
                    Task[] commitTasks = new Task[rmList.Count];
                    for (int i = 0; i < rmList.Count; ++i)
                    {
                        commitTasks[i] = Task.Factory.StartNew((obj) =>
                        {
                            ((RM)obj).Commit(context);
                        },
                        rmList[i]);
                    }
                    // TODO: How should we handle commit timeout?
                    if (Task.WaitAll(commitTasks, RM_TIMEOUT))
                    {
                        System.Console.WriteLine(string.Format("Transaction {0} commited", context.Id));
                    }
                    else
                    {
                        System.Console.WriteLine(string.Format("Transaction {0} commited with some timeouts", context.Id));
                    }
                }
                else
                {
                    System.Console.WriteLine(string.Format("Transaction {0} aborting...", context.Id));
                    Task[] abortTasks = new Task[rmList.Count];
                    for (int i = 0; i < rmList.Count; ++i)
                    {
                        abortTasks[i] = Task.Factory.StartNew((obj) =>
                        {
                            ((RM)obj).Abort(context);
                        },
                        rmList[i]);
                    }
                    // TODO: How should we handle commit timeout?
                    if (Task.WaitAll(abortTasks, RM_TIMEOUT))
                    {
                        System.Console.WriteLine(string.Format("Transaction {0} aborted", context.Id));
                    }
                    else
                    {
                        System.Console.WriteLine(string.Format("Transaction {0} aborted with some timeouts", context.Id));
                    }
                }
            }
        }

        /// <summary>
        // Call from WC in response to a client's abort
        /// </summary>
        /// <param name="context"></param>
        public void Abort(TP.Transaction context)
        {  
            // Get the list of names of the resource manager involved in this transaction
            List<string> rmNameList = GetRMListForTransaction(context);
            if (null == rmNameList)
            {
                System.Console.WriteLine(string.Format("Transaction {0} unknown", context.Id));
            }

            // Get the list of resource managers involved in this transaction
            List<RM> rmList = new List<RM>();
            lock (this.resourceManagers)
            {
                foreach (RM mgr in this.resourceManagers)
                {
                    if (rmNameList.Contains(mgr.GetName()))
                    {
                        rmList.Add(mgr);
                    }
                }

                System.Console.WriteLine(string.Format("Transaction {0} aborting...", context.Id));
                Task[] abortTasks = new Task[rmList.Count];
                for (int i = 0; i < rmList.Count; ++i)
                {
                    abortTasks[i] = Task.Factory.StartNew((obj) =>
                    {
                        ((RM)obj).Abort(context);
                    },
                    rmList[i]);
                }
                // TODO: How should we handle commit timeout?
                if (Task.WaitAll(abortTasks, RM_TIMEOUT))
                {
                    System.Console.WriteLine(string.Format("Transaction {0} aborted", context.Id));
                }
                else
                {
                    System.Console.WriteLine(string.Format("Transaction {0} aborted with some timeouts", context.Id));
                }
            }
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
            // determine if this is an RM we know about
            this.ValidateRM(enlistingRM);

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

        private void ValidateRM(string rmName)
        {
            bool isKnown = false;
            lock(this.resourceManagers)
            {
                foreach (RM manager in this.resourceManagers)
                {
                    if (manager.GetName().Equals(rmName))
                    {
                        isKnown = true;
                        break;
                    }
                }
            }

            if (!isKnown)
            {
                throw new UnknownRMException();
            }
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
