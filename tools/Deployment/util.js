// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict"

let spawn = require('child_process').spawn;

let colors = require('colors/safe');
let moment = require('moment-timezone');

colors.setTheme({
  verbose: 'cyan',
  info: 'green',
  help: 'cyan',
  warn: 'yellow',
  debug: 'blue',
  error: 'red'
});

let logger = {
  info(msg) {
    console.log(colors.info(msg));
  },
  warn(msg) {
    console.log(colors.warn(msg));
  },
  debug(msg) {
    console.log(colors.debug(msg));
  },
  verbose(msg) {
    console.log(colors.verbose(msg));
  },
  error(msg) {
    console.log(colors.error(msg));
  },
}

let git = {
  add(filePath, workDir) {
    return execPromiseFn("git", ["add", filePath], workDir);
  },
  commit(msg, workDir) {
    return execPromiseFn("git", ["commit", "-m", msg], workDir);
  },
  amend(workDir) {
    return execPromiseFn("git", ["commit", "--amend", "--no-edit"], workDir);
  },
  push(current_branch, target_branch, workDir) {
    return execPromiseFn("git", ["push", "origin", current_branch + ":" + target_branch], workDir);
  },
  clone(repoUrl, branch, folderName, workDir) {
    return execPromiseFn("git", ["clone", "-b", branch, repoUrl, folderName], workDir)
  },
  configName(name, workDir) {
    return execPromiseFn("git", ["config", "user.name", name], workDir);
  },
  configEmail(email, workDir) {
    return execPromiseFn("git", ["config", "user.email", email], workDir);
  }
}

function isThirdWeekInSprint() {
  let baseMoment = moment("2016-12-12").tz("Asia/Shanghai");
  let gap = moment().tz("Asia/Shanghai").diff(baseMoment, "weeks");
  return gap % 3 === 2;
}

function execPromiseFn(command, args, workDir) {
  return function () {
    return new Promise(function (resolve, reject) {
      args = args || [];
      workDir = workDir || ".";
      let argStr = args.join(" ");
      let currentDir = process.cwd();

      logger.info("Running command: " + command + " " + argStr);
      try {
        process.chdir(workDir);
      } catch (err) {
        reject(new Error("Error occurs while changing work directory to " + workDir + ", " + err));
      }

      let sp = spawn(process.env.comspec, ['/c', command, ...args]);
      sp.stdout.on("data", function (data) {
        logger.verbose(data.toString());
      });
      sp.stderr.on("data", function (data) {
        logger.error(data.toString());
      });
      sp.on("close", function (code) {
        if (code === 0) {
          logger.info("Finishing command: " + command + " " + argStr);
          process.chdir(currentDir);
          resolve();
        } else {
          reject(new Error("Error occurs while running " + command + " " + argStr + ", exited with code: " + code));
        }
      });
    });
  }
}

exports.logger = logger;
exports.git = git;
exports.execPromiseFn = execPromiseFn;
exports.isThirdWeekInSprint = isThirdWeekInSprint;