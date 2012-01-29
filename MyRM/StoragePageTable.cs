using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyRM
{
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
                && pageTable.Count > logocalPage)
            {
                return pageTable[logocalPage].PageIndex;
            }

            return -1;
        }

        public int SetLogicalPage(int physicalPage)
        {
            PageTableItem item = new PageTableItem()
            {
                IsChanged = true,
                PageIndex = physicalPage,
            };

            pageTable.Add(item);
            return pageTable.IndexOf(item);
        }

        public void UpdatePage(int logicalPage, int physicalPage)
        {
            if (0 > logicalPage
                || (pageTable.Count - 1) < logicalPage)
            {
                throw new InvalidLogicalAddressException();
            }

            pageTable[logicalPage].PageIndex = physicalPage;
            pageTable[logicalPage].IsChanged = true;
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
                item.IsChanged = false;
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
                if (this.pageTable[idx].IsChanged)
                {
                    itemList[idx] = this.pageTable[idx];
                }
            }
            this.pageTable = itemList;

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
