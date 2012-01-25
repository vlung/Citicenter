using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyRM
{
    using System.IO;

    [System.Serializable()]
    class StoragePageTable
    {
        #region Private Members

        private const int HeaderRecordIdx = 0;
        private const int EndOfListAddress = -1;

        private List<StoragePageTableEntry> pageTable;
        private StoragePageTableHeader pageTableHeader;

        #endregion

        #region Public Methods

        public StoragePageTable(int firstFreePageIdx)
        {
            this.pageTable = new List<StoragePageTableEntry>();
            this.pageTableHeader = new StoragePageTableHeader
            {
                TotalEntriesCount = 0,
                FirstFreePageIndex = firstFreePageIdx,
                PageEntriesCount = 0,
                NextPageIndex = EndOfListAddress
            };
        }

        public void WritePageTableData(FileStream stream, int pageIdx)
        {
            Stack<StoragePage> pageStack = new Stack<StoragePage>();

            // build the pages
            StoragePage page = new StoragePage();
            StoragePageTableHeader header = this.pageTableHeader;
            header.PageEntriesCount = 0;
            page.AddRecord(header);

            // build the pages
            foreach (var entry in this.pageTable)
            {
                try
                {
                    page.AddRecord(entry);
                }
                catch (StoragePage.InsuffcientSpaceException e)
                {
                    page.WriteRecord(HeaderRecordIdx, header);
                    pageStack.Push(page);

                    // it is ok if we throw here because it means the entry is larger than the page
                    // so we can never store this entry
                    header = new StoragePageTableHeader()
                    {
                        TotalEntriesCount = this.pageTableHeader.TotalEntriesCount,
                        NextPageIndex = EndOfListAddress,
                        PageEntriesCount = 0,
                        FirstFreePageIndex = EndOfListAddress,
                    };
                    page = new StoragePage();
                    page.AddRecord(header);

                    page.AddRecord(entry);
                    
                }
                finally
                {
                    header.PageEntriesCount++;
                }
            }

            // push the last page
            pageStack.Push(page);

            // write the pages
            int lastPageAddress = EndOfListAddress;
            while (1 < pageStack.Count)
            {
                page = pageStack.Pop();

                // update the header
                header = (StoragePageTableHeader)page.ReadRecord(HeaderRecordIdx);
                header.NextPageIndex = lastPageAddress;
                page.WriteRecord(HeaderRecordIdx, header);

                // get the page index to write to
                lastPageAddress = this.GetNextFreePageAddress(stream);

                // write the page
                page.WritePageData(stream, lastPageAddress);
            }

            // write the root page
            page = pageStack.Pop();

            // update the header
            this.pageTableHeader.NextPageIndex = lastPageAddress;
            page.WriteRecord(0, this.pageTableHeader);

            // write the page
            page.WritePageData(stream, pageIdx);
        }

        private int GetNextFreePageAddress(FileStream stream)
        {
            int nextFreePageIdx = this.pageTableHeader.FirstFreePageIndex;

            // update the free page list
            if (stream.Length <= StoragePage.GetPageAddress(this.pageTableHeader.FirstFreePageIndex))
            {
                this.pageTableHeader.FirstFreePageIndex++;
            }
            else
            {                
                StoragePage page = new StoragePage();
                page.ReadPageData(stream, this.pageTableHeader.FirstFreePageIndex);

                StoragePageTableHeader header = (StoragePageTableHeader)page.ReadRecord(HeaderRecordIdx);
                this.pageTableHeader.FirstFreePageIndex = header.NextPageIndex;
            }

            return nextFreePageIdx;
        }

        public void ReadPageTableData(FileStream stream, int pageIdx)
        {
            // load the page data from file
            StoragePage page = new StoragePage();
            page.ReadPageData(stream, pageIdx);

            // get the header
            this.pageTableHeader = (StoragePageTableHeader)page.ReadRecord(HeaderRecordIdx);
            if (null == this.pageTableHeader)
            {
                throw new InvalidPageTableException();
            }
        }

        #endregion

        #region Helper Classes

        [System.Serializable()]
        class StoragePageTableHeader
        {
            public int NextPageIndex
            {
                get;
                set;
            }

            public int PageEntriesCount
            {
                get;
                set;
            }

            public int FirstFreePageIndex
            {
                get;
                set;
            }

            public int TotalEntriesCount
            {
                get;
                set;
            }
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

        #endregion
    }
}
