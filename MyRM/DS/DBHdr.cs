

namespace MyRM.DS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [System.Serializable()]
    class DBHdr
    {
        #region Private Members

        private static byte[] MagicKey = { 0x1A, 0x2B, 0x3C, 0x4D };

        #endregion

        public int PageManager
        {
            get;
            set;
        }

        public int PageTable
        {
            get;
            set;
        }

        public int ResourceIndex
        {
            get;
            set;
        }
    }
}
