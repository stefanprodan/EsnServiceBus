var esnApp = angular.module('esnApp', [
    'ngRoute',
    'siTable',
    'mgcrea.ngStrap',
    'angularMoment'
]);

esnApp.filter('bytes', function () {
    return function (bytes, precision) {
        if (bytes === 0) { return '0 bytes' }
        if (isNaN(parseFloat(bytes)) || !isFinite(bytes)) return '-';
        if (typeof precision === 'undefined') precision = 1;

        var units = ['bytes', 'kB', 'MB', 'GB', 'TB', 'PB'],
            number = Math.floor(Math.log(bytes) / Math.log(1024)),
            val = (bytes / Math.pow(1024, Math.floor(number))).toFixed(precision);

        return (val.match(/\.0*$/) ? val.substr(0, val.indexOf('.')) : val) + ' ' + units[number];
    }
});

esnApp.config(['$routeProvider',
  function ($routeProvider) {
      $routeProvider.
        when('/dashboard', {
            templateUrl: 'partials/dashboard.html',
            controller: 'DashboardCtrl'
        }).
        when('/services', {
            templateUrl: 'partials/services.html',
            controller: 'ServicesCtrl'
        }).
        when('/services/:serviceId', {
            templateUrl: 'partials/service.html',
            controller: 'ServiceCtrl'
        }).
        when('/hosts', {
            templateUrl: 'partials/hosts.html',
            controller: 'HostsCtrl'
        }).
        when('/hosts/:hostId', {
            templateUrl: 'partials/host.html',
            controller: 'HostCtrl'
        }).
        otherwise({
            redirectTo: '/dashboard'
        });
  }]);

esnApp.controller('DashboardCtrl', function ($scope, $http) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};

    var url = 'registry/dashboard';
    $http.get(url).then(function (dashboard) {
        $scope.info = dashboard.data;
    }, function (err) {
        $scope.error = 'The server cannot be reached at the moment, please try again.';
    });
});

esnApp.controller('ServicesCtrl', function ($scope, $http) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};

    var url = 'registry/running';
    $http.get(url).then(function (services) {
        $scope.services = services.data;
    }, function (err) {
        $scope.error = 'The server cannot be reached at the moment, please try again.';
    });
});

esnApp.controller('ServiceCtrl', function ($scope, $http, $routeParams) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var guid = $routeParams.serviceId;
    var url = 'service/' + guid;
    $http.get(url).then(function (service) {
        $scope.service = service.data;
    }, function (err) {
        $scope.error = 'The server cannot be reached at the moment, please try again.';
    });
});

esnApp.controller('HostsCtrl', function ($scope, $http) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};

    var url = 'registry/hosts';
    $http.get(url).then(function (hosts) {
        $scope.hosts = hosts.data;
    }, function (err) {
        $scope.error = 'The server cannot be reached at the moment, please try again.';
    });
});

esnApp.controller('HostCtrl', function ($scope, $http, $routeParams) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var guid = $routeParams.hostId;
    var url = 'host/' + guid;
    $http.get(url).then(function (host) {
        $scope.host = host.data;
    }, function (err) {
        $scope.error = 'The server cannot be reached at the moment, please try again.';
    });
});

