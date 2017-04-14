// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as glob from "glob";

import { Common, Guard } from "./common";

export class Myget {
    static publishToMyget(artifactsFolder: string, mygetCommand: string, mygetKey: string, mygetUrl: string) {
        Guard.argumentNotNullOrEmpty(artifactsFolder, "artifactsFolder");
        Guard.argumentNotNullOrEmpty(mygetCommand, "mygetCommand");
        Guard.argumentNotNullOrEmpty(mygetKey, "mygetKey");
        Guard.argumentNotNullOrEmpty(mygetUrl, "mygetUrl");

        let packages = glob.sync(artifactsFolder + "/**/!(*.symbols).nupkg");
        let promises = packages.map(p => {
            return Common.execAsync(mygetCommand, ["push", p, mygetKey, "-Source", mygetUrl]);
        });

        return Promise.all(promises);
    }
}