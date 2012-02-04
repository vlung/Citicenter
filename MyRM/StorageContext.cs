namespace MyRM
{
    using System.Collections.Generic;

    using TP;

    public class StorageContext
    {
        #region Constructors

        public StorageContext()
        {
            this.PageTable = new StoragePageTable();
            this.ResourceIndex = new StorageIndex<RID>();
            this.ReservationIndex = new StorageIndex<Customer>();
            
            this.AllocatedPageList = new List<int>();
            this.FreedPageList = new List<int>();
        }

        #endregion

        public StoragePageTable PageTable
        {
            get;
            set;
        }

        public StorageIndex<RID> ResourceIndex
        {
            get;
            set;
        }

        public StorageIndex<Customer> ReservationIndex
        {
            get;
            set;
        }

        public List<int> AllocatedPageList
        {
            get;
            set;
        }

        public List<int> FreedPageList
        {
            get;
            set;
        }
    }
}
