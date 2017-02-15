// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

let path = require("path");
let fs = require("fs");

let gulp = require("gulp");
let nconf = require("nconf");
let spawn = require("child-process-promise").spawn;

let configFile = path.join(__dirname, "config_gulp.json");
if (!fs.existsSync(configFile)) {
    throw new Error("Can't find config file");
}

nconf.add("configuration", { type: "file", file: configFile });

let config = {};
config.docfx = nconf.get("docfx");
config.msbuild = nconf.get("msbuild");
config.choco = nconf.get("choco");
config.firefox = nconf.get("firefox");

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

gulp.task("build", () => {
    if (!config.docfx || !config.docfx["home"]) {
        throw new Error("Can't find docfx home directory.");
    }

    return exec("powershell", ["./build.ps1", "-prod"], config.docfx["home"]);
});

gulp.task("e2eTest:choco", () => {
    if (!config.firefox || !config.firefox["version"]) {
        throw new Error("Can't find firefox version.");
    }

    return exec("choco", ["install", "firefox", "--version=" + config.firefox["version"], "-y"]);
});

gulp.task("e2eTest:buildSeed", ["build", "e2eTest:choco"], () => {
    if (!config.docfx) {
        throw new Error("Can't find docfx configuration.");
    }

    if (!config.docfx["exe"]) {
        throw new Error("Can't find docfx.exe.");
    }

    if (!config.docfx["docfxSeedHome"]) {
        throw new Error("Can't find docfx-seed.");
    }

    return exec(path.join(__dirname, config.docfx["exe"]), ["docfx.json"], config.docfx["docfxSeedHome"]);
});

gulp.task("e2eTest:restore", ["e2eTest:buildSeed"], () => {
    if (!config.docfx || !config.docfx["e2eTestsHome"]) {
        throw new Error("Can't find E2ETest directory.");
    }

    return exec("dotnet", ["restore"], config.docfx["e2eTestsHome"]);
});

gulp.task("e2eTest:test", ["e2eTest:restore"], () => {
    if (!config.docfx || !config.docfx["e2eTestsHome"]) {
        throw new Error("Can't find E2ETest directory.");
    }

    return exec("dotnet", ["test"], config.docfx["e2eTestsHome"]);
});

gulp.task("e2eTest", ["e2eTest:test"]);

gulp.task("dev", ["build", "e2eTest"]);
gulp.task("default", ["dev"]);
