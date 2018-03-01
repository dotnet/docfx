namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

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
            DocumentException firstException = null;
            for (int i = 0; i < elements.Count; i++)
            {
                try
                {
                    results[i] = func(elements[i]);
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
            return results;
        }

        public static void RunAll<TElement>(this IReadOnlyList<TElement> elements, Action<TElement> action)
        {
            RunAll((IEnumerable<TElement>)elements, action);
        }

        public static void RunAll<TElement>(this IEnumerable<TElement> elements, Action<TElement> action)
        {
            if (elements == null)
            {
                throw new ArgumentNullException(nameof(elements));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            DocumentException firstException = null;
            foreach (var element in elements)
            {
                try
                {
                    action(element);
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

        public static void RunAll<TElement>(this IReadOnlyList<TElement> elements, Action<TElement> action, int parallelism)
        {
            RunAll((IEnumerable<TElement>)elements, action, parallelism);
        }

        public static void RunAll<TElement>(this IEnumerable<TElement> elements, Action<TElement> action, int parallelism)
        {
            if (elements == null)
            {
                throw new ArgumentNullException(nameof(elements));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            if (parallelism <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parallelism));
            }
            DocumentException firstException = null;
            Parallel.ForEach(
                elements,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                s =>
                {
                    try
                    {
                        action(s);
                    }
                    catch (DocumentException ex)
                    {
                        Interlocked.CompareExchange(ref firstException, ex, null);
                    }
                });
            if (firstException != null)
            {
                throw new DocumentException(firstException.Message, firstException);
            }
        }
    }
}
