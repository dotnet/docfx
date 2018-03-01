// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as glob from "glob";

import { Common, Guard } from "./common";

export class Myget {
    static async publishToMygetAsync(
        artifactsFolder: string,
        mygetCommand: string,
        mygetKey: string,
        mygetUrl: string,
        releaseNotePath = null): Promise<void> {

        Guard.argumentNotNullOrEmpty(artifactsFolder, "artifactsFolder");
        Guard.argumentNotNullOrEmpty(mygetCommand, "mygetCommand");
        Guard.argumentNotNullOrEmpty(mygetKey, "mygetKey");
        Guard.argumentNotNullOrEmpty(mygetUrl, "mygetUrl");

        if (releaseNotePath) {
            // Ignore to publish myget package if RELEASENOTE.md hasn't been modified.
            let isUpdated = await Common.isReleaseNoteVersionChangedAsync(releaseNotePath);
            if (!isUpdated) {
                console.log(`${releaseNotePath} hasn't been changed. Ignore to publish package to myget.org.`);
                return Promise.resolve();
            }
        }

        let packages = glob.sync(artifactsFolder + "/**/!(*.symbols).nupkg");
        let promises = packages.map(p => {
            return Common.execAsync(mygetCommand, ["push", p, mygetKey, "-Source", mygetUrl]);
        });

        await Promise.all(promises);
        return Promise.resolve();
    }
}