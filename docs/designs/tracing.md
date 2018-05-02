# The tracing system
Software [tracing](https://en.wikipedia.org/wiki/Tracing_(software)) involves a specialized use of logging to record information about a program's execution. This information is typically used by programmers for debugging purposes, and additionally, depending on the type and detail of information contained in a trace log, to diagnose common problems with software. 

In summary, our tracing system should provide:
1. a way for the developers to record messages and diagnose issues from different aspects, including performance, traces and error logs. 
2. a mechanism for end users to easily get current running status. When an error takes place, it provides user friendly message to indicate the next step, for example, when it is a document exception, the message can provide detailed suggestions to fix the error.

## Design
### Trace Type
In general there are two types of traces, one is for end users and one is for developers. Static classes `Reporter` and `Logger` are used respectively.
For `Reporter`, there are 3 levels: `Info`, `Warning`, `Error`.
For `Logger`, there are 2 categories of logs:
1. Performance
    1. Duration in milliseconds
    2. 3 levels: `Info`, `Verbose`, `Diagnostic`
2. Event
    1. Description
    2. 5 levels: `Error`, `Warning`, `Info`, `Verbose`, `Diagnostic`
    3. Different `Code`s for different types of events

`CorrelationId` like `1.102.3.4` is used to track the flow of the log.

## Receivers
Tracing should support different kinds of receivers, such as files and console.

## Error handling
In general, document errors should always be reported and should not throw exceptions. Cases are, for example, invalid file name or wrong metadata format. Such kind of errors are introduced in by end users and can be fixed by updating the input document, such errors should not prevent the program from building other documents.

Exceptions throws with internal errors, for example, when the schema is not well formated. Or with system errors which is generally a bug inside the program, for example, `ArgumentNullException` is a `SystemException` and it is fault to the program that the program terminates. Stacktrace should be dumped so that the developers can easily identify the root cause and fix the bug.