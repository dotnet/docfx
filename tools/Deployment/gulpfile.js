// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

let fs = require("fs");
let path = require("path");

let del = require("del");
let glob = require("glob");
let gulp = require("gulp");
let nconf = require("nconf");
let format = require("string-format");
format.extend(String.prototype, {})

let Common = require("./out/common").Common;
let Guard = require("./out/common").Guard;
let Myget = require("./out/myget").Myget;
let Github = require("./out/github").Github;
let Chocolatey = require("./out/chocolatey").Chocolatey;
let SyncBranch = require("./out/syncBranch").SyncBranch;

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
    "choco": nconf.get("choco"),
    "sync": nconf.get("sync")
};

config.myget.exe = process.env.NUGETEXE || config.myget.exe;
Guard.argumentNotNull(config.docfx, "config.docfx", "Can't find docfx configuration.");
Guard.argumentNotNull(config.firefox, "config.docfx", "Can't find firefox configuration.");
Guard.argumentNotNull(config.myget, "config.docfx", "Can't find myget configuration.");
Guard.argumentNotNull(config.git, "config.docfx", "Can't find git configuration.");
Guard.argumentNotNull(config.choco, "config.docfx", "Can't find choco configuration.");

gulp.task("build", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.home, "config.docfx.home", "Can't find docfx home directory in configuration.");
    return Common.execAsync("powershell", ["./build.ps1", "-prod"], config.docfx.home);
});

gulp.task("build:release", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.home, "config.docfx.home", "Can't find docfx home directory in configuration.");
    return Common.execAsync("powershell", ["./build.ps1", "-prod", "-release"], config.docfx.home);
});

gulp.task("clean", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.artifactsFolder, "config.docfx.artifactsFolder", "Can't find docfx artifacts folder in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.targetFolder, "config.docfx.targetFolder", "Can't find docfx target folder in configuration.");

    let artifactsFolder = path.resolve(config.docfx.artifactsFolder);
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
    Guard.argumentNotNullOrEmpty(config.firefox.version, "config.firefox.version", "Can't find firefox version in configuration.");

    process.env.Path += ";C:/Program Files/Mozilla Firefox";
    return Common.execAsync("choco", ["install", "firefox", "--version=" + config.firefox.version, "-y", "--force"]);
});

gulp.task("e2eTest:restoreSeed", async () => {
    Guard.argumentNotNullOrEmpty(config.docfx.docfxSeedRepoUrl, "config.docfx.docfxSeedRepoUrl", "Can't find docfx-seed repo url in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.docfxSeedHome, "config.docfx.docfxSeedHome", "Can't find docfx-seed in configuration.");

    return await Common.execAsync("git", ["clone", config.docfx.docfxSeedRepoUrl, config.docfx.docfxSeedHome]);
});

gulp.task("e2eTest:buildSeed", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.exe, "config.docfx.exe", "Can't find docfx.exe in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.docfxSeedHome, "config.docfx.docfxSeedHome", "Can't find docfx-seed in configuration.");

    return Common.execAsync(path.resolve(config.docfx["exe"]), ["docfx.json"], config.docfx.docfxSeedHome);
});

gulp.task("e2eTest:restore", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.e2eTestsHome, "config.docfx.e2eTestsHome", "Can't find E2ETest directory in configuration.");

    return Common.execAsync("dotnet", ["restore"], config.docfx.e2eTestsHome);
});

gulp.task("e2eTest:test", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.e2eTestsHome, "config.docfx.e2eTestsHome", "Can't find E2ETest directory in configuration.");

    return Common.execAsync("dotnet", ["test"], config.docfx.e2eTestsHome);
});

gulp.task("e2eTest", gulp.series("e2eTest:installFirefox", "e2eTest:restoreSeed", "e2eTest:buildSeed", "e2eTest:restore", "e2eTest:test"));

