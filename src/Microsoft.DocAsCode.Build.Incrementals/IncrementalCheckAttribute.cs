namespace Microsoft.DocAsCode.Build.Incrementals
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class IncrementalCheckAttribute : Attribute
    {
    }
}
