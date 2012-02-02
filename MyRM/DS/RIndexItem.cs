namespace MyRM.DS
{
    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
    using TP;

    [System.Serializable()]
    public class RIndexItem
    {
        #region Private Members

        private RID i;
        private int p;
        private int r;

        [NonSerialized]
        private bool isDirty;

        #endregion

        
        public bool IsDirty
        {
            get
            {
                return isDirty;
            }
            set
            {
                isDirty = value;
            }
        }

        public RID ResourceId
        {
            get
            {
                return i;
            }
            set
            {
                i = value;
            }
        }

        public int Page
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

        public int Record
        {
            get
            {
                return r;
            }
            set
            {
                r = value;
            }
        }
    }
}
