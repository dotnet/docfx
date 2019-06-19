// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from "fs-extra";
import * as path from "path";

import { Common, Guard } from "./common";
import { GithubApi, AssetInfo, ReleaseDescription } from "./githubApi";

export class Github {
    static async updateGithubReleaseAsync(
        repoUrl: string,
        releaseNotePath: string,
        githubToken: string): Promise<void> {

        Guard.argumentNotNullOrEmpty(repoUrl, "repoUrl");
        Guard.argumentNotNullOrEmpty(releaseNotePath, "releaseNotePath");
        Guard.argumentNotNullOrEmpty(githubToken, "githubToken");

        let isUpdated = await Common.isReleaseNoteVersionChangedAsync(releaseNotePath);
        if (!isUpdated) {
            console.log(`${releaseNotePath} hasn't been changed. Ignored to update github release package.`);
            return Promise.resolve();
        }

        let githubApi = new GithubApi(repoUrl, githubToken);
        let releaseDescription = this.getReleaseDescription(releaseNotePath);

        return githubApi.publishReleaseAsync(releaseDescription);
    }

    static async updateGithubAssetAsync(
        repoUrl: string,
        assetZipPath: string,
        githubToken: string): Promise<void> {

        Guard.argumentNotNullOrEmpty(repoUrl, "repoUrl");
        Guard.argumentNotNullOrEmpty(assetZipPath, "assetZipPath");
        Guard.argumentNotNullOrEmpty(githubToken, "githubToken");
        // TODO: add check: if this zip has been publish, skip this step

        let githubApi = new GithubApi(repoUrl, githubToken);
        let assetInfo = this.getAssetZipInfo(assetZipPath);

        return githubApi.publishAssetAsync(assetInfo);
    }

    static async updateGhPagesAsync(
        repoUrl: string,
        siteFolder: string,
        docfxExe: string,
        docfxJson: string,
        gitUserName: string,
        gitUserEmail: string,
        gitCommitMessage: string,
        githubToken: string) {

        Guard.argumentNotNullOrEmpty(repoUrl, "repoUrl");
        Guard.argumentNotNullOrEmpty(siteFolder, "siteFolder");
        Guard.argumentNotNullOrEmpty(docfxExe, "docfxExe");
        Guard.argumentNotNullOrEmpty(docfxJson, "docfxJson");
        Guard.argumentNotNullOrEmpty(gitUserName, "gitUserName");
        Guard.argumentNotNullOrEmpty(gitUserEmail, "gitUserEmail");
        Guard.argumentNotNullOrEmpty(gitCommitMessage, "gitCommitMessage");
        Guard.argumentNotNullOrEmpty(githubToken, "githubToken");

        await Common.execAsync(docfxExe, [docfxJson]);

        let branch = "gh-pages";
        let targetDir = "docfxsite";

        this.cleanGitInfo(siteFolder);

        await Common.execAsync("git", ["clone", repoUrl, "-b", branch, targetDir]);
        fs.mkdirsSync(path.join(siteFolder, ".git"));
        fs.copySync(path.join(targetDir, ".git"), path.join(siteFolder, ".git"));

        await Common.execAsync("git", ["config", "user.name", gitUserName], siteFolder);
        await Common.execAsync("git", ["config", "user.email", gitUserEmail], siteFolder);
        await Common.execAsync("git", ["add", "."], siteFolder);
        await Common.execAsync("git", ["commit", "-m", gitCommitMessage], siteFolder);
        
        var repoUrlWithToken = "https://dotnet:" + githubToken + "@" + repoUrl.substring("https://".length);
        await Common.execAsync("git", ["remote", "set-url", "origin", repoUrlWithToken], siteFolder);
        return Common.execAsync("git", ["push", "origin", branch], siteFolder);
    }

    private static getReleaseDescription(releaseNotePath: string): ReleaseDescription {
        let version = Common.getVersionFromReleaseNote(releaseNotePath);
        let description = Common.getDescriptionFromReleaseNote(releaseNotePath);
        let releaseDescription = {
            "tag_name": `v${version}`,
            "target_commitish": "master",
            "name": `Version ${version}`,
            "body": description
        };

        return releaseDescription;
    }

    private static getAssetZipInfo(assetPath: string, assetName = null): AssetInfo {
        let data = fs.readFileSync(assetPath);
        let arrayBuffer = new Uint8Array(data).buffer;
        let assetInfo = {
            "contentType": "application/zip",
            "name": assetName || path.basename(assetPath),
            "data": arrayBuffer
        };

        return assetInfo;
    }

    private static cleanGitInfo(repoRootFolder: string) {
        let gitFolder = path.join(repoRootFolder, ".git");
        fs.removeSync(gitFolder);
    }
}