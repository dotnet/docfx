// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public sealed class CompositeResourceReader : ResourceFileReader
    {
        private ResourceFileReader[] _collectionsInOverriddenOrder = null;
        private bool disposed = false;

        public override string Name => "Composite";
        public override IEnumerable<string> Names { get; }
        public override bool IsEmpty { get; }

        public CompositeResourceReader(IEnumerable<ResourceFileReader> collectionsInOverriddenOrder)
        {
            if (collectionsInOverriddenOrder == null || !collectionsInOverriddenOrder.Any())
            {
                IsEmpty = true;
            }
            else
            {
                _collectionsInOverriddenOrder = collectionsInOverriddenOrder.ToArray();
                Names = _collectionsInOverriddenOrder.SelectMany(s => s.Names).Distinct();
            }
        }

        public override Stream GetResourceStream(string name)
        {
            if (IsEmpty) return null;
            for (int i = _collectionsInOverriddenOrder.Length - 1; i > -1; i--)
            {
                var stream = _collectionsInOverriddenOrder[i].GetResourceStream(name);
                if (stream != null)
                {
                    Logger.LogDiagnostic($"Resource \"{name}\" is found from \"{_collectionsInOverriddenOrder[i].Name}\"");
                    return stream;
                }
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            if (_collectionsInOverriddenOrder != null)
            {
                for (int i = 0; i < _collectionsInOverriddenOrder.Length; i++)
                {
                    _collectionsInOverriddenOrder[i].Dispose();
                    _collectionsInOverriddenOrder[i] = null;
                }

                _collectionsInOverriddenOrder = null;
            }

            base.Dispose(disposing);
        }
    }
}
