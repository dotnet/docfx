using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Struct and Types Based on It
namespace Microsoft.DevDiv
{
    // Struct and Class
    /// <summary>
    /// Struct ContainersRefType
    /// </summary>
    public struct ContainersRefType
    {
        ///<summary>
        ///ColorCount
        /// </summary>
        // Field
        public long ColorCount;

        // Enumeration
        /// <summary>
        /// Enumeration ColorType
        /// </summary>
        public enum ColorType
        {
            /// <summary>
            /// red
            /// </summary>
            Red,
            /// <summary>
            /// blue
            /// </summary>
            Blue,
            /// <summary>
            /// yellow
            /// </summary>
            Yellow
        }

        // Delegate
        /// <summary>
        /// Delegate ContainersRefTypeDelegate
        /// </summary>
        public delegate void ContainersRefTypeDelegate();

        ///<summary>
        ///GetColorCount
        /// </summary>
        // Property
        public long GetColorCount
        {
            get
            {
                return ColorCount;
            }

            private set
            {
                ColorCount = value;
            }
        }

        /// <summary>
        /// ContainersRefTypeNonRefMethod
        /// <param name ="parmsArray">array</param>
        /// </summary>
        // Method
        public static int ContainersRefTypeNonRefMethod(params object[] parmsArray)
        {
            return 0;
        }

        // Interface
        public interface ContainersRefTypeChildInterface
        {

        }

        // Class
        public class ContainersRefTypeChild
        {

        }

        // Event
        public event EventHandler ContainersRefTypeEventHandler
        {
            add { }
            remove { }
        }
    }

    // Struct Layout Explicit
    [StructLayout(LayoutKind.Explicit)]
    public class ExplicitLayoutClass
    {

    }
}