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
            this.Id = rID;
            this.Resources = new List<RID>();
            if (null != resources)
            {
                this.Resources.AddRange(resources);
            }
        }

        public Customer Id
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

        public List<RID> Resources
        {
            get
            {
                return this.resourceList;
            }

            set
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

            if (this.resourceList.Contains(resource))
            {
                // already have this resource
                return;
            }

            this.resourceList.Add(resource);
        }

        public bool Equals(Reservation other)
        {
            if (null == other)
            {
                return false;
            }

            // compare ids
            if (!this.Id.Equals(other.Id))
            {
                return false;
            }

            // compare lists
            if (this.resourceList.Count() != other.resourceList.Count())
            {
                return false;
            }

            // compare each resource
            foreach (var resource in this.resourceList)
            {
                if (!other.resourceList.Contains(resource))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                return true;
            }

            if (obj is Reservation)
            {
                return this.Equals((Reservation)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
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
