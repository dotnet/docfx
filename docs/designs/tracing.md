# The tracing system
Software [tracing](https://en.wikipedia.org/wiki/Tracing_(software)) involves a specialized use of logging to record information about a program's execution. This information is typically used by programmers for debugging purposes, and additionally, depending on the type and detail of information contained in a trace log, to diagnose common problems with software. 

In summary, our tracing system should provide:
1. a way for the developers to provide messages, including performance, traces and error logs. 
2. a mechanism for end users to easily narrow down the error, and fix the document or report to GitHub. The error report should contain enough message for the team to narrow down the root cause of the issue.

## Design
### Trace Type
We provide 2 categories of traces:
1. Performance
    1. Duration in miliseconds
    2. 3 levels: `Info`, `Verbose`, `Diagnostic`
2. Event
    1. Description
    2. 5 levels: `Error`, `Warning`, `Info`, `Verbose`, `Diagnostic`
    3. Different `Code`s for different types of events

`CorrelationId` like `1.102.3.4` is used to track the flow of the log.

## Receivers
Tracing should support different kinds of receivers, such as files and console.

## Error handling
Exceptions are in two categories:
1. `DocumentException` is exception caused by document errors, for example, invalid file name or wrong metadata format. Such kind of exceptions are introduced in by end users and can be fixed by updating the input document, however it is too serious error that when the error occurs, it makes no sense to continue build other documents and the program terminates when such error takes place. The program is supposed to catch as many such exceptions as possible.

`DocumentException`s contain `code`, `line`, `column` and `file` info. `file` info can leverage `FileScope` to automatically determine the value.

The program should have the ability to handle multi-threading cases, and catch as many document exceptions as possible.

2. `SystemException` is the system error that it is generally a bug inside DocFX. For example, `ArgumentNullException` is a `SystemException` and it is fault to the program that the program terminates. Stacktrace should be dumped so that the developers can easily identify the root cause and fix the bug.