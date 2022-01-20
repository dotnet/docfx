using System;
using System.Runtime.Serialization;

namespace Mono.Documentation
{
    [Serializable]
    internal class MDocException : Exception
    {
        public MDocException()
        {
        }

        public MDocException(string message) : base(message)
        {
        }

        public MDocException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MDocException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class MDocAssemblyException : Exception
    {
        public string AssemblyName { get; set; }

        public MDocAssemblyException(string assemblyName, string message) : base(message)
        {
            this.AssemblyName = assemblyName;
        }

        public MDocAssemblyException(string assemblyName, string message, Exception innerException) : base(message, innerException)
        {
            this.AssemblyName = assemblyName;
        }

        protected MDocAssemblyException(string assemblyName, SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.AssemblyName = assemblyName;
        }
    }
}