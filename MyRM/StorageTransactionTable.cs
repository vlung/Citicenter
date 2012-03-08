namespace MyRM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using DS;
    using TP;

    [System.Serializable()]
    public class StorageTransactionTable
    {
        #region Private Members

        private Dictionary<Transaction, TransItem> contextTable;
        private List<int> contextTableStoragePages;

        #endregion

        #region Public Methods

        public StorageTransactionTable()
        {
            this.contextTable = new Dictionary<Transaction,TransItem>();
            this.contextTableStoragePages = new List<int>();
        }

        /// <summary>
        /// Adds a transaction data to the list.
        /// </summary>
        /// <param name="contextData">transaction data</param>
        public void Add(TransItem contextData)
        {
            if (this.contextTable.ContainsKey(contextData.Transaction))
            {
                throw new InvalidOperationException();
            }

            this.contextTable.Add(contextData.Transaction, contextData);
        }

        /// <summary>
        /// Retruns true if the transaction is in the list.
        /// </summary>
        /// <param name="context">transaction id</param>
        /// <returns>true or false.</returns>
        public bool Contains(Transaction context)
        {
            return this.contextTable.ContainsKey(context);
        }

        /// <summary>
        /// Gets the entire list of transactions. Called during startup to
        /// complete pre-pared transactions.
        /// </summary>
        /// <returns></returns>
        public List<Transaction> GetTransactionList()
        {
            return this.contextTable.Keys.ToList();
        }

        /// <summary>
        /// Writes the data item to persitent storage as a list of items.
        /// </summary>
        /// <param name="stream">data file to write to</param>
        /// <param name="manager">object that keeps track of free pages in the file</param>
        /// <param name="freedPages">list of pages to be freed when transaction commits</param>
        /// <returns>index of the first page storing the list</returns>
        public int WriteTransactionTableData(FileStreamWrapper stream, StoragePageManager manager, out List<int> freedPages)
        {
            List<int> pageIdxList = null;

            // create the writer
            ListWriter<TransItem> writer = new ListWriter<TransItem>();
            writer.WriteList(stream, manager, this.contextTable.Values.ToList() , out pageIdxList);

            // update the list that stores the physical page idx
            freedPages = this.contextTableStoragePages;
            this.contextTableStoragePages = pageIdxList;

            // return the index of the first page
            return this.contextTableStoragePages[0];
        }

        /// <summary>
        /// Reads the list of data items whose head is stored at the page index provided.
        /// </summary>
        /// <param name="stream">data file to read from</param>
        /// <param name="pageIdx">index of the first physical page storing the list</param>
        /// <returns>returns the index of the first physical page we read data from</returns>
        public int ReadTransactionTableData(FileStreamWrapper stream, int pageIdx)
        {
            List<TransItem> itemList = null;
            List<int> pageIdxList = null;

            // create reader
            ListReader<TransItem> reader = new ListReader<TransItem>();
            reader.ReadList(stream, pageIdx, out itemList, out pageIdxList);

            // clear the current data
            this.contextTable.Clear();

            foreach (TransItem item in itemList)
            {
                this.contextTable.Add(item.Transaction, item);
            }

            // update page index
            this.contextTableStoragePages = pageIdxList;

            // return index of the first page
            return this.contextTableStoragePages[0];
        }

        /// <summary>
        /// Removes the transaction data from the list.
        /// </summary>
        /// <param name="context">transaction id</param>
        /// <returns>the item we just removed from the list</returns>
        public TransItem Remove(Transaction context)
        {
            TransItem item = null;
            if (!this.contextTable.TryGetValue(context, out item))
            {
                return null;
            }
            this.contextTable.Remove(context);

            return item;
        }

        #endregion
    }
}
