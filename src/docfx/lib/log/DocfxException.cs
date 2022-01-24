// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

/// <summary>
/// Throw this exception in case of known error that should stop execution of the program.
/// E.g, loading a malformed configuration.
/// </summary>
internal class DocfxException : Exception
{
    public Error Error { get; }

    public DocfxException(Error error, Exception? innerException = null)
        : base($"{error.Code}: {error.Message}", innerException)
    {
        Error = error;
    }

    public static bool IsDocfxException(Exception ex, out List<DocfxException> exceptions)
    {
        exceptions = new List<DocfxException>();
        ExtractDocfxException(ex, exceptions);
        return exceptions.Count > 0;
    }

    private static void ExtractDocfxException(Exception? ex, List<DocfxException> result)
    {
        switch (ex)
        {
            case null:
                break;

            case DocfxException docfxException:
                result.Add(docfxException);
                break;

            case AggregateException aggregateException:
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    ExtractDocfxException(innerException, result);
                }
                break;

            default:
                ExtractDocfxException(ex.InnerException, result);
                break;
        }
    }
}
