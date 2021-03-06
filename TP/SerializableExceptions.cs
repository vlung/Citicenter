﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TP
{
    [Serializable]
    public class UnknownRMException : System.Exception
    {
        public UnknownRMException()
            : base("UnknownRM - RM needs to register first.")
        {
        }

        public UnknownRMException(string message)
            : base(message)
        {
        }

        public UnknownRMException(string message, System.Exception e)
            : base(message, e)
        {
        }

        protected UnknownRMException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) 
        {
        }
    }

    [Serializable]
    public class AbortTransationException : System.Exception
    {
        public AbortTransationException()
            : base("Unable resolve logical address.")
        {
        }

        public AbortTransationException(string message)
            : base(message)
        {
        }

        public AbortTransationException(string message, System.Exception e)
            : base(message, e)
        {
        }

        protected AbortTransationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) 
        {
        }
    }

    [System.Serializable()]
    public class DeadLockDetected : System.Exception
    {
        public DeadLockDetected()
            : base()
        {
        }

        public DeadLockDetected(string message)
            : base(message)
        {
        }

        public DeadLockDetected(string message, System.Exception e)
            : base(message, e)
        {
        }

        // Constructor: This one is needed for exception serialization
        public DeadLockDetected(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
