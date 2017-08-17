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
const httpRequestFactory_1 = require("../utilities/httpRequestFactory");
class requestUidController {
    static getCompletionItem(partUid) {
        return __awaiter(this, void 0, void 0, function* () {
            let completionItems = [];
            // var text = document.getText(document.lineAt(position).range);
            // var tx = text.substring(text.lastIndexOf("@")+1);
            // if(tx.length > 1 && tx[0] == ' '){
            //     var completionItems = [];
            // }
            var re = yield this.getData('http://xrefservice0810.azurewebsites.net/intellisense/', partUid);
            //console.log(re);
            re.forEach(function (element) {
                let completionItem = vscode_languageserver_1.CompletionItem.create(element.uid);
                completionItem.kind = element.type;
                //completionItem.commitCharacters = ["c","s"];
                completionItem.detail = element.href;
                //completionItem.filterText = "bbb";
                //completionItem.insertText = new vscode.SnippetString(element);
                completionItems.push(completionItem);
            });
            return completionItems;
        });
    }
    static getData(url, uid) {
        return __awaiter(this, void 0, void 0, function* () {
            let encodeUid = encodeURIComponent(uid);
            var data = yield httpRequestFactory_1.httpRequestFactory.getUids(url, encodeUid);
            return data;
        });
    }
}
exports.requestUidController = requestUidController;
//# sourceMappingURL=requestUidController.js.map