// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = {
  env: {
    browser: true
  },
  ignorePatterns: ['**/*.js'],
  extends: ['standard', 'eslint:recommended', 'plugin:@typescript-eslint/recommended'],
  parser: '@typescript-eslint/parser',
  plugins: ['@typescript-eslint'],
  root: true,
  rules: {
    'space-before-function-paren': ['warn', 'never'],
  }
};
