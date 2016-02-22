var fs = require('fs');
var path = require('path');
var program = require('commander');
var config = require('nconf');
var ciUtil = require('../ciUtil');

config.add('global', {type: 'file', file: 'config.json'});
config.add('user', {type: 'file', file: 'ciCoredocs/config.json'});

var gitConfig = config.get('git');
var coreConfig = config.get('coredocs');

program
.option('--step1', 'install docfx by dnu')
.option('--step2', 'copy coreapi to coredocs')
.option('--step3', 'run docfx build to generate website')
.option('--step4', 'update gh-pages')
.parse(process.argv);

if (program.step1) {
  ciUtil.exec('.', 'dnu', ['commands', 'install', 'docfx']);
}
if (program.step2) {
  ciUtil.copy(coreConfig["coreapiFolder"], coreConfig["apiFolder"]);
}
if (program.step3) {
  ciUtil.exec(coreConfig["coredocsFolder"], 'docfx', ['build', 'docfx.json']);
}
if (program.step4) {
  ciUtil.incrementalUpdateRepo(coreConfig["coredocsSiteRepo"], coreConfig["coredocsSiteRepoFolder"],
    coreConfig["siteFolder"], '_site', 'master', gitConfig["msg"],
    gitConfig['username'], gitConfig['email'], coreConfig["copyExcludeFile"]);
}
