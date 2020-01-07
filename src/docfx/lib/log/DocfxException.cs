// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Throw this exception in case of known error that should stop execution of the program.
    /// E.g, loading a malformed configuration.
    /// </summary>
    internal class DocfxException : Exception
    {
        public ErrorLevel? OverwriteLevel { get; }

        public Error Error { get; }

        public DocfxException(Error error, Exception innerException = null, ErrorLevel? overwriteLevel = null)
            : base($"{error.Code}: {error.Message}", innerException)
        {
            Error = error;
            OverwriteLevel = overwriteLevel;
        }

        public static bool IsDocfxException(Exception ex, out IEnumerable<DocfxException> exceptions)
        {
            List<DocfxException> result = null;
            ExtractDocfxException(ex, ref result);
            if (result != null && result.Count > 0)
            {
                exceptions = result;
                return true;
            }
            exceptions = null;
            return false;
        }

        private static void ExtractDocfxException(Exception ex, ref List<DocfxException> result)
        {
            switch (ex)
            {
                case null:
                    break;

                case DocfxException dex:
                    result = result ?? new List<DocfxException>();
                    result.Add(dex);
                    break;

                case AggregateException aex:
                    result = result ?? new List<DocfxException>();
                    foreach (var innerException in aex.InnerExceptions)
                    {
                        ExtractDocfxException(innerException, ref result);
                    }
                    break;

                default:
                    ExtractDocfxException(ex.InnerException, ref result);
                    break;
            }
        }
    }
}
