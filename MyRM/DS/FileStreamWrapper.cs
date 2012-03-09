namespace MyRM.DS
{
    using System;
    using System.IO;

    public class FileStreamWrapper : IDisposable
    {
        #region Private Members

        private FileStream dataFile;

        // IDisposible
        private bool disposed;

        #endregion

        #region Public Methods

        public static FileStreamWrapper CreateObject(string file)
        {
            FileStreamWrapper obj = new FileStreamWrapper();
            obj.Init(file);
            return obj;
        }

        public long Length
        {
            get
            {
                return this.dataFile.Length;
            }
        }

        public int MaxDiskWrites
        {
            get;
            set;
        }

        public void Flush(bool flag)
        {
            this.dataFile.Flush(flag);
        }

        public int Read(byte[] array, int offset, int count)
        {
            return this.dataFile.Read(array, offset, count);
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            return this.dataFile.Seek(offset, origin);
        }

        public void Write(byte[] array, int offset, int count)
        {
            this.dataFile.Write(array, offset, count);
            this.TerminateRMProcess();
        }

        #region IDisposible

        ~FileStreamWrapper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #endregion

        #region Protected Methods

        protected FileStreamWrapper()
        {
            this.dataFile = null;
            this.MaxDiskWrites = 0;
            this.disposed = false;
        }

        protected virtual void Init(string file)
        {
            // open the file
            this.dataFile = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
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

        protected virtual void TerminateRMProcess()
        {
            // chek for self destruct
            if (0 == this.MaxDiskWrites)
            {
                // disabled
                return;
            }

            if (1 == this.MaxDiskWrites)
            {
                // kill the RM
                Environment.Exit(1);
            }

            // decrement the count
            this.MaxDiskWrites--;
        }

        #endregion
    }
}
