namespace MyRM
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using DS;
    using TP;

    public class StorageManager
    {
        #region Constants

        private const int RootPage = 0;

        #endregion

        #region Private Members

        private static object DataFileLock = new object();

        private FileStream dataFile;
        private Dictionary<Transaction, StorageContext> contextMap;

        #endregion

        #region Public Methods

        public static StorageManager CreateObject(string filePath)
        {
            StorageManager obj = new StorageManager();
            obj.Init(filePath);

            return obj;
        }

        public void Commit(Transaction context)
        {
            StorageContext storageContext = this.GetStorageContext(context, true);
            if (null == storageContext)
            {
                throw new Exception();
            }

            lock(DataFileLock)
            {
                DBHdr dbRoot = this.ReadDBRoot();
                if (null == dbRoot)
                {
                    throw new Exception();
                }

                // merge page table
                storageContext.PageTable.ReadPageTableData(
                    this.dataFile, dbRoot.PageTable);

                // merge resource index
                storageContext.ResourceIndex.ReadIndexData(
                    this.dataFile, dbRoot.ResourceIndex);

                // write the page table
                dbRoot.PageTable = storageContext.PageTable.WritePageTableData(
                    this.dataFile, storageContext.FreePageList);

                // write the resource index
                dbRoot.ResourceIndex = storageContext.ResourceIndex.WriteIndexData(
                    this.dataFile, storageContext.FreePageList);

                this.WriteDBRoot(dbRoot);
                this.dataFile.Flush();
            }
        }

        public void Abort(Transaction context)
        {
        }

        public bool Read(Transaction context, out List<Customer> data)
        {
            data = new List<Customer>();
            return true;
        }

        public bool Read(Transaction context, RID.Type rType, out List<Resource> data)
        {
            data = new List<Resource>();
            return true;
        }

        public bool Read(Transaction context, RID rID, out Resource data)
        {
            // TODO: Add locking
            StorageContext storageContext = this.GetStorageContext(context, false);
            if (null == storageContext)
            {
                throw new Exception();
            }

            // look for the resource in the index
            RIndexItem address = storageContext.ResourceIndex.GetResourceAddress(rID);
            if (null == address)
            {
                data = null;
                return false;
            }

            // find the physical page
            int fileAddress = storageContext.PageTable.GetPhysicalPage(address.Page);

            // get the page
            StoragePage page = new StoragePage();
            page.ReadPageData(this.dataFile, fileAddress);

            // read the data
            data = (Resource)page.ReadRecord(address.Record);

            return true;
        }

        public bool Read(Transaction context, Customer rID, out Reservation data)
        {
            data = new Reservation(rID);
            return true;
        }

        public bool Write(Transaction context, Resource data)
        {
            // TODO: Add locking
            StorageContext storageContext = this.GetStorageContext(context, false);
            if (null == storageContext)
            {
                throw new Exception();
            }

            // look for the resource in the index
            RIndexItem address = storageContext.ResourceIndex.GetResourceAddress(data.getID());
            if (null == address)
            {
                address = new RIndexItem
                {
                    Page = -1,
                    Record = -1
                };
            }

            // find the physical page
            int fileAddress = storageContext.PageTable.GetPhysicalPage(address.Page);

            // get the page
            StoragePage page = new StoragePage();
            if (0 <= fileAddress)
            {
                page.ReadPageData(this.dataFile, fileAddress);
            }

            // write the record
            while (true)
            {
                try
                {
                    if (0 > address.Record)
                    {
                        address.Record = page.AddRecord(data);
                    }
                    else
                    {
                        page.WriteRecord(address.Record, data);
                    }
                }
                catch (StoragePage.InsuffcientSpaceException e)
                {
                    // did not fit on last page so allocate a new page
                    page = new StoragePage();
                    fileAddress = -1;
                    continue;
                }

                break;
            }

            // write the page
            if (0 <= fileAddress)
            {
                storageContext.FreePageList.SetFreePage(fileAddress);
            }
            fileAddress = page.WritePageData(
                this.dataFile, storageContext.FreePageList.GetFreePage(this.dataFile));

            // update the page table
            if (0 > address.Page)
            {
                address.Page = storageContext.PageTable.SetLogicalPage(fileAddress);
            }
            else
            {
                storageContext.PageTable.UpdatePage(address.Page, fileAddress);
            }

            // update the index
            storageContext.ResourceIndex.SetResourceAddress(data.getID(), address);

            return true;
        }

        public bool Write(Transaction context, Reservation data)
        {
            return true;
        }

        public bool Delete(Transaction context, RID rID)
        {
            return true;
        }

        public bool Delete(Transaction context, Customer rID)
        {
            return true;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected StorageManager()
        {
            this.contextMap = new Dictionary<Transaction, StorageContext>();
        }

        protected void Init(string filePath)
        {
            lock (DataFileLock)
            {
                // open the file
                this.dataFile = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                DBHdr dbRoot = this.ReadDBRoot();
                if (null == dbRoot) 
                {
                    // setup the data file
                    this.InitializeDataFile();
                }

                // flush the file
                this.dataFile.Flush();
                //this.dataFile.Close();
            }
        }

        protected StorageContext GetStorageContext(Transaction context, bool remove)
        {
            StorageContext storageContext = null;
            if (this.contextMap.TryGetValue(context, out storageContext))
            {
                if (remove)
                {
                    this.contextMap.Remove(context);
                }
                return storageContext;
            }

            lock (dataFile)
            {
                // read the DBRoot record
                DBHdr dbRoot = ReadDBRoot();
                if (null == dbRoot)
                {
                    throw new Exception();
                }

                // read in the page table
                storageContext = new StorageContext();
                storageContext.PageTable = new StoragePageTable();
                storageContext.PageTable.ReadPageTableData(this.dataFile, dbRoot.PageTable);

                // read in the resource index
                storageContext.ResourceIndex = new StorageResourceIndex();
                storageContext.ResourceIndex.ReadIndexData(this.dataFile, dbRoot.ResourceIndex);

                // read the free page list
                storageContext.FreePageList = new StorageFreeSpaceManager();
            }

            this.contextMap.Add(context, storageContext);
            return storageContext;
        }

        #endregion

        #region Private Methods

        private void InitializeDataFile()
        {
            DBHdr dbRoot = new DBHdr();
            WriteDBRoot(dbRoot);

            // create the free space manager
            StorageFreeSpaceManager spaceMgr = new StorageFreeSpaceManager();

            // create the page table
            StoragePageTable pageTable = new StoragePageTable();
            dbRoot.PageTable = pageTable.WritePageTableData(this.dataFile, spaceMgr);

            // create resource index
            StorageResourceIndex resourceIndx = new StorageResourceIndex();
            dbRoot.ResourceIndex = resourceIndx.WriteIndexData(this.dataFile, spaceMgr);

            // write the dbRoot one more time
            WriteDBRoot(dbRoot);
        }        

        private DBHdr ReadDBRoot()
        {
            DBHdr dbRoot = null;
            try
            {
                StoragePage rootPage = new StoragePage();
                rootPage.ReadPageData(this.dataFile, RootPage);
                dbRoot = (DBHdr)rootPage.ReadRecord(0);
            }
            catch (Exception e)
            {
                dbRoot = null;
            }

            return dbRoot;
        }

        private void WriteDBRoot(DBHdr dbRoot)
        {
            StoragePage rootPage = new StoragePage();
            rootPage.AddRecord(dbRoot);
            rootPage.WritePageData(this.dataFile, RootPage);
        }

        #endregion
    }
}
