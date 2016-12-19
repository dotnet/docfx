﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.ComponentModel;

    public static class EnvironmentContext
    {
        private static string _baseDirectory;
        private static string _outputDirectory;

        /// <summary>
        /// The directory path which contains docfx.json.
        /// </summary>
        public static string BaseDirectory => string.IsNullOrEmpty(_baseDirectory) ? "." : _baseDirectory;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetBaseDirectory(string dir)
        {
            _baseDirectory = dir;
        }

        /// <summary>
        /// The output directory path.
        /// </summary>
        public static string OutputDirectory => string.IsNullOrEmpty(_outputDirectory) ? "." : _outputDirectory;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetOutputDirectory(string dir)
        {
            _outputDirectory = dir;
        }

        /// <summary>
        /// Get or set current file abstract layer.
        /// </summary>
        public static IFileAbstractLayer FileAbstractLayer => new RootedFileAbstractLayer(FileAbstractLayerImpl ?? new DefaultFileAbstractLayer());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IFileAbstractLayer FileAbstractLayerImpl { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Clean()
        {
            _baseDirectory = null;
            _outputDirectory = null;
            FileAbstractLayerImpl = null;
        }
    }
}
