// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.transform = function (model) {
  return setArrayLength(model);
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
