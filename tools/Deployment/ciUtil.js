var spawn = require('child_process').spawn;
var fs = require('fs-extra');
var path = require('path');
var async = require('async');

exports.exec = function(workDir, command, args, done) {
  console.log("[WORKING DIRECTORY]: " + workDir);
  console.log('[COMMAND]: ' + command + ' ' + args.join(" "));
  var curDir = process.cwd();
  process.chdir(workDir);

  var sp = spawn(process.env.comspec, ['/c', command, ...args]);
  sp.stdout.on("data", function(data) {
    console.log(data.toString());
  });
  sp.stderr.on("data", function(data) {
    console.error(data.toString());
  });
  sp.on('close', function(code) {
    process.chdir(curDir);
    if (done) {
      if (code !== 0) {
        done("Exited with code: " + code);
      } else {
        done();
      }
    }
  });
}

exports.execAsyncDone = function(workDir, command, args) {
  return function(done) {
    exports.exec(workDir, command, args, done);
  }
}

exports.copy = function(source, dest, excludeFile, done) {
  console.log("copy %s %s", source, dest);
  var cb = function(err) {
    if (done) {
      if (err) {
        done(err);
      } else {
        done();
      }
    }
  }

  if (excludeFile) {
    var excludes = file2array(excludeFile);
    var options = function(file) {
      return excludes.indexOf(file) === -1;
    };
    fs.copy(source, dest, options, cb);
  } else {
    fs.copy(source, dest, {clobber:true}, cb);
  }
}

exports.copyAsyncDone = function(source, dest, excludeFile) {
  return function(done) {
    console.log(excludeFile);
    exports.copy(source, dest, excludeFile, done);
  }
}

exports.remove = function(dir) {
  fs.remove(dir, function(err) {
    if (err) return console.error(err);
  });
}

exports.replaceUpdateRepo = function(repoUrl, repoFolder, srcFolder, tmpFolder, branch, msg, username, email) {
  async.series([
    // git clone -b [branch] repoUrl repoFolder
    exports.execAsyncDone('.', 'git', ['clone', '-b', branch, repoUrl, repoFolder]),
    // cp srcFolder tmpFolder
    exports.copyAsyncDone(srcFolder, tmpFolder),
    // cp repoFolder/.git tmpFolder
    exports.copyAsyncDone(path.join(repoFolder, '.git'), path.join(tmpFolder, '.git')),
    // git config username
    exports.execAsyncDone(tmpFolder, 'git', ['config', 'user.name', username]),
    // git config email
    exports.execAsyncDone(tmpFolder, 'git', ['config', 'user.email', email]),
    // git add .
    exports.execAsyncDone(tmpFolder, 'git', ['add', '.']),
    // git commit -m msg
    exports.execAsyncDone(tmpFolder, 'git', ['commit', '-m', msg]),
    // git push -u origin [branch]
    exports.execAsyncDone(tmpFolder, 'git', ['push', '-u', 'origin', branch])
    ], function(err) {
      if (err) return console.error(err);
    }
  );
}

exports.incrementalUpdateRepo = function(repoUrl, repoFolder, srcFolder, tmpFolder, branch, msg, username, email, excludeFile) {
  async.series([
    // git clone -b gh-pages repoUrl repoFolder
    exports.execAsyncDone('.', 'git', ['clone', '-b', branch, repoUrl, repoFolder]),
    // cp srcFolder tmpFolder
    exports.copyAsyncDone(srcFolder, tmpFolder),
    // xcopy /ey tmpFolder/* repoFolder exclude:exclude.list
    exports.copyAsyncDone(tmpFolder, repoFolder, excludeFile),
    // git config username
    exports.execAsyncDone(repoFolder, 'git', ['config', 'user.name', username]),
    // git config email
    exports.execAsyncDone(repoFolder, 'git', ['config', 'user.email', email]),
    // git add .
    exports.execAsyncDone(repoFolder, 'git', ['add', '.']),
    // git commit -m msg
    exports.execAsyncDone(repoFolder, 'git', ['commit', '-m', msg]),
    // git push -u origin [branch]
    exports.execAsyncDone(repoFolder, 'git', ['push', '-u', 'origin', branch])
    ], function(err) {
      if (err) return console.error(err);
    }
  );
}

function file2array(file) {
  var arr = fs.readFileSync(file).toString().split(/\r?\n/);
  for (var i in arr) {
    arr[i] = path.resolve(arr[i]);
  }
  return arr;
}
