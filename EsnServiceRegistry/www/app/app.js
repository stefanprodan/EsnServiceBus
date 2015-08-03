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

// usage: {{ elapsedSeconds | timespan }} displays: 01:23:45
// usage: {{ elapsedSeconds | timespan : 3 }} displays: 01:23:45.678
esnApp.filter('timespan', ['$filter', function ($filter) {
    return function (input, decimals) {
        var sec_num = parseInt(input, 10),
            decimal = parseFloat(input) - sec_num,
            hours = Math.floor(sec_num / 3600),
            minutes = Math.floor((sec_num - (hours * 3600)) / 60),
            seconds = sec_num - (hours * 3600) - (minutes * 60);

        if (hours < 10) { hours = "0" + hours; }
        if (minutes < 10) { minutes = "0" + minutes; }
        if (seconds < 10) { seconds = "0" + seconds; }
        var time = hours + ':' + minutes + ':' + seconds;
        if (decimals > 0) {
            time += '.' + $filter('number')(decimal, decimals).substr(2);
        }
        return time;
    };
}]);

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

esnApp.controller('DashboardCtrl', function ($scope, $http, $interval) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var promise;
    var url = 'registry/dashboard';

    $scope.getData = function () {
        $http.get(url).then(function (dashboard) {
            $scope.info = dashboard.data;
            $scope.error = null;
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        });
    }

    $scope.getData();
    promise = $interval(function () { $scope.getData(); }, 5000);

    $scope.$on('$destroy', function () {
        $interval.cancel(promise);
    });

});

esnApp.controller('ServicesCtrl', function ($scope, $http, $interval) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var promise;
    var url = 'service';

    $scope.getData = function () {
        $http.get(url).then(function (services) {
            $scope.services = services.data;
            $scope.error = null;
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        });
    }

    $scope.getData();
    promise = $interval(function () { $scope.getData(); }, 5000);

    $scope.$on('$destroy', function () {
        $interval.cancel(promise);
    });
});

esnApp.controller('ServiceCtrl', function ($scope, $http, $routeParams, $interval) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var guid = $routeParams.serviceId;
    var promise;
    var url = 'service/' + guid;
    var urlIns = 'service/instances/' + guid;

    $scope.getData = function () {
        $http.get(url).then(function (service) {
            $scope.service = service.data;
            $scope.error = service.data ? null : 'Service not found.';
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        });

        $http.get(urlIns).then(function (instances) {
            $scope.instances = instances.data;
        }, function (err) {

        });
    }

    $scope.getData();
    promise = $interval(function () { $scope.getData(); }, 5000);

    $scope.$on('$destroy', function () {
        $interval.cancel(promise);
    });

});

esnApp.controller('HostsCtrl', function ($scope, $http, $interval) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var promise;
    var url = 'host';

    $scope.getData = function () {
        $http.get(url).then(function (hosts) {
            $scope.hosts = hosts.data;
            $scope.error = null;
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        });
    }

    $scope.getData();
    promise = $interval(function () { $scope.getData(); }, 5000);

    $scope.$on('$destroy', function () {
        $interval.cancel(promise);
    });

});

esnApp.controller('HostCtrl', function ($scope, $http, $routeParams, $interval) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var guid = $routeParams.hostId;
    var promise;
    var url = 'host/' + guid;
    var urlSrvs = 'service/host/' + guid;

    $scope.getData = function () {
        $http.get(url).then(function (host) {
            $scope.host = host.data;
            $scope.error = host.data ? null : 'Host not found.';
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        });


        $http.get(urlSrvs).then(function (services) {
            $scope.services = services.data;
        }, function (err) {

        });
    }

    $scope.getData();
    promise = $interval(function () { $scope.getData(); }, 5000);

    $scope.$on('$destroy', function () {
        $interval.cancel(promise);
    });
});

