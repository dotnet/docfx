// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict"

let path = require("path");
let util = require("./util");
let nconf = require('nconf');

nconf.add('configuration', { type: 'file', file: path.join(__dirname, 'config.json') });

let config = {};
config.sync = nconf.get("sync");
config.docfx = nconf.get("docfx");

if (util.isThirdWeekInSprint()) {
  util.logger.info("Don't sync in the third week of a sprint");
  process.exit(0);
}
util.serialPromiseFlow([
  util.git.updateOriginUrl(config.docfx.repoUrl, config.docfx.home),
  util.git.push(config.sync.fromBranch, config.sync.targetBranch, config.docfx.home)
]).then(() => {
  util.logger.info("Sync successfully");
});

