
namespace MyRMTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using MyRM;
    using MyRM.DS;

    /// <summary>
    /// Summary description for StoragePageUnitTest
    /// </summary>
    [TestClass]
    public class StoragePageTests
    {
        #region Constants

        private const string DataFileName = "TestDataFile.txt";

        #endregion

        public StoragePageTests()
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

        [TestMethod]
        public void SP_TestAddRecord()
        {
            TestData[] testList = 
            {
                new TestData{ data = "test data  1",   recordIdx = 0},
                new TestData{ data = "test data  2",   recordIdx = 1},
            };

            StoragePage page = new StoragePage();
            AddRecords(page, testList);

            // test adding a record when page is full
            string masterRecord = "1234567890";
            int maxCount = 
                (StoragePage_Accessor.PageSize - sizeof(int))
                / (sizeof(int) + StoragePage_Accessor.Serialize(masterRecord).Length);

            List<TestData> noSpacetestList = new List<TestData>();
            for(int idx = 0; idx < maxCount; idx++)
            {
                TestData record = new TestData
                {
                    data = masterRecord,
                    recordIdx = idx,
                };
                noSpacetestList.Add(record);
            }
            noSpacetestList.Add(new TestData
            {
                data = masterRecord,
                exception = "InsuffcientSpaceException",
            });

            StoragePage page2 = new StoragePage();
            AddRecords(page2, noSpacetestList.ToArray());
        }

        [TestMethod]
        public void SP_TestDeleteRecord()
        {
            TestData[] addList = 
            {
                new TestData{ data = "Record0", recordIdx = 0 }
            };

            TestData[] deleteTestList = 
            {
                new TestData{ recordIdx = -1,   exception = "InvalidRecordException"},
                new TestData{ recordIdx = 2,    exception = "InvalidRecordException"},
                new TestData{ recordIdx = 0,    exception = ""},
                new TestData{ recordIdx = 0,    exception = "InvalidRecordException"},
            };

            StoragePage page = new StoragePage();
            AddRecords(page, addList);
            DeleteRecords(page, deleteTestList);
        }

        [TestMethod]
        public void SP_TestReadWriteRecord()
        {
            TestData[] addList = 
            {
                new TestData{data = "Record0", recordIdx = 0},
                new TestData{data = "Record1", recordIdx = 1},
            };
            TestData[] readAddList = addList;

            TestData[] readWriteTestList =
            {
                new TestData{data = "Record-1",  recordIdx = -1,                exception = "InvalidRecordException"},
                new TestData{data = "RecordMax", recordIdx = int.MaxValue,      exception = "InvalidRecordException"},
                new TestData{data = "RecordLen", recordIdx = addList.Length,    exception = "InvalidRecordException"},
                new TestData{data = "Record0_2", recordIdx = addList[0].recordIdx},
                new TestData{data = "Record1_2", recordIdx = addList[1].recordIdx},
                
            };

            TestData[] readWriteList = readWriteTestList;

            StoragePage page = new StoragePage();
            
            // setup the page by adding some records and
            // verify by reading the records back
            AddRecords(page, addList);
            ReadRecords(page, readAddList);

            // execute writes
            WriteRecords(page, readWriteTestList);
            ReadRecords(page, readWriteList);
        }

        [TestMethod]
        public void SP_TestReadWritePage()
        {
            string dataFile = "SP_TestData1.tpdb";
            if (File.Exists(dataFile))
            {
                File.Delete(dataFile);
            }

            TestData[] pageData =
            {
                new TestData{data = "Record_0", recordIdx = 0},
                new TestData{data = "Record_1", recordIdx = 1},
                new TestData{data = "Record_2", recordIdx = 2}
            };

            int pageIndex = int.MinValue;

            // write the page
            using (FileStreamWrapper dataFileStream = FileStreamWrapper.CreateObject(dataFile))
            {
                // populate the page with some data
                StoragePage page = new StoragePage();
                AddRecords(page, pageData);

                // write the file to disk
                pageIndex = page.WritePageData(dataFileStream, -1);                
            }

            // read the page
            using (FileStreamWrapper dataFileStream = FileStreamWrapper.CreateObject(dataFile))
            {
                // read page from file
                StoragePage page = new StoragePage();
                page.ReadPageData(dataFileStream, pageIndex);

                // validate the page data
                ReadRecords(page, pageData);
            }
        }

        #region Private Helper Methods

        private struct TestData
        {
            public string data;
            public int recordIdx;

            public string exception;

            public override string ToString()
            {
                return string.Format(
                    "{0}|{1}|{2}", data, recordIdx, exception);
            }
        }

        private void AddRecords(StoragePage page, TestData[] data)
        {
            foreach (TestData test in data)
            {
                try
                {
                    int result = page.AddRecord(test.data);
                    Assert.AreEqual(test.recordIdx, result,
                        string.Format("Return value did not match. Test Data=[{0}]", test.ToString()));
                }
                catch (Exception e)
                {
                    Assert.AreEqual(test.exception, e.GetType().Name,
                        "Unexpected exception. Test Data = [{0}]", test.ToString());
                }
            }
        }

        private void DeleteRecords(StoragePage page, TestData[] data)
        {
            foreach (TestData test in data)
            {
                try
                {
                    page.DeleteRecord(test.recordIdx);
                }
                catch (Exception e)
                {
                    Assert.AreEqual(test.exception, e.GetType().Name,
                        "Unexpected exception. Test Data = [{0}]", test.ToString());
                }
            }
        }

        private void WriteRecords(StoragePage page, TestData[] data)
        {
            foreach (TestData test in data)
            {
                try
                {
                    page.WriteRecord(test.recordIdx, test.data);
                }
                catch (Exception e)
                {
                    Assert.AreEqual(test.exception, e.GetType().Name,
                        "Unexpected exception. Test Data = [{0}]", test.ToString());
                }
            }
        }

        private void ReadRecords(StoragePage page, TestData[] data)
        {
            foreach (TestData test in data)
            {
                try
                {
                    string result = (string)page.ReadRecord(test.recordIdx);
                    Assert.AreEqual(test.data, result,
                        "Read record did not match. Test Data = [{0}]", test.ToString());
                }
                catch (Exception e)
                {
                    Assert.AreEqual(test.exception, e.GetType().Name,
                        "Unexpected exception. Test Data = [{0}]", test.ToString());
                }
            }
        }

        #endregion
    }
}
