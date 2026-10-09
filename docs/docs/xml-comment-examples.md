# XML Comment Examples

## Introduction

This page provides comprehensive examples of XML comments that DocFX supports for generating API documentation. The examples cover all major XML comment tags and demonstrate how they are rendered in the generated documentation.

## Basic XML Comment Tags

### Summary

The `<summary>` tag provides a brief description of the type or member.

```csharp
/// <summary>
/// Represents a collection of key/value pairs that are organized
/// based on the hash code of the key.
/// </summary>
public class Dictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
    // Implementation
}
```

### Parameters

The `<param>` tag describes a parameter for a method or constructor.

```csharp
/// <summary>
/// Adds an element with the specified key and value.
/// </summary>
/// <param name="key">The key of the element to add.</param>
/// <param name="value">The value of the element to add.</param>
/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
/// <exception cref="ArgumentException">An element with the same key already exists.</exception>
public void Add(TKey key, TValue value)
{
    // Implementation
}
```

### Type Parameters

The `<typeparam>` tag describes a generic type parameter.

```csharp
/// <summary>
/// Represents a strongly typed list of objects that can be accessed by index.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class List<T> : IList<T>
{
    // Implementation
}
```

### Returns

The `<returns>` tag describes the return value of a method.

```csharp
/// <summary>
/// Gets the value associated with the specified key.
/// </summary>
/// <param name="key">The key of the value to get.</param>
/// <returns>The value associated with the specified key. If the key is not found, 
/// the method throws a <see cref="KeyNotFoundException"/>.</returns>
/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
/// <exception cref="KeyNotFoundException">The key does not exist in the collection.</exception>
public TValue GetValue(TKey key)
{
    // Implementation
}
```

### Value

The `<value>` tag describes the value that a property represents.

```csharp
/// <summary>
/// Gets the number of key/value pairs contained in the dictionary.
/// </summary>
/// <value>The number of key/value pairs contained in the dictionary.</value>
public int Count
{
    get { /* Implementation */ }
}
```

## Advanced XML Comment Tags

### Remarks

The `<remarks>` tag provides additional information about a type or member, supplementing the information in the `<summary>` tag.

```csharp
/// <summary>
/// Represents a unique identifier.
/// </summary>
/// <remarks>
/// <para>The Guid structure provides a unique identification number.</para>
/// <para>The following example demonstrates how to create a new Guid:</para>
/// <code>
/// Guid g = Guid.NewGuid();
/// </code>
/// </remarks>
public struct Guid : IComparable, IFormattable
{
    // Implementation
}
```

### Example

The `<example>` tag provides example code for how to use a method or other library member.

```csharp
/// <summary>
/// Converts the string representation of a number to its 32-bit signed integer equivalent.
/// </summary>
/// <param name="s">A string containing a number to convert.</param>
/// <returns>A 32-bit signed integer equivalent to the number contained in <paramref name="s"/>.</returns>
/// <example>
/// <para>The following example demonstrates how to convert a string to an integer:</para>
/// <code>
/// string numberString = "123";
/// int number = Int32.Parse(numberString);
/// Console.WriteLine(number);  // Output: 123
/// </code>
/// </example>
public static int Parse(string s)
{
    // Implementation
}
```

### Exception

The `<exception>` tag documents the exceptions a method can throw.

```csharp
/// <summary>
/// Creates a shallow copy of the current object.
/// </summary>
/// <returns>A shallow copy of the current object.</returns>
/// <exception cref="NotSupportedException">The current type does not implement the <see cref="ICloneable"/> interface.</exception>
/// <exception cref="MemberAccessException">The caller does not have access to the method implementation.</exception>
public object Clone()
{
    // Implementation
}
```

### See Also

The `<seealso>` tag creates a cross-reference to another type or member.

```csharp
/// <summary>
/// Represents an instant in time, typically expressed as a date and time of day.
/// </summary>
/// <remarks>
/// For more information about working with dates and times, see the <see cref="DateTimeOffset"/> structure.
/// </remarks>
/// <seealso cref="DateTimeOffset"/>
/// <seealso cref="TimeSpan"/>
/// <seealso href="https://learn.microsoft.com/dotnet/standard/datetime/">Date and time in .NET</seealso>
public struct DateTime : IComparable, IFormattable
{
    // Implementation
}
```

