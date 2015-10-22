namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;

    public static class DocumentExceptionExtensions
    {
        public static TResult[] RunAll<TElement, TResult>(this IReadOnlyList<TElement> elements, Func<TElement, TResult> func)
        {
            if (elements == null)
            {
                throw new ArgumentNullException(nameof(elements));
            }
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }
            var results = new TResult[elements.Count];
            List<DocumentException> exceptions = null;
            for (int i = 0; i < elements.Count; i++)
            {
                try
                {
                    results[i] = func(elements[i]);
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
                throw DocumentException.CreateAggregate(exceptions);
            }
            return results;
        }

        public static void RunAll<TElement>(this IReadOnlyList<TElement> elements, Action<TElement> action)
        {
            if (elements == null)
            {
                throw new ArgumentNullException(nameof(elements));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            List<DocumentException> exceptions = null;
            for (int i = 0; i < elements.Count; i++)
            {
                try
                {
                    action(elements[i]);
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
                throw DocumentException.CreateAggregate(exceptions);
            }
        }
    }
}
