// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

    using Microsoft.DocAsCode.Common;

    public class ExternalReferencePackageWriter : IDisposable
    {
        private readonly string _packageFile;
        private readonly Uri _baseUri;
        private readonly ZipArchive _zip;

        private ExternalReferencePackageWriter(string packageFile, Uri baseUri, bool append)
        {
            _packageFile = packageFile;
            _baseUri = baseUri;
            if (append && File.Exists(packageFile))
            {
                _zip = new ZipArchive(new FileStream(_packageFile, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update);
            }
            else
            {
                _zip = new ZipArchive(new FileStream(_packageFile, FileMode.Create, FileAccess.ReadWrite), ZipArchiveMode.Create);
            }
        }

        public static ExternalReferencePackageWriter Create(string packageFile, Uri baseUri)
        {
            return new ExternalReferencePackageWriter(packageFile, baseUri, false);
        }

        public static ExternalReferencePackageWriter Append(string packageFile, Uri baseUri)
        {
            return new ExternalReferencePackageWriter(packageFile, baseUri, true);
        }

        public void AddOrUpdateEntry(string entryName, List<ReferenceViewModel> vm)
        {
            if (entryName == null)
            {
                throw new ArgumentNullException(nameof(entryName));
            }
            if (vm == null)
            {
                throw new ArgumentNullException(nameof(vm));
            }
            if (vm.Count == 0)
            {
                throw new ArgumentException("Empty collection is not allowed.", nameof(vm));
            }
            ZipArchiveEntry entry = null;
            if (_zip.Mode == ZipArchiveMode.Update)
            {
                entry = _zip.GetEntry(entryName);
            }
            entry = entry ?? _zip.CreateEntry(entryName);
            using (var stream = entry.Open())
            using (var sw = new StreamWriter(stream))
            {
                YamlUtility.Serialize(sw, vm);
            }
        }

        public void Dispose()
        {
            _zip.Dispose();
        }
    }
}