gulp.task("publish:myget-dev", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.artifactsFolder, "config.docfx.artifactsFolder", "Can't find artifacts folder in configuration.");
    Guard.argumentNotNullOrEmpty(config.myget.exe, "config.myget.exe", "Can't find nuget command in configuration.");
    Guard.argumentNotNullOrEmpty(config.myget.devUrl, "config.myget.devUrl", "Can't find myget url for docfx dev feed in configuration.");
    Guard.argumentNotNullOrEmpty(process.env.MGAPIKEY, "process.env.MGAPIKEY", "Can't find myget key in Environment Variables.");

    let mygetToken = process.env.MGAPIKEY;
    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);

    return Myget.publishToMygetAsync(artifactsFolder, config.myget["exe"], mygetToken, config.myget["devUrl"]);
});

gulp.task("publish:myget-test", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.artifactsFolder, "config.docfx.artifactsFolder", "Can't find artifacts folder in configuration.");
    Guard.argumentNotNullOrEmpty(config.myget.exe, "config.myget.exe", "Can't find nuget command in configuration.");
    Guard.argumentNotNullOrEmpty(config.myget.testUrl, "config.myget.testUrl", "Can't find myget url for docfx test feed in configuration.");
    Guard.argumentNotNullOrEmpty(process.env.MGAPIKEY, "process.env.MGAPIKEY", "Can't find myget key in Environment Variables.");

    let mygetToken = process.env.MGAPIKEY;
    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);

    return Myget.publishToMygetAsync(artifactsFolder, config.myget["exe"], mygetToken, config.myget["testUrl"]);
});

gulp.task("publish:myget-master", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.artifactsFolder, "config.docfx.artifactsFolder", "Can't find artifacts folder in configuration.");
    Guard.argumentNotNullOrEmpty(config.myget.exe, "config.myget.exe", "Can't find nuget command in configuration.");
    Guard.argumentNotNullOrEmpty(config.myget.masterUrl, "config.myget.masterUrl", "Can't find myget url for docfx master feed in configuration.");
    Guard.argumentNotNullOrEmpty(process.env.MGAPIKEY, "process.env.MGAPIKEY", "Can't find myget key in Environment Variables.");
    Guard.argumentNotNullOrEmpty(config.docfx.releaseNotePath, "config.docfx.releaseNotePath", "Can't find RELEASENOTE.md in configuartion.");

    let mygetToken = process.env.MGAPIKEY;
    let releaseNotePath = path.resolve(config.docfx["releaseNotePath"]);
    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);

    return Myget.publishToMygetAsync(artifactsFolder, config.myget["exe"], mygetToken, config.myget["masterUrl"], releaseNotePath);
});

gulp.task("updateGhPage", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.httpsRepoUrl, "config.docfx.httpsRepoUrl", "Can't find docfx repo url in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.siteFolder, "config.docfx.siteFolder", "Can't find docfx site folder in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.exe, "config.docfx.exe", "Can't find docfx exe in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.docfxJson, "config.docfx.docfxJson", "Can't find docfx.json in configuration.");
    Guard.argumentNotNullOrEmpty(config.git.name, "config.git.name", "Can't find git user name in configuration");
    Guard.argumentNotNullOrEmpty(config.git.email, "config.git.email", "Can't find git user email in configuration");
    Guard.argumentNotNullOrEmpty(config.git.message, "config.git.message", "Can't find git commit message in configuration");
    Guard.argumentNotNullOrEmpty(process.env.TOKEN, "process.env.TOKEN", "No github account token in the environment.");

    let docfxExe = path.resolve(config.docfx.exe);
    let docfxJson = path.resolve(config.docfx.docfxJson);

    return Github.updateGhPagesAsync(config.docfx.httpsRepoUrl, config.docfx.siteFolder, docfxExe, docfxJson, config.git.name, config.git.email, config.git.message, process.env.TOKEN);
});

gulp.task("packAssetZip", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.releaseFolder, "config.docfx.releaseFolder", "Can't find zip source folder in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.assetZipPath, "config.docfx.assetZipPath", "Can't find asset zip destination folder in configuration.");

    let releaseFolder = path.resolve(config.docfx["releaseFolder"]);
    let assetZipPath = path.resolve(config.docfx["assetZipPath"]);

    Common.zipAssests(releaseFolder, assetZipPath);
    return Promise.resolve();
});

