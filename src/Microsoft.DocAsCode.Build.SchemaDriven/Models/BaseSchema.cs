// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    public class BaseSchema
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public JSchemaType? Type { get; set; }

        public JToken Default { get; set; }

        public Dictionary<string, BaseSchema> Properties { get; set; }

        public BaseSchema Items { get; set; }

        public ReferenceType Reference { get; set; }

        public ContentType ContentType { get; set; }

        public List<string> Tags { get; set; }

        public MergeType MergeType { get; set; }

        public List<string> XrefProperties { get; set; }
    }
}