### See

The `<see>` tag provides an inline cross-reference to another type or member.

```csharp
/// <summary>
/// Compares this instance to a specified object and returns an integer that
/// indicates whether this instance is earlier than, the same as, or later than the
/// specified object.
/// </summary>
/// <param name="obj">An object to compare with this instance.</param>
/// <returns>
/// A signed number indicating the relative values of this instance and the value parameter.
/// Less than zero if this instance is earlier than object.
/// Zero if this instance is the same as object.
/// Greater than zero if this instance is later than object.
/// </returns>
/// <remarks>
/// See the <see cref="IComparable"/> interface for more information.
/// </remarks>
public int CompareTo(object obj)
{
    // Implementation
}
```

## Formatting and Special Tags

### Inline Code

The `<c>` tag formats text as code in an inline context.

```csharp
/// <summary>
/// The <c>StringBuilder</c> class represents a mutable string of characters.
/// </summary>
public sealed class StringBuilder : ISerializable
{
    // Implementation
}
```

### Code Block

The `<code>` tag formats a block of text as code.

```csharp
/// <summary>
/// Represents a JSON array.
/// </summary>
/// <remarks>
/// You can create a JSON array using the following code:
/// <code>
/// var jsonArray = new JsonArray();
/// jsonArray.Add(1);
/// jsonArray.Add("text");
/// jsonArray.Add(true);
/// </code>
/// </remarks>
public class JsonArray : JsonElement
{
    // Implementation
}
```

### Lists

The `<list>` tag creates a list or table.

```csharp
/// <summary>
/// Defines special behaviors for serialization.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Use <see cref="OptInAttribute"/> to enable serialization of a type.</description></item>
/// <item><description>Use <see cref="OptOutAttribute"/> to disable serialization of a specific member.</description></item>
/// <item><description>Use <see cref="DataContractAttribute"/> for more precise control over serialization.</description></item>
/// </list>
/// </remarks>
public class SerializationBehavior
{
    // Implementation
}
```

```csharp
/// <summary>
/// Provides a description of the HTTP status codes.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader>
/// <term>Status Code</term>
/// <description>Description</description>
/// </listheader>
/// <item>
/// <term>200</term>
/// <description>OK. The request has succeeded.</description>
/// </item>
/// <item>
/// <term>404</term>
/// <description>Not Found. The requested resource does not exist.</description>
/// </item>
/// <item>
/// <term>500</term>
/// <description>Internal Server Error. An unexpected server error occurred.</description>
/// </item>
/// </list>
/// </remarks>
public enum HttpStatusCode
{
    // Implementation
}
```

### Notes

The `<note>` tag creates a note with various types.

```csharp
/// <summary>
/// Encrypts data using the specified algorithm.
/// </summary>
/// <param name="data">The data to encrypt.</param>
/// <param name="key">The encryption key.</param>
/// <returns>The encrypted data.</returns>
/// <remarks>
/// <note type="security">
/// Always use a strong key and store it securely.
/// </note>
/// <note type="warning">
/// This method is not recommended for sensitive data.
/// </note>
/// <note type="important">
/// The key must be the correct length for the algorithm chosen.
/// </note>
/// </remarks>
public byte[] Encrypt(byte[] data, byte[] key)
{
    // Implementation
}
```

### Paramrefs and Typeparamrefs

The `<paramref>` and `<typeparamref>` tags identify words as parameters or type parameters, respectively.

```csharp
/// <summary>
/// Gets the value associated with the specified key.
/// </summary>
/// <param name="key">The key whose value to get.</param>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <returns>
/// The value associated with the specified key. If <paramref name="key"/> is not found,
/// the method returns the default value for <typeparamref name="TValue"/>.
/// </returns>
public TValue GetValueOrDefault<TKey, TValue>(TKey key)
{
    // Implementation
}
```

## Bringing It All Together

Here's a comprehensive example that uses many of the XML comment tags together:

