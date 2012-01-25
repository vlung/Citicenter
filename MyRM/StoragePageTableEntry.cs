using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyRM
{
    [System.Serializable()]
    class StoragePageTableEntry
    {
        public bool IsChanged
        {
            get;
            set;
        }

        public int PhysicalPage
        {
            get;
            set;
        }
    }
}
