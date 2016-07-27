// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

let path = require('path');
let spawn = require('child_process').spawn;

let colors = require('colors/safe');
let fs = require('fs-extra');
let program = require('commander');
let request = require('request');
let jszip = require('jszip');
let nconf = require('nconf');
let sha1 = require('sha1');

nconf.add('configuration', {type: 'file', file: path.join(__dirname, 'config.json')});
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

function execPromiseFn(command, args, workDir) {
  return function() {
    return new Promise(function(resolve, reject) {
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
      sp.stdout.on("data", function(data) {
        logger.verbose(data.toString());
      });
      sp.stderr.on("data", function(data) {
        logger.warn(data.toString());
      });
      sp.on("close", function(code) {
        if (code === 0) {
          logger.info("Finishing command: " + command + " " + argStr);
          process.chdir(currentDir);
          resolve();
        } else {
          reject(new Error("Error occurs while running " + command + " " + argStr + ", Exited with code: " + code ));
        }
      });
    });
  }
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
  push(branch, workDir) {
    return execPromiseFn("git", ["push", "-u", "origin", branch], workDir);
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

function copyPromiseFn(src, dest, options) {
  return function() {
    logger.info("Start copying " + src + " " + dest);
    options = options || {clobber: true};
    return new Promise(function(resolve, reject) {
      fs.copy(src, dest, options, function(err) {
        if (err) {
          reject("Error occurs while copying " + src + " to " + dest + ", " + err);
        }
        logger.info("Finish copying " + src + " to " + dest);
        resolve();
      });
    });
  }
}

function removePromiseFn(dir) {
  return function() {
    logger.info("Start removing " + dir);
    return new Promise(function(resolve, reject) {
      fs.remove(dir, function(err) {
        if (err) {
          reject("Error occurs while removing " + dir + ", " + err);
        }
        logger.info("Finish removing " + dir);
        resolve();
      });
    });
  }
}

function uploadMygetPromiseFn(nugetExe, releaseFolder, apiKey, sourceUrl) {
  return function() {
    logger.info("Start uploading to myget.org");
    let promises = [];

    function upload(folder) {
      if (!fs.lstatSync(folder).isDirectory()) {
        return;
      }

      fs.readdirSync(folder).forEach(function(file, index) {
        let subPath = path.join(folder, file);
        let segment = file.split('.');
        if (fs.lstatSync(subPath).isFile() && segment.pop() === 'nupkg' && segment.pop() !== 'symbols') {
          promises.push(execPromiseFn(nugetExe, ['push', subPath, config.myget.apiKey, '-Source', sourceUrl])());
        } else {
          upload(subPath);
        }
      });
    }

    upload(releaseFolder);
    logger.info("Finish uploading to myget.org");
    return Promise.all(promises);
  }
}

function zipAssetsPromiseFn(fromDir, destDir) {
  return function() {
    logger.info("Start zipping assets");
    let zip = new jszip();
    fs.readdirSync(fromDir).forEach(function(file) {
      let filePath = path.join(fromDir, file);
      if (fs.lstatSync(filePath).isFile()) {
        let ext = path.extname(filePath);
        if (ext !== '.xml' && ext !== '.pdb') {
          let content = fs.readFileSync(filePath);
          zip.file(file, content);
        }
      }
    });
    let buffer = zip.generate({type:"nodebuffer", compression: "DEFLATE"});
    fs.unlinkSync(destDir);
    fs.writeFileSync(destDir, buffer);
    globalOptions.sha1 = sha1(buffer);
    logger.info("Finish zipping assets");
    return Promise.resolve();
  }
}

function parseReleaseNotePromiseFn(){
  return function() {
    logger.info("Start parsing RELEASENOTE");
    return new Promise(function(resolve, reject) {
      let regex = /^\-{3,}$/g;
      fs.readFile(config.docfx.releaseNote, function(err, data) {
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
        logger.info("Finish parsing RELEASENOTE");
        resolve();
      });
    });
  }
}

function createReleasePromiseFn() {
  return function() {
    logger.info("Start creating release of repo");
    return new Promise(function(resolve, reject) {
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

      request(createOptions, function(error, response, body) {
        if (error) {
          reject(new Error("Error occurs while creating release of repo, " + error));
        }
        logger.info("Finishing creating release of repo");
        globalOptions.upload_url = body.upload_url;
        resolve();
      });
    });
  }
}

function loadZipPromiseFn() {
  return function() {
    logger.info("Start loading assets zip");
    return new Promise(function(resolve, reject) {
      fs.readFile(config.docfx.zipDestFolder, function(err, data) {
        if (err) {
          reject(new Error("Error occurs while loading assets zip, " + err));
        }
        globalOptions.data = data;
        logger.info("Finish loading assets zip");
        resolve();
      });
    });
  }
}

function getLastestReleasePromiseFn() {
  return function() {
    logger.info("Start get latest release");
    return new Promise(function(resolve, reject) {
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
      request(queryOptions, function(err, response, body) {
        if (err) {
          reject(new Error("Error occurs while quering assets"));
        }
        globalOptions.tag_name = body.tag_name;
        globalOptions.upload_url = body.upload_url;
        if (body.assets.length !== 0) {
          globalOptions.assets_url = body.assets[0].url;
        }
        logger.info("Finish quering assets");
        resolve();
      });
    });
  }
}

function deleteAssetsPromiseFn() {
  return function() {
    logger.info("Start deleting assets");
    return new Promise(function(resolve, reject) {
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
      request(deleteOptions, function(err, response, body) {
        if (err) {
          reject(new Error("Error occurs while deleting assets"));
        }
        logger.info("Finish deleting assets");
        resolve();
      });
    });
  }
}

function uploadAssetsPromiseFn() {
  return function() {
    logger.info("Start uploading assets");
    return new Promise(function(resolve, reject) {
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
      request(uploadOptions, function(err, response, body) {
        if (err) {
          reject(new Error("Error occurs while uploading assets, " + err));
        }
        logger.info("Finish uploading assets");
        resolve();
      });
    });
  }
}

function updateReleasePromiseFn() {
  return function() {
    return new Promise(function(resolve, reject) {
      getLastestReleasePromiseFn()().then(function() {
        let promiseFn = globalOptions.tag_name === globalOptions.version ? deleteAssetsPromiseFn() : createReleasePromiseFn();
        promiseFn().then(function() {
          resolve();
        }).catch(function(err) {
          reject(err);
        });
      });
    });
  }
}

function updateChocoConfigPromiseFn() {
  return function() {
    return new Promise(function(resolve, reject) {
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
  return function() {
    return new Promise(function(reslove, reject) {
      if (!globalOptions.pkgName) {
        reject(new Error('package name can not be null/empty/undefined while pushing choco package'));
      }
      return execPromiseFn('choco', ['push', globalOptions.pkgName], config.choco.homeDir)();
    });
  }
}

let serialPromiseFlow = function(promiseArray) {
  return promiseArray.reduce((p, fn) => p.then(fn), Promise.resolve());
}

let runSteps = function(promiseArray) {
  serialPromiseFlow(promiseArray).catch(function(err) {
    logger.error(err);
    process.exit(1);
  });
}

let clearReleaseStep = removePromiseFn(config.docfx.releaseFolder);
let docfxBuildStep = execPromiseFn("build.cmd", ["Release", "PROD"], config.docfx.home);
let e2eTestStep = execPromiseFn(config.msbuild, [config.docfx.e2eproj, "/p:Configuration=Release", "/t:Build"]);
let genereateDocsStep = execPromiseFn(path.resolve(config.docfx.exe), ["docfx.json"], config.docfx.docFolder);
let uploadDevMygetStep = uploadMygetPromiseFn(config.myget.exe, config.docfx.releaseFolder, config.myget.apiKey, config.myget.url.dev);
let uploadMasterMygetStep = uploadMygetPromiseFn(config.myget.exe, config.docfx.releaseFolder, config.myget.apiKey, config.myget.url.master);

let updateGhPageStep = function() {
  let stepsOrder= [
    git.clone(config.docfx.repoUrl, "gh-pages", "docfxsite"),
    copyPromiseFn(config.docfx.siteFolder, "tmp"),
    copyPromiseFn("docfxsite/.git", "tmp/.git"),
    git.configName(config.git.name, "tmp"),
    git.configEmail(config.git.email, "tmp"),
    git.add(".", "tmp"),
    git.commit(config.git.message, "tmp"),
    git.push("gh-pages", "tmp")
  ];
  return serialPromiseFlow(stepsOrder);
}

let updateGithubReleaseStep = function() {
  let stepsOrder = [
    zipAssetsPromiseFn(config.docfx.zipSrcFolder, config.docfx.zipDestFolder),
    parseReleaseNotePromiseFn(),
    loadZipPromiseFn(),
    updateReleasePromiseFn(),
    uploadAssetsPromiseFn()
  ];
  return serialPromiseFlow(stepsOrder);
}

let updateChocoReleaseStep = function() {
  let stepsOrder = [
    updateChocoConfigPromiseFn(),
    execPromiseFn("choco", ['pack'], config.choco.homeDir),
    execPromiseFn("choco", ['apiKey', '-k', process.env.CHOCO_TOKEN, '-source', 'https://chocolatey.org/', config.choco.homeDir]),
    pushChocoPackage()
  ];
  return serialPromiseFlow(stepsOrder);
}

let branchValue;
program
  .arguments('<branch>')
  .action(function(branch) {
    branchValue = branch;
  });

program.parse(process.argv);

if (!branchValue) {
  logger.error("Need specify the repo branch");
  process.exit(1);
}

switch (branchValue.toLowerCase()) {
  case "dev":
    runSteps([
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
    runSteps([
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
    runSteps([
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
    logger.error("Please specify the *right* repo branch name to run this script");
}

