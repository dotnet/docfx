// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {defineConfig, globalIgnores} from 'eslint/config'
import js from '@eslint/js'
import tsesLint from 'typescript-eslint'
import tsesLintParser from '@typescript-eslint/parser'
import neostandard from 'neostandard';

export default defineConfig([
  js.configs.recommended,
  tsesLint.configs.recommended,
  ...neostandard({
    ts: true,
  }),
  {
    languageOptions: {
      parser: tsesLintParser,
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      "@typescript-eslint": neostandard.plugins['typescript-eslint'].plugin,
    },
    rules: {
      "@stylistic/space-before-function-paren": ["error", "never"],
    },
  },
  globalIgnores(["**/*.js"])
])
