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

        public static DocumentException CreateAggregate(IEnumerable<DocumentException> exceptions)
        {
            return new AggregationDocumentException(exceptions);
        }

        public static void RunAll(params Action[] actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }
            List<DocumentException> exceptions = null;
            foreach (var action in actions)
            {
                try
                {
                    action();
                }
                catch (DocumentException ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<DocumentException>();
                    }
                    exceptions.Add(ex);
                }
            }
            if (exceptions?.Count > 0)
            {
                throw CreateAggregate(exceptions);
            }
        }
    }

    [Serializable]
    public class AggregationDocumentException : DocumentException
    {
        public AggregationDocumentException(IEnumerable<DocumentException> exceptions)
            : base("Multiple failures.")
        {
            InnerExceptions = Flat(exceptions).ToImmutableArray();
        }

        protected AggregationDocumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            InnerExceptions = ((DocumentException[])info.GetValue("InnerExceptions", typeof(DocumentException[]))).ToImmutableArray();
        }

        public ImmutableArray<DocumentException> InnerExceptions { get; private set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("InnerExceptions", InnerExceptions.ToArray());
        }

        public override string ToString()
        {
            return base.ToString() + $@"
Inner exceptons:
{string.Join(Environment.NewLine, InnerExceptions)}";
        }

        private static IEnumerable<DocumentException> Flat(IEnumerable<DocumentException> exceptions)
        {
            foreach (var item in exceptions)
            {
                var agg = item as AggregationDocumentException;
                if (agg != null)
                {
                    foreach (var inner in agg.InnerExceptions)
                    {
                        yield return inner;
                    }
                }
                else
                {
                    yield return item;
                }
            }
        }
    }
}
