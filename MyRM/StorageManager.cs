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

        private static object ManagerLock = new object();

        private FileStream dataFile;
        private Dictionary<Transaction, StorageContext> contextMap;
        public StoragePageManager pageManager;

        #endregion

        #region Public Methods

        public static StorageManager CreateObject(string filePath)
        {
            StorageManager obj = new StorageManager();
            obj.aInit(filePath);

            return obj;
        }

        public void Commit(Transaction context)
        {
            lock(ManagerLock)
            {
                StorageContext storageContext = null;
                if (!this.contextMap.TryGetValue(context, out storageContext))
                {
                    // transaction must already have been commited or aborted
                    // nothing to do
                    return;
                }
                this.contextMap.Remove(context);

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
                List<int> oldPageTablePages = null;
                dbRoot.PageTable = storageContext.PageTable.WritePageTableData(
                    this.dataFile, this.pageManager, out oldPageTablePages);

                // write the resource index
                List<int> oldResourceIndexPages = null;
                dbRoot.ResourceIndex = storageContext.ResourceIndex.WriteIndexData(
                    this.dataFile, this.pageManager, out oldResourceIndexPages);

                // update the page manager
                this.pageManager.SetFreePages(oldPageTablePages);
                this.pageManager.SetFreePages(oldResourceIndexPages);
                this.pageManager.SetFreePages(storageContext.FreedPageList);
                dbRoot.PageManager = this.pageManager.WritePageManagerData(
                    this.dataFile);

                this.WriteDBRoot(dbRoot);
                this.dataFile.Flush(true);
            }

            // TODO: unlock
        }

        public void Abort(Transaction context)
        {
            lock (ManagerLock)
            {
                StorageContext storageContext = null;
                if (!this.contextMap.TryGetValue(context, out storageContext))
                {
                    // transaction must already have been commited or aborted
                    // nothing to do
                    return;
                }
                this.contextMap.Remove(context);

                DBHdr dbRoot = this.ReadDBRoot();
                if (null == dbRoot)
                {
                    throw new Exception();
                }

                // update the page manager
                this.pageManager.SetFreePages(storageContext.AllocatedPageList);
                dbRoot.PageManager = this.pageManager.WritePageManagerData(
                    this.dataFile);

                this.WriteDBRoot(dbRoot);
                this.dataFile.Flush(true);
            }

            // TODO: unlock
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
            StorageContext storageContext = this.aGetStorageContext(context);
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
            this.aReadPageData(page, fileAddress);

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
            StorageContext storageContext = this.aGetStorageContext(context);
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
                this.aReadPageData(page, fileAddress);
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
            fileAddress = this.aWritePageData(page, storageContext, fileAddress);

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

        #endregion

        #region Protected Methods

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected StorageManager()
        {
            this.contextMap = new Dictionary<Transaction, StorageContext>();
        }

        protected void aInit(string filePath)
        {
            lock (ManagerLock)
            {
                // create the empty page manager
                this.pageManager = new StoragePageManager();

                // open the file
                this.dataFile = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                DBHdr dbRoot = this.ReadDBRoot();
                if (null == dbRoot)
                {
                    // setup the data file
                    this.InitializeDataFile();

                    // flush the file
                    this.dataFile.Flush(true);
                }
                else
                {
                    // read the data manager
                    this.pageManager.ReadPageManagerData(
                        this.dataFile, dbRoot.PageManager);
                }
            }
        }

        #endregion

        #region Private Methods

        private StorageContext aGetStorageContext(Transaction context)
        {
            lock (dataFile)
            {
                // look for the context in the map
                StorageContext storageContext = null;
                if (this.contextMap.TryGetValue(context, out storageContext))
                {
                    return storageContext;
                }

                // create a brand new storage context
                // read the DBRoot record
                DBHdr dbRoot = ReadDBRoot();
                if (null == dbRoot)
                {
                    throw new Exception();
                }

                // create the storage context
                storageContext = new StorageContext();

                // read in the page table
                storageContext.PageTable.ReadPageTableData(this.dataFile, dbRoot.PageTable);

                // read in the resource index
                storageContext.ResourceIndex.ReadIndexData(this.dataFile, dbRoot.ResourceIndex);

                // insert the context into the map
                this.contextMap.Add(context, storageContext);

                return storageContext;
            }
        }

        private void aReadPageData(StoragePage page, int pageIndex)
        {
            lock(ManagerLock)
            {
                page.ReadPageData(this.dataFile, pageIndex);
            }
        }

        private int aWritePageData(StoragePage page, StorageContext storageContext, int fileAddress)
        {
            lock (ManagerLock)
            {
                // store the index of the current page
                if (0 <= fileAddress)
                {
                    storageContext.FreedPageList.Add(fileAddress); ;
                }

                // write the page
                fileAddress = page.WritePageData(
                    this.dataFile, this.pageManager.GetFreePage(this.dataFile));

                // store the index of the page we just wrote to
                storageContext.AllocatedPageList.Add(fileAddress);

                return fileAddress;
            }
        }

        private void InitializeDataFile()
        {
            // helpers
            List<int> oldPages = null;

            DBHdr dbRoot = new DBHdr();
            WriteDBRoot(dbRoot);

            // create the page table
            StoragePageTable pageTable = new StoragePageTable();
            dbRoot.PageTable = pageTable.WritePageTableData(this.dataFile, this.pageManager, out oldPages);

            // create resource index
            StorageResourceIndex resourceIndx = new StorageResourceIndex();
            dbRoot.ResourceIndex = resourceIndx.WriteIndexData(this.dataFile, this.pageManager, out oldPages);

            // write the page manager            
            dbRoot.PageManager = this.pageManager.WritePageManagerData(this.dataFile);

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
