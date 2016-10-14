// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

// For that the function 'replace' of String only replace the first one
function replaceAllInString(target, search, replacement) {
  return target.split(search).join(replacement);
};
