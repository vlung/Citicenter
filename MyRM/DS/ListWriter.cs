
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

        public void WriteList(FileStream stream, StoragePageManager freeSpaceMgr, List<T> list, out List<int> pages)
        {
            // create the pages
            this.CreateNewPage(list.Count);
            for(int idx = 0; idx < list.Count; idx++)
            {
                var item = list[idx];

                try
                {
                    this.pageList.Peek().AddRecord(item);
                    this.pageHeaderList.Peek().PageEntriesCount++;
                }
                catch (StoragePage.InsuffcientSpaceException e)
                {
                    this.CreateNewPage(list.Count);
                    idx--;
                }
            }

            // write the pages
            int lastPageIndex = ListHdr.EOLPageIndex;
            while (0 < this.pageList.Count)
            {
                lastPageIndex = this.WriteTopPage(stream, freeSpaceMgr, lastPageIndex);
            }

            // set the output parameter
            pages = this.pageIdxList;
        }

        #endregion

        #region Private Methods

        private void CreateNewPage(int count)
        {
            // create a new header object
            ListHdr header = new ListHdr()
            {
                NextPageIndex = ListHdr.EOLPageIndex,
                TotalEntriesCount = count,
                PageEntriesCount = 0,
            };
            this.pageHeaderList.Push(header);

            // create the page - and write a placeholder record for the header
            StoragePage page = new StoragePage();
            page.AddRecord(header);
            this.pageList.Push(page);
        }

        private int WriteTopPage(FileStream stream, StoragePageManager mgr, int lastPageAddress)
        {
            // update the header
            ListHdr header = this.pageHeaderList.Pop();
            header.NextPageIndex = lastPageAddress;

            // update the page
            StoragePage page = this.pageList.Pop();
            page.WriteRecord(ListHdr.HeaderRecordIdx, header);

            // get the free page to write to
            int pageIdx = mgr.GetFreePage(stream);

            // write the page
            pageIdx = page.WritePageData(stream, pageIdx);
            this.pageIdxList.Insert(0, pageIdx);

            return pageIdx;
        }

        #endregion
    }
}
