function transform(model, _attrs, _global){
  var result = setArrayLength(model);
  if (_global) {
    result.__global = {};
    for (var key in _global) {
      if (_global.hasOwnProperty(key)) {
        result.__global[key] = _global[key];
      }
    }
  }
  return result;
}

function setArrayLength(entity)
{
  var vm = {};
  for (var key in entity)
  {
    if (entity.hasOwnProperty(key)) {
      if (entity[key] instanceof Array)
      {
        vm[key] = entity[key];
        vm[key.concat(".length")] = entity[key].length;
      }
      else if (entity[key] instanceof Object)
      {
        vm[key] = setArrayLength(entity[key]);
      }
      else
      {
        vm[key] = entity[key];
      }
    }
  }
  return vm;
}
