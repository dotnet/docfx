namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.IO;

    public class BuildCommandFixture : IDisposable
    {
        public string OutputFolder { get; }
        public string InputFolder { get; }
        public string TemplateFolder { get; }

        public BuildCommandFixture()
        {
            OutputFolder = "BuildCommandTestOutput";
            InputFolder = "BuildCommandTestInput";
            TemplateFolder = "BuildCommandTestTemplate";
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
