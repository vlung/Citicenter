using System;
using TP;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters;
using System.Collections;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MyTM
{
    #region Execution with timeout classes

    public abstract class ExecuteWithTimeoutBase
    {
        public const int Execute_Timeout = 5000; // Set timeout to 5 seconds

        protected Thread t;
        public bool completed { get; protected set; }
        public string name { get; protected set; }

        public ExecuteWithTimeoutBase(string name)
        {
            completed = false;
            this.name = name;
            t = null;
        }

        ~ExecuteWithTimeoutBase()
        {
            if (t != null && t.IsAlive)
            {
                t.Abort();
            }
        }

        public void Run()
        {
            if (t == null)
            {
                throw new InvalidOperationException();
            }
            t.Start();
            completed = t.Join(Execute_Timeout);
            if (!completed)
            {
                t.Abort();
                t = null;
                throw new TimeoutException(String.Format("{0} take more than {1}ms to execute. Aborting...", name, Execute_Timeout));
            }
            t = null;
        }
    }

    public class ExecuteActionWithTimeout : ExecuteWithTimeoutBase
    {
        protected Action action;

        public ExecuteActionWithTimeout(string name, Action action)
            : base(name)
        {
            this.action = action;
            t = new Thread(RunAction);
            t.Name = string.Format("ExecuteActionWithTimeout:{0} {1}", name, action.Method.Name);
        }

        protected void RunAction()
        {
            action();
        }
    }

    public class ExecuteFuncWithTimeout<ResultT> : ExecuteWithTimeoutBase
    {
        protected Func<ResultT> func;
        public ResultT result { get; private set; }

        public ExecuteFuncWithTimeout(string name, Func<ResultT> func) : base(name)
        {
            this.func = func;
            t = new Thread(RunFunc);
            t.Name = string.Format("ExecuteFuncWithTimeout:{0} {1}", name, func.Method.Name);
        }

        protected void RunFunc()
        {
            result = func();
        }
    }

    #endregion

    #region CommittedTransactions class
    // CommittedTransactions is a singleton class that keep tracks of committed transactions
    // with partial Done response in a file. This allows the RM to confirm whether a transaction
    // has been committed to deal with certainty and it also allows the TM to requery the RM for
    // Done status in case the Done message was lost in transmission.
    public class CommittedTransactions
    {
        const string CommittedTransactionFilename = "ComXact.txt";

        public class CommittedTransactionsValue
        {
            public enum TransactionType { Commit = 0, Abort = 1 };
            public TransactionType transactionType { get; set; }
            public List<string> nackRMList { get; set; }

            public CommittedTransactionsValue()
            {
                transactionType = TransactionType.Commit;
                nackRMList = new List<string>();
            }

            public CommittedTransactionsValue(TransactionType transactionType, List<string> nackRMList)
            {
                this.transactionType = transactionType;
                this.nackRMList = nackRMList;
            }
        }
            
        private static readonly CommittedTransactions instance = new CommittedTransactions();
        public Dictionary<string, CommittedTransactionsValue> transactionList {get; protected set;}

        // Make sure the class must be instantiated using the GetInstance method
        private CommittedTransactions()
        {
            transactionList = new Dictionary<string, CommittedTransactionsValue>();
            ReadFromFile();
        }

        public static CommittedTransactions GetInstance()
        {
            return instance;
        }

        private string GetFilename()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), CommittedTransactionFilename);
        }

        private bool SerializeFromString(string s, out string key, out CommittedTransactionsValue value)
        {
            key = null;
            value = null;

            string[] tokens = s.Split(',');

            if (tokens.Length == 0)
            {
                return false;
            }
            key = tokens[0];
            value = new CommittedTransactionsValue();
            if (tokens.Length >= 2)
            {
                value.transactionType = (int.Parse(tokens[1]) == 0) ? CommittedTransactionsValue.TransactionType.Commit : CommittedTransactionsValue.TransactionType.Abort;
            }
            value.nackRMList = tokens.Skip(2).ToList<string>();
            return true;
        }

        private string SerializeToString(string key, CommittedTransactionsValue value, bool forceWrite = false)
        {
            if (forceWrite || (value != null && value.nackRMList.Count > 0))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(key);

                if (value != null && value.nackRMList != null)
                {
                    sb.Append("," + ((value.transactionType == CommittedTransactionsValue.TransactionType.Commit) ? "0" : "1"));

                    foreach (string item in value.nackRMList)
                    {
                        sb.Append("," + item);
                    }
                }
                return sb.ToString();
            }
            else
            {
                return null;
            }
        }

        public void UpdateAndFlush(string transactionId, CommittedTransactionsValue value)
        {
            // Update transaction list
            if (value == null || value.nackRMList == null || value.nackRMList.Count == 0)
            {
                transactionList.Remove(transactionId);
            }
            else
            {
                transactionList[transactionId] = value;
            }

            // Append transaction information to file. If this is an existing transaction, it may
            // cause the file to have multiple occurrences of the same transaction ID. We will take
            // care of this in ReadFromFile() to make sure the last entry wins, this will avoid the need
            // to rewrite the whole file every time an existing transaction is updated.
            using (System.IO.StreamWriter sw = new StreamWriter(GetFilename(), true))
            {
                sw.WriteLine(SerializeToString(transactionId, value, true));
            }
        }

        public void ReadFromFile()
        {         
            transactionList.Clear();
            try
            {
                string[] content = File.ReadAllLines(GetFilename());
                foreach (string s in content)
                {
                    string key;
                    CommittedTransactionsValue value;

                    if (SerializeFromString(s, out key, out value))
                    {
                        if (value != null && value.nackRMList.Count > 0)
                        {
                            transactionList[key] = value;
                        }
                        else
                        {
                            transactionList.Remove(key);
                        }
                    }
                }
            }
            catch(Exception)
            {
                transactionList.Clear();
            }
        }

        public void WriteToFile()
        {
            using (System.IO.StreamWriter sw = new StreamWriter(GetFilename(), false))
            {
                foreach (string key in transactionList.Keys)
                {
                    sw.WriteLine(SerializeToString(key, transactionList[key]));
                }
            }
        }
    }
    #endregion

    /// <summary>
	/*  Transaction Manager */
	/// </summary>
    public class MyTM : System.MarshalByRefObject, TP.TM
    {
        #region Private Properties

        private HashSet<RM> resourceManagers;
        private Dictionary<Transaction, List<string>> activeTransactions;
        private static int RM_TIMEOUT = 5000;
        private CommittedTransactions committedTransactions;

        #endregion

        #region Public Methods

        public MyTM()
        {
            System.Console.WriteLine("Transaction Manager instantiated");
            resourceManagers = new HashSet<RM>();
            activeTransactions = new Dictionary<Transaction, List<string>>();
            committedTransactions = CommittedTransactions.GetInstance();
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
                List<ExecuteFuncWithTimeout<bool>> execPrepareList = new List<ExecuteFuncWithTimeout<bool>>();
                try
                {
                    for (int i = 0; i < rmList.Count; ++i)
                    {
                        ExecuteFuncWithTimeout<bool> exec = new ExecuteFuncWithTimeout<bool>(rmList[i].GetName(), () => rmList[i].Prepare(context));
                        execPrepareList.Add(exec);
                        exec.Run();
                    }
                }
                catch (TimeoutException)
                {
                    System.Console.WriteLine(string.Format("Transaction {0} timed out while waiting for RequestToPrepare. Aborting transaction...", context.Id));
                    allPrepared = false;
                }

                if (allPrepared)
                {
                    // If all resource managers are ready responded to the request to prepare, check if
                    // any of them responded NO to the request.
                    foreach (ExecuteFuncWithTimeout<bool> exec in execPrepareList)
                    {
                        if (exec.completed && exec.result == false)
                        {
                            allPrepared = false;
                            System.Console.WriteLine(string.Format("Transaction {0} received No for RequestToPrepare. Aborting transaction...", context.Id));
                            break;
                        }
                    }
                }
                execPrepareList.Clear();

                // If all resource managers responded Prepared to the Request to prepare, commit the transaction.
                if (allPrepared)
                {
                    CommittedTransactions.CommittedTransactionsValue committedTransactionValue = new CommittedTransactions.CommittedTransactionsValue();

                    System.Console.WriteLine(string.Format("Transaction {0} received Prepared from all resource managers. Committing transaction...", context.Id));
                    for (int i = 0; i < rmList.Count; ++i)
                    {
                        ExecuteActionWithTimeout exec = new ExecuteActionWithTimeout(rmList[i].GetName(), () => rmList[i].Commit(context));
                        try
                        {
                            exec.Run();
                        }
                        catch (TimeoutException)
                        {
                            committedTransactionValue.nackRMList.Add(rmList[i].GetName());
                            System.Console.WriteLine(string.Format("Transaction {0} timed out while waiting for RM {1} to commit...", context.Id, rmList[i].GetName()));
                        }
                    }
 
                    if (committedTransactionValue.nackRMList.Count == 0)
                    {
                        System.Console.WriteLine(string.Format("Transaction {0} commited", context.Id));
                    }
                    else
                    {
                        committedTransactions.UpdateAndFlush(context.Id.ToString(), committedTransactionValue);
                        System.Console.WriteLine(string.Format("Transaction {0} commited with some timeouts", context.Id));
                    }
                }
                else
                {
                    CommittedTransactions.CommittedTransactionsValue abortedTransactionValue = new CommittedTransactions.CommittedTransactionsValue();
                    abortedTransactionValue.transactionType = CommittedTransactions.CommittedTransactionsValue.TransactionType.Abort;

                    System.Console.WriteLine(string.Format("Transaction {0} aborting...", context.Id));
                    for (int i = 0; i < rmList.Count; ++i)
                    {
                        ExecuteActionWithTimeout exec = new ExecuteActionWithTimeout(rmList[i].GetName(), () => rmList[i].Abort(context));
                        try
                        {
                            exec.Run();
                        }
                        catch (TimeoutException)
                        {
                            abortedTransactionValue.nackRMList.Add(rmList[i].GetName());
                            System.Console.WriteLine(string.Format("Transaction {0} timed out while waiting for RM {1} to abort...", context.Id, rmList[i].GetName()));
                        }
                    }

                    if (abortedTransactionValue.nackRMList.Count == 0)
                    {
                        System.Console.WriteLine(string.Format("Transaction {0} aborted", context.Id));
                    }
                    else
                    {
                        committedTransactions.UpdateAndFlush(context.Id.ToString(), abortedTransactionValue);
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

                CommittedTransactions.CommittedTransactionsValue abortedTransactionValue = new CommittedTransactions.CommittedTransactionsValue();
                abortedTransactionValue.transactionType = CommittedTransactions.CommittedTransactionsValue.TransactionType.Abort;

                System.Console.WriteLine(string.Format("Transaction {0} aborting...", context.Id));
                for (int i = 0; i < rmList.Count; ++i)
                {
                    ExecuteActionWithTimeout exec = new ExecuteActionWithTimeout(rmList[i].GetName(), () => rmList[i].Abort(context));
                    try
                    {
                        exec.Run();
                    }
                    catch (TimeoutException)
                    {
                        abortedTransactionValue.nackRMList.Add(rmList[i].GetName());
                        System.Console.WriteLine(string.Format("Transaction {0} timed out while waiting for RM {1} to abort...", context.Id, rmList[i].GetName()));
                    }
                }

                if (abortedTransactionValue.nackRMList.Count == 0)
                {
                    System.Console.WriteLine(string.Format("Transaction {0} aborted", context.Id));
                }
                else
                {
                    committedTransactions.UpdateAndFlush(context.Id.ToString(), abortedTransactionValue);
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


        /// <summary>
        /// Called by RM.
        /// This method checks the 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public TransactionStatus GetTransactionStatus(Transaction context)
        {
            if (activeTransactions.ContainsKey(context))
            {
                return TransactionStatus.ACTIVE;
            }
            else if (committedTransactions.transactionList.ContainsKey(context.Id.ToString()))
            {
                if (committedTransactions.transactionList[context.Id.ToString()].transactionType ==
                    CommittedTransactions.CommittedTransactionsValue.TransactionType.Commit)
                {
                    return TransactionStatus.COMMITED;
                }

            }
            return TransactionStatus.ABORTED;
        }

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
            Console.Out.WriteLine("Recovery started...");
            foreach (string transactionId in committedTransactions.transactionList.Keys)
            {
                CommittedTransactions.CommittedTransactionsValue entry = committedTransactions.transactionList[transactionId];
                Transaction context = new Transaction();
                context.Id = new Guid(transactionId);

                Console.Out.WriteLine(string.Format("Recovering transaction {0}...", transactionId));

                foreach (string rmName in entry.nackRMList)
                {
                    RM rm = GetResourceMananger(rmName);
                    if (rm == null)
                    {
                        Console.Out.WriteLine(string.Format("\tFailed to find resource manager {0}", rmName));
                    }
                    else
                    {
                        ExecuteActionWithTimeout exec;

                        if (entry.transactionType == CommittedTransactions.CommittedTransactionsValue.TransactionType.Commit)
                        {
                            Console.Out.Write(string.Format("\tRe-committing resource manager {0}...", rmName));
                            exec = new ExecuteActionWithTimeout(rmName, () => rm.Commit(context));
                        }
                        else
                        {
                            Console.Out.Write(string.Format("\tRe-aborting resource manager {0}...", rmName));
                            exec = new ExecuteActionWithTimeout(rmName, () => rm.Abort(context));
                        }

                        try
                        {
                            exec.Run();
                            Console.Out.WriteLine("Successful!");
                            committedTransactions.transactionList[transactionId].nackRMList.Remove(rmName);
                        }
                        catch (TimeoutException)
                        {
                            System.Console.WriteLine("Failed!");
                        }
                    }
                }
            }
            // Update file to reflect committed transactions status
            committedTransactions.WriteToFile();
            Console.Out.WriteLine("Recovery completed.");
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
