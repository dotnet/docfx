describe("contentService", function(){
  beforeEach(angular.mock.module("docascode.contentService"));
  it("should contain a contentService service", 
  angular.mock.inject(function(contentService){
    expect(contentService).not.toEqual(null);
  }));
})