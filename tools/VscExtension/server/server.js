/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
'use strict';
Object.defineProperty(exports, "__esModule", { value: true });
const vscode_languageserver_1 = require("vscode-languageserver");
const requestHandler = require("./handlers/requestHandler");
const httpRequestFactory_1 = require("./utilities/httpRequestFactory");
const fs = require("fs");
const path = require("path");
// Create a connection for the server. The connection uses Node's IPC as a transport
let connection = vscode_languageserver_1.createConnection(new vscode_languageserver_1.IPCMessageReader(process), new vscode_languageserver_1.IPCMessageWriter(process));
// Create a simple text document manager. The text document manager
// supports full document sync only
let documents = new vscode_languageserver_1.TextDocuments();
// Make the text document manager listen on the connection
// for open, change and close text document events
documents.listen(connection);
requestHandler.documents.listen(connection);
// After the server has started the client sends an initialize request. The server receives
// in the passed params the rootPath of the workspace plus the client capabilities. 
let workspaceRoot;
connection.onInitialize((params) => {
    workspaceRoot = params.rootPath;
    if (workspaceRoot != null) {
        let fullPath = path.join(workspaceRoot, 'docfx.json');
        if (fs.existsSync(fullPath))
            exports.docfxJson = JSON.parse(fs.readFileSync(fullPath, 'utf8'));
    }
    if (exports.docfxJson != undefined) {
        httpRequestFactory_1.httpRequestFactory.xrefService = exports.docfxJson.xrefService;
        if (httpRequestFactory_1.httpRequestFactory.xrefService != undefined)
            httpRequestFactory_1.httpRequestFactory.isDocfxProject = true;
    }
    if (httpRequestFactory_1.httpRequestFactory.isDocfxProject) {
        // This handler provides the initial list of the completion items.
        connection.onCompletion(requestHandler.completionHandler);
        connection.onDocumentLinks(requestHandler.documentLinkHandler);
        return {
            capabilities: {
                // Tell the client that the server works in FULL text document sync mode
                textDocumentSync: documents.syncKind,
                // Tell the client that the server support code complete
                completionProvider: {
                    resolveProvider: false
                    //triggerCharacters:["M"]
                },
                documentLinkProvider: {
                    resolveProvider: false
                }
            }
        };
    }
    return {
        capabilities: {
            // Tell the client that the server works in FULL text document sync mode
            textDocumentSync: documents.syncKind
        }
    };
});
// Listen on the connection
connection.listen();
//# sourceMappingURL=server.js.map