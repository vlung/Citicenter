﻿namespace MyRM
{
    using System;
    using TP;

    /// <summary>
    /// Class to keep track of inventory information
    /// </summary>
    [System.Serializable()]
    public class Resource
    {
        private static readonly long serialVersionUID = 15980438631067294L;

        private RID i;
        private int c;
        private int p;

        public Resource() 
            : this(new RID(), 0, 0)
        { 
        }

        public Resource(RID rID)
            : this(rID, 0, 0)
        {
        }
        public Resource(RID rID, int c, int p)
        {
            this.i = rID;
            this.c = c;
            this.p = p;
        }

        public string Name
        {
            get
            {
                return i.getName();
            }
        }

        public RID Id 
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
        
        public RID.Type getType() 
        { 
            return i.getType(); 
        }
        
        public int getCount() 
        { 
            return c; 
        }

        public int getPrice() 
        { 
            return p; 
        }

        public void incrCount() 
        { 
            ++this.c; 
        }

        public void incrCount(int c) 
        { 
            this.c += c; 
        }

        public void decrCount(int c) 
        { 
            this.c -= c; 
        }

        public void decrCount() 
        { 
            --this.c; 
        }

        public void setCount(int count) 
        { 
            this.c = count; 
        }

        public void setPrice(int price) 
        { 
            this.p = price;
        }

        public bool Equals(Resource other)
        {
            if (null == other)
            {
                return false;
            }

            return (this.i.Equals(other.i)
                && this.p.Equals(other.p)
                && this.c.Equals(other.c));
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                return true;
            }

            if (obj is Resource)
            {
                return this.Equals((Resource)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return i.GetHashCode();
        }

        public override String ToString()
        {
            return i.ToString() + "," + c + "," + p;
        }
    }
}
