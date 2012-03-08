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

        /// <summary>
        /// Gets the logical address for a resource from the map.
        /// </summary>
        /// <param name="resourceId">id for the desired resource data</param>
        /// <returns>struct containing the logical address of the resource</returns>
        public IndexItem<T> GetResourceAddress(T resourceId)
        {
            IndexItem<T> address = null;
            if (!this.indexMap.TryGetValue(resourceId, out address))
            {
                return null;
            }

            return address;
        }

        /// <summary>
        /// Adds an entry to the map.
        /// </summary>
        /// <param name="resourceId">id for the resource data</param>
        /// <param name="address">struct containing the logical address of the resource data</param>
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

        /// <summary>
        /// Clears the "dirty" flag from all entries to ensure that future reads from
        /// persitent storage over-write all entries.
        /// </summary>
        public void ClearDirtyFlags()
        {
            // mark all items as clean
            foreach (var item in this.indexMap.Values)
            {
                if (null == item)
                {
                    continue;
                }
                item.IsDirty = false;
            }
        }

        /// <summary>
        /// Writes the index item to persitent storage as a list of items.
        /// We handle deletes by setting the "value" in the map to null and 
        /// not writing any items whose value is null to persistent storage.
        /// </summary>
        /// <param name="stream">data file to write to</param>
        /// <param name="manager">object that keeps track of free pages in the file</param>
        /// <param name="freedPages">list of pages to be freed when transaction commits</param>
        /// <returns>index of the first page storing the list</returns>
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

            // return the index of the first page
            return this.indexStoragePages[0];
        }

        /// <summary>
        /// Reads the list of index items whose head is stored at the page index provided.
        /// The data read from persistent storage is merges with the data already in memory
        /// using the following protocol:
        ///     if the in memory data has the "IsDirty" flag set then we keep the in memory data
        ///     else we over-write the in-memory data with the data from disk
        /// </summary>
        /// <param name="stream">data file to read from</param>
        /// <param name="pageIdx">index of the first physical page storing the list</param>
        /// <returns>returns the index of the first physical page we read data from</returns>
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
