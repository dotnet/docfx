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
        let lineStartPosition = vscode_languageserver_1.Position.create(textDocumentPosition.position.line, 0);
        var startPos = exports.documents.get(textDocumentPosition.textDocument.uri).offsetAt(lineStartPosition);
        var endPos = exports.documents.get(textDocumentPosition.textDocument.uri).offsetAt(textDocumentPosition.position);
        var line = text.substring(startPos, endPos);
        const regEx = /((@[^ \r\n>]+)$)|((<xref:[^ \r\n>]+)$)/g;
        let match = regEx.exec(line);
        let uid;
        if (match != null) {
            if (match[0][0] == '@') {
                uid = match[0].substr(1);
            }
            else {
                uid = match[0].substr(6);
            }
        }
        if (completionItem == undefined) {
            completionItem = yield requestUidController_1.requestUidController.getCompletionItem(uid);
            return completionItem;
        }
        else {
            completionItem = completionItem.filter(item => item.label.includes(uid));
            if (completionItem.length < 10) {
                completionItem = yield requestUidController_1.requestUidController.getCompletionItem(uid);
            }
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
            let uid;
            if (text[match.index] == '@') {
                uid = text.substr(match.index + 1, match[0].length - 1);
            }
            else {
                uid = text.substr(match.index + 6, match[0].length - 7);
            }
            let xrefSpecs = yield requestUidController_1.requestUidController.getData(uid);
            if (xrefSpecs != undefined && xrefSpecs.length > 0 && xrefSpecs[0].uid == uid) {
                let documentLink = vscode_languageserver_1.DocumentLink.create(vscode_languageserver_1.Range.create(startPos, endPos), xrefSpecs[0].href);
                documentLinks.push(documentLink);
            }
        }
        return documentLinks;
    });
}
exports.documentLinkHandler = documentLinkHandler;
//# sourceMappingURL=requestHandler.js.map