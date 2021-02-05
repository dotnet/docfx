using System;
using System.Runtime.CompilerServices;

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Roslyn.Helpers
{
    internal class EnumOps
    {
        public static unsafe bool HasAllFlags<T>(T value, T flags) where T : unmanaged
        {
            if (sizeof(T) == 1)
                return (Unsafe.As<T, byte>(ref value) | Unsafe.As<T, byte>(ref flags)) == Unsafe.As<T, byte>(ref value);
            else if (sizeof(T) == 2)
                return (Unsafe.As<T, ushort>(ref value) | Unsafe.As<T, ushort>(ref flags)) == Unsafe.As<T, ushort>(ref value);
            else if (sizeof(T) == 4)
                return (Unsafe.As<T, uint>(ref value) | Unsafe.As<T, uint>(ref flags)) == Unsafe.As<T, uint>(ref value);
            else if (sizeof(T) == 8)
                return (Unsafe.As<T, ulong>(ref value) | Unsafe.As<T, ulong>(ref flags)) == Unsafe.As<T, ulong>(ref value);

            throw new NotSupportedException();
        }

        public static unsafe T ClearFlags<T>(T value, T flags) where T : unmanaged
        {
            if (sizeof(T) == 1)
            {
                var result = (byte)(Unsafe.As<T, byte>(ref value) & ~Unsafe.As<T, byte>(ref flags));
                return Unsafe.As<byte, T>(ref result);
            }
            else if (sizeof(T) == 2)
            {
                var result = (ushort)(Unsafe.As<T, ushort>(ref value) & ~Unsafe.As<T, ushort>(ref flags));
                return Unsafe.As<ushort, T>(ref result);
            }
            else if (sizeof(T) == 4)
            {
                var result = Unsafe.As<T, uint>(ref value) & ~Unsafe.As<T, uint>(ref flags);
                return Unsafe.As<uint, T>(ref result);
            }
            else if (sizeof(T) == 8)
            {
                var result = Unsafe.As<T, ulong>(ref value) & ~Unsafe.As<T, ulong>(ref flags);
                return Unsafe.As<ulong, T>(ref result);
            }

            throw new NotSupportedException();
        }
    }
}
