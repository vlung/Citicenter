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
    public class StorageIndex<T>
    {
        #region Private Members

        private Dictionary<T, IndexItem<T>> indexMap;
        private List<int> indexStoragePages;

        #endregion

        #region Public Methods

        public StorageIndex()
        {
            this.indexMap = new Dictionary<T, IndexItem<T>>();
            this.indexStoragePages = new List<int>();
        }

        public IEnumerable<T> GetIdList()
        {
            return indexMap.Keys;
        }

        public IndexItem<T> GetResourceAddress(T resourceId)
        {
            IndexItem<T> address = null;
            if (!this.indexMap.TryGetValue(resourceId, out address))
            {
                return null;
            }

            return address;
        }

        public void SetResourceAddress(T resourceId, IndexItem<T> address)
        {
            if (null == resourceId)
            {
                throw new Exception("Unknown resource!");
            }

            if (null != address)
            {
                // make sure the address contains the resource and
                // the store the value
                address.ResourceId = resourceId;
                address.IsDirty = true;
            }

            this.indexMap[resourceId] = address;
        }

        public int WriteIndexData(FileStreamWrapper stream, StoragePageManager manager, out List<int> freedPages)
        {
            List<int> pageIdxList = null;

            // create the writer
            ListWriter<IndexItem<T>> writer = new ListWriter<IndexItem<T>>();
            writer.WriteList(
                stream, 
                manager, 
                this.indexMap.Values.Where(c => c != null).ToList(), 
                out pageIdxList);

            // update the list that stores the physical page idx
            freedPages = this.indexStoragePages;
            this.indexStoragePages = pageIdxList;

            // mark all items as clean
            foreach (var item in this.indexMap.Values)
            {
                if (null == item)
                {
                    continue;
                }
                item.IsDirty = false;
            }

            // return the index of the first page
            return this.indexStoragePages[0];
        }

        public int ReadIndexData(FileStreamWrapper stream, int pageIdx)
        {
            List<IndexItem<T>> itemList = null;
            List<int> pageIdxList = null;

            // create reader
            ListReader<IndexItem<T>> reader = new ListReader<IndexItem<T>>();
            reader.ReadList(stream, pageIdx, out itemList, out pageIdxList);

            // merge with current data
            for (int idx = 0; idx < itemList.Count; idx++)
            {
                var item = itemList[idx];

                IndexItem<T> indexEntry = null;
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

        public List<int> GetStoragePages()
        {
            return this.indexStoragePages;
        }

        #endregion
    }
}
