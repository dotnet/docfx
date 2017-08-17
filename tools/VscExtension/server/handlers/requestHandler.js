"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
const vscode_languageserver_1 = require("vscode-languageserver");
const requestUidController_1 = require("../controllers/requestUidController");
exports.documents = new vscode_languageserver_1.TextDocuments();
function completionHandler(textDocumentPosition) {
    return __awaiter(this, void 0, void 0, function* () {
        // The pass parameter contains the position of the text document in 
        // which code complete got requested. For the example we ignore this
        // info and always provide the same completion items.
        //console.log("hehe:"+textDocumentPosition.textDocument.uri);
        var text = exports.documents.get(textDocumentPosition.textDocument.uri).getText();
        var tx = text.substring(text.lastIndexOf("@") + 1);
        // var f = vscode.workspace.openTextDocument;
        // var t = f(textDocumentPosition.textDocument.uri);
        // .then(document=>{
        // 	let text = document.getText(document.lineAt(textDocumentPosition.position.line).range);
        // 	var tx = text.substring(text.lastIndexOf("@")+1);
        // });
        var data = yield requestUidController_1.requestUidController.getCompletionItem(tx);
        console.log("data::");
        return data;
        // return [
        // 	{
        // 		label: 'TypeScript',
        // 		kind: CompletionItemKind.Text,
        // 		data: 1
        // 	},
        // 	{
        // 		label: 'JavaScript',
        // 		kind: CompletionItemKind.Text,
        // 		data: 2
        // 	}
        // ]
    });
}
exports.completionHandler = completionHandler;
function highlightHandler(textDocumentPosition) {
    return __awaiter(this, void 0, void 0, function* () {
        console.log("highlight");
        const regEx = /(@([^ \r\n>]+))|(<xref:([^ \r\n>]+)>)/g;
        let textDocument = exports.documents.get(textDocumentPosition.textDocument.uri);
        const text = textDocument.getText();
        let match;
        let documentHighlight = [];
        while (match = regEx.exec(text)) {
            const startPos = textDocument.positionAt(match.index);
            const endPos = textDocument.positionAt(match.index + match[0].length);
            let highLight = vscode_languageserver_1.DocumentHighlight.create(vscode_languageserver_1.Range.create(startPos, endPos), 2);
            documentHighlight.push(highLight);
        }
        return documentHighlight;
    });
}
exports.highlightHandler = highlightHandler;
function documentLinkHandler(documentLinkParams) {
    return __awaiter(this, void 0, void 0, function* () {
        const regEx = /(@([^ \r\n>]+))|(<xref:([^ \r\n>]+)>)/g;
        let textDocument = exports.documents.get(documentLinkParams.textDocument.uri);
        const text = textDocument.getText();
        let match;
        let documentLinks = [];
        while (match = regEx.exec(text)) {
            const startPos = textDocument.positionAt(match.index);
            const endPos = textDocument.positionAt(match.index + match[0].length);
            let tx = textDocument.getText();
            let uid;
            if (tx[match.index] == '@') {
                uid = tx.substr(match.index + 1, match[0].length - 1);
            }
            else {
                uid = tx.substr(match.index + 6, match[0].length - 7);
            }
            console.log("docunmentLink:" + uid + ":" + regEx.lastIndex);
            let xrefSpecs = yield requestUidController_1.requestUidController.getData('http://xrefservice0810.azurewebsites.net/uids/', uid);
            if (xrefSpecs.length > 0) {
                let documentLink = vscode_languageserver_1.DocumentLink.create(vscode_languageserver_1.Range.create(startPos, endPos), xrefSpecs[0].href);
                documentLinks.push(documentLink);
            }
        }
        return documentLinks;
    });
}
exports.documentLinkHandler = documentLinkHandler;
//# sourceMappingURL=requestHandler.js.map