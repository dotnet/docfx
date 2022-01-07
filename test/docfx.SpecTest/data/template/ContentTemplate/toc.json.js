exports.transform = function (model) {
  return {
    content: JSON.stringify(model)
  }
}