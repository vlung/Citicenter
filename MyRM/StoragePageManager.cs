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

        private const int SelfWriteBarrier = -1;

        private List<int> freePages;

        private List<int> managerStoragePages;
        private bool writingSelf;

        #endregion

        #region Public Methods

        public StoragePageManager()
        {
            this.freePages = new List<int>();

            this.managerStoragePages = new List<int>();
            this.writingSelf = false;
        }

        public int GetFreePage()
        {
            int freePage = -1;

            lock (this.freePages)
            {
                do
                {
                    if (0 == this.freePages.Count)
                    {
                        return -1;
                    }


                    if (this.writingSelf)
                    {
                        int lastIndex = this.freePages.Count - 1;
                        freePage = this.freePages[lastIndex];
                        if(SelfWriteBarrier == freePage)
                        {
                            return -1;
                        }
                        this.freePages.RemoveAt(lastIndex);
                    }
                    else
                    {
                        freePage = this.freePages[0];
                        this.freePages.RemoveAt(0);
                    }
                } 
                while (SelfWriteBarrier == freePage);
            }

            return freePage;
        }

        public void SetFreePage(int page)
        {
            lock (this.freePages)
            {
                if (this.freePages.Contains(page))
                {
                    // page already marked as free so nothing to do
                    return;
                }

                this.freePages.Add(page);
            }
        }

        public void SetFreePages(List<int> pages)
        {
            foreach (var page in pages)
            {
                this.SetFreePage(page);
            }
        }

        public int WritePageManagerData(FileStreamWrapper stream)
        {
            lock (this.freePages)
            {
                this.writingSelf = true;

                // make the list of pages to write
                this.freePages.Insert(0, SelfWriteBarrier);
                this.freePages.InsertRange(0, this.managerStoragePages);

                // create writer
                ListWriter<int> writer = new ListWriter<int>();
                writer.WriteList(stream, this, this.freePages, out this.managerStoragePages);

                this.writingSelf = false;
            }

            return this.managerStoragePages[0];
        }

        public int ReadPageManagerData(FileStreamWrapper stream, int pageIdx)
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
