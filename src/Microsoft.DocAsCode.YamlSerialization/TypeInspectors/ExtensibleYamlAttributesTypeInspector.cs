namespace Microsoft.DocAsCode.YamlSerialization.TypeInspectors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using YamlDotNet.Serialization;

    /// <summary>
    /// Applies the <see cref="YamlMemberAttribute"/> to another <see cref="ITypeInspector"/>.
    /// </summary>
    public sealed class ExtensibleYamlAttributesTypeInspector : ExtensibleTypeInspectorSkeleton
    {
        private readonly IExtensibleTypeInspector innerTypeDescriptor;

        public ExtensibleYamlAttributesTypeInspector(IExtensibleTypeInspector innerTypeDescriptor)
        {
            this.innerTypeDescriptor = innerTypeDescriptor;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            return innerTypeDescriptor.GetProperties(type, container)
                .Where(p => p.GetCustomAttribute<YamlIgnoreAttribute>() == null)
                .Select(p =>
                {
                    var descriptor = new PropertyDescriptor(p);
                    var member = p.GetCustomAttribute<YamlMemberAttribute>();
                    if (member != null)
                    {
                        if (member.SerializeAs != null)
                        {
                            descriptor.TypeOverride = member.SerializeAs;
                        }

                        descriptor.Order = member.Order;
                        descriptor.ScalarStyle = member.ScalarStyle;

                        if (member.Alias != null)
                        {
                            descriptor.Name = member.Alias;
                        }
                    }

                    return (IPropertyDescriptor)descriptor;
                })
                .OrderBy(p => p.Order);
        }

        public override IPropertyDescriptor GetProperty(Type type, object container, string name) =>
            innerTypeDescriptor.GetProperty(type, container, name);
    }
}
