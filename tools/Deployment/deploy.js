// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

let path = require('path');

let fs = require('fs-extra');
let program = require('commander');
let request = require('request');
let jszip = require('jszip');
let nconf = require('nconf');
let sha1 = require('sha1');

let util = require('./util');

nconf.add('configuration', { type: 'file', file: path.join(__dirname, 'config.json') });
let config = {};
config.docfx = nconf.get('docfx');
config.myget = nconf.get('myget');
config.msbuild = nconf.get('msbuild');
config.git = nconf.get('git');
config.choco = nconf.get('choco');

if (config.myget) {
  config.myget.apiKey = process.env.MGAPIKEY;
} else {
  throw new Error("Cannot find myget.org apikey");
}

let globalOptions = {
  query_url: config.docfx.releaseUrl + '/latest'
};

function copyPromiseFn(src, dest, options) {
  return function () {
    util.logger.info("Start copying " + src + " " + dest);
    options = options || { clobber: true };
    return new Promise(function (resolve, reject) {
      fs.copy(src, dest, options, function (err) {
        if (err) {
          reject("Error occurs while copying " + src + " to " + dest + ", " + err);
        }
        util.logger.info("Finish copying " + src + " to " + dest);
        resolve();
      });
    });
  }
}

function removePromiseFn(dir) {
  return function () {
    util.logger.info("Start removing " + dir);
    return new Promise(function (resolve, reject) {
      fs.remove(dir, function (err) {
        if (err) {
          reject("Error occurs while removing " + dir + ", " + err);
        }
        util.logger.info("Finish removing " + dir);
        resolve();
      });
    });
  }
}

function uploadMygetPromiseFn(nugetExe, releaseFolder, apiKey, sourceUrl) {
  return function () {
    util.logger.info("Start uploading to myget.org");
    let promises = [];

    function upload(folder) {
      if (!fs.lstatSync(folder).isDirectory()) {
        return;
      }

      fs.readdirSync(folder).forEach(function (file, index) {
        let subPath = path.join(folder, file);
        let segment = file.split('.');
        if (fs.lstatSync(subPath).isFile() && segment.pop() === 'nupkg' && segment.pop() !== 'symbols') {
          promises.push(util.execPromiseFn(nugetExe, ['push', subPath, config.myget.apiKey, '-Source', sourceUrl])());
        } else {
          upload(subPath);
        }
      });
    }

    upload(releaseFolder);
    util.logger.info("Finish uploading to myget.org");
    return Promise.all(promises);
  }
}

function zipAssetsPromiseFn(fromDir, destDir) {
  return function () {
    util.logger.info("Start zipping assets");
    let zip = new jszip();
    fs.readdirSync(fromDir).forEach(function (file) {
      let filePath = path.join(fromDir, file);
      if (fs.lstatSync(filePath).isFile()) {
        let ext = path.extname(filePath);
        if (ext !== '.xml' && ext !== '.pdb') {
          let content = fs.readFileSync(filePath);
          zip.file(file, content);
        }
      }
    });
    let buffer = zip.generate({ type: "nodebuffer", compression: "DEFLATE" });
    fs.unlinkSync(destDir);
    fs.writeFileSync(destDir, buffer);
    globalOptions.sha1 = "$sha1       = '" + sha1(buffer) + "'";
    util.logger.info("Finish zipping assets");
    return Promise.resolve();
  }
}

function parseReleaseNotePromiseFn() {
  return function () {
    util.logger.info("Start parsing RELEASENOTE");
    return new Promise(function (resolve, reject) {
      let regex = /^\-{3,}$/g;
      fs.readFile(config.docfx.releaseNote, function (err, data) {
        if (err) {
          reject(new Error("Error occurs while parsing RELEASENOTE, " + err));
        }
        let lines = data.toString().split(/\r?\n/g);

        let record = [];
        for (let i = 0; i < lines.length; ++i) {
          if (regex.test(lines[i])) {
            record.push(i);
          }
          if (record.length >= 2) {
            break;
          }
        }

        if (record.length === 0) {
          reject(new Error("No version number found in the RELEASENOTE"));
        }
        if (record.length === 1) {
          globalOptions.version = lines[record[0] - 1];
          globalOptions.content = lines.slice(record[0] + 1).join('\n');
        } else {
          globalOptions.version = lines[record[0] - 1];
          globalOptions.content = lines.slice(record[0] + 1, record[1] - 2).join('\n');
        }
        globalOptions.rawVersion = globalOptions.version.slice(1);
        util.logger.info("Finish parsing RELEASENOTE");
        resolve();
      });
    });
  }
}

