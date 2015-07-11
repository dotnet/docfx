describe("Test for ContainerController", function(){
  var mockScope, controller, backend;

  beforeEach(angular.mock.module("docsApp"));
  
  beforeEach(angular.mock.inject(function($httpBackend){
    backend = $httpBackend;
    backend.when("GET", "toc.yml").respond("-id: 'a'");
  }));

  beforeEach(angular.mock.inject(function($controller, $rootScope, $http){
    mockScope = $rootScope.$new();
    mockScope.toc = {};
    mockScope.toc.content = [
      {name: 'Home', href: 'index.md' },
      {name: 'About', href: 'about.md' },
    ];

    controller = $controller("DocsController", {
      $scope: mockScope,
      $http: $http
    });
  }));
  it("should fetch yml", function(){
    // backend.flush();
  });
  it("Test getTocHref", function(){
    // expect(mockScope.filteredItems('home').filter(function(s){s.name === 'Home';})[0].hide).toEqual(false);
  })
});