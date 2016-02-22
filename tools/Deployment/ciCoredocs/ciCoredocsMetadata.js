var fs = require('fs');
var path = require('path');
var program = require('commander');
var config = require('nconf');
var ciUtil = require('../ciUtil');

config.add('global', {type: 'file', file: 'config.json'});
config.add('user', {type: 'file', file: 'ciCoredocs/config.json'});

var gitConfig = config.get('git');
var coreConfig = config.get('coredocs');
var toolConfig = config.get('tool');

program
.option('--step1', 'install docfx by dnu')
.option('--step2', 'build coreclr')
.option('--step3', 'run docfx metadata generate YAML')
.option('--step4', 'merge intellisense')
.option('--step5', 'merge source info')
.option('--step6', 'update coreapi repo')
.parse(process.argv);

if (program.step1) {
  ciUtil.exec('.', 'dnu', ['commands', 'install', 'docfx']);
}
if (program.step2) {
  ciUtil.exec(coreConfig['coreclrFolder'], 'build.cmd', []);
}
if (program.step3) {
  ciUtil.exec(coreConfig['coredocsFolder'], 'docfx', ['metadata', 'docfx.json']);
}
if (program.step4) {
  ciUtil.exec(coreConfig['coredocsFolder'], toolConfig['MergeDeveloperComments'], ['api', toolConfig['intellisense']]);
}
if (program.step5) {
  ciUtil.exec(coreConfig['coredocsFolder'], toolConfig['MergeSourceInfo'], ['api_src', 'api']);
}
if (program.step6) {
  ciUtil.replaceUpdateRepo(coreConfig["coreapiRepo"], coreConfig["coreapiFolder"], coreConfig["apiFolder"],
    '_coreapi', 'master', gitConfig["msg"], gitConfig['username'], gitConfig['email']);
}
