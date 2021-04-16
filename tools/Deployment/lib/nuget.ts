// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as glob from "glob";

import { Common, Guard } from "./common";

export class Nuget {
    static async publishAsync(
        artifactsFolder: string,
        nugetPath: string,
        token: string,
        url: string,
        releaseNotePath = null): Promise<void> {

        Guard.argumentNotNullOrEmpty(artifactsFolder, "artifactsFolder");
        Guard.argumentNotNullOrEmpty(nugetPath, "nugetPath");
        Guard.argumentNotNullOrEmpty(url, "url");

        if (releaseNotePath) {
            // Ignore to publish nuget package if RELEASENOTE.md hasn't been modified.
            let isUpdated = await Common.isReleaseNoteVersionChangedAsync(releaseNotePath);
            if (!isUpdated) {
                console.log(`${releaseNotePath} hasn't been changed. Ignore to publish package.`);
                return Promise.resolve();
            }
        }

        let packages = glob.sync(artifactsFolder + "/**/!(*.symbols).nupkg");
        let promises = packages.map((p: string) => {
            return Common.execAsync(nugetPath, ["push", p, token, "-Source", url, "-SkipDuplicate"]);
        });

        await Promise.all(promises);
        return Promise.resolve();
    }
}