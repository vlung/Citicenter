using System;
using System.Collections;
using TP;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels;
namespace MyRM
{
 /// <summary>
 /// class MyRM implements TP.RM
 /// </summary>
 public class MyRM : System.MarshalByRefObject, TP.RM
 {
   MyLM lockManager;
   private Dictionary<RID, Resource> resources;
   private Dictionary<Customer, HashSet<RID>> reservations;
   private string name;

   static TP.TM transactionManager = null;

   internal class GlobalState
   {
       public enum RunMode
       {
           Loop,
           Wait,
           Kill
       }
       
       public static RunMode Mode = RunMode.Loop;      

       public const string DefaultName = "MyRM";
       const int MaxNameLength = 21;
       static string name = null;

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
	
    /**
    * keep track of inventory information.
    */
    [System.Serializable()]
    class Resource{
        private static readonly long serialVersionUID = 15980438631067294L;
        
        private RID rID;
        private int count;
        private int price;
        
        public Resource() {}
        public Resource(RID rID) {
            this.rID = rID;
        }
        public Resource(RID rID,int c,int p) {
            this.rID = rID;
            this.count = c;
            this.price = p;
        }
        
        public String getName() { return rID.getName(); }
        public RID getID() { return rID; }
        public RID.Type getType() { return rID.getType(); }
        public int getCount() { return count; }
        public int getPrice() { return price; }

        public void incrCount() { ++this.count; }
        public void incrCount(int c) { this.count += c; }
        
        public void decrCount(int c) { this.count -= c; }
        public void decrCount() { --this.count; }
        
        public void setCount(int count) { this.count = count; }
        public void setPrice(int price) { this.price = price; }
        
        
        public int hashCode() { return rID.GetHashCode(); }

        public String toString() {
            return rID.getName() + "," + count + "," + price;
        }
    }
    	
  public MyRM()
  {
      this.lockManager = new MyLM();
      name = "MyRM";
      resources = new Dictionary<RID, Resource>();
      reservations = new Dictionary<Customer, HashSet<RID>>();
  }

  public void SetName(string _name)
  {
      name = _name;
  }

  public string GetName()
  {
      return name;
  }

  class RMParser : CommandLineParser
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

      System.Collections.Specialized.ListDictionary channelProperties = new System.Collections.Specialized.ListDictionary();

      channelProperties.Add("port", port_num);
      channelProperties.Add("name", GlobalState.Name);

      HttpChannel channel = new HttpChannel(channelProperties, new SoapClientFormatterSinkProvider(),new SoapServerFormatterSinkProvider());

      System.Console.WriteLine(string.Format("Starting resource manager for {0} on port {1}", GlobalState.Name, port_num));
      System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(channel, false);

      System.Runtime.Remoting.RemotingConfiguration.RegisterWellKnownServiceType
      (Type.GetType("MyRM.MyRM")									// Assembly name
            , "RM.soap"												// URI
            , System.Runtime.Remoting.WellKnownObjectMode.Singleton	// Instancing mode
      );

     
      if (String.Compare(parser["tm"], "none", true) != 0)
      {
          while (transactionManager == null)
          {
              try
              {                
                  transactionManager = (TP.TM)System.Activator.GetObject(typeof(TP.TM),parser["tm"]);

                  Transaction tid = transactionManager.Start();
                  string[] urls = channel.GetUrlsForUri("RM.soap");
                  foreach (string url in urls)
                  {
                     transactionManager.Register(url + "$" + GlobalState.Name);

                  }
                  
                  transactionManager.Abort(tid);
                  
              }
              catch (ArgumentException)
              {
                  transactionManager = null;
                  Console.WriteLine("Waiting 1 second for Transaction Manager \"{0}\"", parser["tm"]);
                  System.Threading.Thread.Sleep(1000);
              }
          }
       

      }

      Console.WriteLine("{0} RM: Transaction Manager retrieved at {1}", GlobalState.Name, parser["tm"]);

      while (GlobalState.Mode == GlobalState.RunMode.Loop)
          System.Threading.Thread.Sleep(2000);

      int loopCount = 0;

      while (GlobalState.Mode == GlobalState.RunMode.Wait && loopCount < 15)
      {
          System.Threading.Thread.Sleep(1000);
          loopCount++;
          Console.WriteLine("{0}: Waiting for transaction complete ({1} second(s))", GlobalState.Name, loopCount);
      }

      Console.WriteLine("{0}: Exitting", GlobalState.Name);
  }
     
 
  // Call to TM to enlist for distributed transaction
  public void Enlist(TP.Transaction context)
  {
    // transactionManager.Enlist(context);
  }

  public void Commit(TP.Transaction context)
  {
    // transactionManager.Commit(context);
  }

  public void Abort(TP.Transaction context)
  {
    // transactionManager.Abort(context);
  }
    
