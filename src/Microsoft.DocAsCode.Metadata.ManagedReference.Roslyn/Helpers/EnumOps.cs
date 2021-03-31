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
                return UnsafeValue.As<byte, T>((byte)(Unsafe.As<T, byte>(ref value) & ~Unsafe.As<T, byte>(ref flags)));
            else if (sizeof(T) == 2)
                return UnsafeValue.As<short, T>((short)(Unsafe.As<T, short>(ref value) & ~Unsafe.As<T, short>(ref flags)));
            else if (sizeof(T) == 4)
                return UnsafeValue.As<int, T>(Unsafe.As<T, int>(ref value) & ~Unsafe.As<T, int>(ref flags));
            else if (sizeof(T) == 8)
                return UnsafeValue.As<long, T>(Unsafe.As<T, long>(ref value) & ~Unsafe.As<T, long>(ref flags));

            throw new NotSupportedException();
        }
        private static class UnsafeValue
        {
            public static TTo As<TFrom, TTo>(TFrom source) => Unsafe.As<TFrom, TTo>(ref source);
        }
    }
}
