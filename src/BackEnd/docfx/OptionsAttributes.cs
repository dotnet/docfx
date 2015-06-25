namespace docfx
{
    using System;

    public class AssemblyLicenseAttribute : CustomAssemblyTextAttribute
    {
        public AssemblyLicenseAttribute(params string[] lines) : base(lines) { }
    }

    public class AssemblyUsageAttribute : CustomAssemblyTextAttribute
    {
        public AssemblyUsageAttribute(params string[] lines) : base(lines) { }
    }

    public abstract class CustomAssemblyTextAttribute : Attribute
    {
        private string[] _lines;
        public CustomAssemblyTextAttribute(params string[] lines)
        {
            _lines = lines;
        }

        public string Value
        {
            get
            {
                return string.Join(Environment.NewLine, _lines);
            }
        }
    }
}
