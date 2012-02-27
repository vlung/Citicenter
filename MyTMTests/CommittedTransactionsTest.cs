using MyTM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyTMTests
{
    
    
    /// <summary>
    ///This is a test class for CommittedTransactionsTest and is intended
    ///to contain all CommittedTransactionsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CommittedTransactionsTest
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
        public void TM_CommittedTransactions_SerializeStringToKeyValueTest1()
        {
            CommittedTransactions_Accessor target = new CommittedTransactions_Accessor();
            string s = "id,1,rm1,rm2";
            string key;
            string keyExpected = "id";
            CommittedTransactions.CommittedTransactionsValue value = null;
            CommittedTransactions.CommittedTransactionsValue valueExpected = new CommittedTransactions.CommittedTransactionsValue(
                CommittedTransactions.CommittedTransactionsValue.TransactionType.Abort,
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
        public void TM_CommittedTransactions_SerializeStringToKeyValueTest2()
        {
            CommittedTransactions_Accessor target = new CommittedTransactions_Accessor();
            string s = "id,1";
            string key;
            string keyExpected = "id";
            CommittedTransactions.CommittedTransactionsValue value = null;
            CommittedTransactions.CommittedTransactionsValue valueExpected = new CommittedTransactions.CommittedTransactionsValue();
            bool expected = true;
            bool actual;
            actual = target.SerializeFromString(s, out key, out value);
            Assert.AreEqual(keyExpected, key);
            Assert.AreEqual(value.transactionType, CommittedTransactions.CommittedTransactionsValue.TransactionType.Abort);
            Assert.IsTrue(valueExpected.nackRMList.Except(value.nackRMList).Count() == 0);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for SerializeKeyValueToString
        ///</summary>
        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_CommittedTransactions_SerializeKeyValueToStringTest1()
        {
            CommittedTransactions_Accessor target = new CommittedTransactions_Accessor();
            string key = "id";
            CommittedTransactions.CommittedTransactionsValue value = new CommittedTransactions.CommittedTransactionsValue(
                CommittedTransactions.CommittedTransactionsValue.TransactionType.Commit,
                new List<string> { "rm1", "rm2" });
            string expected = "id,0,rm1,rm2";
            string actual;
            actual = target.SerializeToString(key, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_CommittedTransactions_SerializeKeyValueToStringTest2()
        {
            CommittedTransactions_Accessor target = new CommittedTransactions_Accessor();
            string key = "id";
            CommittedTransactions.CommittedTransactionsValue value = null;
            string expected = null;
            string actual;
            actual = target.SerializeToString(key, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_CommittedTransactions_SerializeKeyValueToStringTest3()
        {
            CommittedTransactions_Accessor target = new CommittedTransactions_Accessor();
            string key = "id";
            CommittedTransactions.CommittedTransactionsValue value = null;
            string expected = "id";
            string actual;
            actual = target.SerializeToString(key, value, true);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_CommittedTransactions_FuncTest1()
        {
            CommittedTransactions_Accessor target = new CommittedTransactions_Accessor();
            target.transactionList.Clear();
            target.WriteToFile(); // Write empty list to file to initialize state

            CommittedTransactions.CommittedTransactionsValue rmlist1 = new CommittedTransactions.CommittedTransactionsValue(
                CommittedTransactions.CommittedTransactionsValue.TransactionType.Commit, 
                new List<string> { "rm1", "rm2" });
            CommittedTransactions.CommittedTransactionsValue rmlist2 = new CommittedTransactions.CommittedTransactionsValue(
                CommittedTransactions.CommittedTransactionsValue.TransactionType.Abort, 
                new List<string> { "rm3", "rm4" });

            target.UpdateAndFlush("id1", rmlist1);
            target.UpdateAndFlush("id2", rmlist2);

            target.ReadFromFile();

            Assert.IsTrue(target.transactionList.ContainsKey("id1"));
            Assert.IsTrue(target.transactionList.ContainsKey("id2"));
            Assert.IsTrue(target.transactionList["id1"].transactionType == CommittedTransactions.CommittedTransactionsValue.TransactionType.Commit);
            Assert.IsTrue(target.transactionList["id2"].transactionType == CommittedTransactions.CommittedTransactionsValue.TransactionType.Abort);
            Assert.IsTrue(rmlist1.nackRMList.Except(target.transactionList["id1"].nackRMList).Count() == 0);
            Assert.IsTrue(rmlist2.nackRMList.Except(target.transactionList["id2"].nackRMList).Count() == 0);
        }

        [TestMethod()]
        [DeploymentItem("MyTM.exe")]
        public void TM_CommittedTransactions_FuncTest2()
        {
            CommittedTransactions_Accessor target = new CommittedTransactions_Accessor();
            target.transactionList.Clear();
            target.WriteToFile(); // Write empty list to file to initialize state

            CommittedTransactions.CommittedTransactionsValue rmlist1 = new CommittedTransactions.CommittedTransactionsValue(
                CommittedTransactions.CommittedTransactionsValue.TransactionType.Commit,
                new List<string> { "rm1", "rm2" });
            CommittedTransactions.CommittedTransactionsValue rmlist2 = new CommittedTransactions.CommittedTransactionsValue(
                CommittedTransactions.CommittedTransactionsValue.TransactionType.Abort,
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
