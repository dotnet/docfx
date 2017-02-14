// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

let path = require('path');
let fs = require('fs');

let gulp = require("gulp");
let spawn = require("child-process-promise").spawn;

function exec(command, args, workDir) {
    let cwd = process.cwd();
    if (workDir) {
        process.chdir(path.join(__dirname, workDir));
    }

    let promise = spawn(command, args);
    let childProcess = promise.childProcess;
    childProcess.stdout.on('data', (data) => {
        process.stdout.write(data.toString());
    });
    childProcess.stderr.on('data', (data) => {
        process.stderr.write(data.toString());
    })
    return promise.then(() => {
        process.chdir(cwd);
    });
}

gulp.task("build", () => {
    return exec("powershell", ["./build.ps1", "-prod"], "../../../docfx/");
});

gulp.task("e2eTest:choco", () => {
    return exec("choco", ["install", "firefox", "--version=46.0.1", "-y"]);
});

gulp.task("e2eTest:buildSeed", ["build", "e2eTest:choco"], () => {
    return exec("../docfx/target/Release/docfx/docfx.exe", ["docfx.json"], "../../../docfx-seed/");
});

gulp.task("e2eTest:restore", ["e2eTest:buildSeed"], () => {
    return exec("dotnet", ["restore"], "../../test/docfx.E2E.Tests/");
});

gulp.task("e2eTest:test", ["e2eTest:restore"], () => {
    return exec("dotnet", ["test"], "../../test/docfx.E2E.Tests/");
});

gulp.task("e2eTest", ["e2eTest:test"]);

gulp.task("dev", ["build", "e2eTest"]);
gulp.task("default", ["dev"]);
