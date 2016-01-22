namespace Microsoft.DocAsCode.Metadata
{
    using System;

    public class DisplayNameAttribute : Attribute
    {
        public virtual string Name { get; protected set; }

        public DisplayNameAttribute(string name)
        {
            Name = name;
        }
    }
}
