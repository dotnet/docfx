// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from "fs";
import * as path from "path";

import * as cp from "child-process-promise";
import * as jszip from "jszip";
import * as sha1 from "sha1";
import * as moment from "moment-timezone";

export class Common {
    static async execAsync(command: string, args: Array<string>, workDir = null): Promise<void> {
        Guard.argumentNotNullOrEmpty(command, "command");
        Guard.argumentNotNull(args, "args");

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

        let promise = cp.spawn(command, args);
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
        Guard.argumentNotNullOrEmpty(assetFolder, "assetFolder");
        Guard.argumentNotNullOrEmpty(targetPath, "targetPath");

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

    static computeSha1FromZip(zipPath: string): string {
        Guard.argumentNotNullOrEmpty(zipPath, "zipPath");

        if (!fs.existsSync(zipPath)) {
            throw new Error(`${zipPath} doesn't exist.`);
        }

        let buffer = fs.readFileSync(zipPath);
        return sha1(buffer);
    }

    static getVersionFromReleaseNote(releaseNotePath: string): string {
        Guard.argumentNotNullOrEmpty(releaseNotePath, "releaseNotePath");

        if (!fs.existsSync(releaseNotePath)) {
            throw new Error(`${releaseNotePath} doesn't exist.`);
        }

        let regex = /\(Current\s+Version:\s+v([\d\.]+)\)/i;
        let content = fs.readFileSync(releaseNotePath, "utf8");

        let match = regex.exec(content);
        if (!match || match.length < 2) {
            throw new Error(`Can't parse version from ${releaseNotePath} in current version part.`);
        }

        return match[1].trim();
    }

    static getDescriptionFromReleaseNote(releaseNotePath: string): string {
        Guard.argumentNotNullOrEmpty(releaseNotePath, "releaseNotePath");

        if (!fs.existsSync(releaseNotePath)) {
            throw new Error(`${releaseNotePath} doesn't exist.`);
        }

        let regex = /\n\s*v[\d\.]+\s*\r?\n-{3,}\r?\n([\s\S]+?)(?:\r?\n\s*v[\d\.]+\s*\r?\n-{3,}|$)/i;
        let content = fs.readFileSync(releaseNotePath, "utf8");

        let match = regex.exec(content);
        if (!match || match.length < 2) {
            throw new Error(`Can't parse description from ${releaseNotePath} in current version part.`);
        }

        return match[1].trim();
    }

    static async isReleaseNoteVersionChangedAsync(releaseNotePath: string): Promise<boolean> {
        let versionFromTag = await this.getCurrentVersionFromGitTag();
        let versionFromReleaseNote = this.getVersionFromReleaseNote(releaseNotePath);

        return `v${versionFromReleaseNote}`.toLowerCase() !== versionFromTag.toLowerCase();
    }

    static async getCurrentVersionFromGitTag(): Promise<string> {
        let result = await cp.exec("git describe --abbrev=0 --tags");
        let content = result.stdout.trim();
        if (!content) {
            return null;
        }

        return content;
    }

    static isThirdWeekInSprint(): boolean {
        let baseMoment = moment("2016-12-12").tz("Asia/Shanghai");
        let gap = moment().tz("Asia/Shanghai").diff(baseMoment, "weeks");
        return gap % 3 === 2;
    }
}

export class Guard {
    static argumentNotNull(argumentValue: Object, argumentName: string, message = null) {
        if (argumentValue === null || argumentValue === undefined) {
            message = message || `${argumentName} can't be null/undefined.`;
            throw new Error(message);
        }
    }

    static argumentNotNullOrEmpty(stringValue: string, argumentName: string, message = null) {
        if (stringValue === null || stringValue == undefined || stringValue === "") {
            message = message || `${argumentName} can't be null/undefined or empty string.`;
            throw new Error(message);
        }
    }
}