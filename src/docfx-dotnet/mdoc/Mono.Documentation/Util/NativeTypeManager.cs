using System.Collections.Generic;

using Mono.Cecil;

namespace Mono.Documentation.Util
{
    static class NativeTypeManager
    {

        static Dictionary<string, string> toNativeType = new Dictionary<string, string> (){

            {"int", "nint"},
            {"Int32", "nint"},
            {"System.Int32", "System.nint"},
            {"uint", "nuint"},
            {"UInt32", "nuint"},
            {"System.UInt32", "System.nuint"},
            {"float", "nfloat"},
            {"Single", "nfloat"},
            {"System.Single", "System.nfloat"},
            {"SizeF", "CoreGraphics.CGSize"},
            {"System.Drawing.SizeF", "CoreGraphics.CGSize"},
            {"PointF", "CoreGraphics.CGPoint"},
            {"System.Drawing.PointF", "CoreGraphics.CGPoint"},
            {"RectangleF", "CoreGraphics.CGRect" },
            {"System.Drawing.RectangleF", "CoreGraphics.CGRect"}
        };

        static Dictionary<string, string> fromNativeType = new Dictionary<string, string> (){

            {"nint", "int"},
            {"System.nint", "System.Int32"},
            {"nuint", "uint"},
            {"System.nuint", "System.UInt32"},
            {"nfloat", "float"},
            {"System.nfloat", "System.Single"},
            {"CoreGraphics.CGSize", "System.Drawing.SizeF"},
            {"CoreGraphics.CGPoint", "System.Drawing.PointF"},
            {"CoreGraphics.CGRect", "System.Drawing.RectangleF"},
            {"MonoTouch.CoreGraphics.CGSize", "System.Drawing.SizeF"},
            {"MonoTouch.CoreGraphics.CGPoint", "System.Drawing.PointF"},
            {"MonoTouch.CoreGraphics.CGRect", "System.Drawing.RectangleF"}
        };

        public static string ConvertToNativeType (string typename)
        {
            string nvalue;

            bool isOut = false;
            bool isArray = false;
            string valueToCompare = StripToComparableType (typename, ref isOut, ref isArray);

            if (toNativeType.TryGetValue (valueToCompare, out nvalue))
            {

                if (isArray)
                {
                    nvalue += "[]";
                }
                if (isOut)
                {
                    nvalue += "&";
                }
                return nvalue;
            }
            return typename;
        }
        public static string ConvertFromNativeType (string typename)
        {
            string nvalue;

            bool isOut = false;
            bool isArray = false;
            string valueToCompare = StripToComparableType (typename, ref isOut, ref isArray);

            if (fromNativeType.TryGetValue (valueToCompare, out nvalue))
            {
                if (isArray)
                {
                    nvalue += "[]";
                }
                if (isOut)
                {
                    nvalue += "&";
                }
                return nvalue;
            }
            // it wasn't one of the native types ... just return it
            return typename;
        }

        static string StripToComparableType (string typename, ref bool isOut, ref bool isArray)
        {
            string valueToCompare = typename;
            if (typename.EndsWith ("[]"))
            {
                valueToCompare = typename.Substring (0, typename.Length - 2);
                isArray = true;
            }
            if (typename.EndsWith ("&"))
            {
                valueToCompare = typename.Substring (0, typename.Length - 1);
                isOut = true;
            }
            if (typename.Contains ("<"))
            {
                // TODO: Need to recursively process generic parameters
            }
            return valueToCompare;
        }

        public static string GetTranslatedName (TypeReference t)
        {
            string typename = t.FullName;

            bool isInAssembly = MDocUpdater.IsInAssemblies (t.Module.Name);
            if (isInAssembly && !typename.StartsWith ("System") && MDocUpdater.HasDroppedNamespace (t))
            {
                string nameWithDropped = string.Format ("{0}.{1}", MDocUpdater.droppedNamespace, typename);
                return nameWithDropped;
            }
            return typename;
        }
    }
}