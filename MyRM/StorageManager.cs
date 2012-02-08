namespace MyRM
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using DS;
    using TP;

    public class StorageManager : IDisposable
    {
        #region Constants

        private const int RootPage = 0;

        private const string PageLockPrefix = "P";
        private const string ResourceLockPrefix = "RSC";
        private const string ReservationLockPrefix = "RSV";

        #endregion

        #region Private Members

        // cs for entire class
        private static object ManagerLock = new object();

        // storage members
        private FileStream dataFile;
        private Dictionary<Transaction, StorageContext> contextMap;
        public StoragePageManager pageManager;

        // lock manager
        private MyLM lockManager;

        // IDisposible
        private bool disposed;

        #endregion

        #region Public Methods

        public static StorageManager CreateObject(string filePath)
        {
            StorageManager obj = new StorageManager();
            obj.Init(filePath);

            return obj;
        }

        #region IDisposible

        ~StorageManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public void Abort(Transaction context)
        {
            aAbort(context);

            // unlock all
            this.lockManager.UnlockAll(context);
        }

        public void Commit(Transaction context)
        {
            aCommit(context);

            // unlock all
            this.lockManager.UnlockAll(context);
        }        

        /// <summary>
        /// Gets the list of all customers.
        /// 
        /// No locking needed since we are only enforcing "Read commited" 
        /// degree of isolation for this query transaction.
        /// We get that for free here since we only access the reservation
        /// index which is read atomically.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <returns>true if successful, false otherwise</returns>
        public bool Read(Transaction context, out List<Customer> data)
        {
            StorageContext storageContext = this.aGetStorageContext(context);
            if (null == storageContext)
            {
                throw new Exception();
            }

            data = storageContext
                    .ReservationIndex
                        .GetIdList()
                            .ToList();

            return true;
        }

        /// <summary>
        /// Gets the list of all resources of a certain type.
        /// 
        /// We will provide "read commited" isolation here, buy locking the record during the read 
        /// and then unlocking it as soon as we are done with the read.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Read(Transaction context, RID.Type rType, out List<Resource> data)
        {
            StorageContext storageContext = this.aGetStorageContext(context);
            if (null == storageContext)
            {
                throw new Exception();
            }

            data = new List<Resource>();
            foreach (var id in storageContext.ResourceIndex.GetIdList())
            {
                if (id.getType() != rType)
                {
                    continue;
                }

                // lock the resource
                this.LockResource(context, MyLM.LockMode.Read, id);

                Resource resource = null;
                if (!Read<RID, Resource>(
                        context, storageContext, storageContext.ResourceIndex, id, false, out resource))
                {
                    continue;
                }

                // unlock the resource
                this.UnLockResource(context, MyLM.LockMode.Read, id);

                data.Add(resource);
            }
            
            return true;
        }

        public bool Read(Transaction context, RID rId, out Resource data)
        {
            // Aquire a read lock on the resource id to ensure that we will be 
            // able to merge the index, and that the resource is not moved
            this.LockResource(context, MyLM.LockMode.Read, rId);

            StorageContext storageContext = this.aGetStorageContext(context);
            if (null == storageContext)
            {
                throw new Exception();
            }

            return Read<RID, Resource>(
                context, storageContext, storageContext.ResourceIndex, rId, true, out data);
        }

        public bool Read(Transaction context, Customer rId, out Reservation data)
        {
            // Aquire a read lock on the reservation id to ensure that we will be 
            // able to merge the index, and that the record is not moved
            this.LockReservation(context, MyLM.LockMode.Read, rId);

            StorageContext storageContext = this.aGetStorageContext(context);
            if (null == storageContext)
            {
                throw new Exception();
            }

            return Read<Customer, Reservation>(
                context, storageContext, storageContext.ReservationIndex, rId, true, out data);
        }

        public bool Write(Transaction context, RID rId, Resource data)
        {
            // Aquire a read lock on the reservation id in case we need to move the
            // record, we are going to write lock the page later on so noone can
            // read or write this record anyways
            this.LockResource(context, MyLM.LockMode.Write, rId);

            StorageContext storageContext = this.aGetStorageContext(context);
            if (null == storageContext)
            {
                throw new Exception();
            }

            return Write<RID, Resource>(
                context, storageContext, storageContext.ResourceIndex, rId, data);
        }       

        public bool Write(Transaction context, Customer rId, Reservation data)
        {
            // Aquire a read lock on the reservation id in case we need to move the
            // record, we are going to write lock the page later on so noone can
            // read or write this record anyways
            this.LockReservation(context, MyLM.LockMode.Write, rId);

            StorageContext storageContext = this.aGetStorageContext(context);
            if (null == storageContext)
            {
                throw new Exception();
            }

            return Write<Customer, Reservation>(
                context, storageContext, storageContext.ReservationIndex, rId, data);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected StorageManager()
        {
            this.contextMap = new Dictionary<Transaction, StorageContext>();
            this.pageManager = new StoragePageManager();

            this.lockManager = new MyLM();

            this.disposed = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // close the data files
                this.dataFile.Dispose();
            }
        }

        protected virtual bool Read<I, R>(Transaction context, StorageContext storageContext, StorageIndex<I> index, I rID, bool lockPage , out R data)
        {
            // look for the resource in the index
            IndexItem<I> address = index.GetResourceAddress(rID);
            if (null == address)
            {
                data = default(R);
                return false;
            }

            if (lockPage)
            {
                // Aquire a lock on the logical page address to ensure that the page is not
                // being written while we read the data
                this.LockPage(context, MyLM.LockMode.Read, address.Page);
            }

            // find the physical page
            int fileAddress = storageContext.PageTable.GetPhysicalPage(address.Page);

            // get the page
            StoragePage page = new StoragePage();
            this.aReadPageData(page, fileAddress);

            // read the data
            data = (R)page.ReadRecord(address.Record);

            return true;
        }

        protected virtual bool Write<I, R>(Transaction context, StorageContext storageContext, StorageIndex<I> index, I rID, R data)
        {
            // look for the resource in the index
            IndexItem<I> address = index.GetResourceAddress(rID);
            if (null == address
                && null == data)
            {
                // nothing to do:
                // user probably wanted to delete an non-existing item
                return true;
            }
            else if (null == address
                     && null != data)
            {
                address = new IndexItem<I>
                {
                    Page = -1,
                    Record = -1
                };
            }

            // Aquire a lock on the logical page address to ensure that we have access to the page
            this.LockPage(context, MyLM.LockMode.Write, address.Page);

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
            if (null == data)
            {
                // handle deletes
                address = null;
            }
            index.SetResourceAddress(rID, address);

            return true;
        } 

        #endregion

        #region Private Methods

        #region Atomic Methods

        private void aAbort(Transaction context)
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
        }

        private void aCommit(Transaction context)
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

                // merge page table
                storageContext.PageTable.ReadPageTableData(
                    this.dataFile, dbRoot.PageTable);

                // merge resource index
                storageContext.ResourceIndex.ReadIndexData(
                    this.dataFile, dbRoot.ResourceIndex);

                // merge reservation index
                storageContext.ReservationIndex.ReadIndexData(
                    this.dataFile, dbRoot.ReservationIndex);

                // write the page table
                List<int> oldPageTablePages = null;
                dbRoot.PageTable = storageContext.PageTable.WritePageTableData(
                    this.dataFile, this.pageManager, out oldPageTablePages);

                // write the resource index
                List<int> oldResourceIndexPages = null;
                dbRoot.ResourceIndex = storageContext.ResourceIndex.WriteIndexData(
                    this.dataFile, this.pageManager, out oldResourceIndexPages);

                // write the reservation index
                List<int> oldReservationIndexPages = null;
                dbRoot.ReservationIndex = storageContext.ReservationIndex.WriteIndexData(
                    this.dataFile, this.pageManager, out oldReservationIndexPages);

                // update the page manager
                this.pageManager.SetFreePages(oldPageTablePages);
                this.pageManager.SetFreePages(oldResourceIndexPages);
                this.pageManager.SetFreePages(oldReservationIndexPages);
                this.pageManager.SetFreePages(storageContext.FreedPageList);
                dbRoot.PageManager = this.pageManager.WritePageManagerData(
                    this.dataFile);

                this.WriteDBRoot(dbRoot);
                this.dataFile.Flush(true);
            }
        }

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
                storageContext.PageTable.ReadPageTableData(
                    this.dataFile, dbRoot.PageTable);

                // read in the resource index
                storageContext.ResourceIndex.ReadIndexData(
                    this.dataFile, dbRoot.ResourceIndex);

                // read in the reservation index
                storageContext.ReservationIndex.ReadIndexData(
                    this.dataFile, dbRoot.ReservationIndex);

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
                    storageContext.FreedPageList.Add(fileAddress);
                }

                // write the page
                fileAddress = page.WritePageData(
                    this.dataFile, this.pageManager.GetFreePage(this.dataFile));

                // store the index of the page we just wrote to
                storageContext.AllocatedPageList.Add(fileAddress);

                return fileAddress;
            }
        }

        #endregion

        #region Initialization Helper Methods

        /// <summary>
        /// Remarks:
        /// No locking needed beause this method cannot be invoked on the object.
        /// It is only called during initialization before the object is returned 
        /// from the object factory method.
        /// </summary>
        /// <param name="filePath"></param>
        private void Init(string filePath)
        {
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

        private void InitializeDataFile()
        {
            // helpers
            List<int> oldPages = null;

            DBHdr dbRoot = new DBHdr();
            WriteDBRoot(dbRoot);

            // create the page table
            StoragePageTable pageTable = new StoragePageTable();
            dbRoot.PageTable = pageTable.WritePageTableData(
                this.dataFile, this.pageManager, out oldPages);

            // create resource index
            StorageIndex<RID> resourceIndx = new StorageIndex<RID>();
            dbRoot.ResourceIndex = resourceIndx.WriteIndexData(
                this.dataFile, this.pageManager, out oldPages);

            // create reservation index
            StorageIndex<Customer> reservationIndex = new StorageIndex<Customer>();
            dbRoot.ReservationIndex = reservationIndex.WriteIndexData(
                this.dataFile, this.pageManager, out oldPages);

            // write the page manager            
            dbRoot.PageManager = this.pageManager.WritePageManagerData(this.dataFile);

            // write the dbRoot one more time
            WriteDBRoot(dbRoot);
        }

        #endregion

        #region Lock Helper Methods

        private void LockPage(Transaction context, MyLM.LockMode mode, int page)
        {
            LockableID id = new LockableID(
                PageLockPrefix, new LockableID(page.ToString()));
            this.Lock(context, mode, id);
        }

        private void UnLockPage(Transaction context, MyLM.LockMode mode, int page)
        {
            LockableID id = new LockableID(
                PageLockPrefix, new LockableID(page.ToString()));
            this.UnLock(context, mode, id);
        }

        private void LockResource(Transaction context, MyLM.LockMode mode, RID rId)
        {
            LockableID id = new LockableID(
                ResourceLockPrefix, new LockableID(rId.ToString()));
            this.Lock(context, mode, id);
        }

        private void UnLockResource(Transaction context, MyLM.LockMode mode, RID rId)
        {
            LockableID id = new LockableID(
                ResourceLockPrefix, new LockableID(rId.ToString()));
            this.UnLock(context, mode, id);
        }

        private void LockReservation(Transaction context, MyLM.LockMode mode, Customer rId)
        {
            LockableID id = new LockableID(
                ReservationLockPrefix, new LockableID(rId.ToString()));
            this.UnLock(context, mode, id);
        }

        private void UnLockReservation(Transaction context, MyLM.LockMode mode, Customer rId)
        {
            LockableID id = new LockableID(
                ReservationLockPrefix, new LockableID(rId.ToString()));
            this.Lock(context, mode, id);
        }

        private void Lock(Transaction context, MyLM.LockMode mode, LockableID id)
        {
            switch (mode)
            {
                case MyLM.LockMode.Read:
                    {
                        this.lockManager
                                .LockForRead(context, id);
                    }
                    break;

                case MyLM.LockMode.Write:
                    {
                        this.lockManager
                                .LockForWrite(context, id);
                    }
                    break;
                default:
                    throw new Exception("Invalid lock mode requested");
            }
        }

        private void UnLock(Transaction context, MyLM.LockMode mode, LockableID id)
        {
            switch (mode)
            {
                case MyLM.LockMode.Read:
                    {
                        this.lockManager
                                .UnlockRead(context, id);
                    }
                    break;

                case MyLM.LockMode.Write:
                    {
                        this.lockManager
                                .UnlockWrite(context, id);
                    }
                    break;
                default:
                    throw new Exception("Invalid lock mode requested");
            }
        }

        #endregion

        #region DB Root Helper Methods

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

        #endregion
    }
}
