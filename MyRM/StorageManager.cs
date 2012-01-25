namespace MyRM
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using TP;

    public class StorageManager
    {
        #region Private Members

        private static byte[] MagicKey = {0x1A, 0x2B, 0x3C, 0x4D};
        private static int RootPage = 1;
        private static int Default_PageTablePage = 2;

        private static object DataFileLock = new object();

        private FileStream dataFile;
        private Dictionary<Transaction, StorageContext> contextMap;

        #endregion

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
            data = new Reservation(rID);
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
            this.contextMap = new Dictionary<Transaction, StorageContext>();
        }

        protected void Init(string filePath)
        {
            lock (DataFileLock)
            {
                // open the file
                this.dataFile = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                // setup the data file if needed
                if (!this.IsInitializedDataFile())
                {
                    this.InitializeDataFile();
                }

                // flush the file
                this.dataFile.Flush();
                this.dataFile.Close();
            }
        }

        #endregion

        #region Private Methods

        private bool IsInitializedDataFile()
        {
            byte[] data = new byte[MagicKey.Length];

            // read the magic key from the file
            this.dataFile.Seek(0, SeekOrigin.Begin);

            int bytesRead = this.dataFile.Read(data, 0, data.Length);
            if (bytesRead != data.Length)
            {
                return false;
            }

            return MagicKey.SequenceEqual(data);
        }

        private void InitializeDataFile()
        {
            // write the magic key
            this.dataFile.Seek(0, SeekOrigin.Begin);
            this.dataFile.Write(MagicKey, 0, MagicKey.Length);

            // create the page table
            StoragePageTable pageTable = new StoragePageTable(Default_PageTablePage + 1);
            pageTable.WritePageTableData(this.dataFile, Default_PageTablePage);
        }

        #endregion
    }
}
