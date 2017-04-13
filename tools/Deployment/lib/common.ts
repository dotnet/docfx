// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from "fs";
import * as path from "path";

import * as childProcess from "child-process-promise";
import * as jszip from "jszip";
import * as sha1 from "sha1";

let spawn = childProcess.spawn;

export class Common {
    static async exec(command, args, workDir = null) {
        let cwd = process.cwd();
        if (workDir) {
            if (!path.isAbsolute(workDir)) {
                workDir = path.join(cwd, workDir);
            }
            if (!fs.existsSync(workDir)) {
                throw new Error(`Can't find ${workDir}.`);
            }

            process.chdir(workDir);
        }

        let promise = spawn(command, args);
        let childProcess = promise.childProcess;
        childProcess.stdout.on("data", (data) => {
            process.stdout.write(data.toString());
        });
        childProcess.stderr.on("data", (data) => {
            process.stderr.write(data.toString());
        })
        return promise.then(() => {
            process.chdir(cwd);
        });
    }

    static zipAssests(assetFolder: string, targetPath: string) {
        let zip = new jszip();

        fs.readdirSync(assetFolder).forEach(file => {
            let filePath = path.join(assetFolder, file);
            if (fs.lstatSync(filePath).isFile()) {
                let ext = path.extname(filePath);
                if (ext !== '.xml' && ext !== '.pdb') {
                    let content = fs.readFileSync(filePath);
                    zip.file(file, content);
                }
            }
        });

        let buffer = zip.generate({ type: "nodebuffer", compression: "DEFLATE" });

        if (fs.existsSync(targetPath)) {
            fs.unlinkSync(targetPath);
        }

        fs.writeFileSync(targetPath, buffer);
    }

    static computeSha1FromZip(zipPath) {
        if (!zipPath) {
            throw new Error(`${zipPath} can't null or undefined.`);
        }
        if (!fs.existsSync(zipPath)) {
            throw new Error(`${zipPath} doesn't exist.`);
        }

        let buffer = fs.readFileSync(zipPath);
        return sha1(buffer);
    }

    static getVersionFromReleaseNote(releaseNotePath): string {
        if (!fs.existsSync(releaseNotePath)) {
            throw new Error(`${releaseNotePath} doesn't exist.`);
        }

        let regex = /\(Current\s+Version:\s+[vV]([\d\.]+)\)/;
        let content = fs.readFileSync(releaseNotePath, "utf8");

        let match = regex.exec(content);
        if (!match || match.length < 2) {
            throw new Error(`Can't parse version from ${releaseNotePath} in current version part.`);
        }

        return match[1].trim();
    }

    static getDescriptionFromReleaseNote(releaseNotePath): string {
        if (!fs.existsSync(releaseNotePath)) {
            throw new Error(`${releaseNotePath} doesn't exist.`);
        }

        let regex = /---{3,}\r?\n([\s\S]+?)[vV][\d\.]+\r?\n---{3,}\r?\n/;
        let content = fs.readFileSync(releaseNotePath, "utf8");

        let match = regex.exec(content);
        if (!match || match.length < 2) {
            throw new Error(`Can't parse description from ${releaseNotePath} in current version part.`);
        }

        return match[1].trim();
    }

    static isReleaseNoteUpdated(releaseNotePath) {
        // TODO: implement
        throw new Error("Not implemented.");
    }
}
