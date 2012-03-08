
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

        public int GetLastLogicalPage()
        {
            return (this.pageTable.Count - 1);
        }

        /// <summary>
        /// Gets the physical page index for a logical page.
        /// Returns the physical page index of the "last" (highes index) logical
        /// page if the logical page index is less than 0  or greater than 
        /// the "last".
        /// </summary>
        /// <param name="logicalPage">logical page index</param>
        /// <returns>physical page index</returns>
        public int GetPhysicalPage(int logicalPage)
        {
            if (logicalPage >= 0
                && this.pageTable.Count > logicalPage)
            {
                return this.pageTable[logicalPage].PageIndex;
            }
            else if (this.pageTable.Count <= logicalPage
                && this.pageTable.Count > 0)
            {
                return this.pageTable[this.pageTable.Count - 1].PageIndex;
            }

            return -1;
        }

        /// <summary>
        /// Sets the physical page index for the next logical page in the sequence.
        /// </summary>
        /// <param name="physicalPage">physical page index</param>
        /// <returns>logical page index</returns>
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

        /// <summary>
        /// Updates the physical page address for a logical page.
        /// </summary>
        /// <param name="logicalPage">logical page index</param>
        /// <param name="physicalPage">new physical page index</param>
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

        /// <summary>
        /// Clears the "dirty" flag from all entries to ensure that future reads from
        /// persitent storage over-write all entries.
        /// </summary>
        public void ClearDirtyFlags()
        {
            // mark all items as clean
            foreach (PageTableItem item in this.pageTable)
            {
                item.IsDirty = false;
            }
        }

        /// <summary>
        /// Writes the page table item to persitent storage as a list of items.
        /// </summary>
        /// <param name="stream">data file</param>
        /// <param name="manager">object that keeps track of free pages in the file</param>
        /// <param name="freedPages">list of pages to be freed when transaction commits</param>
        /// <returns>index of the first page storing the list</returns>
        public int WritePageTableData(FileStreamWrapper stream, StoragePageManager manager, out List<int> freedPages)
        {
            List<int> pageIdxList = null;

            // create the writer
            ListWriter<PageTableItem> writer = new ListWriter<PageTableItem>();
            writer.WriteList(stream, manager, this.pageTable, out pageIdxList);

            // update the list that stores the physical page idx
            freedPages = this.pageTableStoragePages;
            this.pageTableStoragePages = pageIdxList;

            // return the index of the first page
            return this.pageTableStoragePages[0];
        }

        /// <summary>
        /// Reads the list of page table items whose head is stored at the page index provided.
        /// The data read from persistent storage is merges with the data already in memory
        /// using the following protocol:
        ///     if the in memory data has the "IsDirty" flag set then we keep the in memory data
        ///     else we over-write the in-memory data with the data from disk
        /// </summary>
        /// <param name="stream">data file to read from</param>
        /// <param name="pageIdx">index of the first physical page storing the list</param>
        /// <returns>returns the index of the first physical page we read data from</returns>
        public int ReadPageTableData(FileStreamWrapper stream, int pageIdx)
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

            // add the missing ones
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

        /// <summary>
        /// Gets the list of pages that store the data in persistent storage.
        /// </summary>
        /// <returns>list of physical page indeces</returns>
        public List<int> GetStoragePages()
        {
            return this.pageTableStoragePages;
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