gulp.task("publish:gh-release", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.releaseNotePath, "config.docfx.releaseNotePath", "Can't find RELEASENOTE.md in configuartion.");
    Guard.argumentNotNullOrEmpty(process.env.TOKEN, "process.env.TOKEN", "No github account token in the environment.");

    let githubToken = process.env.TOKEN;
    let releaseNotePath = path.resolve(config.docfx["releaseNotePath"]);
    return Github.updateGithubReleaseAsync(config.docfx.sshRepoUrl, releaseNotePath, githubToken);
});

gulp.task("publish:gh-asset", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.assetZipPath, "config.docfx.assetZipPath", "Can't find asset zip destination folder in configuration.");
    Guard.argumentNotNullOrEmpty(process.env.TOKEN, "process.env.TOKEN", "No github account token in the environment.");

    let githubToken = process.env.TOKEN;
    let assetZipPath = path.resolve(config.docfx["assetZipPath"]);
    return Github.updateGithubAssetAsync(config.docfx.sshRepoUrl, assetZipPath, githubToken);
});

gulp.task("publish:chocolatey", () => {
    Guard.argumentNotNullOrEmpty(config.choco.homeDir, "config.choco.homeDir", "Can't find homedir for chocolatey in configuration.");
    Guard.argumentNotNullOrEmpty(config.choco.nuspec, "config.choco.nuspec", "Can't find nuspec for chocolatey in configuration.");
    Guard.argumentNotNullOrEmpty(config.choco.chocoScript, "config.choco.chocoScript", "Can't find script for chocolatey in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.releaseNotePath, "config.docfx.releaseNotePath", "Can't find RELEASENOTE path in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.assetZipPath, "config.docfx.assetZipPath", "Can't find released zip path in configuration.");
    Guard.argumentNotNullOrEmpty(process.env.CHOCO_TOKEN, "process.env.CHOCO_TOKEN", "No chocolatey.org account token in the environment.");

    let chocoToken = process.env.CHOCO_TOKEN;

    let releaseNotePath = path.resolve(config.docfx["releaseNotePath"]);
    let assetZipPath = path.resolve(config.docfx["assetZipPath"]);

    let chocoScript = path.resolve(config.choco["chocoScript"]);
    let nuspec = path.resolve(config.choco["nuspec"]);
    let homeDir = path.resolve(config.choco["homeDir"]);

    return Chocolatey.publishToChocolateyAsync(releaseNotePath, assetZipPath, chocoScript, nuspec, homeDir, chocoToken);
});

gulp.task("syncBranchCore", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.httpsRepoUrlWithToken, "config.docfx.httpsRepoUrlWithToken", "Can't find docfx repo url with token in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.home, "config.docfx.home", "Can't find docfx home directory in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.account, "config.docfx.account", "Can't find account in configuration.");
    Guard.argumentNotNullOrEmpty(config.sync.fromBranch, "config.sync.fromBranch", "Can't find source branch in sync configuration.");
    Guard.argumentNotNullOrEmpty(config.sync.targetBranch, "config.sync.targetBranch", "Can't find target branch in sync configuration.");
    Guard.argumentNotNullOrEmpty(process.env.TOKEN, "process.env.TOKEN", "No github account token in the environment.");

    if (Common.isThirdWeekInSprint()) {
        console.log("Ignore to sync in the third week of a sprint");
        process.exit(2);
    }

    var repoInfo = {
        "account": config.docfx.account,
        "token": process.env.TOKEN
    };
    var repoUrl = config.docfx.httpsRepoUrlWithToken.format(repoInfo);

    let docfxHome = path.resolve(config.docfx.home);
    return SyncBranch.runAsync(repoUrl, docfxHome, config.sync.fromBranch, config.sync.targetBranch);
});
gulp.task("test", gulp.series("clean", "build", "e2eTest", "publish:myget-test"));
gulp.task("dev", gulp.series("clean", "build", "e2eTest"));
gulp.task("dev:release", gulp.series("clean", "build", "e2eTest", "publish:myget-dev"));

gulp.task("master:build", gulp.series("clean", "build:release", "e2eTest", "updateGhPage"));
gulp.task("master:release", gulp.series("packAssetZip", "publish:myget-master", "publish:gh-release", "publish:gh-asset", "publish:chocolatey"));

gulp.task("default", gulp.series("dev"));
