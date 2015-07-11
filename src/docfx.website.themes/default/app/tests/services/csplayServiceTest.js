describe("csplayService", function(){
  beforeEach(angular.mock.module("docascode.csplayService"));
  it("should contain a csplayService service", 
  angular.mock.inject(function(csplayService){
    expect(csplayService).not.toEqual(null);
  }));
})