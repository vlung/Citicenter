using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TP
{
	/// <summary>
	/*	 This namespace provides the interfaces that define this project
		 We also provide files implementing some limited functionality for
		 these interfaces. You are welcome to write them yourself if you
		 prefer. */		 
	/// </summary>
    [Serializable()]
	public class Transaction : IComparable<Transaction>
	{
		/// <summary>
		/*   Transaction Identifier */
		/// </summary>
		public System.Guid Id;

		/// <summary>
		/*   Constructor */
		/// </summary> 
		public Transaction()
		{
			this.Id = System.Guid.NewGuid();
		}

		/* Override Equals and GetHashCode so that hashtables hash on the TX Guid */
		public override bool Equals(object o)
		{
			Transaction tx = o as Transaction;
			if(tx==null) return false;

			return tx.Id.Equals(this.Id);
		}

		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

        public override string ToString()
        {
            return base.ToString() + ":" + Id.ToString();
        }


        public int CompareTo(Transaction other)
        {
            return Id.CompareTo(other.Id);
        }
     }

	/// <summary>
	/*   Customer class */
	/// </summary>
    [Serializable()]
    public class Customer : IComparable<Customer>,Lockable
	{
		/* Customer Identifier */
		public System.Guid Id;

		/// <summary>
		/// Constructor. Generates a new Id for the customer
		/// </summary>
		public Customer()
		{
            this.Id = System.Guid.NewGuid();
        
        }

        public Customer(string s)
        {
            this.Id = new System.Guid(s);
        }

		public override int GetHashCode()
		{
			// Debug: 
			// Console.WriteLine(this+" GetHashCode="+this.Name.GetHashCode());

			return this.Id.GetHashCode ();
		}

		public override bool Equals(object obj)
		{
			// Debug: 
			// Console.WriteLine(this+" Equals "+obj+" = "+this.Name.Equals());

			TP.Customer cust = obj as TP.Customer;
			if(cust!=null) 
				return this.Id.Equals(cust.Id);

			return false;
		}
        public override string ToString()
        {
            return base.ToString() + ":" + Id.ToString();
        }

        public int CompareTo(Customer other)
        {
            return Id.CompareTo(other.Id);
        }

	}

	/// <summary>
	/*   Reservable Item class */
	/// </summary>
    [Serializable()]
	public class Item
	{
		public string Name;

		public Item(string name)
		{
			this.Name = name;
		}

		    public Item(int name)
		    {
			    this.Name = name.ToString();
		    }

		    public override int GetHashCode()
		    {
			    // Debug:
			    // Console.WriteLine(this+" GetHashCode="+this.Name.GetHashCode());
			
			    return this.Name.GetHashCode ();
		    }

		public override bool Equals(object obj)
		{
			// Debug:
			// Console.WriteLine(this+" Equals "+obj+" = "+this.Name.Equals());
			
			TP.Item item = obj as TP.Item;
			if(item!=null)
			{
				return this.Name.Equals(item.Name);
			}
			
			return false;
		}
	}

	
	/// <summary>
	/// Lock manager interface
	/// </summary>
	public interface LM
	{
		/// <param name="context"></param>
		/// <param name="resource"></param>
		void LockForRead(Transaction context, Lockable resource);

		/// <param name="context"></param>
		/// <param name="resource"></param>
        void LockForWrite(Transaction context, Lockable resource);
		
		/// <param name="context"></param>
		void UnlockAll(Transaction context);

        /// <param name="ms"></param>
        void setDeadlockTimeout(long ms);

        
	}

    /**
     * A marker interface to indicate the type of
     * lockable item. A Lockable item must properly implement
     * {@link Object#hashCode()}, {@link Object#equals(Object)}, and 
     * {@link Object#toString()} methods to be compatible with
     * {@link LM}.
     */
    public interface Lockable
    {
    }


	/// <summary>
	/*   Transaction manager interface */
	/// </summary>
	public interface TM
	{
		Transaction Start();

		/// <param name="context"></param>
		void Commit(Transaction context);


        RM GetResourceMananger(string name);
        
		/// <param name="context"></param>
		void Abort(Transaction context);
		
		/// <summary>
		/*   Enlist the RM as a member of this transaction */
		/// </summary>
		/// <param name="context"></param>
        /// <param name="enlistingRM"></param>
		bool Enlist(Transaction context, string enlistingRM);

        /// <summary>
		/* Register rm so that later TM could coordinate for two-phase commit. */
		/// </summary>
		/// <param name="rm"></param>
        void Register(string rm);
	}

	/// <summary>
	/*   Workflow controller interface */
	/// </summary>
	public interface WC
	{
		/// <param name="c"></param>
		/// <param name="flights"></param>
		/// <param name="location"></param>
		/// <param name="car"></param>
		/// <param name="room"></param>
		/// <returns>success</returns>
		bool ReserveItinerary(TP.Customer c, string[] flights, string location, bool car, bool room);

		/* This implies calling the Add method of the Resource Manager that handles flights.
		   Item here is usually the location name */
		//bool AddFlight(Transaction context, Item i, int count, int price );
        void Abort(Transaction context);
        void Commit(Transaction context);
        Transaction Start();

        bool AddSeats(Transaction context, string flight, int flightSeats, int flightPrice);
        bool CancelItinerary(Customer customer);        
        bool AddRooms(Transaction context, string location, int count, int price );
        bool AddCars(Transaction context, string location, int count, int price);

        bool DeleteFlight(Transaction context, string flight);
        bool DeleteRooms(Transaction context, string location, int numRooms);
		bool DeleteCars(Transaction context, string location, int numCars);
        bool DeleteSeats(Transaction context, string flight, int numSeats);
        
        string[] ListCars(Transaction context);
		Customer[] ListCustomers(Transaction context);
        string[] ListFlights(Transaction context);
        string[] ListRooms(Transaction context);

        int  QueryFlight(Transaction context, string flight);
		int  QueryFlightPrice(Transaction context, string flight);
		int  QueryRoom(Transaction context, string location);
		int  QueryRoomPrice(Transaction context, string location);
		int  QueryCar(Transaction context, string location);
		int  QueryCarPrice(Transaction context, string location);
        string QueryItinerary(Transaction context, Customer customer);
        int QueryItineraryPrice(Transaction context, Customer customer);

	}

	/// <summary>
	/*   Resource Manager Interface */
	/// </summary>
    public interface RM
    {
        /**
         * get the name of this RM
         * @return the name of this RM
         */
        string GetName();
        void SetName(string name);
        
        /// <summary>
        /// This function enlist the RM in the transaction
        /// </summary>
        /// <param name="context"></param>
        void Enlist(Transaction context);

        /// <summary>
        /// This function prepares the specified transaction for two phase commit
        /// </summary>
        /// <param name="context">Identifier of the transaction</param>
        /// <returns>True if the preparation is successful (ie Prepared).</returns>
        bool Prepare(Transaction context);

        /// <param name="context"></param>
        void Commit(Transaction context);

        /// <param name="context"></param>
        void Abort(Transaction context);

        /// <summary>
        /// Add "count" items 
        /// This method is equivalent to the addXXX in the Java Interface
        /// </summary>
        /// <param name="context"></param>
        /// <param name="resource"></param>
        /// <param name="count"></param>
        /// <param name="price"></param>
        /// <returns>success</returns>
        bool Add(Transaction context, RID resource, int count, int price);

        /// <summary>
        /// Remove exactly qty unreserved resource from this RM.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="resource"></param>
        /// <param name="count"></param>
        bool Delete(Transaction context, RID resource, int count);


        /// <summary>
        /* Drop resource from this RM. All reservations on resource must be dropped as well. */
        /// </summary>
        /// <param name="context"></param>
        /// <param name="resource"></param>
        bool Delete(Transaction context, RID resource);

        /// <summary>
        /*   Query: equivalent to QueryCars, QueryFlights, QueryRooms in the Java interface */
        /// </summary>
        /// <param name="context"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        int Query(Transaction context, RID resource);

        /// <param name="context"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        int QueryPrice(Transaction context, RID resource);

        /// <summary>
        /// list of customers who reserve resources managed by this RM
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Customer[] ListCustomers(Transaction context);

        /// <summary>
        /// list of resources
        /// </summary>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <returns>array of comma separated strings of available resource information</returns>
        string[] ListResources(Transaction context, RID.Type type);

        //string QueryCustomerInfo(Transaction context, Customer c);

        /// <param name="context"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        //int QueryCustomer(Transaction context, Customer c);

        /// <param name="context"></param>
        /// <param name="c"></param>
        /// <param name="resource"></param>
        /// <returns>sucess</returns>
        bool Reserve(Transaction context, Customer c, RID resource);

        /// <summary>
        /// Get the bill for the customer return a string representation of reservations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="customer"></param>
        string QueryReserved(Transaction context, Customer customer);

        /// <summary>
        /// Get the total amount of money the customer owes in this RM
        /// </summary>
        /// <param name="context"></param>
        /// <param name="customer"></param>
        int QueryReservedPrice(Transaction context, Customer customer);

        /// <param name="context"></param>
        /// <param name="c"></param>
        void UnReserve(Transaction context, Customer c);

        /// <summary>
        /// Shutdown should gracefully clean up its files, so when it
        ///	restarts, it should not need to recover the state
        /// </summary>
        void Shutdown();

        /// <summary>
        ///  Exit (simulate a failure) after a specified number of disk writes.
        ///  Support for this method requires a wrapper around the system's 
        ///  write system call that decrements the counter set by this method.
        ///  This counter should be set to zero by default, which makes the wrapper
        ///  doing nothing.  If the counter is non-zero, the wrapper should decrement								     
        ///  it, see if it is zero, and if so call exit().
        ///  This method is not part of a transaction. It is intended to simulate an
        ///	 RM failure.
        /// </summary>
        /// <param name="diskWritesToWait"></param>
        void SelfDestruct(int diskWritesToWait);
    }
}
