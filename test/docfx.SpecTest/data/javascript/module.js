var foo = require('./module-export.js')

module.exports.foo = function () {
    return foo()
}

exports.bar = function () {
    return 'bar'
}
