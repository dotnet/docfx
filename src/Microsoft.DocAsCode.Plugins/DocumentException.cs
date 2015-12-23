namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Runtime.Serialization;

    [Serializable]
    public class DocumentException : Exception
    {
        public DocumentException() { }
        public DocumentException(string message) : base(message) { }
        public DocumentException(string message, Exception inner) : base(message, inner) { }
        protected DocumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public static void RunAll(params Action[] actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            DocumentException firstException = null;
            foreach (var action in actions)
            {
                try
                {
                    action();
                }
                catch (DocumentException ex)
                {
                    if (firstException == null)
                    {
                        firstException = ex;
                    }
                }
            }
            if (firstException != null)
            {
                throw new DocumentException(firstException.Message, firstException);
            }
        }
    }
}
