// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

let fs = require("fs");
let path = require("path");

let del = require("del");
let glob = require("glob");
let gulp = require("gulp");
let nconf = require("nconf");

let Common = require("./out/common").Common;
let Myget = require("./out/myget").Myget;
let Github = require("./out/github").Github;
let Chocolatey = require("./out/chocolatey").Chocolatey;

let configFile = path.resolve("config_gulp.json");

if (!fs.existsSync(configFile)) {
    throw new Error("Can't find config file");
}

nconf.add("configuration", { type: "file", file: configFile });

let config = {
    "docfx": nconf.get("docfx"),
    "firefox": nconf.get("firefox"),
    "myget": nconf.get("myget"),
    "git": nconf.get("git"),
    "choco": nconf.get("choco")
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

if (!config.git) {
    throw new Error("Can't find git configuration.");
}

if (!config.choco) {
    throw new Error("Can't find chocolatey configuration.");
}

gulp.task("build", () => {
    if (!config.docfx["home"]) {
        throw new Error("Can't find docfx home directory in configuration.");
    }

    return Common.execAsync("powershell", ["./build.ps1", "-prod"], config.docfx["home"]);
});

gulp.task("clean", () => {
    if (!config.docfx["artifactsFolder"]) {
        throw new Error("Can't find docfx artifacts folder in configuration.");
    }

    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);

    if (!config.docfx["targetFolder"]) {
        throw new Error("Can't find docfx target folder in configuration.");
    }

    let targetFolder = path.resolve(config.docfx["targetFolder"]);

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

    return Common.execAsync("choco", ["install", "firefox", "--version=" + config.firefox["version"], "-y"]);
});

gulp.task("e2eTest:buildSeed", () => {
    if (!config.docfx["exe"]) {
        throw new Error("Can't find docfx.exe in configuration.");
    }

    if (!config.docfx["docfxSeedHome"]) {
        throw new Error("Can't find docfx-seed in configuration.");
    }

    return Common.execAsync(path.resolve(config.docfx["exe"]), ["docfx.json"], config.docfx["docfxSeedHome"]);
});

gulp.task("e2eTest:restore", () => {
    if (!config.docfx["e2eTestsHome"]) {
        throw new Error("Can't find E2ETest directory in configuration.");
    }

    return Common.execAsync("dotnet", ["restore"], config.docfx["e2eTestsHome"]);
});

gulp.task("e2eTest:test", () => {
    if (!config.docfx["e2eTestsHome"]) {
        throw new Error("Can't find E2ETest directory in configuration.");
    }

    return Common.execAsync("dotnet", ["test"], config.docfx["e2eTestsHome"]);
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

    if (!process.env.MGAPIKEY) {
        throw new Error("Can't find myget key in configuration.");
    }

    let mygetToken = process.env.MGAPIKEY;
    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);
    return Myget.publishToMygetAsync(artifactsFolder, config.myget["exe"], mygetToken, config.myget["devUrl"]);
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

    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);
    return Myget.publishToMygetAsync(artifactsFolder, config.myget["exe"], config.myget["apiKey"], config.myget["testUrl"]);
});

gulp.task("publish:myget-master", () => {
    if (!config.docfx["home"]) {
        throw new Error("Can't find home path in configuration.");
    }

    if (!config.docfx["releaseNotePath"]) {
        throw new Error("Can't find RELEASENOTE path in configuration.");
    }

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

    let gitRootPath = path.resolve(config.docfx["home"]);
    let releaseNotePath = path.resolve(config.docfx["releaseNotePath"]);
    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);
    return Myget.publishToMygetAsync(artifactsFolder, config.myget["exe"], config.myget["apiKey"], config.myget["masterUrl"], gitRootPath, releaseNotePath);
});