function createReleasePromiseFn() {
  return function () {
    util.logger.info("Start creating release of repo");
    return new Promise(function (resolve, reject) {
      if (!process.env.TOKEN) {
        reject(new Error('No github account token in the environment.'));
      }
      if (!globalOptions.rawVersion) {
        reject(new Error('Empty version number is not allowed'));
      }

      let createOptions = {
        method: 'POST',
        url: config.docfx.releaseUrl,
        json: true,
        headers: {
          'User-Agent': 'request',
          'Authorization': 'token ' + process.env.TOKEN
        },
        body: {
          "tag_name": globalOptions.version,
          "target_commitish": "master",
          "name": "Version " + globalOptions.rawVersion,
          "body": globalOptions.content || ""
        }
      }

      request(createOptions, function (error, response, body) {
        if (error) {
          reject(new Error("Error occurs while creating release of repo, " + error));
        }
        util.logger.info("Finishing creating release of repo");
        globalOptions.upload_url = body.upload_url;
        resolve();
      });
    });
  }
}

function loadZipPromiseFn() {
  return function () {
    util.logger.info("Start loading assets zip");
    return new Promise(function (resolve, reject) {
      fs.readFile(config.docfx.zipDestFolder, function (err, data) {
        if (err) {
          reject(new Error("Error occurs while loading assets zip, " + err));
        }
        globalOptions.data = data;
        util.logger.info("Finish loading assets zip");
        resolve();
      });
    });
  }
}

function getLastestReleasePromiseFn() {
  return function () {
    util.logger.info("Start get latest release");
    return new Promise(function (resolve, reject) {
      if (!process.env.TOKEN) {
        reject(new Error('No github account token in the environment.'));
      }

      if (!globalOptions.query_url) {
        reject(new Error("No query url found while getting latest release"));
      }
      let queryOptions = {
        method: 'GET',
        url: globalOptions.query_url,
        json: true,
        headers: {
          'User-Agent': 'request',
          'Authorization': 'token ' + process.env.TOKEN,
        },
      }
      request(queryOptions, function (err, response, body) {
        if (err) {
          reject(new Error("Error occurs while quering assets"));
        }
        globalOptions.tag_name = body.tag_name;
        globalOptions.upload_url = body.upload_url;
        if (body.assets.length !== 0) {
          globalOptions.assets_url = body.assets[0].url;
        }
        util.logger.info("Finish quering assets");
        resolve();
      });
    });
  }
}

function deleteAssetsPromiseFn() {
  return function () {
    util.logger.info("Start deleting assets");
    return new Promise(function (resolve, reject) {
      if (!process.env.TOKEN) {
        reject(new Error('No github account token in the environment.'));
      }

      if (!globalOptions.assets_url) {
        resolve();
      }
      let deleteOptions = {
        method: 'DELETE',
        url: globalOptions.assets_url,
        json: true,
        headers: {
          'User-Agent': 'request',
          'Authorization': 'token ' + process.env.TOKEN,
        },
      }
      request(deleteOptions, function (err, response, body) {
        if (err) {
          reject(new Error("Error occurs while deleting assets"));
        }
        util.logger.info("Finish deleting assets");
        resolve();
      });
    });
  }
}

function uploadAssetsPromiseFn() {
  return function () {
    util.logger.info("Start uploading assets");
    return new Promise(function (resolve, reject) {
      if (!process.env.TOKEN) {
        reject(new Error('No github account token in the environment.'));
      }

      if (!globalOptions.upload_url) {
        reject(new Error("Upload assets error, current release " + globalOptions.version + " may already exists."));
      }
      let uploadOptions = {
        method: 'POST',
        url: globalOptions.upload_url.slice(0, -13) + "?name=docfx.zip",
        headers: {
          'User-Agent': 'request',
          'Authorization': 'token ' + process.env.TOKEN,
          'Content-Type': 'application/zip'
        },
        body: globalOptions.data
      }
      request(uploadOptions, function (err, response, body) {
        if (err) {
          reject(new Error("Error occurs while uploading assets, " + err));
        }
        util.logger.info("Finish uploading assets");
        resolve();
      });
    });
  }
}

function updateReleasePromiseFn() {
  return function () {
    return new Promise(function (resolve, reject) {
      getLastestReleasePromiseFn()().then(function () {
        let promiseFn = globalOptions.tag_name === globalOptions.version ? deleteAssetsPromiseFn() : createReleasePromiseFn();
        promiseFn().then(resolve).catch(reject);
      });
    });
  }
}

function updateChocoConfigPromiseFn() {
  return function () {
    return new Promise(function (resolve, reject) {
      if (!process.env.CHOCO_TOKEN) {
        reject(new Error('No chocolatey.org account token in the environment.'));
      }
      if (!globalOptions.rawVersion) {
        reject(new Error('rawVersion can not be null/empty/undefined when update choco config file'));
      }
      // update chocolateyinstall.ps1
      let chocoScriptContent = fs.readFileSync(config.choco.chocoScript, "utf8");
      chocoScriptContent = chocoScriptContent
        .replace(/v[\d\.]+/, globalOptions.version)
        .replace(/^(\$sha1\s+=\s+')[\d\w]+(')$/gm, globalOptions.sha1);
      fs.writeFileSync(config.choco.chocoScript, chocoScriptContent);

      // update docfx.nuspec
      let nuspecContent = fs.readFileSync(config.choco.nuspec, "utf8");
      nuspecContent = nuspecContent.replace(/(<version>)[\d\.]+(<\/version>)/, '$1' + globalOptions.rawVersion + '$2')
      fs.writeFileSync(config.choco.nuspec, nuspecContent);

      globalOptions.pkgName = "docfx." + globalOptions.rawVersion + ".nupkg";
      resolve();
    });
  }
}

