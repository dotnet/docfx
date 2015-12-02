namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.IO;

    public class MetadataCommandFixture : IDisposable
    {
        public string OutputFolder { get; }
        public string ProjectFolder { get; }

        public MetadataCommandFixture()
        {
            OutputFolder = "MetadataCommandTestOutput";
            ProjectFolder = "MetadataCommandTestProject";
            if (Directory.Exists(OutputFolder))
            {
                Directory.Delete(OutputFolder, true);
            }
            if (Directory.Exists(ProjectFolder))
            {
                Directory.Delete(ProjectFolder, true);
            }
            Directory.CreateDirectory(OutputFolder);
            Directory.CreateDirectory(ProjectFolder);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(OutputFolder))
                {
                    Directory.Delete(OutputFolder, true);
                }
                if (Directory.Exists(ProjectFolder))
                {
                    Directory.Delete(ProjectFolder, true);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
