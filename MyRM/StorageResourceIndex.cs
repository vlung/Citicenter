namespace MyRM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using DS;
    using TP;

    [System.Serializable()]
    public class StorageResourceIndex
    {
        #region Private Members

        private Dictionary<RID, RIndexItem> indexMap;
        private List<int> indexStoragePages;

        #endregion

        #region Public Methods

        public StorageResourceIndex()
        {
            this.indexMap = new Dictionary<RID, RIndexItem>();
            this.indexStoragePages = new List<int>();
        }

        public RIndexItem GetResourceAddress(RID resourceId)
        {
            RIndexItem address = null;
            if (!this.indexMap.TryGetValue(resourceId, out address))
            {
                return null;
            }

            return address;
        }

        public void SetResourceAddress(RID resourceId, RIndexItem address)
        {
            if (null == resourceId)
            {
                throw new Exception("Unknown resource!");
            }

            if (null != address)
            {
                // make sure the address contains the RID and
                // the store the value
                address.ResourceId = resourceId;
                address.IsDirty = true;
            }

            this.indexMap[resourceId] = address;
        }

        public int WriteIndexData(FileStream stream, StorageFreeSpaceManager mgr)
        {
            List<int> pageIdxList = null;

            // create the writer
            ListWriter<RIndexItem> writer = new ListWriter<RIndexItem>();
            writer.WriteList(
                stream, 
                mgr, 
                this.indexMap.Values.Where(c => c != null).ToList(), 
                out pageIdxList);

            // update the list that stores the physical page idx
            mgr.SetFreePages(this.indexStoragePages);
            this.indexStoragePages = pageIdxList;

            // mark all items as clean
            foreach (var item in this.indexMap.Values)
            {
                item.IsDirty = false;
            }

            // return the index of the first page
            return this.indexStoragePages[0];
        }

        public int ReadIndexData(FileStream stream, int pageIdx)
        {
            List<RIndexItem> itemList = null;
            List<int> pageIdxList = null;

            // create reader
            ListReader<RIndexItem> reader = new ListReader<RIndexItem>();
            reader.ReadList(stream, pageIdx, out itemList, out pageIdxList);

            // merge with current data
            for (int idx = 0; idx < itemList.Count; idx++)
            {
                var item = itemList[idx];

                RIndexItem indexEntry = null;
                if (this.indexMap.TryGetValue(item.ResourceId, out indexEntry))
                {
                    if (null == indexEntry
                        || indexEntry.IsDirty)
                    {
                        continue;
                    }
                }

                this.indexMap[item.ResourceId] = item;
            }

            // update page index
            this.indexStoragePages = pageIdxList;

            // return index of the first page
            return this.indexStoragePages[0];
        }

        #endregion
    }
}
