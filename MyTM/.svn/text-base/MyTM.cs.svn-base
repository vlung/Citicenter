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
	public class MyTM: System.MarshalByRefObject, TP.TM
	{

        private SynchronizedCollection<RM> resourceManagers;
		public MyTM()
		{
			System.Console.WriteLine("Transaction Manager instantiated");
            resourceManagers = new SynchronizedCollection<RM>();
		}

        public RM GetResourceMananger(string name)
        {
            foreach (RM rm in resourceManagers)
            {
                if (rm.GetName().Contains(name.ToLower()))
                    return rm;
            }
            return null;
        }
		public TP.Transaction Start()
		{
			Transaction context = new Transaction();
			System.Console.WriteLine( string.Format("TM: Transaction {0} started", context.Id));
			return context;
		}
 
	    /// <summary>
	    //	 Call from WC in response to a client's commit
	    /// </summary>
	    /// <param name="context"></param>
	    public void Commit(TP.Transaction context)
	    {
		    System.Console.WriteLine(string.Format("Transaction {0} commited", context.Id));
	    }

	    /// <summary>
	    // Call from WC in response to a client's abort
	    /// </summary>
	    /// <param name="context"></param>
	    public void Abort(TP.Transaction context)
	    {
		    System.Console.WriteLine(string.Format("Transaction {0} aborted", context.Id));
	    }

	    /// <summary>
	    /*  Called by RM.
		    This method notifies TM that it is involved in a given transaction
		    TM keeps track of which RM is enlisted with which transaction to do distributed transactions */
	    /// </summary>
	    /// <param name="context"></param>
        public bool Enlist(TP.Transaction context, string enlistingRM)
	    {
		    System.Console.WriteLine(string.Format( "Transaction {0} enlisted", context.Id ));
            return false;
	    }

        public void Register(string msg)
        {
            string [] URL = msg.Split('$');
            Console.WriteLine("Register "+ URL[0]);
            TP.RM newRM = (TP.RM)System.Activator.GetObject(typeof(TP.RM), URL[0]);
            try
            {
               newRM.SetName(URL[1]);
            }
            catch (RemotingException e)
            { 
                Console.WriteLine(e.ToString());
            }
            resourceManagers.Add(newRM);
            
        }

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

}
}
