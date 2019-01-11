var foo = require('./foo.js')

function fail(a) {
    a.go()
}

exports.transform = function (obj) {
    if (obj.error) {
        fail()
    }
    return {
        content: JSON.stringify(foo.bar(obj))
    }
}
