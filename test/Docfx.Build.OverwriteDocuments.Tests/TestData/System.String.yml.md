# `System.String`
``` yaml
author: rpetrusha
ms.author: ronpet
manager: wpickett
```
## `summary`
Represents text as a sequence of UTF-16 code units.

## `remarks`
A string is a sequential collection of characters that is used to represent text. A [String](https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netframework-4.7) object is a sequential collection of System.Char objects that represent a string; a System.Char object corresponds to a UTF-16 code unit. The value of the String object is the content of the sequential collection of System.Char objects, and that value is immutable (that is, it is read-only). For more information about the immutability of strings, see the Immutability and the StringBuilder class section later in this topic. The maximum size of a String object in memory is 2GB, or about 1 billion characters.

In this section:

- Instantiating a String object
- Char objects and Unicode characters
- Strings and The Unicode Standard
- Strings and embedded null characters
- Strings and indexes
- Null strings and empty strings
- Immutability and the StringBuilder class
- Ordinal vs. culture-sensitive operations
- Normalization
- String operations by category

### Instantiating a String object

You can instantiate a String object in the following ways:

- By assigning a string literal to a String variable. This is the most commonly used method for creating a string. The following example uses assignment to create several strings. Note that in C#, because the backslash (\) is an escape character, literal backslashes in a string must be escaped or the entire string must be @-quoted. 

## `return/description`
Markdown content
## `parameters[id="parameterName"]/description`
Markdown content

# `System.String.#ctor(System.Char*)`

## `Summary`
Markdown content