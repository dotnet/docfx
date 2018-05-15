# Errors and Warnings

These are the errors and warnings grouped by category to help you understand their purpose.

> ❌ This is an error  
> ⚠️ This is a warning  
> 💡 Guide to help you troubleshoot  

## General Errors

### circular-reference

❌ A series of document references creates a closed loop.

💡 Check the error message to find which files caused the reference loop, then update the affected documents to break the loop.

## Config

### config-not-found

❌ `docfx.yml` does not exist in the current working directory or the directory specified in command line arguments.

💡 Specify the correct directory, or create a new docset with `docfx new`

### invalid-config

❌ `docfx.yml` is either an invalid yaml or does not confirm to our config schema.

💡 Try check your `docfx.yml` content with a [YAML linter](http://www.yamllint.com/).

## Markdown

## Table of Contents

## Schema Documents
