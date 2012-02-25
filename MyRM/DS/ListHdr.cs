using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyRM.DS
{
    [System.Serializable()]
    class ListHdr
    {
        #region Constants

        public const int HeaderRecordIdx = 0;
        public const int EOLPageIndex = -1;

        #endregion

        #region Private Members

        private int p;
        private int pc;
        private int tc;

        [NonSerialized]
        private int pageWriteIndex;

        #endregion

        public int NextPageIndex
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

        public int PageEntriesCount
        {
            get
            {
                return pc;
            }
            set
            {
                pc = value;
            }
        }

        public int TotalEntriesCount
        {
            get
            {
                return tc;
            }
            set
            {
                tc = value;
            }
        }
       
        public int PageWriteIndex
        {
            get
            {
                return this.pageWriteIndex;
            }

            set
            {
                this.pageWriteIndex = value;
            }
        }
    }
}
