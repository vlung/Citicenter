using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyRM
{
    public class StorageContext
    {
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

        public StorageFreeSpaceManager FreePageList
        {
            get;
            set;
        }
    }
}
