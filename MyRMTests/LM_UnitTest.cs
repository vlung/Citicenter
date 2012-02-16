using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyRM;
using TP;

namespace UnitTestProject
{
    [TestClass]
    public class LM_UnitTest
    {
        class LM_Rid_Bundle
        {
            public MyLM lm { get; set; }
            public RID rid { get; set; }
            public int status { get; set; }
            public AutoResetEvent event1;

            public LM_Rid_Bundle(MyLM lm, RID rid) { this.lm = lm; this.rid = rid; status = 0; event1 = new AutoResetEvent(false); }
        }

        [TestMethod]
        public void LM_GetReadLockForManyTransactions()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();
            Transaction t2 = new Transaction();

            lm.LockForRead(t1, res1);
            lm.LockForRead(t2, res1);
        }

        [TestMethod]
        [ExpectedException(typeof(MyLM.DeadLockDetected), "Deadlock exception should be thrown since another transaction already has a write lock on the resource.")]
        public void LM_GetReadlockOnResLockedWithWriteLock()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();
            Transaction t2 = new Transaction();

            lm.setDeadlockTimeout(500); // Shorten deadlock timeout to speed up unit test
            lm.LockForWrite(t1, res1);
            lm.LockForRead(t2, res1);
        }

        [TestMethod]
        [ExpectedException(typeof(MyLM.DeadLockDetected), "Deadlock exception should be thrown since another transaction already has a write lock on the resource.")]
        public void LM_GetWritelockOnResLockedWithWriteLock()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();
            Transaction t2 = new Transaction();

            lm.setDeadlockTimeout(500); // Shorten deadlock timeout to speed up unit test
            lm.LockForWrite(t1, res1);
            lm.LockForWrite(t2, res1);
        }

        [TestMethod]
        [ExpectedException(typeof(MyLM.DeadLockDetected), "Deadlock exception should be thrown since another transaction already has a write lock on the resource.")]
        public void LM_GetWritelockOnResLockedWithReadLock()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();
            Transaction t2 = new Transaction();

            lm.setDeadlockTimeout(500); // Shorten deadlock timeout to speed up unit test
            lm.LockForRead(t1, res1);
            lm.LockForWrite(t2, res1);
        }

        [TestMethod]
        public void LM_DowngradeWriteLockToReadLock()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();

            lm.LockForWrite(t1, res1);
            lm.LockForRead(t1, res1);
        }

        [TestMethod]
        public void LM_UpgradeReadLockToWriteLock1()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();

            lm.LockForRead(t1, res1); 
            lm.LockForWrite(t1, res1);
        }

        [TestMethod]
        public void LM_UpgradeReadLockToWriteLock2()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();

            LM_Rid_Bundle param = new LM_Rid_Bundle(lm, res1);

            Thread thread = new Thread(AttemptWriteLockOnResource);

            lm.setDeadlockTimeout(3000); // Shorten deadlock timeout to 3 seconds speed up unit test
            lm.LockForRead(t1, res1);
            thread.Start(param);
            Thread.Sleep(1000);
            lm.LockForWrite(t1, res1);
            thread.Join();
            // Make sure that the second thread failed with deadlock exception.
            // This should happen because the first thread is still holding either the read lock or write lock
            // when the second thread trys to acquire a write lock. The second thread won't be able to steal the
            // lock from the first thread because two phase locking was used when the LM upgrades the read lock
            // to a write lock.
            Assert.AreEqual(param.status, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(MyLM.DeadLockDetected), "Deadlock exception should be thrown since another transaction still have a read lock on the resource")]
        public void LM_UpgradeReadLockToWriteLock3()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();
            Transaction t2 = new Transaction();

            lm.setDeadlockTimeout(500); // Shorten deadlock timeout to speed up unit test
            lm.LockForRead(t1, res1); 
            lm.LockForRead(t2, res1);
            lm.LockForWrite(t1, res1);
        }

        [TestMethod]
        [ExpectedException(typeof(MyLM.DeadLockDetected), "Deadlock exception should be thrown since another transaction still have a write lock on the resource")]
        public void LM_UpgradeReadLockToWriteLock4()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();
            Transaction t2 = new Transaction();

            lm.setDeadlockTimeout(500); // Shorten deadlock timeout to speed up unit test
            lm.LockForWrite(t2, res1);
            lm.LockForWrite(t1, res1);
        }

        [TestMethod]
        public void LM_UpgradeReadLockToWriteLock5()
        {
            MyLM lm = new MyLM();
            RID res1 = new RID();

            Transaction t1 = new Transaction();

            lm.LockForRead(t1, res1);
            lm.LockForWrite(t1, res1);
            lm.LockForWrite(t1, res1); // This should be a noop since the transaction already has a write lock on the resource.
        }

        private void AttemptWriteLockOnResource(object data)
        {
            LM_Rid_Bundle bundle = (LM_Rid_Bundle)data;

            Transaction t = new Transaction();
            try
            {
                bundle.lm.LockForWrite(t, bundle.rid);
                bundle.lm.UnlockAll(t);
                bundle.status = 0;
            }
            catch (MyLM.DeadLockDetected e)
            {
                bundle.status = 1;
            }
        }
    }
}
