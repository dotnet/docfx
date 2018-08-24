namespace Microsoft.Docs.Build
{
    [DataSchema]
    public class TestModel
    {
        [Markdown]
        public string Description { get; set; }

        [InlineMarkdown]
        public string InlineDescription { get; set; }
    }
}
