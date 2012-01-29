using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyRM
{
    using System.IO;

    public class StorageFreeSpaceManager
    {
        #region Private Members

        private Queue<int> freePages; 

        #endregion

        #region Public Methods

        public StorageFreeSpaceManager()
        {
            this.freePages = new Queue<int>();
        }

        public int GetFreePage(FileStream stream)
        {
            if (0 == this.freePages.Count)
            {
                return -1;
            }

            return this.freePages.Dequeue();
        }

        public void SetFreePage(int page)
        {
            this.freePages.Enqueue(page);
        }

        public void SetFreePages(List<int> pages)
        {
            foreach (var page in pages)
            {
                this.freePages.Enqueue(page);
            }
        }

        #endregion
    }
}
