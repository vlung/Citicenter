
namespace MyRM.DS
{
    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
    using System.IO;

    class ListWriter<T>
    {
        #region Private Members

        private Stack<ListHdr> pageHeaderList;
        private Stack<StoragePage> pageList;
        private List<int> pageIdxList;

        #endregion

        #region Public Methods

        public ListWriter()
        {
            this.pageHeaderList = new Stack<ListHdr>();
            this.pageList = new Stack<StoragePage>();
            this.pageIdxList = new List<int>();
        }

        /// <summary>
        /// Writes a list of items to persistent storage using a "chain" format, where each
        /// page stores a "pointer" to the next page containing the following range of elements.
        /// </summary>
        /// <param name="stream">data file to write to</param>
        /// <param name="freeSpaceMgr">object that keeps track of available pages in the file</param>
        /// <param name="list">list of items to write</param>
        /// <param name="pages">list of pages we wrote to</param>
        public void WriteList(FileStreamWrapper stream, StoragePageManager freeSpaceMgr, List<T> list, out List<int> pages)
        {
            // create the pages
            this.CreateNewPage(list.Count, freeSpaceMgr);
            for(int idx = 0; idx < list.Count; idx++)
            {
                var item = list[idx];

                try
                {
                    this.pageList.Peek().AddRecord(item);
                    this.pageHeaderList.Peek().PageEntriesCount++;
                }
                catch (StoragePage.InsuffcientSpaceException)
                {
                    this.CreateNewPage(list.Count, freeSpaceMgr);
                    idx--;
                }
            }

            // write the pages
            int lastPageIndex = ListHdr.EOLPageIndex;
            while (0 < this.pageList.Count)
            {
                lastPageIndex = this.WriteTopPage(stream, lastPageIndex);
            }

            // set the output parameter
            pages = this.pageIdxList;
        }

        #endregion

        #region Private Methods

        private void CreateNewPage(int count, StoragePageManager freeSpaceMgr)
        {
            // create a new header object
            ListHdr header = new ListHdr()
            {
                NextPageIndex = ListHdr.EOLPageIndex,
                TotalEntriesCount = count,
                PageEntriesCount = 0,
                PageWriteIndex = freeSpaceMgr.GetFreePage()
            };
            this.pageHeaderList.Push(header);

            // create the page - and write a placeholder record for the header
            StoragePage page = new StoragePage();
            page.AddRecord(header);
            this.pageList.Push(page);
        }

        private int WriteTopPage(FileStreamWrapper stream, int lastPageAddress)
        {
            // update the header
            ListHdr header = this.pageHeaderList.Pop();
            header.NextPageIndex = lastPageAddress;

            // update the page
            StoragePage page = this.pageList.Pop();
            page.WriteRecord(ListHdr.HeaderRecordIdx, header);

            // get the page to write to
            int pageIdx = header.PageWriteIndex;

            // write the page
            pageIdx = page.WritePageData(stream, pageIdx);
            this.pageIdxList.Insert(0, pageIdx);

            return pageIdx;
        }

        #endregion
    }
}
