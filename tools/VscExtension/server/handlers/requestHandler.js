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
let completionItem;
exports.documents = new vscode_languageserver_1.TextDocuments();
function completionHandler(textDocumentPosition) {
    return __awaiter(this, void 0, void 0, function* () {
        var text = exports.documents.get(textDocumentPosition.textDocument.uri).getText();
        var tx = text.substring(text.lastIndexOf("@") + 1);
        if (completionItem == undefined) {
            completionItem = yield requestUidController_1.requestUidController.getCompletionItem(tx);
            console.log("completion == undefined " + tx);
            return completionItem;
        }
        else {
            completionItem = completionItem.filter(item => item.label.includes(tx));
            if (completionItem.length < 10) {
                completionItem = yield requestUidController_1.requestUidController.getCompletionItem(tx);
            }
            console.log("completion > 10 " + tx);
            return completionItem;
        }
    });
}
exports.completionHandler = completionHandler;
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
            console.log("docunmentLink:hh" + uid + ":" + regEx.lastIndex);
            let xrefSpecs = yield requestUidController_1.requestUidController.getData(uid);
            if (xrefSpecs != undefined && xrefSpecs.length > 0) {
                let documentLink = vscode_languageserver_1.DocumentLink.create(vscode_languageserver_1.Range.create(startPos, endPos), xrefSpecs[0].href);
                documentLinks.push(documentLink);
            }
        }
        return documentLinks;
    });
}
exports.documentLinkHandler = documentLinkHandler;
//# sourceMappingURL=requestHandler.js.map