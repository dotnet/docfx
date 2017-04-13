// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as glob from "glob";

import { Common } from "./common";

export class Myget {
    static publishToMyget(artifactsFolder, mygetCommand, mygetKey, mygetUrl) {
        let packages = glob.sync(artifactsFolder + "/**/!(*.symbols).nupkg");
        let promises = packages.map(p => {
            return Common.exec(mygetCommand, ["push", p, mygetKey, "-Source", mygetUrl]);
        });
        return Promise.all(promises);
    }
}