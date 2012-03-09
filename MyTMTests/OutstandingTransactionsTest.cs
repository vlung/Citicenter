using MyTM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyTMTests
{
    /// <summary>
    ///This is a test class for OutstandingTransactionsTest and is intended
    ///to contain all OutstandingTransactionsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class OutstandingTransactionsTest
    {


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
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for SerializeStringToKeyValue
        ///</summary>
        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_OutstandingTransactions_SerializeStringToKeyValueTest1()
        {
            OutstandingTransactions_Accessor target = new OutstandingTransactions_Accessor();
            string s = "id,1,rm1,rm2";
            string key;
            string keyExpected = "id";
            OutstandingTransactions.OutstandingTransactionsValue value = null;
            OutstandingTransactions.OutstandingTransactionsValue valueExpected = new OutstandingTransactions.OutstandingTransactionsValue(
                OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Abort,
                new List<string> { "rm1", "rm2" });
            bool expected = true;
            bool actual;
            actual = target.SerializeFromString(s, out key, out value);
            Assert.AreEqual(keyExpected, key);
            Assert.AreEqual(valueExpected.transactionType, value.transactionType);
            Assert.IsTrue(valueExpected.nackRMList.Except(value.nackRMList).Count() == 0);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_OutstandingTransactions_SerializeStringToKeyValueTest2()
        {
            OutstandingTransactions_Accessor target = new OutstandingTransactions_Accessor();
            string s = "id,1";
            string key;
            string keyExpected = "id";
            OutstandingTransactions.OutstandingTransactionsValue value = null;
            OutstandingTransactions.OutstandingTransactionsValue valueExpected = new OutstandingTransactions.OutstandingTransactionsValue();
            bool expected = true;
            bool actual;
            actual = target.SerializeFromString(s, out key, out value);
            Assert.AreEqual(keyExpected, key);
            Assert.AreEqual(value.transactionType, OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Abort);
            Assert.IsTrue(valueExpected.nackRMList.Except(value.nackRMList).Count() == 0);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for SerializeKeyValueToString
        ///</summary>
        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_OutstandingTransactions_SerializeKeyValueToStringTest1()
        {
            OutstandingTransactions_Accessor target = new OutstandingTransactions_Accessor();
            string key = "id";
            OutstandingTransactions.OutstandingTransactionsValue value = new OutstandingTransactions.OutstandingTransactionsValue(
                OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Commit,
                new List<string> { "rm1", "rm2" });
            string expected = "id,0,rm1,rm2";
            string actual;
            actual = target.SerializeToString(key, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_OutstandingTransactions_SerializeKeyValueToStringTest2()
        {
            OutstandingTransactions_Accessor target = new OutstandingTransactions_Accessor();
            string key = "id";
            OutstandingTransactions.OutstandingTransactionsValue value = null;
            string expected = null;
            string actual;
            actual = target.SerializeToString(key, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_OutstandingTransactions_SerializeKeyValueToStringTest3()
        {
            OutstandingTransactions_Accessor target = new OutstandingTransactions_Accessor();
            string key = "id";
            OutstandingTransactions.OutstandingTransactionsValue value = null;
            string expected = "id";
            string actual;
            actual = target.SerializeToString(key, value, true);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_OutstandingTransactions_FuncTest1()
        {
            OutstandingTransactions_Accessor target = new OutstandingTransactions_Accessor();
            target.transactionList.Clear();
            target.WriteToFile(); // Write empty list to file to initialize state

            OutstandingTransactions.OutstandingTransactionsValue rmlist1 = new OutstandingTransactions.OutstandingTransactionsValue(
                OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Commit,
                new List<string> { "rm1", "rm2" });
            OutstandingTransactions.OutstandingTransactionsValue rmlist2 = new OutstandingTransactions.OutstandingTransactionsValue(
                OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Abort,
                new List<string> { "rm3", "rm4" });

            target.UpdateAndFlush("id1", rmlist1);
            target.UpdateAndFlush("id2", rmlist2);

            target.ReadFromFile();

            Assert.IsTrue(target.transactionList.ContainsKey("id1"));
            Assert.IsTrue(target.transactionList.ContainsKey("id2"));
            Assert.IsTrue(target.transactionList["id1"].transactionType == OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Commit);
            Assert.IsTrue(target.transactionList["id2"].transactionType == OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Abort);
            Assert.IsTrue(rmlist1.nackRMList.Except(target.transactionList["id1"].nackRMList).Count() == 0);
            Assert.IsTrue(rmlist2.nackRMList.Except(target.transactionList["id2"].nackRMList).Count() == 0);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_OutstandingTransactions_FuncTest2()
        {
            OutstandingTransactions_Accessor target = new OutstandingTransactions_Accessor();
            target.transactionList.Clear();
            target.WriteToFile(); // Write empty list to file to initialize state

            OutstandingTransactions.OutstandingTransactionsValue rmlist1 = new OutstandingTransactions.OutstandingTransactionsValue(
                OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Commit,
                new List<string> { "rm1", "rm2" });
            OutstandingTransactions.OutstandingTransactionsValue rmlist2 = new OutstandingTransactions.OutstandingTransactionsValue(
                OutstandingTransactions.OutstandingTransactionsValue.TransactionType.Abort,
                new List<string> { "rm3", "rm4" });

            target.UpdateAndFlush("id1", rmlist1);
            target.UpdateAndFlush("id2", rmlist2);
            // Assume that id2 is now fully committed, the file will have 2 instances of id2
            // after this call
            target.UpdateAndFlush("id2", null);

            target.ReadFromFile();

            Assert.IsTrue(target.transactionList.ContainsKey("id1"));
            // Make sure id2 does not exist in the transactionList
            Assert.IsTrue(target.transactionList.ContainsKey("id2") == false);
            Assert.IsTrue(rmlist1.nackRMList.Except(target.transactionList["id1"].nackRMList).Count() == 0);

            // Do a full rewrite, id2 should not exist on the file anymore
            target.WriteToFile();

            target.ReadFromFile();

            Assert.IsTrue(target.transactionList.ContainsKey("id1"));
            // Make sure id2 does not exist in the transactionList
            Assert.IsTrue(target.transactionList.ContainsKey("id2") == false);
            Assert.IsTrue(rmlist1.nackRMList.Except(target.transactionList["id1"].nackRMList).Count() == 0);
        }
    }
}
