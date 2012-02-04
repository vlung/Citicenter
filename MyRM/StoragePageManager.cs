namespace MyRM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using DS;

    public class StoragePageManager
    {
        #region Private Members

        private Queue<int> freePages;

        private List<int> managerStoragePages;
        private bool writingSelf;

        #endregion

        #region Public Methods

        public StoragePageManager()
        {
            this.freePages = new Queue<int>();

            this.managerStoragePages = new List<int>();
            this.writingSelf = false;
        }

        public int GetFreePage(FileStream stream)
        {
            if (0 == this.freePages.Count
                || this.writingSelf)
            {
                return -1;
            }

            return this.freePages.Dequeue();
        }

        public void SetFreePage(int page)
        {
            if (this.freePages.Contains(page))
            {
                // page already marked as free so nothing to do
                return;
            }

            this.freePages.Enqueue(page);
        }

        public void SetFreePages(List<int> pages)
        {
            foreach (var page in pages)
            {
                this.SetFreePage(page);
            }
        }

        public int WritePageManagerData(FileStream stream)
        {
            this.writingSelf = true;

            // make the list of pages to write
            List<int> freePageList = this.freePages.ToList();
            freePageList.AddRange(this.managerStoragePages);

            // create writer
            ListWriter<int> writer = new ListWriter<int>();
            writer.WriteList(stream, this, freePageList, out this.managerStoragePages);

            // update the free page index
            this.SetFreePages(freePageList);

            this.writingSelf = false;
            return this.managerStoragePages[0];
        }

        public int ReadPageManagerData(FileStream stream, int pageIdx)
        {
            List<int> itemList = null;
            List<int> pageIdxList = null;

            // create reader
            ListReader<int> reader = new ListReader<int>();
            reader.ReadList(stream, pageIdx, out itemList, out pageIdxList);

            // merge with current data
            this.SetFreePages(itemList);

            // update page index
            this.managerStoragePages = pageIdxList;

            return this.managerStoragePages[0];
        }

        #endregion
    }
}
