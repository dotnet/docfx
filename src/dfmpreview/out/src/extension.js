'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
var vscode_1 = require("vscode");
var path = require("path");
var child_process = require('child_process');
var previewresult = "";
var provider;
var document_uri;
var Is_end = true;
var ENDCODE = 7;
// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
function activate(context) {
    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    //console.log('Congratulations, your extension "previewtest-ts" is now active!');
    var dfm_process = new PreviewCore(context);
    provider = new MDDocumentContentProvider(context); //the html holder
    var registration = vscode_1.workspace.registerTextDocumentContentProvider('markdown', provider);
    //event registe
    var d1 = vscode_1.commands.registerCommand('DFM.showpreview', function (uri) { return showPreview(dfm_process); });
    var d2 = vscode_1.commands.registerCommand('DFM.showpreviewToside', function (uri) { return showPreview(dfm_process, uri, true); });
    var d3 = vscode_1.commands.registerCommand('DFM.showsource', showSource);
    context.subscriptions.push(d1, d2, d3, registration);
    vscode_1.workspace.onDidSaveTextDocument(function (document) {
        if (isMarkdownFile(document)) {
            document_uri = getMarkdownUri(document.uri);
            dfm_process.callDfm();
        }
    });
    vscode_1.workspace.onDidChangeTextDocument(function (event) {
        if (isMarkdownFile(event.document)) {
            document_uri = getMarkdownUri(event.document.uri);
            dfm_process.callDfm();
        }
    });
    vscode_1.workspace.onDidChangeConfiguration(function () {
        vscode_1.workspace.textDocuments.forEach(function (document) {
            if (document.uri.scheme === 'markdown') {
                // update all generated md documents
                document_uri = document_uri;
                dfm_process.callDfm();
            }
        });
    });
}
exports.activate = activate;
//check the file type 
function isMarkdownFile(document) {
    return document.languageId === 'markdown'
        && document.uri.scheme !== 'markdown'; // prevent processing of own documents
}
function getMarkdownUri(uri) {
    return uri.with({ scheme: 'markdown', path: uri.path + '.rendered', query: uri.toString() });
}
function showPreview(dfm_preview, uri, sideBySide) {
    if (sideBySide === void 0) { sideBySide = false; }
    dfm_preview._is_firsttime = true;
    var resource = uri;
    if (!(resource instanceof vscode_1.Uri)) {
        if (vscode_1.window.activeTextEditor) {
            // we are relaxed and don't check for markdown files
            resource = vscode_1.window.activeTextEditor.document.uri;
        }
    }
    if (!(resource instanceof vscode_1.Uri)) {
        if (!vscode_1.window.activeTextEditor) {
            // this is most likely toggling the preview
            return vscode_1.commands.executeCommand('markdown.showSource');
        }
        // nothing found that could be shown or toggled
        return;
    }
    var thenable = vscode_1.commands.executeCommand('vscode.previewHtml', getMarkdownUri(resource), getViewColumn(sideBySide), "Preview '" + path.basename(resource.fsPath) + "'");
    document_uri = getMarkdownUri(resource);
    dfm_preview.callDfm();
    return thenable;
}
function getViewColumn(sideBySide) {
    var active = vscode_1.window.activeTextEditor;
    if (!active) {
        return vscode_1.ViewColumn.One;
    }
    if (!sideBySide) {
        return active.viewColumn;
    }
    switch (active.viewColumn) {
        case vscode_1.ViewColumn.One:
            return vscode_1.ViewColumn.Two;
        case vscode_1.ViewColumn.Two:
            return vscode_1.ViewColumn.Three;
    }
    return active.viewColumn;
}
function showSource(mdUri) {
    if (!mdUri) {
        return vscode_1.commands.executeCommand('workbench.action.navigateBack');
    }
    var docUri = vscode_1.Uri.parse(mdUri.query);
    for (var _i = 0, _a = vscode_1.window.visibleTextEditors; _i < _a.length; _i++) {
        var editor = _a[_i];
        if (editor.document.uri.toString() === docUri.toString()) {
            return vscode_1.window.showTextDocument(editor.document, editor.viewColumn);
        }
    }
    return vscode_1.workspace.openTextDocument(docUri).then(function (doc) {
        return vscode_1.window.showTextDocument(doc);
    });
}
//this class is to call the dfmserver(child_process) by send information
var PreviewCore = (function () {
    function PreviewCore(context) {
        var extpath = context.asAbsolutePath('./DfmParse/Dfm_test.exe');
        this._spawn = child_process.spawn(extpath);
        this._waiting = false;
        this._spawn.stdout.on('data', function (data) {
            var tmp = data.toString();
            var endcharcode = tmp.charCodeAt(tmp.length - 1);
            if (Is_end && endcharcode == ENDCODE) {
                previewresult = tmp;
                provider.update(document_uri);
            }
            else if (Is_end && endcharcode != ENDCODE) {
                previewresult = tmp;
                Is_end = false;
            }
            else if (!Is_end && endcharcode != ENDCODE) {
                previewresult += tmp;
            }
            else {
                previewresult += tmp;
                Is_end = true;
                provider.update(document_uri);
            }
        });
        this._spawn.stderr.on('data', function (data) {
            console.log("error " + data + '\n');
        });
        this._spawn.on('exit', function (code) {
            console.log('child process exit with code ' + code);
        });
    }
    PreviewCore.prototype.sendtext = function () {
        var rtpath = vscode_1.workspace.rootPath;
        var editor = vscode_1.window.activeTextEditor;
        if (!editor) {
            return;
        }
        var doc = editor.document;
        var docContent = doc.getText();
        var filename = doc.fileName;
        var rtpath_length = rtpath.length;
        var filepath = filename.substr(rtpath_length + 1, filename.length - rtpath_length);
        if (doc.languageId === "markdown") {
            var num_of_row = docContent.split("\r\n").length;
            this._spawn.stdin.write(num_of_row + '\n');
            this._spawn.stdin.write(rtpath + '\n');
            this._spawn.stdin.write(filepath + '\n');
            this._spawn.stdin.write(docContent + '\n');
        }
    };
    PreviewCore.prototype.callDfm = function () {
        var _this = this;
        if (this._is_firsttime) {
            //for the firt time , because the activeTextEditor will be translate to the viewColumn.two.
            this._is_firsttime = false;
            this.sendtext();
        }
        else if (!this._waiting) {
            this._waiting = true;
            setTimeout(function () {
                _this._waiting = false;
                _this.sendtext();
            }, 300);
        }
    };
    return PreviewCore;
}());
var MDDocumentContentProvider = (function () {
    function MDDocumentContentProvider(context) {
        this._onDidChange = new vscode_1.EventEmitter();
        this._context = context;
        this._waiting = false;
    }
    MDDocumentContentProvider.prototype.getMediaPath = function (mediaFile) {
        return this._context.asAbsolutePath(path.join('media', mediaFile));
    };
    MDDocumentContentProvider.prototype.provideTextDocumentContent = function (uri) {
        var _this = this;
        return vscode_1.workspace.openTextDocument(vscode_1.Uri.parse(uri.query)).then(function (document) {
            var head = [].concat('<!DOCTYPE html>', '<html>', '<head>', '<meta http-equiv="Content-type" content="text/html;charset=UTF-8">', "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + _this.getMediaPath('tomorrow.css') + "\" >", "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + _this.getMediaPath('markdown.css') + "\" >", "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + _this.getMediaPath('main.css') + "\" >", "<base href=\"" + document.uri.toString(true) + "\">", '</head>', '<body>').join('\n');
            var body = previewresult;
            var tail = [
                ("<script type=\"text/javascript\" src=\"" + _this.getMediaPath('docfx.vendor.js') + "\"></script>"),
                ("<script type=\"text/javascript\" src=\"" + _this.getMediaPath('main.js') + "\"></script>"),
                "<script>hljs.initHighlightingOnLoad();</script>",
                '</body>',
                '</html>'
            ].join('\n');
            return head + body + tail;
        });
    };
    Object.defineProperty(MDDocumentContentProvider.prototype, "onDidChange", {
        get: function () {
            return this._onDidChange.event;
        },
        enumerable: true,
        configurable: true
    });
    MDDocumentContentProvider.prototype.update = function (uri) {
        this._onDidChange.fire(uri);
    };
    return MDDocumentContentProvider;
}());
//# sourceMappingURL=extension.js.map