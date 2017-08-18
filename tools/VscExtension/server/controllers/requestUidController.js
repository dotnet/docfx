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
            var xrefSpecs = yield this.getData(partUid);
            xrefSpecs.forEach(element => {
                let completionItem = vscode_languageserver_1.CompletionItem.create(element.uid);
                completionItem.kind = element.type;
                completionItem.detail = element.href;
                completionItems.push(completionItem);
            });
            return completionItems;
        });
    }
    static getData(uid) {
        return __awaiter(this, void 0, void 0, function* () {
            let encodeUid = encodeURIComponent(uid);
            var data = yield httpRequestFactory_1.httpRequestFactory.getUids(encodeUid);
            return data;
        });
    }
}
exports.requestUidController = requestUidController;
//# sourceMappingURL=requestUidController.js.map