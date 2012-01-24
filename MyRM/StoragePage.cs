﻿namespace MyRM
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class StoragePage
    {

        #region Constants

        /// <summary>
        /// We will use a 4 kB size page
        /// </summary>
        private const int PageSize = 4 * 1024;

        private static readonly Encoding RecordDataEncoder = Encoding.Unicode;

        #endregion

        #region Private Member Variables

        /// <summary>
        /// List of records stored on the page
        /// </summary>
        List<byte[]> recordList;

        #endregion

        #region Public Methods

        public StoragePage()
        {
            recordList = new List<byte[]>();
        }

        public int AddRecord(string data)
        {
            byte[] record = RecordDataEncoder.GetBytes(data);
            if (0 > this.GetAvailableSpace() - this.GetRecordSize(record))
            {
                throw new InsuffcientSpaceException();
            }

            // lets find a space to insert this data
            int index = this.recordList.IndexOf(null);
            if (-1 == index)
            {
                index = this.recordList.Count;
            }

            // add the data
            this.recordList.Insert(index, record);

            // return to index
            return this.recordList.IndexOf(record);
        }

        public void DeleteRecord(int recordIdx)
        {
            if (recordIdx >= this.recordList.Count
                || 0 > recordIdx)
            {
                throw new InvalidRecordException();
            }

            this.recordList[recordIdx] = null;
        }

        public void WriteRecord(int recordIdx, string data)
        {
            if (recordIdx >= this.recordList.Count
                || 0 > recordIdx)
            {
                throw new InvalidRecordException();
            }

            // encode the data
            byte[] record = RecordDataEncoder.GetBytes(data);
            if (0 > this.GetAvailableSpace() + this.GetRecordSize(recordIdx) - this.GetRecordSize(record))
            {
                throw new InsuffcientSpaceException();
            }

            this.recordList[recordIdx] = record;
        }

        public string ReadRecord(int recordIdx)
        {
            if (recordIdx >= this.recordList.Count
                || 0 > recordIdx)
            {
                throw new InvalidRecordException();
            }

            string data = RecordDataEncoder.GetString(
                this.recordList[recordIdx]);

            return data;
        }

        public void WritePageData(FileStream stream, int pageIdx)
        {
            // get the page data
            byte[] dataBuffer = new byte[PageSize];
            this.WritePageData(dataBuffer);

            // write to the file stream
            stream.Seek(pageIdx * PageSize, SeekOrigin.Begin);
            stream.Write(dataBuffer, 0, dataBuffer.Length);
        }

        public void ReadPageData(FileStream stream, int pageIdx)
        {
            byte[] dataBuffer = new byte[PageSize];

            // read from the file stream
            stream.Seek(pageIdx * PageSize, SeekOrigin.Begin);
            stream.Read(dataBuffer, 0, dataBuffer.Length);

            // initialize members
            this.recordList = new List<byte[]>();
            this.ReadPageData(dataBuffer);
        }

        #endregion

        #region Private Methods

        private int GetAvailableSpace()
        {
            int usedSpace = 0;

            // record count indicator
            usedSpace += sizeof(int);

            // record index
            // the +1 accounts for the space that would be used for the index entry
            // of a new record
            usedSpace += sizeof(int) * (this.recordList.Count + 1);

            // space used by each record
            foreach (byte[] record in this.recordList)
            {
                usedSpace += this.GetRecordSize(record);
            }

            return (PageSize - usedSpace);
        }

        private int GetRecordSize(byte[] record)
        {
            return (sizeof(byte) * record.Length);
        }

        private int GetRecordSize(int recordIdx)
        {
            return this.GetRecordSize(this.recordList[recordIdx]);
        }

        /// <summary>
        /// Binary Format:
        /// |record count|record(0) size|record(0) data|record(1) size|record(1) data|...
        /// |record(n) size|record(n) data|
        /// </summary>
        /// <param name="buffer"></param>
        private void WritePageData(byte[] buffer)
        {
            BinaryWriter pageWriter = new BinaryWriter(
                                        new MemoryStream(buffer));

            // set the write cursor to the start of the stream
            pageWriter.Seek(0, SeekOrigin.Begin);

            // write the data
            pageWriter.Write(this.recordList.Count);
            foreach (byte[] record in this.recordList)
            {
                pageWriter.Write(null == record ? 0 : record.Length);
                if (null != record
                    && 0 < record.Length)
                {
                    pageWriter.Write(record, 0, record.Length);
                }
            }
        }

        /// <summary>
        /// Binary Format:
        /// |record count|record(0) size|record(0) data|record(1) size|record(1) data|...
        /// |record(n) size|record(n) data| 
        /// </summary>
        /// <param name="buffer"></param>
        private void ReadPageData(byte[] buffer)
        {
            BinaryReader pageReader = new BinaryReader(
                                        new MemoryStream(buffer));

            // set the read cursor to the start of the stream
            pageReader.BaseStream.Seek(0, SeekOrigin.Begin);

            // read the data
            int recordCount = pageReader.ReadInt32();
            for (int idx = 0; idx < recordCount; idx++)
            {
                byte[] record = null;

                int recordSize = pageReader.ReadInt32();
                if (0 < recordSize)
                {
                    pageReader.ReadBytes(recordSize);
                }

                this.recordList.Add(record);
            }
        }

        #endregion

        #region Exception Classes

        public class InvalidRecordException : System.Exception
        {
            public InvalidRecordException()
                : base("Record does not exist in page.")
            {
            }

            public InvalidRecordException(string message)
                : base(message)
            {
            }

            public InvalidRecordException(string message, System.Exception e)
                : base(message, e)
            {
            }
        }

        public class InsuffcientSpaceException : System.Exception
        {
            public InsuffcientSpaceException()
                : base("The page does not have sufficent space to store the data.")
            {
            }

            public InsuffcientSpaceException(string message)
                : base(message)
            {
            }

            public InsuffcientSpaceException(string message, System.Exception e)
                : base(message, e)
            {
            }
        }

        #endregion
    }
}
