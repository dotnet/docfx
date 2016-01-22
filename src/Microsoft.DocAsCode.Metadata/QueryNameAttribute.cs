namespace Microsoft.DocAsCode.Metadata
{
    using System;

    public class QueryNameAttribute : Attribute
    {
        public virtual string Name { get; protected set; }

        public QueryNameAttribute(string name)
        {
            Name = name;
        }
    }
}
