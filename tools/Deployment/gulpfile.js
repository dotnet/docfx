// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

let fs = require("fs");
let path = require("path");

let del = require("del");
let gulp = require("gulp");
let nconf = require("nconf");
let format = require("string-format");
format.extend(String.prototype, {})

let Common = require("./out/common").Common;
let Guard = require("./out/common").Guard;
let Nuget = require("./out/nuget").Nuget;
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
    "git": nconf.get("git"),
    "choco": nconf.get("choco"),
    "sync": nconf.get("sync"),
    "azdevops": nconf.get("azdevops"),
    "nuget": nconf.get("nuget"),
};

Guard.argumentNotNull(config.docfx, "config.docfx", "Can't find docfx configuration.");
Guard.argumentNotNull(config.azdevops, "config.azdevops", "Can't find Azure DevOps configuration.");
Guard.argumentNotNull(config.git, "config.git", "Can't find git configuration.");
Guard.argumentNotNull(config.choco, "config.choco", "Can't find choco configuration.");
Guard.argumentNotNull(config.nuget, "config.nuget", "Can't find nuget configuration.");

gulp.task("build", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.home, "config.docfx.home", "Can't find docfx home directory in configuration.");
    return Common.execAsync("powershell", ["./build.ps1", "-prod"], config.docfx.home);
});

gulp.task("pack", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.home, "config.docfx.home", "Can't find docfx home directory in configuration.");
    return Common.execAsync("powershell", ["./pack.ps1"], config.docfx.home);
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

gulp.task("e2eTest:buildDocfxSite", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.exe, "config.docfx.exe", "Can't find docfx.exe in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.docfxSeedHome, "config.docfx.docfxSeedHome", "Can't find docfx-seed in configuration.");

    return Common.execAsync(path.resolve(config.docfx["exe"]), [path.resolve(config.docfx.docfxJson)]);
});

gulp.task("e2eTest", gulp.series("e2eTest:restoreSeed", "e2eTest:buildSeed", "e2eTest:buildDocfxSite"));

gulp.task("publish:azdevops-ppe-login", () => {
    return Common.execAsync(process.env.NUGETEXE, ["sources", "add", "-name", "docs-build-v2-ppe", "-source", config.azdevops["ppeUrl"], "-username", "anything", "-password", process.env.AZDEVOPSPAT]);
})

gulp.task("publish:azdevops-ppe", () => {
    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);
    return Nuget.publishAsync(artifactsFolder, process.env.NUGETEXE, "anything", config.azdevops["ppeUrl"]);
});

gulp.task("publish:azdevops-prod-login", () => {
    return Common.execAsync(process.env.NUGETEXE, ["sources", "add", "-name", "docs-build-v2-prod", "-source", config.azdevops["prodUrl"], "-username", "anything", "-password", process.env.AZDEVOPSPAT]);
})

gulp.task("publish:azdevops-prod", () => {
    let releaseNotePath = path.resolve(config.docfx["releaseNotePath"]);
    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);

    return Nuget.publishAsync(artifactsFolder, process.env.NUGETEXE, "anything", config.azdevops["prodUrl"], releaseNotePath);
});

gulp.task("publish:nuget", () => {
    let releaseNotePath = path.resolve(config.docfx["releaseNotePath"]);
    let artifactsFolder = path.resolve(config.docfx["artifactsFolder"]);

    return Nuget.publishAsync(artifactsFolder, process.env.NUGETEXE, process.env.NUGETAPIKEY, config.nuget["nuget.org"], releaseNotePath);
});

gulp.task("updateGhPage", () => {
    Guard.argumentNotNullOrEmpty(config.docfx.httpsRepoUrl, "config.docfx.httpsRepoUrl", "Can't find docfx repo url in configuration.");
    Guard.argumentNotNullOrEmpty(config.docfx.siteFolder, "config.docfx.siteFolder", "Can't find docfx site folder in configuration.");
    Guard.argumentNotNullOrEmpty(config.git.name, "config.git.name", "Can't find git user name in configuration");
    Guard.argumentNotNullOrEmpty(config.git.email, "config.git.email", "Can't find git user email in configuration");
    Guard.argumentNotNullOrEmpty(config.git.message, "config.git.message", "Can't find git commit message in configuration");
    Guard.argumentNotNullOrEmpty(process.env.TOKEN, "process.env.TOKEN", "No github account token in the environment.");

    return Github.updateGhPagesAsync(config.docfx.httpsRepoUrl, config.docfx.siteFolder, config.git.name, config.git.email, config.git.message, process.env.TOKEN);
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
gulp.task("dev", gulp.series("clean", "build", "e2eTest"));
gulp.task("dev:build", gulp.series("clean", "build", "e2eTest"));
gulp.task("dev:release", gulp.series("pack", "publish:azdevops-ppe-login", "publish:azdevops-ppe"));

gulp.task("main:build", gulp.series("clean", "build:release", "e2eTest", "updateGhPage"));
gulp.task("main:pack", gulp.series("pack"));
gulp.task("main:release", gulp.series("packAssetZip", "publish:azdevops-prod-login", "publish:azdevops-prod", "publish:nuget", "publish:gh-release", "publish:gh-asset", "publish:chocolatey"));

gulp.task("default", gulp.series("dev"));
