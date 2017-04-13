// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from "fs-extra";
import * as path from "path";

import { Common } from "./common";
import { GithubApi, AssetInfo, ReleaseDescription } from "./githubApi";

export class Github {
    static updateGithubReleaseAsync(repoUrl: string, releaseNotePath: string, releaseFolder: string, assetZipPath: string, githubToken: string) {
        Common.zipAssests(releaseFolder, assetZipPath);

        let githubApi = new GithubApi(repoUrl, githubToken);
        let releaseDescription = this.getReleaseDescription(releaseNotePath);

        let data = fs.readFileSync(releaseNotePath);
        let arrayBuffer = new Uint8Array(data).buffer;
        let assetInfo = this.getAssetZipInfo(assetZipPath);

        githubApi.publishReleaseAndAssetAsync(releaseDescription, assetInfo)
            .then(() => {
                console.log("publish release and asset successful");
            })
            .catch((err) => {
                console.error(err);
            });
    }

    private static getReleaseDescription(releaseNotePath: string): ReleaseDescription {
        let version = Common.getVersionFromReleaseNote(releaseNotePath);
        let description = Common.getDescriptionFromReleaseNote(releaseNotePath);
        let releaseDescription = {
            "tag_name": version,
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

    static async updateGhPagesAsync(repoUrl: string, siteFolder: string, gitUserName: string, gitUserEmail: string, gitCommitMessage: string) {
        let branch = "gh-pages";
        let targetDir = "docfxsite";

        this.cleanGitInfo(siteFolder);

        // await Common.exec("git", ["clone", repoUrl, "-b", branch, targetDir]);
        fs.mkdirsSync(path.join(siteFolder, ".git"));
        fs.copySync(path.join(targetDir, ".git"), path.join(siteFolder, ".git"));

        await Common.exec("git", ["config", "user.name", gitUserName], siteFolder);
        await Common.exec("git", ["config", "user.email", gitUserEmail], siteFolder);
        await Common.exec("git", ["add", "."], siteFolder);
        await Common.exec("git", ["commit", "-m", gitCommitMessage], siteFolder);
        return Common.exec("git", ["push", "origin", branch], siteFolder);
    }

    private static cleanGitInfo(repoRootFolder: string) {
        let gitFolder = path.join(repoRootFolder, ".git");
        fs.removeSync(gitFolder);
    }
}