// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System;
    using System.IO;

    public class RestApiDocumentProcessorFixture : IDisposable
    {
        public string OutputFolder { get; }
        public string InputFolder { get; }
        public string TemplateFolder { get; }

        public RestApiDocumentProcessorFixture()
        {
            OutputFolder = "RestApiDocumentProcessorTestOutput";
            InputFolder = "RestApiDocumentProcessorTestInput";
            TemplateFolder = "RestApiDocumentProcessorTestTemplate";
            Directory.CreateDirectory(TemplateFolder);
            Directory.CreateDirectory(OutputFolder);
            Directory.CreateDirectory(InputFolder);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(OutputFolder))
                {
                    Directory.Delete(OutputFolder, true);
                }
                if (Directory.Exists(InputFolder))
                {
                    Directory.Delete(InputFolder, true);
                }
                if (Directory.Exists(TemplateFolder))
                {
                    Directory.Delete(TemplateFolder, true);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
