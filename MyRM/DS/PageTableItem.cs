namespace MyRM.DS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TP;

    [System.Serializable()]
    class PageTableItem
    {
        #region Private Members

        // use one letter names to make serialization compact
        private bool d;
        private int p;

        #endregion

        public bool IsChanged
        {
            get
            {
                return d;
            }
            set
            {
                d = value;
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
