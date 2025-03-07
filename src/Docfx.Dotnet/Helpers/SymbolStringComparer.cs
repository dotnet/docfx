// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Docfx.Dotnet;

/// <summary>
/// StringComparer that simulate <see cref="StringComparer.InvariantCulture"> behavior for ASCII chars.
/// </summary>
/// <remarks>
/// .NET StringComparer ignores non-printable chars on string comparison
/// (e.g. StringComparer.InvariantCulture.Compare("\x0000 ZZZ \x0000"," ZZZ ")) returns 0).
/// This feature is not implement by this comparer.
/// </remarks>
internal sealed class SymbolStringComparer : IComparer<string>
{
    public static readonly SymbolStringComparer Instance = new();

    private SymbolStringComparer() { }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var xSpan = x.AsSpan();
        var ySpan = y.AsSpan();

        int minLength = Math.Min(xSpan.Length, ySpan.Length);

        int savedFirstDiff = 0;
        for (int i = 0; i < minLength; ++i)
        {
            var xChar = xSpan[i];
            var yChar = ySpan[i];

            if (xChar == yChar)
                continue;

            if (char.IsAscii(xChar) && char.IsAscii(yChar))
            {
                // Gets custom char order
                var xOrder = AsciiCharSortOrders[xChar];
                var yOrder = AsciiCharSortOrders[yChar];

                var result = xOrder.CompareTo(yOrder);
                if (result == 0)
                    continue;

                // Custom order logics for method parameters.
                if ((xChar == ',' && yChar == ')') || (xChar == ')' && yChar == ','))
                    return -result; // Returns result with inverse sign.

                // Save first char case comparison result. (To simulate `StringComparer.InvariantCulture` behavior).
                if (char.ToUpper(xChar) == char.ToUpper(yChar))
                {
                    if (savedFirstDiff == 0)
                        savedFirstDiff = result;
                    continue;
                }

                return result;
            }
            else
            {
                // Compare non-ASCII char with ordinal order
                int result = xChar.CompareTo(yChar);
                if (result != 0)
                    return result;
            }
        }

        // Return saved result if case difference exists and string length is same.
        if (savedFirstDiff != 0 && x.Length == y.Length)
            return savedFirstDiff;

        // Otherwise compare text length.
        return x.Length.CompareTo(y.Length);
    }

    // ASCII character order lookup table.
    // This table is based on StringComparer.InvariantCulture's charactor sort order.
    private static readonly byte[] AsciiCharSortOrders =
    [
        0,    // NUL
        0,    // SOH
        0,    // STX
        0,    // ETX
        0,    // EOT
        0,    // ENQ
        0,    // ACK
        0,    // BEL
        0,    // BS
        28,   // HT (Horizontal Tab)
        29,   // LF (Line Feed)
        30,   // VT (Vertical Tab)
        31,   // FF (Form Feed)
        32,   // CR (Carriage Return)
        0,    // SO
        0,    // SI
        0,    // DLE
        0,    // DC1
        0,    // DC2
        0,    // DC3
        0,    // DC4
        0,    // NAK
        0,    // SYN
        0,    // ETB
        0,    // CAN
        0,    // EM
        0,    // SUB
        0,    // ESC
        0,    // FS
        0,    // GS
        0,    // RS
        0,    // US
        33,   // SP (Space)
        39,   // !
        43,   // "
        55,   // #
        65,   // $
        56,   // %
        54,   // &
        42,   // '
        44,   // (
        45,   // )
        51,   // *
        59,   // +
        36,   // ,
        35,   // -
        41,   // .
        52,   // /
        66,   // 0
        67,   // 1
        68,   // 2
        69,   // 3
        70,   // 4
        71,   // 5
        72,   // 6
        73,   // 7
        74,   // 8
        75,   // 9
        38,   // :
        37,   // ;
        60,   // <
        61,   // =
        62,   // >
        40,   // ?
        50,   // @
        77,   // A
        79,   // B
        81,   // C
        83,   // D
        85,   // E
        87,   // F
        89,   // G
        91,   // H
        93,   // I
        95,   // J
        97,   // K
        99,   // L
        101,  // M
        103,  // N
        105,  // O
        107,  // P
        109,  // Q
        111,  // R
        113,  // S
        115,  // T
        117,  // U
        119,  // V
        121,  // W
        123,  // X
        125,  // Y
        127,  // Z
        46,   // [
        53,   // \
        47,   // ]
        58,   // ^
        34,   // _
        57,   // `
        76,   // a
        78,   // b
        80,   // c
        82,   // d
        84,   // e
        86,   // f
        88,   // g
        90,   // h
        92,   // i
        94,   // j
        96,   // k
        98,   // l
        100,  // m
        102,  // n
        104,  // o
        106,  // p
        108,  // q
        110,  // r
        112,  // s
        114,  // t
        116,  // u
        118,  // v
        120,  // w
        122,  // x
        124,  // y
        126,  // z
        48,   // {
        63,   // |
        49,   // }
        64,   // ~
        0,    // ESC
    ];
}
