# The tracing system
Software [tracing](https://en.wikipedia.org/wiki/Tracing_(software)) involves a specialized use of logging to record information about a program's execution. This information is typically used by programmers for debugging purposes, and additionally, depending on the type and detail of information contained in a trace log, to diagnose common problems with software. 

In summary, our tracing system should provide:
1. a way for the developers to record messages and diagnose issues from different aspects, including performance, traces and error logs. 
2. a mechanism for end users to easily get current running status. When an error takes place, it provides user friendly message to indicate the next step, for example, when it is a document exception, the message can provide detailed suggestions to fix the error.

## Design
### Trace Type
In general there are two types of traces, one is for end users and one is for developers. `Reporter` and `Logger` are used respectively.
For `Reporter`, reporters are part of the build result so it is instance level reports. There are 3 levels: `Info`, `Warning`, `Error` and contains well-messaged messages and `code`s for end-users.

For `Logger`, there are 2 categories of logs:
1. Performance
    1. Duration in milliseconds
    2. 3 levels: `Info`, `Verbose`, `Diagnostic`
2. Event
    1. Description
    2. 5 levels: `Error`, `Warning`, `Info`, `Verbose`, `Diagnostic`
    3. Different `Code`s for different types of events

`CorrelationId` like `1.102.3.4` will be used to track the flow of the log.

## Receivers
Tracing should support different kinds of receivers, such as files and console.

## Error handling
There are two kinds of exceptions in general. One is expected exceptions and docfx knows how to handle them, they are generally inherited from `DocFXException` abstract class. For example, bad-formated config file `docfx.yml` may possibly lead to an `InvalidConfigException`, invalid schema lead to an `InvalidSchemaException`, etc. Most document errors, in general, should always be reported and should not throw exceptions. Cases are, for example, invalid file name or wrong metadata format. Such kind of errors are introduced in by end users and can be fixed by updating the input document, such errors should not prevent the program from building other documents.

There will also be unexpected exceptions, these system errors are generally indicating bugs inside the program, for example, `ArgumentNullException` is a `SystemException` and it is fault to the program that the program terminates. Stacktrace should be dumped so that the developers can easily identify the root cause and fix the bug.