gulp.task("updateGhPage", () => {
    if (!config.docfx["repoUrl"]) {
        throw new Error("Can't find docfx repo url in configuration.");
    }

    if (!config.docfx["siteFolder"]) {
        throw new Error("Can't find docfx site folder in configuration.");
    }

    if (!config.git["name"]) {
        throw new Error("Can't find git user name in configuration");
    }

    if (!config.git["email"]) {
        throw new Error("Can't find git user email in configuration");
    }

    if (!config.git["message"]) {
        throw new Error("Can't find git commit message in configuration");
    }

    let promise = Github.updateGhPagesAsync(config.docfx["repoUrl"], config.docfx["siteFolder"], config.git["name"], config.git["email"], config.git["message"]);
    promise.then(() => {
        console.log("Update github pages successfully.");
    }).catch(err => {
        console.log(`Failed to update github pages, ${err}`);
        process.exit(1);
    })
});

gulp.task("publish:gh-release", () => {
    if (!config.docfx["home"]) {
        throw new Error("Can't find home path in configuration.");
    }

    if (!config.docfx["releaseNotePath"]) {
        throw new Error("Can't find RELEASENOTE path in configuration.");
    }

    if (!config.docfx["releaseFolder"]) {
        throw new Error("Can't find zip source folder in configuration.");
    }

    if (!config.docfx["assetZipPath"]) {
        throw new Error("Can't find asset zip destination folder in configuration.");
    }

    if (!process.env.TOKEN) {
        throw new Error('No github account token in the environment.');
    }

    let githubToken = process.env.TOKEN;

    let gitRootPath = path.resolve(config.docfx["home"]);
    let releaseNotePath = path.resolve(config.docfx["releaseNotePath"]);
    let releaseFolder = path.resolve(config.docfx["releaseFolder"]);
    let assetZipPath = path.resolve(config.docfx["assetZipPath"]);

    let promise = Github.updateGithubReleaseAsync(config.docfx["repoUrl"], gitRootPath, releaseNotePath, releaseFolder, assetZipPath, githubToken);
    promise.then(() => {
        console.log("Update github release and assets successfully.");
    }).catch(err => {
        console.log(`Failed to update github release and assets, ${err}`);
        process.exit(1);
    });
});

gulp.task("publish:chocolatey", () => {
    if (!config.docfx["home"]) {
        throw new Error("Can't find home path in configuration.");
    }

    if (!config.choco["homeDir"]) {
        throw new Error("Can't find homedir for chocolatey in configuration.");
    }

    if (!config.choco["nuspec"]) {
        throw new Error("Can't find nuspec for chocolatey in configuration.");
    }

    if (!config.choco["chocoScript"]) {
        throw new Error("Can't find script for chocolatey in configuration.");
    }

    if (!config.docfx["releaseNotePath"]) {
        throw new Error("Can't find RELEASENOTE path in configuration.");
    }

    if (!config.docfx["assetZipPath"]) {
        throw new Error("Can't find released zip path in configuration.");
    }

    // if (!process.env.CHOCO_TOKEN) {
    //     throw new Error('No chocolatey.org account token in the environment.');
    // }

    let chocoToken = process.env.CHOCO_TOKEN;

    let gitRootPath = path.resolve(config.docfx["home"]);
    let releaseNotePath = path.resolve(config.docfx["releaseNotePath"]);
    let assetZipPath = path.resolve(config.docfx["assetZipPath"]);

    let chocoScript = path.resolve(config.choco["chocoScript"]);
    let nuspec = path.resolve(config.choco["nuspec"]);
    let homeDir = path.resolve(config.choco["homeDir"]);

    let promise = Chocolatey.publishToChocolateyAsync(gitRootPath, releaseNotePath, assetZipPath, chocoScript, nuspec, homeDir, chocoToken);
    promise.then(() => {
        console.log("Publish to chocolatey successfully.");
    }).catch(err => {
        console.log(`Failed to publish to chocolatey, ${err}`);
        process.exit(1);
    });
});

gulp.task("test", gulp.series("clean", "build", "e2eTest", "publish:myget-test"));
gulp.task("dev", gulp.series("clean", "build", "e2eTest"));
gulp.task("stable", gulp.series("clean", "build", "e2eTest", "publish:myget-dev"));
gulp.task("master", gulp.series("clean", "build", "e2eTest", "updateGhPage", "publish:gh-release", "publish:chocolatey", "publish:myget-master"));
gulp.task("debug", gulp.series("publish:chocolatey"));
gulp.task("default", gulp.series("dev"));
