describe("urlService", function(){
  beforeEach(angular.mock.module("docascode.urlService"));
  it("should contain a urlService service", 
  angular.mock.inject(function(urlService){
    expect(urlService).not.toEqual(null);
  }));
})