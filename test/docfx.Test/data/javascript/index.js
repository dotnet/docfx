var foo = require('./foo.js')
var a = require('./a/a.js')

function fail(a) {
    a.go()
}

exports.main = function (obj) {
    if (obj.error) {
        fail()
    }
    if (obj.a) {
        return a.a()
    }
    return foo.bar(obj)
}
