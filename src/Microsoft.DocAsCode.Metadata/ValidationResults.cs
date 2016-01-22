namespace Microsoft.DocAsCode.Metadata
{
    using System.Collections.Generic;
    using System.Linq;

    public class ValidationResults
    {
        public ValidationResults(IEnumerable<ValidationResult> results)
        {
            Items.AddRange(from r in results where !r.IsSuccess select r);
        }

        public bool IsSuccess => Items.Count == 0;
        public List<ValidationResult> Items { get; } = new List<ValidationResult>();
    }
}
