// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    public static class Constants
    {
        public const string YamlExtension = ".yml";

        public const string MapFileExtension = ".map";

        public const int DefaultParallelism = 4;

        public const string IndexFileName = "index.json";

        /// <summary>
        /// TODO: add other property name const
        /// </summary>
        public static class PropertyName
        {
            public const string Uid = "uid";
            public const string Id = "id";
            public const string Href = "href";
            public const string Type = "type";
            public const string Source = "source";
            public const string Path = "path";
            public const string DocumentType = "documentType";
            public const string Title = "title";
            public const string Conceptual = "conceptual";
            public const string Documentation = "documentation";
        }
    }
}
