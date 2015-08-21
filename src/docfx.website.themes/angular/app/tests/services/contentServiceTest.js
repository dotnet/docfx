// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
describe("contentService", function(){
  beforeEach(angular.mock.module("docascode.contentService"));
  it("should contain a contentService service", 
  angular.mock.inject(function(contentService){
    expect(contentService).not.toEqual(null);
  }));
})