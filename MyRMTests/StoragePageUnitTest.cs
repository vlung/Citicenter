
namespace MyRMTests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MyRM;


    /// <summary>
    /// Summary description for StoragePageUnitTest
    /// </summary>
    [TestClass]
    public class StoragePageUnitTest
    {
        #region Constants

        private const string DataFileName = "TestDataFile.txt";

        #endregion

        public StoragePageUnitTest()
        {
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
        [ClassInitialize]
        public static void StoragePageUnitTestInitialize(TestContext testContext)
        {
            // clean up the data file
            string fullDataFilePath = Path.Combine(testContext.TestDir, DataFileName);
            if (File.Exists(fullDataFilePath))
            {
                File.Delete(fullDataFilePath);
            }
        }

        #endregion

        struct TestDataAddRecord
        {
            public string data;
            public int result;
        }

        [TestMethod]
        public void TestAddRecord()
        {
            TestDataAddRecord[] testDataList = 
            {
                new TestDataAddRecord{ data = "test data  1",   result = 0},
                new TestDataAddRecord{ data = "test data  2",   result = 1},
            };


            StoragePage page = new StoragePage();
            foreach (TestDataAddRecord testData in testDataList)
            {
                int result = page.AddRecord(testData.data);
                Assert.AreEqual(testData.result, result, 
                    string.Format("Return value did not match. Test Data=[{0}]", testData.data));
            }
            
        }

        struct TestDataDeleteRecord
        {
            public int recordIdx;
            public string exception;
        }

        [TestMethod]
        public void TestDeleteRecord()
        {
            TestDataDeleteRecord[] testDataList = 
            {
                new TestDataDeleteRecord{ recordIdx = -1,   exception = "InvalidRecordException"},
                new TestDataDeleteRecord{ recordIdx = 2,    exception = "InvalidRecordException"},
                new TestDataDeleteRecord{ recordIdx = 0,    exception = ""},
                new TestDataDeleteRecord{ recordIdx = 0,    exception = "InvalidRecordException"},
            };


            StoragePage page = new StoragePage();
            Assert.AreEqual(0, page.AddRecord("test data record"), "Adding sample record failed!");

            foreach (TestDataDeleteRecord testData in testDataList)
            {
                try
                {
                    page.DeleteRecord(testData.recordIdx);
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testData.exception, e.GetType().Name,
                        string.Format("Remove Record = [{0}]", testData.recordIdx));
                }
            }

        }
    }
}
