namespace MyRM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TP;

    [System.Serializable()]
    class StoragePageTableEntry
    {
        private bool f;
        private int p;

        public bool IsChanged
        {
            get
            {
                return f;
            }
            set
            {
                f = value;
            }
        }

        public int PageIndex
        {
            get
            {
                return p;
            }
            set
            {
                p = value;
            }
        }
    }
}
