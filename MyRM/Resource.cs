namespace MyRM
{
    using System;
    using TP;

    /// <summary>
    /// Class to keep track of inventory information
    /// </summary>
    [System.Serializable()]
    class Resource
    {
        private static readonly long serialVersionUID = 15980438631067294L;

        private RID rID;
        private int count;
        private int price;

        public Resource() 
        { 
        }

        public Resource(RID rID)
        {
            this.rID = rID;
        }
        public Resource(RID rID, int c, int p)
        {
            this.rID = rID;
            this.count = c;
            this.price = p;
        }

        public String getName()
        { 
            return rID.getName(); 
        }

        public RID getID() 
        { 
            return rID;
        }
        
        public RID.Type getType() 
        { 
            return rID.getType(); 
        }
        
        public int getCount() 
        { 
            return count; 
        }

        public int getPrice() 
        { 
            return price; 
        }

        public void incrCount() 
        { 
            ++this.count; 
        }

        public void incrCount(int c) 
        { 
            this.count += c; 
        }

        public void decrCount(int c) 
        { 
            this.count -= c; 
        }

        public void decrCount() 
        { 
            --this.count; 
        }

        public void setCount(int count) 
        { 
            this.count = count; 
        }

        public void setPrice(int price) 
        { 
            this.price = price;
        }
        
        public int hashCode() 
        { 
            return rID.GetHashCode(); 
        }

        public String toString()
        {
            return rID.getName() + "," + count + "," + price;
        }
    }
}