function pushChocoPackage() {
  return function () {
    return new Promise(function (resolve, reject) {
      if (!globalOptions.pkgName) {
        reject(new Error('package name can not be null/empty/undefined while pushing choco package'));
      }
      let promiseFn = util.execPromiseFn('choco', ['push', globalOptions.pkgName], config.choco.homeDir);
      promiseFn().then(resolve).catch(reject);
    });
  }
}

let clearReleaseStep = removePromiseFn(config.docfx.releaseFolder);
let docfxBuildStep = util.execPromiseFn("build.cmd", ["-prod"], config.docfx.home);
let genereateDocsStep = util.execPromiseFn(path.resolve(config.docfx.exe), ["docfx.json"], config.docfx.docFolder);
let uploadDevMygetStep = uploadMygetPromiseFn(config.myget.exe, config.docfx.releaseFolder, config.myget.apiKey, config.myget.url.dev);
let uploadMasterMygetStep = uploadMygetPromiseFn(config.myget.exe, config.docfx.releaseFolder, config.myget.apiKey, config.myget.url.master);

let e2eTestStep = function () {
  let stepsOrder = [
    util.execPromiseFn("choco", ["install", "firefox", "--version=46.0.1", "-y"]),
    util.execPromiseFn(path.resolve(config.docfx.exe), ["docfx.json"], config.docfx.docfxSeedHome),
    util.execPromiseFn("dotnet", ["restore"], config.docfx.e2eTestsHome),
    util.execPromiseFn("dotnet", ["test"], config.docfx.e2eTestsHome)
  ];
  return util.serialPromiseFlow(stepsOrder);
}

let updateGhPageStep = function () {
  let stepsOrder = [
    util.git.clone(config.docfx.repoUrl, "gh-pages", "docfxsite"),
    copyPromiseFn(config.docfx.siteFolder, "tmp"),
    copyPromiseFn("docfxsite/.git", "tmp/.git"),
    util.git.configName(config.git.name, "tmp"),
    util.git.configEmail(config.git.email, "tmp"),
    util.git.add(".", "tmp"),
    util.git.commit(config.git.message, "tmp"),
    util.git.push("gh-pages", "gh-pages", "tmp")
  ];
  return util.serialPromiseFlow(stepsOrder);
}

let updateGithubReleaseStep = function () {
  let stepsOrder = [
    zipAssetsPromiseFn(config.docfx.zipSrcFolder, config.docfx.zipDestFolder),
    parseReleaseNotePromiseFn(),
    loadZipPromiseFn(),
    updateReleasePromiseFn(),
    uploadAssetsPromiseFn()
  ];
  return util.serialPromiseFlow(stepsOrder);
}

let updateChocoReleaseStep = function () {
  let stepsOrder = [
    updateChocoConfigPromiseFn(),
    util.execPromiseFn("choco", ['pack'], config.choco.homeDir),
    util.execPromiseFn("choco", ['apiKey', '-k', process.env.CHOCO_TOKEN, '-source', 'https://chocolatey.org/', config.choco.homeDir]),
    pushChocoPackage()
  ];
  return util.serialPromiseFlow(stepsOrder);
}

let branchValue;
program
  .arguments('<branch>')
  .action(function (branch) {
    branchValue = branch;
  });

program.parse(process.argv);

if (!branchValue) {
  util.logger.error("Need specify the repo branch");
  process.exit(1);
}

switch (branchValue.toLowerCase()) {
  case "dev":
    util.runSteps([
      // step 1: clear the possible release exists
      clearReleaseStep,
      // step2: run build.cmd
      docfxBuildStep,
      // step3: run e2e test
      e2eTestStep,
      // step4: run docfx.exe to generate documentation
      genereateDocsStep
    ]);
    break;
  case "nightly-build":
    util.runSteps([
      // step 1: clear the possible release exists
      clearReleaseStep,
      // step2: run build.cmd
      docfxBuildStep,
      // step3: run e2e test
      e2eTestStep,
      // step4: run docfx.exe to generate documentation
      genereateDocsStep,
      // step5: upload release to myget.org
      uploadDevMygetStep
    ]);
    break;
  case "master":
    util.runSteps([
      // step 1: clear the possible release exists
      clearReleaseStep,
      // step2: run build.cmd
      docfxBuildStep,
      // step3: run e2e test
      e2eTestStep,
      // step4: run docfx.exe to generate documentation
      genereateDocsStep,
      // step5: update gh-pages
      updateGhPageStep,
      // step6: zip and upload release
      updateGithubReleaseStep,
      // step7: upload to chocolatey.org
      updateChocoReleaseStep,
      // step8: upload release to myget.org
      uploadMasterMygetStep
    ]);
    break;
  default:
    util.logger.error("Please specify the *right* repo branch name to run this script");
}
