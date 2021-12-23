namespace Mono.Documentation.Updater.Formatters
{
    class CSharpNativeTypeMemberFormatter : CSharpFullMemberFormatter
    {
        public CSharpNativeTypeMemberFormatter(TypeMap map) : base(map) { }

        protected override string GetCSharpType (string t)
        {
            string moddedType = base.GetCSharpType (t);

            switch (moddedType)
            {
                case "int": return "nint";
                case "uint":
                    return "nuint";
                case "float":
                    return "nfloat";
                case "System.Drawing.SizeF":
                    return "CoreGraphics.CGSize";
                case "System.Drawing.PointF":
                    return "CoreGraphics.CGPoint";
                case "System.Drawing.RectangleF":
                    return "CoreGraphics.CGPoint";
            }
            return null;
        }
    }
}