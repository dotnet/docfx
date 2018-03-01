// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as axios from "axios";
import { Guard } from "./common";

export class GithubApi {
    private readonly request;
    private readonly userAndRepo;

    constructor(repoUrl: string, token: string) {
        this.userAndRepo = this.getUserAndRepo(repoUrl);
        this.request = axios.create({
            baseURL: "https://api.github.com",
            headers: {
                'User-Agent': 'axios',
                'Authorization': `token ${token}`
            }
        });
    }

    async publishReleaseAndAssetAsync(releaseDescription: ReleaseDescription, assetInfo: AssetInfo) {
        Guard.argumentNotNull(releaseDescription, "releaseDescription");
        Guard.argumentNotNull(assetInfo, "assetInfo");

        await this.publishReleaseAsync(releaseDescription);
        return this.publishAssetAsync(assetInfo);
    }

    // publish asset to latest release
    async publishAssetAsync(info: AssetInfo) {
        const latestReleaseInfo = await this.getLatestReleaseAsync();
        if (latestReleaseInfo.data["assets"]) {
            const assets = latestReleaseInfo.data["assets"];
            assets.forEach(async item => {
                if (item["name"] === info.name) {
                    await this.deleteAssetByUrlAsync(item["url"]);
                }
            });
        }
        return this.uploadAssetAsync(latestReleaseInfo.data["id"], info);
    }

    // publish release
    async publishReleaseAsync(description: ReleaseDescription) {
        let latestReleaseInfo;
        try {
            latestReleaseInfo = await this.getLatestReleaseAsync();
        } catch (err) {
            // no release exists
            if (err.response.status === 404) {
                return this.createReleaseAsync(description);
            }
        }
        if (latestReleaseInfo.data["tag_name"] === description.tag_name) {
            return this.updateReleaseAsync(latestReleaseInfo.data["id"], description);
        } else {
            return this.createReleaseAsync(description);
        }
    }

    async deleteLatestReleaseAsync(releaseId: string) {
        const releaseInfo = await this.getLatestReleaseAsync();
        const config = {
            url: `/repos/${this.userAndRepo}/releases/${releaseInfo.data["id"]}`,
            method: "DELETE",
        };
        return this.request(config);
    }

    private async createReleaseAsync(releaseInfo: ReleaseDescription) {
        const config = {
            url: `/repos/${this.userAndRepo}/releases`,
            method: "POST",
            data: releaseInfo
        };
        return this.request(config);
    }

    private async getLatestReleaseAsync() {
        const config = {
            url: `/repos/${this.userAndRepo}/releases/latest`,
            method: "GET",
        };
        return this.request(config);
    }

    private async updateReleaseAsync(releaseId: string, description: ReleaseDescription) {
        const config = {
            url: `/repos/${this.userAndRepo}/releases/${releaseId}`,
            method: "PATCH",
            data: description
        };
        return this.request(config);
    }

    private async deleteReleaseAsync(releaseId: string) {
        const config = {
            url: `/repos/${this.userAndRepo}/releases/${releaseId}`,
            method: "DELETE",
        };
        return this.request(config);
    }

    private async uploadAssetAsync(releaseId: string, info: AssetInfo) {
        const config = {
            url: `/repos/${this.userAndRepo}/releases/${releaseId}/assets?name=${info.name}`,
            baseURL: "https://uploads.github.com/",
            method: "POST",
            headers: {
                'Content-Type': info.contentType,
            },
            data: info.data
        }
        return this.request(config);
    }

    private async deleteAssetByUrlAsync(assetUrl: string) {
        const config = {
            url: assetUrl,
            method: "DELETE",
        }
        return this.request(config);
    }

    private getUserAndRepo(repoUrl: string): string {
        const regex = /^git@(.+):(.+?)(\.git)?$/;
        let match = regex.exec(repoUrl);

        if (!match || match.length < 3) {
            throw new Error(`Can't parse ${repoUrl}`);
        }

        return match[2];
    }
}

export interface ReleaseDescription {
    tag_name: string;
    target_commitish?: string;
    name?: string;
    body?: string;
    draft?: string;
    prelease?: boolean;
}

export interface AssetInfo {
    contentType: string;
    name: string;
    data: ArrayBuffer;
    lable?: string;
}