---
uid: docfx_environment_variables_reference
title: docfx environment variables reference
---

# Docfx environment variables

## `DOCFX_KEEP_DEBUG_INFO`

If set true. Keep following debug info in output HTML. 
- `sourceFile`
- `sourceStartLineNumber`
- `sourceEndLineNumber`
- `jsonPath`
- `data-raw-source`
- `nocheck`

## `DOCFX_GIT_TIMEOUT`

Used to overide git command timeout. (Default: `10,000` [ms])

## `DOCFX_SOURCE_BRANCH_NAME`

Used to override git branch name.

## `DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST`

Used to disable CRL check.
This setting is intended to be used on offline environment.
