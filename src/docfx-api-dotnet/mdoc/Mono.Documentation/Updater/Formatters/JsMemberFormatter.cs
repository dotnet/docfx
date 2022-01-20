using System.Text;
using Mono.Cecil;
using Mono.Documentation;
using Mono.Documentation.Updater;

namespace Mono.Documentation.Updater.Formatters
{
    public class JsMemberFormatter : JsFormatter
    {
        public override string Language => Consts.Javascript;

        private readonly MemberFormatter usageFormatter;
        public override MemberFormatter UsageFormatter => usageFormatter;

        public JsMemberFormatter() : this(null) {}
        public JsMemberFormatter(TypeMap map) : base(map) {
            usageFormatter = new JsUsageFormatter(map);
        }

        protected override string GetMethodDeclaration(MethodDefinition method)
        {
            var buf = new StringBuilder();

            buf.Append("function ");
            buf.Append(GetMethodName(method));
            buf.Append("(");
            AppendParameters(buf, method, method.Parameters);
            buf.Append(")");

            return buf.ToString();
        }

        protected override string GetTypeDeclaration(TypeDefinition type)
        {
            // What version of ES/JS is supported? For example, we need to know what kind of syntax to use to declare a type
            // [RP] This depends on your target platform.
            // In UWP, it’s the same “version” of ES that is currently supported by the Edge browser. 
            // For Windows 8.0, this was IE10, and for Windows 8.1, this was IE11. 
            // Since Windows 10 and Edge, the browser has been evergreen, and is well into the ES2015+ versions.
            
            var publicConstructor = GetConstructor(type);
            return GetDeclaration(publicConstructor);
        }
        
        protected override string GetConstructorDeclaration(MethodDefinition constructor)
        {

            var buf = new StringBuilder();

            buf.Append("function ");
            AppendTypeName(buf, constructor.DeclaringType.Name);
            buf.Append("(");
            AppendParameters(buf, constructor, constructor.Parameters);
            buf.Append(")");

            return buf.ToString();
        }

        public override bool IsSupported(MemberReference mref)
        {
            if (mref is PropertyDefinition || mref is EventDefinition)
            {
                return false;
            }
            return base.IsSupported(mref);
        }

        public override bool IsSupported(TypeReference tref)
        {
            var type = tref.Resolve();
            if (type == null)
                return false;

            if (type.IsEnum ||
                type.IsValueType ||
                DocUtils.IsDelegate(type))
                return false;

            return base.IsSupported(tref);
        }
    }
}
