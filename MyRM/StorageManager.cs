namespace MyRM
{
    using System.Collections.Generic;
    using TP;

    class StorageManager
    {

        #region Public Methods

        public static StorageManager CreateObject(string filePath)
        {
            StorageManager obj = new StorageManager();
            obj.Init(filePath);

            return obj;
        }

        public void Commit(Transaction context)
        {
        }

        public void Abort(Transaction context)
        {
        }

        public bool Read(Transaction context, out List<Customer> data)
        {
            data = new List<Customer>();
            return true;
        }

        public bool Read(Transaction context, RID.Type rType, out List<Resource> data)
        {
            data = new List<Resource>();
            return true;
        }

        public bool Read(Transaction context, RID rID, out Resource data)
        {
            data = new Resource();
            return true;
        }

        public bool Read(Transaction context, Customer rID, out Reservation data)
        {
            data = new Reservation();
            return true;
        }

        public bool Write(Transaction context, Resource data)
        {
            return true;
        }

        public bool Write(Transaction context, Reservation data)
        {
            return true;
        }

        public bool Delete(Transaction context, RID rID)
        {
            return true;
        }

        public bool Delete(Transaction context, Customer rID)
        {
            return true;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected StorageManager()
        {
        }

        protected void Init(string filePath)
        {
        }

        #endregion
    }
}