```csharp
/// <summary>
/// Represents a generic cache that stores items with expiration policies.
/// </summary>
/// <typeparam name="TKey">The type of keys used to identify cached items.</typeparam>
/// <typeparam name="TValue">The type of items stored in the cache.</typeparam>
/// <remarks>
/// <para>The <see cref="Cache{TKey, TValue}"/> class provides a thread-safe way to store and retrieve items with expiration policies.</para>
/// <para>Items can be set to expire after a fixed duration, at a specific time, or based on a custom policy.</para>
/// <note type="important">
/// This implementation is not distributed and is intended for use in a single-process application.
/// </note>
/// 
/// <para>The cache supports the following expiration policies:</para>
/// <list type="bullet">
/// <item>
/// <description><c>AbsoluteExpiration</c>: Items expire at a specific point in time.</description>
/// </item>
/// <item>
/// <description><c>SlidingExpiration</c>: Items expire after a period of inactivity.</description>
/// </item>
/// <item>
/// <description><c>CustomExpiration</c>: Items expire based on a custom callback function.</description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// Here's an example of how to use the Cache class:
/// <code>
/// // Create a new cache
/// var cache = new Cache&lt;string, User&gt;();
/// 
/// // Add an item that expires after 10 minutes
/// cache.Set("user:123", new User { Id = 123, Name = "John" }, TimeSpan.FromMinutes(10));
/// 
/// // Retrieve the item
/// if (cache.TryGet("user:123", out User user))
/// {
///     Console.WriteLine($"User name: {user.Name}");
/// }
/// </code>
/// </example>
/// <seealso cref="IDisposable"/>
/// <seealso href="https://learn.microsoft.com/dotnet/standard/caching/">Caching in .NET</seealso>
public class Cache<TKey, TValue> : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class with default settings.
    /// </summary>
    /// <example>
    /// <code>
    /// var cache = new Cache&lt;string, int&gt;();
    /// </code>
    /// </example>
    public Cache()
    {
        // Implementation
    }

    /// <summary>
    /// Gets the number of items currently in the cache.
    /// </summary>
    /// <value>The number of items in the cache.</value>
    /// <remarks>
    /// This count includes items that may have expired but haven't been removed yet.
    /// Use <see cref="CleanExpiredItems"/> to remove expired items.
    /// </remarks>
    public int Count
    {
        get { /* Implementation */ }
    }

    /// <summary>
    /// Adds or updates an item in the cache with an absolute expiration time.
    /// </summary>
    /// <param name="key">The key of the item to add.</param>
    /// <param name="value">The value to add to the cache.</param>
    /// <param name="absoluteExpiration">The absolute expiration date for the item.</param>
    /// <returns><c>true</c> if the item was added; <c>false</c> if it was updated.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="absoluteExpiration"/> is in the past.</exception>
    /// <example>
    /// <code>
    /// cache.Set("key", "value", DateTime.UtcNow.AddMinutes(30));
    /// </code>
    /// </example>
    public bool Set(TKey key, TValue value, DateTime absoluteExpiration)
    {
        // Implementation
        return true;
    }

    /// <summary>
    /// Attempts to get a cached item by its key.
    /// </summary>
    /// <param name="key">The key of the item to get.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, the default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// <c>true</c> if the cache contains an item with the specified key and it has not expired;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    public bool TryGet(TKey key, out TValue value)
    {
        // Implementation
        value = default;
        return false;
    }

    /// <summary>
    /// Removes all expired items from the cache.
    /// </summary>
    /// <returns>The number of items that were removed.</returns>
    /// <remarks>
    /// <para>
    /// This method is automatically called periodically by the cache maintenance task.
    /// </para>
    /// <para>
    /// You can call this method manually if you want to immediately reclaim memory
    /// used by expired items.
    /// </para>
    /// </remarks>
    public int CleanExpiredItems()
    {
        // Implementation
        return 0;
    }

    /// <summary>
    /// Releases all resources used by the cache.
    /// </summary>
    /// <remarks>
    /// Call this method when you're finished using the cache.
    /// </remarks>
    public void Dispose()
    {
        // Implementation
    }
}
```

## What's Next?

For more information about XML documentation comments in C#, see:

- [XML Documentation Comments (C# Programming Guide)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)
- [Recommended XML tags for C# documentation comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)