using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyRM
{
    using TP;

    [System.Serializable()]
    public class Reservation
    {
        private Customer rID;
        private List<RID> resourceList;

        public Reservation(Customer rID)
            : this(rID, null)
        {
        }

        public Reservation(Customer rID, RID[] resources)
        {
            this.ID = rID;
            this.Resources = resources;
        }

        public Customer ID
        {
            get
            {
                return this.rID;
            }

            private set
            {
                this.rID = value;
            }
        }

        public RID[] Resources
        {
            get
            {
                if (null == this.resourceList)
                {
                    return null;
                }
                return this.resourceList.ToArray();
            }

            private set
            {
                if (null == this.resourceList)
                {
                    this.resourceList = new List<RID>();
                }

                this.resourceList.Clear();
                this.resourceList.AddRange(value);
            }
        }

        public void AddResource(RID resource)
        {
            if (null == this.resourceList)
            {
                this.resourceList = new List<RID>();
            }

            this.resourceList.Add(resource);
        }

        public int hashCode()
        {
            return rID.GetHashCode();
        }

        public string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(rID.ToString());

            foreach(var resource in this.resourceList)
            {
                builder.Append(",");
                builder.Append(resource.ToString());
            }

            return builder.ToString();
        }
    }
}
