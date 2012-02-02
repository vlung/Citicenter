
namespace MyRM
{
    using System.Collections.Generic;
    using System.IO;
    using DS;

    [System.Serializable()]
    public class StoragePageTable
    {
        #region Private Members

        private List<PageTableItem> pageTable;
        private List<int> pageTableStoragePages;

        #endregion

        #region Public Methods

        public StoragePageTable()
        {
            this.pageTable = new List<PageTableItem>();
            this.pageTableStoragePages = new List<int>();
        }

        public int GetPhysicalPage(int logocalPage)
        {
            if (0 <= logocalPage
                && this.pageTable.Count > logocalPage)
            {
                return this.pageTable[logocalPage].PageIndex;
            }
            else if (0 < this.pageTable.Count)
            {
                return this.pageTable[this.pageTable.Count - 1].PageIndex;
            }

            return -1;
        }

        public int SetLogicalPage(int physicalPage)
        {
            PageTableItem item = new PageTableItem()
            {
                IsDirty = true,
                PageIndex = physicalPage,
            };

            this.pageTable.Add(item);
            return this.pageTable.IndexOf(item);
        }

        public void UpdatePage(int logicalPage, int physicalPage)
        {
            if (0 > logicalPage
                || (this.pageTable.Count - 1) < logicalPage)
            {
                throw new InvalidLogicalAddressException();
            }

            this.pageTable[logicalPage].PageIndex = physicalPage;
            this.pageTable[logicalPage].IsDirty = true;
        }        

        public int WritePageTableData(FileStream stream, StorageFreeSpaceManager mgr)
        {
            List<int> pageIdxList = null;

            // create the writer
            ListWriter<PageTableItem> writer = new ListWriter<PageTableItem>();
            writer.WriteList(stream, mgr, this.pageTable, out pageIdxList);

            // update the list that stores the physical page idx
            mgr.SetFreePages(this.pageTableStoragePages);
            this.pageTableStoragePages = pageIdxList;

            // mark all items as clean
            foreach (PageTableItem item in this.pageTable)
            {
                item.IsDirty = false;
            }

            // return the index of the first page
            return this.pageTableStoragePages[0];
        }

        public int ReadPageTableData(FileStream stream, int pageIdx)
        {
            List<PageTableItem> itemList = null;
            List<int> pageIdxList = null;

            // create reader
            ListReader<PageTableItem> reader = new ListReader<PageTableItem>();
            reader.ReadList(stream, pageIdx, out itemList, out pageIdxList);

            // merge with current data
            for (int idx = 0; idx < this.pageTable.Count && idx < itemList.Count; idx++)
            {
                if (!this.pageTable[idx].IsDirty)
                {
                    this.pageTable[idx] = itemList[idx];
                }
            }

            // add the missing one
            if (this.pageTable.Count < itemList.Count)
            {
                this.pageTable.AddRange(
                    itemList.GetRange(
                        this.pageTable.Count,
                        (itemList.Count - this.pageTable.Count)));
            }

            // update page index
            this.pageTableStoragePages = pageIdxList;

            // return index of the first page
            return this.pageTableStoragePages[0];
        }

        #endregion

        #region Exception Classes

        public class InvalidPageTableException : System.Exception
        {
            public InvalidPageTableException()
                : base("Unable to deserialize page table.")
            {
            }

            public InvalidPageTableException(string message)
                : base(message)
            {
            }

            public InvalidPageTableException(string message, System.Exception e)
                : base(message, e)
            {
            }
        }

        public class InvalidLogicalAddressException : System.Exception
        {
            public InvalidLogicalAddressException()
                : base("Unable resolve logical address.")
            {
            }

            public InvalidLogicalAddressException(string message)
                : base(message)
            {
            }

            public InvalidLogicalAddressException(string message, System.Exception e)
                : base(message, e)
            {
            }
        }

        #endregion
    }
}
