// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

let fs = require("fs");
let path = require("path");

let del = require("del");
let glob = require("glob");
let gulp = require("gulp");
let nconf = require("nconf");
let spawn = require("child-process-promise").spawn;

let configFile = path.join(__dirname, "config_gulp.json");
if (!fs.existsSync(configFile)) {
    throw new Error("Can't find config file");
}

nconf.add("configuration", { type: "file", file: configFile });

let config = {
    "docfx": nconf.get("docfx"),
    "firefox": nconf.get("firefox"),
    "myget": nconf.get("myget")
};

if (!config.docfx) {
    throw new Error("Can't find docfx configuration.");
}

if (!config.firefox) {
    throw new Error("Can't find firefox configuration.");
}

if (!config.myget) {
    throw new Error("Can't find myget configuration.");
}

config.myget["apiKey"] = process.env.MGAPIKEY;

function exec(command, args, workDir) {
    let cwd = process.cwd();
    if (workDir) {
        process.chdir(path.join(__dirname, workDir));
    }

    let promise = spawn(command, args);
    let childProcess = promise.childProcess;
    childProcess.stdout.on("data", (data) => {
        process.stdout.write(data.toString());
    });
    childProcess.stderr.on("data", (data) => {
        process.stderr.write(data.toString());
    })
    return promise.then(() => {
        process.chdir(cwd);
    });
}

function publish(artifactsFolder, mygetCommand, mygetKey, mygetUrl) {
    let packages = glob.sync(artifactsFolder + "/**/!(*.symbols).nupkg");
    let promises = packages.map(p => {
        return exec(mygetCommand, ["push", p, mygetKey, "-Source", mygetUrl]);
    });
    return Promise.all(promises);
}

gulp.task("build", () => {
    if (!config.docfx || !config.docfx["home"]) {
        throw new Error("Can't find docfx home directory in configuration.");
    }

    return exec("powershell", ["./build.ps1", "-prod"], config.docfx["home"]);
});

gulp.task("clean", () => {
    if (!config.docfx["artifactsFolder"]) {
        throw new Error("Can't find docfx artifacts folder in configuration.");
    }

    let artifactsFolder = path.join(__dirname, config.docfx["artifactsFolder"]);

    if (!config.docfx["targetFolder"]) {
        throw new Error("Can't find docfx target folder in configuration.");
    }

    let targetFolder = path.join(__dirname, config.docfx["targetFolder"]);

    return del([artifactsFolder, targetFolder], { force: true }).then((paths) => {
        if (!paths || paths.length === 0) {
            console.log("Folders not exist, no need to clean.");
        } else {
            console.log("Deleted: \n", paths.join("\n"));
        }
    });
});

gulp.task("e2eTest:installFirefox", () => {
    if (!config.firefox["version"]) {
        throw new Error("Can't find firefox version in configuration.");
    }

    return exec("choco", ["install", "firefox", "--version=" + config.firefox["version"], "-y"]);
});

gulp.task("e2eTest:buildSeed", () => {
    if (!config.docfx["exe"]) {
        throw new Error("Can't find docfx.exe in configuration.");
    }

    if (!config.docfx["docfxSeedHome"]) {
        throw new Error("Can't find docfx-seed in configuration.");
    }

    return exec(path.join(__dirname, config.docfx["exe"]), ["docfx.json"], config.docfx["docfxSeedHome"]);
});

gulp.task("e2eTest:restore", () => {
    if (!config.docfx["e2eTestsHome"]) {
        throw new Error("Can't find E2ETest directory in configuration.");
    }

    return exec("dotnet", ["restore"], config.docfx["e2eTestsHome"]);
});

gulp.task("e2eTest:test", () => {
    if (!config.docfx["e2eTestsHome"]) {
        throw new Error("Can't find E2ETest directory in configuration.");
    }

    return exec("dotnet", ["test"], config.docfx["e2eTestsHome"]);
});

gulp.task("e2eTest", gulp.series("e2eTest:installFirefox", "e2eTest:buildSeed", "e2eTest:restore", "e2eTest:test"));

gulp.task("publish:myget-dev", () => {
    if (!config.docfx["artifactsFolder"]) {
        throw new Error("Can't find artifacts folder in configuration.");
    }

    if (!config.myget["exe"]) {
        throw new Error("Can't find nuget command in configuration.");
    }

    if (!config.myget["apiKey"]) {
        throw new Error("Can't find myget api key in configuration.");
    }

    if (!config.myget["devUrl"]) {
        throw new Error("Can't find myget url for docfx dev feed in configuration.");
    }

    let artifactsFolder = path.join(__dirname, config.docfx["artifactsFolder"]);
    return publish(artifactsFolder, config.myget["exe"], config.myget["apiKey"], config.myget["devUrl"]);
});

gulp.task("publish:myget-test", () => {
    if (!config.docfx["artifactsFolder"]) {
        throw new Error("Can't find artifacts folder in configuration.");
    }

    if (!config.myget["exe"]) {
        throw new Error("Can't find nuget command in configuration.");
    }

    if (!config.myget["apiKey"]) {
        throw new Error("Can't find myget api key in configuration.");
    }

    if (!config.myget["testUrl"]) {
        throw new Error("Can't find myget url for docfx test feed in configuration.");
    }

    let artifactsFolder = path.join(__dirname, config.docfx["artifactsFolder"]);
    return publish(artifactsFolder, config.myget["exe"], config.myget["apiKey"], config.myget["testUrl"]);
});

gulp.task("publish:myget-master", () => {
    if (!config.docfx["artifactsFolder"]) {
        throw new Error("Can't find artifacts folder in configuration.");
    }

    if (!config.myget["exe"]) {
        throw new Error("Can't find nuget command in configuration.");
    }

    if (!config.myget["apiKey"]) {
        throw new Error("Can't find myget api key in configuration.");
    }

    if (!config.myget["masterUrl"]) {
        throw new Error("Can't find myget url for docfx master feed in configuration.");
    }

    let artifactsFolder = path.join(__dirname, config.docfx["artifactsFolder"]);
    return publish(artifactsFolder, config.myget["exe"], config.myget["apiKey"], config.myget["masterUrl"]);
});

gulp.task("test", gulp.series("clean", "build", "e2eTest", "publish:myget-test"));
gulp.task("dev", gulp.series("clean", "build", "e2eTest"));
gulp.task("stable", gulp.series("clean", "build", "e2eTest", "publish:myget-dev"));

gulp.task("default", gulp.series("dev"));
