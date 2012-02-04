namespace MyRM.DS
{
    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
    using TP;

    [System.Serializable()]
    public class IndexItem<T>
    {
        #region Private Members

        private T i;
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

        public T ResourceId
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
