// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
describe("markdownService", function(){
  beforeEach(angular.mock.module("docascode.markdownService"));
  it("should contain a markdownService service", 
  angular.mock.inject(function(markdownService){
    expect(markdownService).not.toEqual(null);
  }));
});