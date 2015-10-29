// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
describe("csplayService", function(){
  beforeEach(angular.mock.module("docascode.csplayService"));
  it("should contain a csplayService service", 
  angular.mock.inject(function(csplayService){
    expect(csplayService).not.toEqual(null);
  }));
});