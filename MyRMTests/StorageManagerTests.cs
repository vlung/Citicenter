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

            // TEST write
            Resource[] data = 
            {
                new Resource(new RID(RID.Type.CAR, "Seattle"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Boston"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "San Diego"), 10, 45)
                , new Resource(new RID(RID.Type.FLIGHT, "New York"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Dallas"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Houston"), 10, 45)
                , new Resource(new RID(RID.Type.ROOM, "Toronto"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Montreal"), 10, 45)
                , new Resource(new RID(RID.Type.FLIGHT, "Vancouver"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Ottawa"), 10, 45)
                , new Resource(new RID(RID.Type.ROOM, "Portland"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "New Jersey"), 10, 45)
                , new Resource(new RID(RID.Type.ROOM, "Salt Lake City"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Paris"), 10, 45)
            };

            // write the data
            WriteResources(null, mgr, data, false);          

            // read the data in a new transaction
            ReadResources(null, mgr, data);
            
            // TEST write abort
            Resource[] data2 = 
            {
                new Resource(data[0].Id, 11, 21)
                , new Resource(data[1].Id, 12, 22)
            };

            // write the data
            WriteResources(null, mgr, data2, true);

            // read the data in a new transaction
            ReadResources(null, mgr, data);

            // TEST update
            Resource[] data3 = data;
            data3[0].setCount(data2[0].getCount());
            data3[0].setPrice(data2[0].getPrice());
            data3[1].setCount(data2[1].getCount());
            data3[1].setPrice(data2[1].getPrice());

            // write the data
            WriteResources(null, mgr, data2, false);

            // read the data in a new transaction
            ReadResources(null, mgr, data3);

            // TEST read non existing data
            Resource missingItem = null;
            Transaction context2 = new Transaction();
            if (mgr.Read(context2, new RID(RID.Type.FLIGHT, "DOES NOT EXIST"), out missingItem)
                || null != missingItem)
            {
                Assert.Fail("Test read of missing item failed.");
            }
            
            // TEST read resource list
            List<Resource> carList = null;
            if (!mgr.Read(context2, RID.Type.CAR, out carList))
            {
                Assert.Fail("Test read of car list failed.");
            }
            foreach(var car in data3.Where(c => c.Id.getType() == RID.Type.CAR))
            {
                if (!carList.Contains(car))
                {
                    Assert.Fail("{0}: Read resource list failed to retrieve [{1}].", context2.Id.ToString(), car);
                }
            }

            mgr.Prepare(context2);
            mgr.Commit(context2);
        }

        [TestMethod]
        public void TestDeleteResource()
        {
            string dataFile = "TestData22.tpdb";
            if (File.Exists(dataFile))
            {
                File.Delete(dataFile);
            }

            // cerate the storage manager
            StorageManager mgr = StorageManager.CreateObject(dataFile);

            // write some data
            Resource[] data = 
            {
                new Resource(new RID(RID.Type.CAR, "Seattle"), 10, 45)
                , new Resource(new RID(RID.Type.CAR, "Boston"), 10, 45)
            };

            // write the data
            WriteResources(null, mgr, data, false);

            // DELETE Item 2
            Transaction context2 = new Transaction();
            if (!mgr.Write(context2, data[1].Id, null))
            {
                Assert.Fail("Test delete item failed.");
            }

            Resource missingItem = null;
            if (mgr.Read(context2, data[1].Id, out missingItem)
                || null != missingItem)
            {
                Assert.Fail("Test read of missing item failed.");
            }

            // try to delete item again
            if (!mgr.Write(context2, data[1].Id, null))
            {
                Assert.Fail("Test delete item failed.");
            }
            mgr.Prepare(context2);
            mgr.Commit(context2);

            // read all
            Resource[] data2 =
            {
                data[0]
            };

            ReadResources(null, mgr, data2);
        }

        [TestMethod]
        public void TestReadWriteReservations()
        {
            string dataFile = "TestData3.tpdb";
            if (File.Exists(dataFile))
            {
                File.Delete(dataFile);
            }

            // cerate the storage manager
            StorageManager mgr = StorageManager.CreateObject(dataFile);

            // TEST write
            Reservation[] data = 
            {
                new Reservation(new Customer(), new RID[]{ new RID(RID.Type.FLIGHT, "1234"), new RID(RID.Type.CAR, "Boston"), new RID(RID.Type.ROOM, "Boston") } )
                , new Reservation(new Customer(), new RID[]{ new RID(RID.Type.FLIGHT, "1234"), new RID(RID.Type.CAR, "Miami"), new RID(RID.Type.ROOM, "New York") } )
                , new Reservation(new Customer(), new RID[]{ new RID(RID.Type.FLIGHT, "2345"), new RID(RID.Type.CAR, "Seattle"), new RID(RID.Type.ROOM, "Portland") } )
                , new Reservation(new Customer(), new RID[]{ new RID(RID.Type.FLIGHT, "3456"), new RID(RID.Type.CAR, "Seattle"), new RID(RID.Type.ROOM, "Boston") } )
                , new Reservation(new Customer(), new RID[]{ new RID(RID.Type.FLIGHT, "4567"), new RID(RID.Type.CAR, "Miami"), new RID(RID.Type.ROOM, "Boston") } )
            };

            // write the data
            WriteReservations(null, mgr, data, false);

            // read the data in a new transaction
            ReadReservations(null, mgr, data);

            // TEST write abort
            Reservation[] data2 = 
            {
                new Reservation(data[0].Id, new RID[]{ new RID(RID.Type.FLIGHT, "6767"), new RID(RID.Type.CAR, "Boston"), new RID(RID.Type.ROOM, "Dallas") } )
                , new Reservation(data[1].Id, new RID[]{ new RID(RID.Type.FLIGHT, "9933"), new RID(RID.Type.CAR, "Chicago"), new RID(RID.Type.ROOM, "New York") } )
            };

            // write the data
            WriteReservations(null, mgr, data2, true);

            // read the data in a new transaction
            ReadReservations(null, mgr, data);

            // TEST update
            Reservation[] data3 = data;
            data3[0].Resources = data2[0].Resources;
            data3[1].Resources = data2[1].Resources;

            // write the data
            WriteReservations(null, mgr, data2, false);

            // read the data in a new transaction
            ReadReservations(null, mgr, data3);

            // TEST read non existing data
            Transaction context2 = new Transaction();
            Resource missingItem = null;
            if (mgr.Read(context2, new RID(RID.Type.FLIGHT, "DOES NOT EXIST"), out missingItem)
                || null != missingItem)
            {
                Assert.Fail("Test read of missing item failed.");
            }

            // TEST read resource list
            List<Customer> customerList = null;
            if (!mgr.Read(context2, out customerList))
            {
                Assert.Fail("Test read of customer list failed.");
            }
            foreach (var customer in data3.Select(c => c.Id))
            {
                if (!customerList.Contains(customer))
                {
                    Assert.Fail("{0}: Read resource list failed to retrieve [{1}].", context2.Id.ToString(), customer);
                }
            }

            mgr.Prepare(context2);
            mgr.Commit(context2);
        }

        [TestMethod]
        public void TestDeleteReservation()
        {
            string dataFile = "TestData32.tpdb";
            if (File.Exists(dataFile))
            {
                File.Delete(dataFile);
            }

            // cerate the storage manager
            StorageManager mgr = StorageManager.CreateObject(dataFile);

            // write some data
            Reservation[] data = 
            {
                new Reservation(new Customer(), new RID[]{ new RID(RID.Type.FLIGHT, "1234"), new RID(RID.Type.CAR, "Boston"), new RID(RID.Type.ROOM, "Boston") } )
                , new Reservation(new Customer(), new RID[]{ new RID(RID.Type.FLIGHT, "1234"), new RID(RID.Type.CAR, "Miami"), new RID(RID.Type.ROOM, "New York") } )
            };

            // write the data
            WriteReservations(null, mgr, data, false);

            // DELETE Item 2
            Transaction context2 = new Transaction();
            if (!mgr.Write(context2, data[1].Id, null))
            {
                Assert.Fail("Test delete item failed.");
            }

            Reservation missingItem = null;
            if (mgr.Read(context2, data[1].Id, out missingItem)
                || null != missingItem)
            {
                Assert.Fail("Test read of missing item failed.");
            }

            // try to delete item a second time
            if (!mgr.Write(context2, data[1].Id, null))
            {
                Assert.Fail("Test delete item failed.");
            }
            mgr.Prepare(context2);
            mgr.Commit(context2);

            // read all
            Reservation[] data2 =
            {
                data[0]
            };

            ReadReservations(null, mgr, data2);
        }

        #region Private Helper Methods

        private static void ReadResources(Transaction context, StorageManager storage, Resource[] data)
        {
            bool createTransaction = (null == context);
            if (createTransaction)
            {
                context = new Transaction();
            }

            foreach (var item in data)
            {
                Resource output = null;
                if (!storage.Read(context, item.Id, out output))
                {
                    Assert.Fail("{0}: Read of [{1}] was un-successful.", context.Id.ToString(), item.Id.ToString());
                }
                Assert.AreEqual<Resource>(item, output, "{0}: Read was un-successful.", context.Id.ToString());
            }

            if (createTransaction)
            {
                storage.Prepare(context);
                storage.Commit(context);
            }
        }

        private static void ReadReservations(Transaction context, StorageManager storage, Reservation[] data)
        {
            bool createTransaction = (null == context);
            if (createTransaction)
            {
                context = new Transaction();
            }

            foreach (var item in data)
            {
                Reservation output = null;
                if (!storage.Read(context, item.Id, out output))
                {
                    Assert.Fail("{0}: Read of [{1}] was un-successful.", context.Id.ToString(), item.Id.ToString());
                }
                Assert.AreEqual<Reservation>(item, output, "{0}: Read was un-successful.", context.Id.ToString());
            }

            if (createTransaction)
            {
                storage.Prepare(context);
                storage.Commit(context);
            }
        }

        private static void WriteResources(Transaction context, StorageManager storage, Resource[] data, bool abort)
        {
            bool createTransaction = (null == context);
            if (createTransaction)
            {
                context = new Transaction();
            }

            // write the data
            foreach (var item in data)
            {
                storage.Write(context, item.Id, item);
            }

            // read the data in the same transaction
            ReadResources(context, storage, data);

            if (createTransaction
                && !abort)
            {
                storage.Prepare(context);
                storage.Commit(context);
            }
            else if (createTransaction
                && abort)
            {
                storage.Abort(context);
            }
        }

        private static void WriteReservations(Transaction context, StorageManager storage, Reservation[] data, bool abort)
        {
            bool createTransaction = (null == context);
            if (createTransaction)
            {
                context = new Transaction();
            }

            // write the data
            foreach (var item in data)
            {
                storage.Write(context, item.Id, item);
            }

            // read the data in the same transaction
            ReadReservations(context, storage, data);

            if (createTransaction
                && !abort)
            {
                storage.Prepare(context);
                storage.Commit(context);
            }
            else if (createTransaction
                && abort)
            {
                storage.Abort(context);
            }
        }

        #endregion
    }
}
