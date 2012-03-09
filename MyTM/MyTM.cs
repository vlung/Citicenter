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
    // These are a set of classes that facilities the execution of a function call
    // with timeout. The idea is to simulate server or network connection error by
    // running an infinite loop in the callee. Since the caller will be blocked,
    // therefore we need a mechanism to make function calls with timeout so the caller
    // can handle the timeout accordingly.

    // This is the base class for allowing function calls with timeout
    public abstract class ExecuteWithTimeoutBase
    {
        public const int Execute_Timeout = 5000; // Set timeout to 5 seconds

        protected Thread t; // Thread for executing the function call
        public bool completed { get; protected set; } // True if the function call (ie thread) has run to completion
        public string name { get; protected set; } // Name of the execution

        public ExecuteWithTimeoutBase(string name)
        {
            completed = false;
            this.name = name;
            t = null;
        }

        ~ExecuteWithTimeoutBase()
        {
            // Kill thread if it is still running
            if (t != null && t.IsAlive)
            {
                t.Abort();
            }
        }

        // Execute the function call
        public void Run()
        {
            // Throw exception if thread is not initialized
            if (t == null)
            {
                throw new InvalidOperationException();
            }
            // Start thread and run until it times out or runs to completion
            t.Start();
            completed = t.Join(Execute_Timeout);
            // If the thread times out, abort the therad and throw a timeout exception
            if (!completed)
            {
                t.Abort();
                t = null;
                throw new TimeoutException(String.Format("{0} take more than {1}ms to execute. Aborting...", name, Execute_Timeout));
            }
            t = null;
        }
    }

    // This is a class that allows execution of functions that do not return any result
    public class ExecuteActionWithTimeout : ExecuteWithTimeoutBase
    {
        protected Action action; // Contains the function block to be run

        public ExecuteActionWithTimeout(string name, Action action)
            : base(name)
        {
            this.action = action;
            t = new Thread(RunAction); // Creates a new thread with the function block
            t.Name = string.Format("ExecuteActionWithTimeout:{0} {1}", name, action.Method.Name);
        }

        // Execute the function block
        protected void RunAction()
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    // This is a class that allows execution of functions that return a result
    public class ExecuteFuncWithTimeout<ResultT> : ExecuteWithTimeoutBase
    {
        protected Func<ResultT> func; // Contains the function block to be run
        public ResultT result { get; private set; } // Contains the result of the function call

        public ExecuteFuncWithTimeout(string name, Func<ResultT> func) : base(name)
        {
            this.func = func;
            t = new Thread(RunFunc); // Creates a new thread with the function block
            t.Name = string.Format("ExecuteFuncWithTimeout:{0} {1}", name, func.Method.Name);
        }

        // Execute the function block and store its result
        protected void RunFunc()
        {
            try
            {
                result = func();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    #endregion

    #region OutstandingTransactions class
    // OutstandingTransactions is a singleton class that keep tracks of outstanding transactions
    // with partial Done response in a file. This allows the RM to confirm whether a transaction
    // has been committed to deal with certainty and it also allows the TM to requery the RM for
    // Done status in case the Done message was lost in transmission.
    public class OutstandingTransactions
    {
        const string CommittedTransactionFilename = ".\\ComXact.txt"; // Name of the file that contains the outstanding commits 

        // The OutstandingTransactionsValue class specifies the meta-data associated with a specific transaction
        public class OutstandingTransactionsValue
        {
            public enum TransactionType { Commit = 0, Abort = 1 }; // A type that indicates the transaction type
            public TransactionType transactionType { get; set; } // Contains the transaction type
            public List<string> nackRMList { get; set; } // Contains the list of RMs that have not acknowledged the commit or abort

            public OutstandingTransactionsValue()
            {
                transactionType = TransactionType.Commit;
                nackRMList = new List<string>();
            }

            public OutstandingTransactionsValue(TransactionType transactionType, List<string> nackRMList)
            {
                this.transactionType = transactionType;
                this.nackRMList = nackRMList;
            }
        }
           
        // This is the singleton instance of the class
        private static readonly OutstandingTransactions instance = new OutstandingTransactions();

        // A hashtable mapping transaction ID to CommittedTransactionValue
        public Dictionary<string, OutstandingTransactionsValue> transactionList {get; protected set;}

        // Make sure the class must be instantiated using the GetInstance method by using a private constructor
        private OutstandingTransactions()
        {
            transactionList = new Dictionary<string, OutstandingTransactionsValue>();
            ReadFromFile();
        }

        // Returns the singleton instance of the class
        public static OutstandingTransactions GetInstance()
        {
            return instance;
        }

        // Get the name of the file used for storing outstanding transactions
        public static string GetFilename()
        {
            return CommittedTransactionFilename;
        }

        // Serialize the key and CommittedTransactionValue value from an encoded string
        private bool SerializeFromString(string s, out string key, out OutstandingTransactionsValue value)
        {
            key = null;
            value = null;

            string[] tokens = s.Split(','); // Split the string into tokens using a delimiter

            if (tokens.Length == 0)
            {
                return false;
            }
            key = tokens[0]; // The key is always the first token
            value = new OutstandingTransactionsValue();
            // The transaction type is the 2nd token (if it exists)
            if (tokens.Length >= 2)
            {
                value.transactionType = (int.Parse(tokens[1]) == 0) ? OutstandingTransactionsValue.TransactionType.Commit : OutstandingTransactionsValue.TransactionType.Abort;
            }
            // The rest of the tokens (if they exist) is the list of RMs that have not acknowledge the commit/abort
            value.nackRMList = tokens.Skip(2).ToList<string>();
            return true;
        }

        // Serialize a key and OutstandingTransactionsValue value to an encoded string
        // The forceWrite parameter indicates whether to serialize to a string anyway even if there are no NACK RMs in the
        // OutstandingTransactionsValue. This is needed sometimes (such as in recovery) to indicate a transaction has
        // committed/aborted successfully and there are no remaining NACK RMs.
        private string SerializeToString(string key, OutstandingTransactionsValue value, bool forceWrite = false)
        {
            // Only serialize if forceWrite is specified or if the list of NACK RMs is not empty
            if (forceWrite || (value != null && value.nackRMList.Count > 0))
            {
                // Write the key to the string
                System.Text.StringBuilder sb = new System.Text.StringBuilder(key);

                if (value != null && value.nackRMList != null)
                {
                    // Write the transaction type to the string
                    sb.Append("," + ((value.transactionType == OutstandingTransactionsValue.TransactionType.Commit) ? "0" : "1"));

                    // Write the list of NACK RMs to the string
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

        // Write a outstanding transaction entry to the file immediately
        public void UpdateAndFlush(string transactionId, OutstandingTransactionsValue value)
        {
            lock (this)
            {
                // If the list of NACK RMs is empty, the transaction has committed/aborted successfully
                // so remove it from the list of outstanding transactions
                if (value == null || value.nackRMList == null || value.nackRMList.Count == 0)
                {
                    transactionList.Remove(transactionId);
                }
                // Otherwise, add/update the outstanding transaction list.
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
        }

        // This function reads the list of outstanding transaction from the designated file
        public void ReadFromFile()
        {
            lock (this)
            {
                transactionList.Clear(); // Clear the outstanding transaction list
                try
                {
                    string[] content = File.ReadAllLines(GetFilename()); // Read the whole file into memory
                    // Serialize each line of the file into a key and a OutstandingTransactionsValue value and add them
                    // to the list of outstanding transaction
                    foreach (string s in content)
                    {
                        string key;
                        OutstandingTransactionsValue value;

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
                catch (Exception)
                {
                    // If anything bad happens reading the file or parsing the file, clear
                    // the list of outstanding transaction.
                    transactionList.Clear();
                }
            }
        }

        // This function writes the list of outstanding transaction to the designated file
        public void WriteToFile()
        {
            lock (this)
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
    }
    #endregion

    /// <summary>
	/*  Transaction Manager */
	/// </summary>
    public class MyTM : System.MarshalByRefObject, TP.TM
    {
        #region Private Properties

        private Dictionary<string, RM> resourceManagers;   // A hash set containing the list of RMs enlisted to the TM
        private Dictionary<Transaction, List<string>> activeTransactions; // A list of active transactions
        private OutstandingTransactions OutstandingTransactions; // A list of outstanding (committed/aborted but not fully ACKed) transactions

        #endregion

        #region Public Methods

        public MyTM()
        {
            System.Console.WriteLine("Transaction Manager instantiated");
            resourceManagers = new Dictionary<string, RM>();
            activeTransactions = new Dictionary<Transaction, List<string>>();
            OutstandingTransactions = OutstandingTransactions.GetInstance();
        }

        #region Methods called from WC

        /// <summary>
        /// Call from WC to start a new transaction.
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
                foreach (string name in rmNameList)
                {
                    RM manager = GetResourceMananger(name);
                    if (null == manager)
                    {
                        continue;
                    }

                    rmList.Add(manager);
                }

                // Request to prepare for all resource managers involved in this transaction
                bool allPrepared = true;
                List<ExecuteFuncWithTimeout<bool>> execPrepareList = new List<ExecuteFuncWithTimeout<bool>>();
                try
                {
                    // Execute the Prepare() function for each associated RMs with timeout
                    for (int i = 0; i < rmList.Count; ++i)
                    {
                        ExecuteFuncWithTimeout<bool> exec = new ExecuteFuncWithTimeout<bool>(rmList[i].GetName(), () => rmList[i].Prepare(context));
                        execPrepareList.Add(exec);
                        exec.Run();
                    }
                }
                // If a timeout occurs, it means that RMs are not all prepared and we should abort
                // the transaction
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

                // Initialize the list of RM to outstanding transaction list in case of recovery
                OutstandingTransactions.OutstandingTransactionsValue committedTransactionValue = new OutstandingTransactions.OutstandingTransactionsValue(
                    OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Commit,
                    rmNameList);

                // If all resource managers responded Prepared to the Request to prepare, commit the transaction.
                if (allPrepared)
                {
                    // Write transaction id and list of RM to outstanding transaction list before committing the transaction
                    // Flush the entry to the outstanding transaction file immediately
                    OutstandingTransactions.UpdateAndFlush(context.Id.ToString(), committedTransactionValue);

                    System.Console.WriteLine(string.Format("Transaction {0} received Prepared from all resource managers. Committing transaction...", context.Id));

                    // Execute commit() in all associated RMs
                    for (int i = 0; i < rmList.Count; ++i)
                    {
                        ExecuteActionWithTimeout exec = new ExecuteActionWithTimeout(rmList[i].GetName(), () => rmList[i].Commit(context));
                        try
                        {
                            exec.Run();
                            // Remove RM from list of RM when we received Done (ie no timeout has occurred)
                            committedTransactionValue.nackRMList.Remove(rmList[i].GetName());
                        }
                        catch (TimeoutException)
                        {
                            System.Console.WriteLine(string.Format("Transaction {0} timed out while waiting for RM {1} to commit...", context.Id, rmList[i].GetName()));
                        }
                    }

                    // Write transaction id and list of unacknowledged RMs to the outstanding transaction list.
                    // Flush the entry to the outstanding transaction file immediately
                    OutstandingTransactions.UpdateAndFlush(context.Id.ToString(), committedTransactionValue);

                    if (committedTransactionValue.nackRMList.Count == 0)
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
                    // Initialize the list of RM to outstanding transaction list in case of recovery
                    // Write transaction id and list of RM to outstanding transaction list before aborting the transaction
                    // Flush the entry to the outstanding transaction file immediately
                    committedTransactionValue.transactionType = OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Abort;
                    OutstandingTransactions.UpdateAndFlush(context.Id.ToString(), committedTransactionValue);

                    System.Console.WriteLine(string.Format("Transaction {0} aborting...", context.Id));

                    // Execute abort() in all associated RMs
                    for (int i = 0; i < rmList.Count; ++i)
                    {
                        ExecuteActionWithTimeout exec = new ExecuteActionWithTimeout(rmList[i].GetName(), () => rmList[i].Abort(context));
                        try
                        {
                            exec.Run();
                        }
                        catch (TimeoutException)
                        {
                            System.Console.WriteLine(string.Format("Transaction {0} timed out while waiting for RM {1} to abort...", context.Id, rmList[i].GetName()));
                        }
                    }

                    // PRESUMED ABORT: Once the TM sends out the abort, it forgets about the transaction immediately.
                    // So we write transaction id and an _empty_ list of unacknowledged RMs to the outstanding transaction list.
                    // This will cancel out the entry we logged earlier about the transaction and all its RMs.
                    // Flush the entry to the outstanding transaction file immediately
                    committedTransactionValue.nackRMList.Clear();
                    OutstandingTransactions.UpdateAndFlush(context.Id.ToString(), committedTransactionValue);
                    if (committedTransactionValue.nackRMList.Count == 0)
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
                foreach (string name in rmNameList)
                {
                    RM manager = GetResourceMananger(name);
                    if (null == manager)
                    {
                        continue;
                    }

                    rmList.Add(manager);
                }

                // Write transaction id and list of RM to outstanding transaction list before aborting the transaction
                // Flush the entry to the outstanding transaction file immediately
                OutstandingTransactions.OutstandingTransactionsValue abortedTransactionValue = new OutstandingTransactions.OutstandingTransactionsValue(
                    OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Abort,
                    rmNameList);
                OutstandingTransactions.UpdateAndFlush(context.Id.ToString(), abortedTransactionValue);

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
                        System.Console.WriteLine(string.Format("Transaction {0} timed out while waiting for RM {1} to abort...", context.Id, rmList[i].GetName()));
                    }
                }

                // PRESUMED ABORT: Once the TM sends out the abort, it forgets about the transaction immediately.
                // So we write transaction id and an _empty_ list of unacknowledged RMs to the outstanding transaction list.
                // This will cancel out the entry we logged earlier about the transaction and all its RMs.
                // Flush the entry to the outstanding transaction file immediately
                abortedTransactionValue.nackRMList.Clear();
                OutstandingTransactions.UpdateAndFlush(context.Id.ToString(), abortedTransactionValue);
                if (abortedTransactionValue.nackRMList.Count == 0)
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
                if (!this.resourceManagers.ContainsKey(name))
                {
                    return null;
                }

                try
                {
                    // get the RM and check to see if it is still alive
                    RM rm = this.resourceManagers[name];
                    rm.GetName();

                    return rm;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    this.resourceManagers.Remove(name);

                    return null;
                }
                
            }
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

                // Add the RM to the active transaction only if it has not been added already
                if (!this.activeTransactions[context].Contains(enlistingRM))
                {
                    this.activeTransactions[context].Add(enlistingRM);
                }
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
            else if (OutstandingTransactions.transactionList.ContainsKey(context.Id.ToString()))
            {
                if (OutstandingTransactions.transactionList[context.Id.ToString()].transactionType ==
                    OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Commit)
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
                // add the new RM
                this.resourceManagers[newRM.GetName()] = newRM;
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

        // This function reviews all outstanding transaction and reissues Commit() or Abort() to the
        // unackowledged RMs
        public void recovery()
        {
            Console.Out.WriteLine("Recovery started...");
            List<string> deleteTransactionList = new List<string>(); // Keep track of transactions that are fully acknowledged by all its RMs

            foreach (string transactionId in OutstandingTransactions.transactionList.Keys)
            {
                // For each outstanding transaction, get its transaction id and its OutstandingTransactions value
                OutstandingTransactions.OutstandingTransactionsValue entry = OutstandingTransactions.transactionList[transactionId];
                Transaction context = new Transaction();
                context.Id = new Guid(transactionId);

                Console.Out.WriteLine(string.Format("Recovery: Recovering transaction {0}...", transactionId));

                // For each of the remaining NACK RMs, reissue the Commit or Abort
                for (int i = 0; i < entry.nackRMList.Count; ++i)
                {
                    string rmName = entry.nackRMList[i];
                    RM rm = GetResourceMananger(rmName);
                    if (rm == null)
                    {
                        Console.Out.WriteLine(string.Format("Recovery: Failed to find resource manager {0}", rmName));
                    }
                    else
                    {
                        ExecuteActionWithTimeout exec;

                        if (entry.transactionType == OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Commit)
                        {
                            Console.Out.WriteLine(string.Format("Recovery: Re-committing resource manager {0}...", rmName));
                            exec = new ExecuteActionWithTimeout(rmName, () => rm.Commit(context));
                        }
                        else
                        {
                            Console.Out.WriteLine(string.Format("Recovery: Re-aborting resource manager {0}...", rmName));
                            exec = new ExecuteActionWithTimeout(rmName, () => rm.Abort(context));
                        }

                        // Execute the commit or abort
                        try
                        {
                            exec.Run();
                            Console.Out.WriteLine("Recovery: Successful!");

                            // Remove RM from the current transaction since we received the Done message (ie the call didn't time out)
                            OutstandingTransactions.transactionList[transactionId].nackRMList.RemoveAt(i);
                            --i;
                        }
                        catch (TimeoutException)
                        {
                            System.Console.WriteLine("Recovery: Failed!");
                        }
                    }
                }
                
                // PRESUMED ABORT: If the transaction is Abort, we don't care about the Done message and we forget about
                // the transaction immediately.
                // For commit transactions, mark transaction for removal if we received Done from all RMs
                if (entry.transactionType == OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Abort ||
                    entry.nackRMList.Count == 0)
                {
                    deleteTransactionList.Add(transactionId);
                }
            }

            // Delete transaction marked for removal from the outstanding transaction list
            foreach (string s in deleteTransactionList)
            {
                OutstandingTransactions.transactionList.Remove(s);
            }
            // Update file to reflect outstanding transactions status
            // This is a full rewrite of the outstanding transaction file and hence it will implicitly garbage collect
            // the transactions that are no longer outstanding.
            OutstandingTransactions.WriteToFile();
            Console.Out.WriteLine("Recovery completed.");
        }


        protected void startUp()
        {
        }


        protected void readyToServe()
        {
        }

        #endregion

        #region Private Methods

        // This function returns the list of RMs associated with the given transaction and
        // automatically removes the given transaction from the active transaction list
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

        // Checks whether the give RM is known and throws UnknownRMException if it is not known.
        private void ValidateRM(string rmName)
        {
            lock(this.resourceManagers)
            {
                RM manager = GetResourceMananger(rmName);
                if (null == manager)
                {
                    throw new UnknownRMException();
                }
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

            // activate the object
            string[] urls = channel.GetUrlsForUri("TM.soap");
            if (1 != urls.Length)
            {
                throw new InvalidOperationException();
            }

            MyTM transactionManager = (MyTM)System.Activator.GetObject(typeof(TP.TM), urls[0]);
            if (null == transactionManager)
            {
                throw new InvalidProgramException();
            }

            // Do recovery every 30 seconds to recommit/reabort unacknowledged transactions
            // as well as doing garbage collection on the outstanding transaction file
            while (true)
            {
                Console.WriteLine("Recovery will run in 30 seconds...");
                System.Threading.Thread.Sleep(30000);
                transactionManager.recovery();
            }
        }

        #endregion
    }
}
