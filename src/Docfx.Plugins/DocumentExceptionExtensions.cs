// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;

namespace Docfx.Plugins;

public static class DocumentExceptionExtensions
{
    public static TResult[] RunAll<TElement, TResult>(this IReadOnlyList<TElement> elements, Func<TElement, TResult> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(func);

        var results = new TResult[elements.Count];
        DocumentException firstException = null;
        for (int i = 0; i < elements.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                results[i] = func(elements[i]);
            }
            catch (DocumentException ex)
            {
                firstException ??= ex;
            }
        }
        if (firstException != null)
        {
            throw new DocumentException(firstException.Message, firstException);
        }
        return results;
    }

    public static void RunAll<TElement>(this IReadOnlyList<TElement> elements, Action<TElement> action, CancellationToken cancellationToken = default)
    {
        RunAll((IEnumerable<TElement>)elements, action, cancellationToken);
    }

    public static void RunAll<TElement>(this IEnumerable<TElement> elements, Action<TElement> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(action);

        DocumentException firstException = null;
        foreach (var element in elements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                action(element);
            }
            catch (DocumentException ex)
            {
                firstException ??= ex;
            }
        }
        if (firstException != null)
        {
            throw new DocumentException(firstException.Message, firstException);
        }
    }

    public static void RunAll<TElement>(this IReadOnlyList<TElement> elements, Action<TElement> action, int parallelism, CancellationToken cancellationToken = default)
    {
        RunAll((IEnumerable<TElement>)elements, action, parallelism, cancellationToken);
    }

    public static void RunAll<TElement>(this IEnumerable<TElement> elements, Action<TElement> action, int parallelism, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(action);

        if (parallelism <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parallelism));
        }

        try
        {
            DocumentException firstException = null;
            Parallel.ForEach(
                elements,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = parallelism,
                    CancellationToken = cancellationToken,
                },
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
        catch (AggregateException ex)
        {
            // If exceptions are OperationCanceledException. throw only first exception.
            var innerExceptions = ex.Flatten().InnerExceptions.ToArray();
            if (innerExceptions.All(x => x is OperationCanceledException))
                ExceptionDispatchInfo.Throw(innerExceptions[0]);

            throw;
        }

    }
}
