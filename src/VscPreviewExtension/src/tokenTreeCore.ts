// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri, ExtensionContext } from "vscode";
import { TokenTreeContentProvider } from "./tokenTreeContentProvider";
import { ChildProcessHost } from "./childProcessHost"

// Create a child process(DfmRender) by "_spawn" to render a html
export class TokenTreeCore extends ChildProcessHost {
    public provider: TokenTreeContentProvider;

    protected initializeProvider(context: ExtensionContext) {
        this.provider = new TokenTreeContentProvider(context);
    }

    protected writeToStdin(rootPath, filePath, numOfRow, docContent) {
        this._spawn.stdin.write(this.appendWrap("jsonmarkup"));
        // this._spawn.stdin.write(this.appendWrap(rootPath));
        // this._spawn.stdin.write(this.appendWrap(filePath));
        this._spawn.stdin.write(this.appendWrap(numOfRow));
        this._spawn.stdin.write(this.appendWrap(docContent));
    }
}