// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
describe("urlService", function(){
  beforeEach(angular.mock.module("docascode.urlService"));
  it("should contain a urlService service", 
  angular.mock.inject(function(urlService){
    expect(urlService).not.toEqual(null);
  }));
})