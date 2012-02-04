namespace MyRMTests
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System.IO;
    using MyRM;

    /// <summary>
    /// Summary description for StoragePageTableTests
    /// </summary>
    [TestClass]
    public class StoragePageTableTests
    {
        public StoragePageTableTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestReadWriteTable()
        {
            int entryCount = 40;
            int physicalPageDistance = 5;

            string dataFile = "TestData2.tpdb";
            if (File.Exists(dataFile))
            {
                File.Delete(dataFile);
            }

            List<int> freedPages = null;
            StoragePageManager spaceMgr = new StoragePageManager();
            StoragePageTable pageTable2 = new StoragePageTable();
            StoragePageTable pageTable = new StoragePageTable();
            for (int idx = 0; idx < entryCount; idx++)
            {
                pageTable.SetLogicalPage(idx + physicalPageDistance);
            }

            using (FileStream dataFileStream = File.Open(dataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                int root = pageTable.WritePageTableData(dataFileStream, spaceMgr, out freedPages);
                dataFileStream.Seek(0, SeekOrigin.Begin);
                pageTable2.ReadPageTableData(dataFileStream, root);
            }

            for (int idx = 0; idx < entryCount; idx++)
            {
                int physicalAddress = pageTable2.GetPhysicalPage(idx);
                Assert.AreEqual(idx + physicalPageDistance, physicalAddress);
            }
        }
    }
}
