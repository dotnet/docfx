describe("markdownService", function(){
  beforeEach(angular.mock.module("docascode.markdownService"));
  it("should contain a markdownService service", 
  angular.mock.inject(function(markdownService){
    expect(markdownService).not.toEqual(null);
  }));
})