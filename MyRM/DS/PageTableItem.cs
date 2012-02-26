namespace MyRM.DS
{
    using System;

    [System.Serializable()]
    class PageTableItem
    {
        #region Private Members

        // use one letter names to make serialization compact
        private int p;
        private bool d;

        #endregion

        public PageTableItem()
        {
            this.IsDirty = false;
            this.PageIndex = -1;
        }
        
        public bool IsDirty
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
