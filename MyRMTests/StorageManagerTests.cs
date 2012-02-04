namespace MyRMTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MyRM;
    using TP;

    /// <summary>
    /// Summary description for StoragePageTableTests
    /// </summary>
    [TestClass]
    public class StorageManagerTests
    {
        public StorageManagerTests()
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
        public void TestInitializeDataFile()
        {
            string dataFile = "TestData1.tpdb";
            if (File.Exists(dataFile))
            {
                File.Delete(dataFile);
            }

            // cerate the storage manager
            StorageManager mgr = StorageManager.CreateObject(dataFile);
        }

        [TestMethod]
        public void TestReadWriteResource()
        {
            string dataFile = "TestData2.tpdb";
            if (File.Exists(dataFile))
            {
                File.Delete(dataFile);
            }

            // cerate the storage manager
            StorageManager mgr = StorageManager.CreateObject(dataFile);

            Resource[] data = 
            {
                new Resource(new RID(RID.Type.CAR, "Seattle"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Boston"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "San Diego"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "New York"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Dallas"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Houston"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Toronto"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Montreal"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Vancouver"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Ottawa"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Portland"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "New Jersey"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Salt Lake City"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Paris"), 10, 45)
            };

            // write the data
            Transaction context1 = new Transaction();
            foreach (var item in data)
            {
                mgr.Write(context1, item);
            }

            // read the data in the same transaction
            foreach (var item in data)
            {
                Resource output = null;
                if (!mgr.Read(context1, item.getID(), out output))
                {
                    Assert.Fail("Same transaction: Read of [{0}] was un-successful.", item.ToString());
                }
                Assert.AreEqual<Resource>(item, output, "Same transaction: Read was un-successful.");
            }
            mgr.Commit(context1);

            // read the data in a new transaction
            Transaction context2 = new Transaction();
            foreach (var item in data)
            {
                Resource output = null;
                if (!mgr.Read(context2, item.getID(), out output))
                {
                    Assert.Fail("Different transaction: Read of [{0}] was un-successful.", item.ToString());
                }
                Assert.AreEqual<Resource>(item, output, "Different transaction: Read was un-successful.");
            }
            mgr.Commit(context2);

            // read non existing data
            Resource missingItem = null;
            if (mgr.Read(context2, new RID(RID.Type.FLIGHT, "DOES NOT EXIST"), out missingItem)
                || null != missingItem)
            {
                Assert.Fail("Test read of missing item failed.");
            }
            
            // test abort
            Resource[] data2 = 
            {
                new Resource(new RID(RID.Type.CAR, "Seattle"), 10, 43)
                , new Resource(new RID(RID.Type.CAR, "Boston"), 10, 43)
                , new Resource(new RID(RID.Type.CAR, "San Diego"), 10, 40)
                , new Resource(new RID(RID.Type.CAR, "New York"), 10, 47)
                , new Resource(new RID(RID.Type.CAR, "Montreal"), 10, 33)
                , new Resource(new RID(RID.Type.CAR, "Vancouver"), 10, 55)
            };

            // write the data
            Transaction context3 = new Transaction();
            foreach (var item in data2)
            {
                mgr.Write(context3, item);
            }
            mgr.Abort(context3);

            // read the data in a new transaction
            Transaction context4 = new Transaction();
            foreach (var item in data)
            {
                Resource output = null;
                if (!mgr.Read(context4, item.getID(), out output))
                {
                    Assert.Fail("Different transaction: Read of [{0}] was un-successful.", item.ToString());
                }
                Assert.AreEqual<Resource>(item, output, "Different transaction: Read was un-successful.");
            }
            mgr.Commit(context4);
        }
    }
}
