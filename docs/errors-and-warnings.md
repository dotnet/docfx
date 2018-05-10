# Errors and Warnings

These are the errors and warnings grouped by category to help you understand their purpose.

> ❌ This is an error  
> ⚠️ This is a warning  
> 💡 Guide to help you troubleshoot  

## General Errors

### fatal

❌ An unexpected fatal error has occurred.

💡 To help us improve, search for an existing issue or create a new issue at https://github.com/dotnet/docfx with the provided error description.

### circular-reference

❌ A series of document references creates a closed loop.

💡 Check the error message to find which files caused the reference loop, then update the affected documents to break the loop.

## Config

### config-not-found

❌ `docfx.yaml` does not exist in the current working directory or the docset directory specified in command line arguments.

💡 Specify the correct docset directory if you are building an existing docset, or create a new docset with `docfx new`

### invalid-config

❌ `docfx.yaml` is either an invalid yaml or does not confirm to our config schema.

💡 Try check your `docfx.yaml` content with a [YAML linter](http://www.yamllint.com/).

## Markdown

## Table of Contents

## Schema Documents
