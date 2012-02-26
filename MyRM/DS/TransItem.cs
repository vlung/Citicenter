namespace MyRM.DS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TP;

    [System.Serializable()]
    public class TransItem
    {
        #region Constants

        public const int NotStored = -1;

        #endregion

        #region Private Members
        
        private int p;
        private int r;
        private int c;
        private int a;
        private int f;
        private Transaction t;

        [NonSerialized]
        private StorageContext contextData;

        [NonSerialized]
        private List<int> usedPages;

        #endregion

        public TransItem()
        {
            this.PageTableStartPage = NotStored;
            this.ResourceIndexStartPage = NotStored;
            this.ReservationIndexStartPage = NotStored;
            this.AllocatedPageListStartPage = NotStored;
            this.FreedPageListStartPage = NotStored;

            this.Transaction = null;
            this.TransactionData = null;
            this.StoragePageList = new List<int>();
        }

        public int PageTableStartPage
        {
            get
            {
                return this.p;
            }

            set
            {
                this.p = value;
            }
        }

        public int ResourceIndexStartPage
        {
            get
            {
                return this.r;
            }

            set
            {
                this.r = value;
            }
        }

        public int ReservationIndexStartPage
        {
            get
            {
                return this.c;
            }

            set
            {
                this.c = value;
            }
        }

        public int AllocatedPageListStartPage
        {
            get
            {
                return this.a;
            }

            set
            {
                this.a = value;
            }
        }

        public int FreedPageListStartPage
        {
            get
            {
                return this.f;
            }

            set
            {
                this.f = value;
            }
        }

        public Transaction Transaction
        {
            get
            {
                return this.t;
            }

            set
            {
                this.t = value;
            }
        }

        public StorageContext TransactionData
        {
            get
            {
                return this.contextData;
            }

            set
            {
                this.contextData = value;
            }
        }

        public List<int> StoragePageList
        {
            get
            {
                return this.usedPages;
            }

            set
            {
                this.usedPages = value;
            }
        }
    }
}
