// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict"

let util = require("./util");

if (util.isThirdWeekInSprint()) {
  util.logger.info("Don't sync in the third week of a sprint");
  return 0;
}

util.git.push("dev", "stable")().then(function () {
  util.logger.info("Sync successfully");
  return 0;
}).catch(function () {
  util.logger.error("Failed to sync");
  return 1;
});
