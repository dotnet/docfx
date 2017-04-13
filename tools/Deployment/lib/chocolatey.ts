// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from "fs";

import { Common } from "./common";

export class Chocolatey {
    public static async publishToChocolateyAsync(releaseNotePath: string, assetZipPath: string, chocoScriptPath: string, chocoNuspecPath: string, chocoHomeDir: string, chocoToken: string): Promise<void> {
        // TODO: Ignore to publish chocolatey package if RELEASENOTE.md hasn't been modified.
        let version = Common.getVersionFromReleaseNote(releaseNotePath);
        let sha1 = Common.computeSha1FromZip(assetZipPath);
        let nupkgName = `docfx.${version}.nupkg`;

        this.updateChocoConfig(chocoScriptPath, chocoNuspecPath, version, sha1);
        await this.chocoPackAsync(chocoHomeDir);
        await this.prepareChocoAsync(chocoHomeDir, chocoToken);

        return Common.exec("choco", ["push", nupkgName], chocoHomeDir);
    }

    private static async chocoPackAsync(homeDir: string): Promise<void> {
        return Common.exec("choco", ['pack'], homeDir);
    }

    private static async prepareChocoAsync(homeDir: string, chocoToken: string): Promise<void> {
        return Common.exec("choco", ["apiKey", "-k", chocoToken, "-source", "https://chocolatey.org/", homeDir]);
    }

    private static updateChocoConfig(scriptPath: string, nuspecPath: string, version: string, sha1: string): void {
        let chocoScriptContent = fs.readFileSync(scriptPath, "utf8");
        chocoScriptContent = chocoScriptContent
            .replace(/v[\d\.]+/, "v" + version)
            .replace(/(\$sha1\s*=\s*['"])([\d\w]+)(['"])/, `$1${sha1}$2`);
        fs.writeFileSync(scriptPath, chocoScriptContent);

        let nuspecContent = fs.readFileSync(nuspecPath, "utf8");
        nuspecContent = nuspecContent.replace(/(<version>)[\d\.]+(<\/version>)/, `$1${version}$2`);
        fs.writeFileSync(nuspecPath, nuspecContent);
    }
}
