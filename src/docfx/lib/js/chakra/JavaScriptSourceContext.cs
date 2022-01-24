// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ChakraHost.Hosting;

/// <summary>
///     A cookie that identifies a script for debugging purposes.
/// </summary>
public struct JavaScriptSourceContext : IEquatable<JavaScriptSourceContext>
{
    /// <summary>
    /// The context.
    /// </summary>
    private readonly IntPtr _context;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaScriptSourceContext"/> struct.
    /// </summary>
    /// <param name="context">The context.</param>
    private JavaScriptSourceContext(IntPtr context)
    {
        _context = context;
    }

    /// <summary>
    ///     Gets an empty source context.
    /// </summary>
    public static JavaScriptSourceContext None
    {
        get { return new JavaScriptSourceContext(new IntPtr(-1)); }
    }

    /// <summary>
    ///     The equality operator for source contexts.
    /// </summary>
    /// <param name="left">The first source context to compare.</param>
    /// <param name="right">The second source context to compare.</param>
    /// <returns>Whether the two source contexts are the same.</returns>
    public static bool operator ==(JavaScriptSourceContext left, JavaScriptSourceContext right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     The inequality operator for source contexts.
    /// </summary>
    /// <param name="left">The first source context to compare.</param>
    /// <param name="right">The second source context to compare.</param>
    /// <returns>Whether the two source contexts are not the same.</returns>
    public static bool operator !=(JavaScriptSourceContext left, JavaScriptSourceContext right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     Subtracts an offset from the value of the source context.
    /// </summary>
    /// <param name="context">The source context to subtract the offset from.</param>
    /// <param name="offset">The offset to subtract.</param>
    /// <returns>A new source context that reflects the subtraction of the offset from the context.</returns>
    public static JavaScriptSourceContext operator -(JavaScriptSourceContext context, int offset)
    {
        return FromIntPtr(context._context - offset);
    }

    /// <summary>
    ///     Subtracts an offset from the value of the source context.
    /// </summary>
    /// <param name="left">The source context to subtract the offset from.</param>
    /// <param name="right">The offset to subtract.</param>
    /// <returns>A new source context that reflects the subtraction of the offset from the context.</returns>
    public static JavaScriptSourceContext Subtract(JavaScriptSourceContext left, int right)
    {
        return left - right;
    }

    /// <summary>
    ///     Decrements the value of the source context.
    /// </summary>
    /// <param name="context">The source context to decrement.</param>
    /// <returns>A new source context that reflects the decrementing of the context.</returns>
    public static JavaScriptSourceContext operator --(JavaScriptSourceContext context)
    {
        return FromIntPtr(context._context - 1);
    }

    /// <summary>
    ///     Decrements the value of the source context.
    /// </summary>
    /// <param name="left">The source context to decrement.</param>
    /// <returns>A new source context that reflects the decrementing of the context.</returns>
    public static JavaScriptSourceContext Decrement(JavaScriptSourceContext left)
    {
        return --left;
    }

    /// <summary>
    ///     Adds an offset from the value of the source context.
    /// </summary>
    /// <param name="context">The source context to add the offset to.</param>
    /// <param name="offset">The offset to add.</param>
    /// <returns>A new source context that reflects the addition of the offset to the context.</returns>
    public static JavaScriptSourceContext operator +(JavaScriptSourceContext context, int offset)
    {
        return FromIntPtr(context._context + offset);
    }

    /// <summary>
    ///     Adds an offset from the value of the source context.
    /// </summary>
    /// <param name="left">The source context to add the offset to.</param>
    /// <param name="right">The offset to add.</param>
    /// <returns>A new source context that reflects the addition of the offset to the context.</returns>
    public static JavaScriptSourceContext Add(JavaScriptSourceContext left, int right)
    {
        return left + right;
    }

    /// <summary>
    ///     Increments the value of the source context.
    /// </summary>
    /// <param name="context">The source context to increment.</param>
    /// <returns>A new source context that reflects the incrementing of the context.</returns>
    public static JavaScriptSourceContext operator ++(JavaScriptSourceContext context)
    {
        return FromIntPtr(context._context + 1);
    }

    /// <summary>
    ///     Increments the value of the source context.
    /// </summary>
    /// <param name="left">The source context to increment.</param>
    /// <returns>A new source context that reflects the incrementing of the context.</returns>
    public static JavaScriptSourceContext Increment(JavaScriptSourceContext left)
    {
        return ++left;
    }

    /// <summary>
    ///     Creates a new source context. 
    /// </summary>
    /// <param name="cookie">
    ///     The cookie for the source context.
    /// </param>
    /// <returns>The new source context.</returns>
    public static JavaScriptSourceContext FromIntPtr(IntPtr cookie)
    {
        return new JavaScriptSourceContext(cookie);
    }

    /// <summary>
    ///     Checks for equality between source contexts.
    /// </summary>
    /// <param name="other">The other source context to compare.</param>
    /// <returns>Whether the two source contexts are the same.</returns>
    public bool Equals(JavaScriptSourceContext other)
    {
        return _context == other._context;
    }

    /// <summary>
    ///     Checks for equality between source contexts.
    /// </summary>
    /// <param name="obj">The other source context to compare.</param>
    /// <returns>Whether the two source contexts are the same.</returns>
    public override bool Equals(object? obj)
    {
        return obj is JavaScriptSourceContext context && Equals(context);
    }

    /// <summary>
    ///     The hash code.
    /// </summary>
    /// <returns>The hash code of the source context.</returns>
    public override int GetHashCode()
    {
        return _context.ToInt32();
    }
}
