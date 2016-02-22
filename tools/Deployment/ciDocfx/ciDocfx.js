var fs = require('fs');
var path = require('path');
var util = require('util');
var program = require('commander');
var jszip = require('jszip');
var config = require('nconf');
var ciUtil = require('../ciUtil');

config.add('configuration', {type: 'file', file: path.join(__dirname, 'config.json')});
var docfxConfig = config.get('docfx');
var gitConfig = config.get('git');
var mygetConfig = config.get('myget');

var uploadMyget = function(nugetExe, releaseFolder, apiKey, sourceUrl) {
  fs.readdirSync(releaseFolder).forEach(function(file, index) {
    var subPath = path.join(releaseFolder, file);
    if (fs.lstatSync(subPath).isFile() && file.indexOf('symbol') === -1) {
      ciUtil.exec('.', nugetExe, ['push', subPath, apiKey, '-Source', sourceUrl]);
    }
  });
}

var zipDocfx = function(fromDir, destDir) {
  var zip = new jszip();
  fs.readdirSync(fromDir).forEach(function(file) {
    var filePath = path.join(fromDir, file);
    if (fs.lstatSync(filePath).isFile()) {
      var ext = path.extname(filePath);
      if (ext !== '.xml' && ext !== '.pdb') {
        var content = fs.readFileSync(filePath);
        zip.file(file, content);
      }
    }
  });
  var buffer = zip.generate({type:"nodebuffer", compression: "DEFLATE"});
  fs.unlinkSync(destDir);
  fs.writeFileSync(destDir, buffer);
}

program
.option('--step1', 'clear artifacts/Release')
.option('--step2', 'build docfx')
.option('--step3', 'run e2e tests')
.option('--step4', 'upload myget.org')
.option('--step5', 'generate gh-pages')
.option('--step6', 'zip docfx.exe')
.option('--step7', 'update gh-pages')
.parse(process.argv);

if (program.step1) {
  ciUtil.remove(docfxConfig["releaseFolder"]);
}
if (program.step2) {
  ciUtil.exec(docfxConfig['homeFolder'], 'build.cmd', ["Release PROD"]);
}
if (program.step3) {
  ciUtil.exec(".", "msbuild", [docfxConfig['e2eproj'], "/p:Configuration=Release"]);
}
if (program.step4) {
  uploadMyget(mygetConfig['nugetExe'], docfxConfig['releaseFolder'], mygetConfig['apiKey'], mygetConfig['sourceUrl']);
}
if (program.step5) {
  ciUtil.exec(docfxConfig['docFolder'], path.resolve(docfxConfig['docfxExe']), []);
}
if (program.step6) {
  zipDocfx(docfxConfig['zipSrcFolder'], docfxConfig['zipDestFolder']);
}
if (program.step7) {
  ciUtil.replaceUpdateRepo(docfxConfig['repoUrl'], "docfxGhPages", docfxConfig['docSiteFolder'], "_site", 'gh-pages', gitConfig['msg'], gitConfig['username'], gitConfig['email']);
}
