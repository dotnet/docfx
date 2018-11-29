// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Common } from "./common";

export class SyncBranch {
    static async runAsync(repoUrl: string, docfxHomeDir: string, fromBranch: string, targetBranch: string): Promise<void> {
        await Common.execAsync("git", ["remote", "set-url", "origin", repoUrl], docfxHomeDir);
        await Common.execAsync("git", ["push", "origin", "origin/" + fromBranch + ":" + targetBranch], docfxHomeDir);
        console.log("Sync successfully");
    }
}
