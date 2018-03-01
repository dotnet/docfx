// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, ExtensionContext, TextDocumentContentProvider, EventEmitter, Event, Uri } from "vscode";
import * as path from "path";

export class ContentProvider implements TextDocumentContentProvider {
    public static port = "4001";

    protected _content: string;

    private _context: ExtensionContext;
    private _onDidChange = new EventEmitter<Uri>();

    constructor(context: ExtensionContext) {
        this._context = context;
    }

    public provideTextDocumentContent(uri: Uri): Thenable<string> {
        return workspace.openTextDocument(Uri.parse(uri.query)).then(document => {
            return "";
        });
    }

    get onDidChange(): Event<Uri> {
        return this._onDidChange.event;
    }

    public update(uri: Uri, content: string) {
        this._content = content;
        this._onDidChange.fire(uri);
    }

    protected getMediaJsPath(mediaFile: string): string {
        return this._context.asAbsolutePath(path.join("media", "js", mediaFile));
    }

    protected getMediaCssPath(mediaFile: string): string {
        return this._context.asAbsolutePath(path.join("media", "css", mediaFile));
    }

    protected getNodeModulesPath(resourceName: string): string {
        return this._context.asAbsolutePath(path.join("node_modules", resourceName));
    }
}
