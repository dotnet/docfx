// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DocAsTest
{
    public class TestData
    {
        /// <summary>
        /// Gets the absolute path of the declaring file path.
        /// <summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets the one based start line number in the declaring file.
        /// <summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets the one based ordinal in the declaring file.
        /// <summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// Gets the summary of this data fragment.
        /// <summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets the markdown fenced code tip. E.g. yml for ````yml
        /// <summary>
        public string FenceTip { get; internal set; }

        /// <summary>
        /// Gets the content of this data fragment.
        /// <summary>
        public string Content { get; set; }
    }
}
