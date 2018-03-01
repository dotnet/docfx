// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    public interface IFileAbstractLayer
    {
        bool CanRead { get; }

        bool CanWrite { get; }

        IEnumerable<string> GetAllInputFiles();

        bool Exists(string file);

        Stream OpenRead(string file);

        Stream Create(string file);

        void Copy(string sourceFileName, string destFileName);

        ImmutableDictionary<string, string> GetProperties(string file);

        string GetPhysicalPath(string file);

        IEnumerable<string> GetExpectedPhysicalPath(string file);
    }
}