  // Need to add code
  // This method adds a resource to the available ones
  public bool Add(TP.Transaction context, TP.RID i, int count, int price)
  {

      
      if (!resources.ContainsKey(i))
      {
          resources.Add(i, new Resource(i, count, price));
      }
      else
      {
          Resource ii = resources[i];
          ii.incrCount(count);
          ii.setPrice(price);

          // TODO add locking code here
      }
      return true;
  }


  // Need to add code
  public bool Delete(TP.Transaction context, RID rid)
  { 
        // TODO add locking code here
        bool removed = resources.Remove(rid);

        // drop all reservations on removed resource
        if ( removed ) {
            foreach ( HashSet<RID> e in reservations.Values ) {
                e.Remove(rid);
            }
        }
        
        return removed;
  }
      
  

  public bool Delete(Transaction xid, RID rid, int count)
   {
        // TODO add locking code here
        Resource removed = resources[rid];
        if ( removed == null ) {
            // silently discard
        } else {
            if ( removed.getCount() > count ) {
                removed.decrCount(count);
            } else {
                removed.setCount(0);
            }
        }
        return true;
    }

		
  /// <summary>
  /*  NEED TO ADD CODE For STEP 2
		Calling shutdown causes RM to exit gracefully.
		This means, it waits for all the existing transactions 
		to end and enlist requests for new transactions are refused. 
		If any of the existing transactions blocks forever, 
		a retry/timeout mechanism is used to exit.
		No recovery is done on startup */
  /// </summary>
  public void Shutdown()
  {
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
    /*   Need to add code here
	    returns the amount available for the specified item type */
    /// </summary>
    public int Query(TP.Transaction context, RID rid)
    {
        Console.WriteLine("RM: Query");
        Resource resource = resources[rid];
  
	    if(resource == null)
	    {
            throw new ArgumentException(rid+" does not exist");
	    }
	    else
	    {
		    return resource.getCount();
	    }
    }

    // <summary>
    /* Need to add code here
	 returns the price for the specified item type */
    // </summary>
    public int QueryPrice(Transaction xid, RID i)
    {
        Resource ii = resources[i];
        if ( ii == null ) {
            throw new ArgumentException(i+" does not exist");
        }
        return ii.getPrice();
    }
  
  
    public String QueryReserved(Transaction context, Customer customer)
    {
        StringBuilder buf = new StringBuilder(512);
        
        HashSet<RID> reserved = reservations[customer];
        if ( reserved != null ) {
            foreach ( RID rid in reserved ) {
                if ( buf.Length > 0 ) {
                    buf.Append(',');
                }
                buf.Append(rid);
            }
        }
        return buf.ToString();
    }

    
    public int QueryReservedPrice(Transaction context, Customer customer)
    {
        int bill = 0;
        
        HashSet<RID> reserved = reservations[customer];
        if ( reserved != null ) {
            foreach ( RID rid in reserved ) {
                Resource r = resources[rid];
                if ( r == null ) {
                    throw new InvalidOperationException(rid+" does not exist in RM");
                }
                bill += r.getPrice();
            }
        }
        
        return bill;
    }

    public bool Reserve(Transaction context, Customer c, RID i)
    {
        Resource ii = resources[i];
        
        if ( ii == null ) {
            throw new InvalidOperationException(i+" does not exist!");
        }
        if ( ii.getCount() == 0 ) {
            return false;
        }
        
        HashSet<RID> r = reservations[c];
        if ( r == null ) {
            r = new HashSet<RID>();
            r.Add(ii.getID());
            reservations.Add(c, r);
        } else {
            r.Add(ii.getID());
        }
        
        ii.decrCount();
        
        return true;
    }

    
    public void UnReserve(Transaction context, Customer c)
    {
        HashSet<RID> r = reservations[c];
        if ( r == null ) {
            // silently discard
        } else {
            // TODO need to add lock
            foreach ( RID rid in r ) {
                Resource ii = resources[rid];
                if ( ii == null ) {
                    // FIXME warn that the rID does not exist!
                } else {
                    ii.incrCount();
                }
            }
            reservations.Remove(c);
        }
    }

    public String[] ListResources(Transaction context, RID.Type type)
    {
        List<string> result = new List<string>(resources.Count);
        foreach ( Resource resource in resources.Values) {
            if ( type == resource.getType() ) {
                result.Add(resource.toString());
            }
        }
        return result.ToArray();
    }

    public Customer[] ListCustomers(Transaction context)
    {
        Customer[] customers = new Customer[reservations.Count];
        reservations.Keys.CopyTo(customers,0);
        return customers;

    }
    

    /**
     * @todo setup {@link #selfDestruct(int)} here
     */
    
    protected void Init(String[] args){
        // TODO set self destruct counter
     
    }

    
    protected void InitStorage() {
        // TODO create database files, transaction logs
    }

    
    protected void Recovery() {
        // TODO recover state from database file
    }

   
    protected void StartUp() {
        // TODO deadlock detector, retry timeout
    }
    
    


 }
}
