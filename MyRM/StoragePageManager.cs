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

        /// <summary>
        /// Retrieves the next free page to write to.
        /// Since we call this method when we write this list itself to persistent storage
        /// we insert a barrier value to prevent us over-writing pages that are just being freed by the
        /// transaction being currently commited. This prevents us from destroying the store if
        /// writing the DBHeader fails.
        /// </summary>
        /// <returns>physical page index</returns>
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

        /// <summary>
        /// Adds the page index to the list of tracked free pages.
        /// </summary>
        /// <param name="page"></param>
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

        /// <summary>
        /// Adds the page indexes to the list of tracked free pages.
        /// </summary>
        /// <param name="pages">list of physical page indeces</param>
        public void SetFreePages(List<int> pages)
        {
            foreach (var page in pages)
            {
                this.SetFreePage(page);
            }
        }

        /// <summary>
        /// Writes the data item to persitent storage as a list of items.
        /// </summary>
        /// <param name="stream">data file to write to</param>
        /// <param name="manager">object that keeps track of free pages in the file</param>
        /// <param name="freedPages">list of pages to be freed when transaction commits</param>
        /// <returns>index of the first page storing the list</returns>
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

        /// <summary>
        /// Reads the list of data items whose head is stored at the page index provided.
        /// </summary>
        /// <param name="stream">data file to read from</param>
        /// <param name="pageIdx">index of the first physical page storing the list</param>
        /// <returns>returns the index of the first physical page we read data from</returns>
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
