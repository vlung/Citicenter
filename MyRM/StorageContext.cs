using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyRM
{
    public class StorageContext
    {
        #region Constructors

        public StorageContext()
        {
            this.PageTable = new StoragePageTable();
            this.ResourceIndex = new StorageResourceIndex();
            
            this.AllocatedPageList = new List<int>();
            this.FreedPageList = new List<int>();
        }

        #endregion

        public StoragePageTable PageTable
        {
            get;
            set;
        }

        public StorageResourceIndex ResourceIndex
        {
            get;
            set;
        }

        public List<int> AllocatedPageList
        {
            get;
            set;
        }

        public List<int> FreedPageList
        {
            get;
            set;
        }
    }
}
