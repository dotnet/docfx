// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
describe("Directive", function () {
  beforeEach(angular.mock.module("docascode.directives"));
  describe("markdown directive", function () {
    var $compile;
    var $scope;
    var $httpBackend;

    beforeEach(angular.mock.inject(function (_$compile_, _$rootScope_) {
      $compile = _$compile_;
      $scope = _$rootScope_.$new();
    }));
    it("should render the header and text as passed in by $scope",
      angular.mock.inject(function () {
        $scope.a = "**This is content**";
        var template = $compile("<markdown data='a'></markdown>")($scope);
        
        // Now run a $digest cycle to update your template with new data
        $scope.$digest();
        var templateAsHtml = template.html();
        expect(templateAsHtml).toContain("<p><strong>This is content</strong></p>");
      }));
  });
});