

namespace MyRM.DS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;

    class ListReader<T>
    {
        #region Private Members

        private List<T> itemList;
        private List<int> pageList;

        #endregion

        #region Public Methods

        public ListReader()
        {
            this.itemList = new List<T>();
            this.pageList = new List<int>();
        }

        public void ReadList(FileStream stream, int pageIdx, out List<T> list, out List<int> pages)
        {
            // read pages one by one
            while (ListHdr.EOLPageIndex != pageIdx)
            {
                // read the page data
                StoragePage page = new StoragePage();
                int readPage = page.ReadPageData(stream, pageIdx);
                if (readPage != pageIdx)
                {
                    throw new InvalidListException();
                }

                // read the header
                ListHdr header = (ListHdr)page.ReadRecord(ListHdr.HeaderRecordIdx);
                if (null == header)
                {
                    throw new InvalidListException();
                }

                // process the page
                this.ReadCurrentPage(page, header.PageEntriesCount);

                // update the page index
                this.pageList.Add(pageIdx);
                pageIdx = header.NextPageIndex;
            }

            // set the output variables
            list = this.itemList;
            pages = this.pageList;
        }

        #endregion

        #region Private Methods

        private void ReadCurrentPage(StoragePage page, int itemCount)
        {
            for (int idx = 0; idx < (itemCount + 1); idx++)
            {
                if (ListHdr.HeaderRecordIdx == idx)
                {
                    // skip over the header record
                    continue;
                }

                T item = (T)page.ReadRecord(idx);
                this.itemList.Add(item);
            }
        }

        #endregion

        #region Exception Classes

        public class InvalidListException : System.Exception
        {
            public InvalidListException()
                : base("Unable to deserialize list.")
            {
            }

            public InvalidListException(string message)
                : base(message)
            {
            }

            public InvalidListException(string message, System.Exception e)
                : base(message, e)
            {
            }
        }

        #endregion
    }
}
