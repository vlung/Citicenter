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

        public void Add(TransItem contextData)
        {
            if (this.contextTable.ContainsKey(contextData.Transaction))
            {
                throw new InvalidOperationException();
            }

            this.contextTable.Add(contextData.Transaction, contextData);
        }

        public bool Contains(Transaction context)
        {
            return this.contextTable.ContainsKey(context);
        }

        public List<Transaction> GetTransactionList()
        {
            return this.contextTable.Keys.ToList();
        }

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
