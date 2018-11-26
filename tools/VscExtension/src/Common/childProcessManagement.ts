// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as childProcess from "child_process";

export class ChildProcessManagement {
    static spawn(command: string, options) : childProcess.ChildProcess {
        let file, args;
        if (process.platform === 'win32') {
            file = 'cmd.exe';
            // execute chcp 65001 to make sure console's code page supports UTF8
            // https://github.com/nodejs/node-v0.x-archive/issues/2190
            args = ['/s', '/c', '"chcp 65001 >NUL & ' + command + '"'];
            options = Object.assign({}, options);
            options.windowsVerbatimArguments = true;
        }
        else {
            file = '/bin/sh';
            args = ['-c', 'chmod 777 ', command];
        }
        return childProcess.spawn(file, args, options);
    };
}
