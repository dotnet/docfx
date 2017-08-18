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
const axios_1 = require("axios");
class httpRequestFactory {
    static getUids(uid) {
        return __awaiter(this, void 0, void 0, function* () {
            if (httpRequestFactory.xrefService != undefined) {
                for (var i = 0; i < httpRequestFactory.xrefService.length; i++) {
                    try {
                        let promise = yield this._client.get(httpRequestFactory.xrefService[i] + uid);
                        let data = promise.data;
                        if (data != undefined)
                            return data;
                    }
                    catch (error) {
                        console.log(error);
                    }
                }
            }
            return [];
        });
    }
}
httpRequestFactory.isDocfxProject = false;
httpRequestFactory.xrefService = [];
httpRequestFactory._client = axios_1.default.create({
    headers: { 'Content-type': 'application/json', 'Accept-type': 'application/json' }
});
exports.httpRequestFactory = httpRequestFactory;
//# sourceMappingURL=httpRequestFactory.js.map