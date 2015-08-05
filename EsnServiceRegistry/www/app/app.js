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
        when('/issues', {
            templateUrl: 'partials/issues.html',
            controller: 'IssuesCtrl'
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

esnApp.controller('DashboardCtrl', function ($scope, $http, $timeout) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var timeoutPromise;
    var url = 'registry/dashboard';

    var getData = function () {
        $http.get(url).then(function (dashboard) {
            $scope.info = dashboard.data;
            $scope.error = null;
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        }).finally(function () {
            timeoutPromise = $timeout(getData, 5000);
        });
    }

    getData();

    $scope.$on('$destroy', function () {
        if (timeoutPromise) {
            $timeout.cancel(timeoutPromise);
        }
    });
});

esnApp.controller('IssuesCtrl', function ($scope, $http, $timeout) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var timeoutPromise;
    var errorMsg = 'The server cannot be reached at the moment, retying...';

    var urlIssues = 'services/issues';
    var urlDecommission = 'services/decommission/';

    $scope.decommissionService = function (serviceGuid) {
        var url = urlDecommission + serviceGuid;
        $http.get(url).then(function () {
            $scope.getData();
        }, function (err) {
            $scope.error = errorMsg;
        });
    }

    var getData = function () {
        $http.get(urlIssues).then(function (services) {
            $scope.services = services.data;
            $scope.error = null;
        }, function (err) {
            $scope.error = errorMsg;
        }).finally(function () {
            timeoutPromise = $timeout(getData, 5000);
        });
    }

    getData();

    $scope.$on('$destroy', function () {
        if (timeoutPromise) {
            $timeout.cancel(timeoutPromise);
        }
    });
});

esnApp.controller('ServicesCtrl', function ($scope, $http, $timeout) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var timeoutPromise;
    var url = 'services';

    var getData = function () {
        $http.get(url).then(function (services) {
            $scope.services = services.data;
            $scope.error = null;
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        }).finally(function () {
            timeoutPromise = $timeout(getData, 5000);
        });
    }

    getData();

    $scope.$on('$destroy', function () {
        if (timeoutPromise) {
            $timeout.cancel(timeoutPromise);
        }
    });
});

esnApp.controller('ServiceCtrl', function ($scope, $http, $routeParams, $timeout, $modal) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};

    var guid = $routeParams.serviceId;
    var timeoutPromise;
    var url = 'services/' + guid;
    var urlEdit = 'services/' + guid + '/edit';
    var urlIns = 'services/instances/' + guid;
    var urlCluster = 'services/cluster/' + guid;

    var editModal = $modal({ scope: $scope, templateUrl: 'partials/service.edit.html', show: false });

    $scope.openEdit = function () {
        $timeout.cancel(timeoutPromise);
        editModal.$promise.then(editModal.show);
    }

    $scope.closeEdit = function () {
        getData();
        editModal.$promise.then(editModal.hide);
    }

    $scope.doEdit = function () {

        $http.post(urlEdit, { Tags: $scope.service.TagList }, { headers: { 'Content-Type': 'application/json' } })
        .then(function (response) {
            $scope.doEditError = null;
            getData();
            editModal.$promise.then(editModal.hide);
        }, function (err) {
            console.log(err);
            $scope.doEditError = 'The server cannot be reached at the moment, please try again.';
        });
    }

    var getData = function () {
        $http.get(url).then(function (service) {
            $scope.service = service.data;
            $scope.error = service.data ? null : 'Service not found.';

            if (service.data) {
                $scope.service.TagList = service.data.Tags.join(', ');
            }
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        }).finally(function () {
            timeoutPromise = $timeout(getData, 5000);
        });

        $http.get(urlIns).then(function (instances) {
            $scope.instances = instances.data;
        }, function (err) {

        });

        $http.get(urlCluster).then(function (cluster) {
            $scope.cluster = cluster.data;
        }, function (err) {

        });
    }

    getData();

    $scope.$on('$destroy', function () {
        if (timeoutPromise) {
            $timeout.cancel(timeoutPromise);
        }
    });

});

esnApp.controller('HostsCtrl', function ($scope, $http, $timeout) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    var timeoutPromise;
    var url = 'hosts';

    var getData = function () {
        $http.get(url).then(function (hosts) {
            $scope.hosts = hosts.data;
            $scope.error = null;
        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        }).finally(function () {
            timeoutPromise = $timeout(getData, 5000);
        });
    }

    getData();

    $scope.$on('$destroy', function () {
        if (timeoutPromise) {
            $timeout.cancel(timeoutPromise);
        }
    });

});

esnApp.controller('HostCtrl', function ($scope, $http, $routeParams, $timeout, $modal) {
    $scope.filter = {
        $: ''
    };
    $scope.params = {};
    $scope.tags = '';
    var guid = $routeParams.hostId;

    var urlHost = 'hosts/' + guid;
    var urlHostEdit = urlHost + '/edit';
    var urlServices = 'services/host/' + guid;

    var timeoutPromise;

    var editModal = $modal({ scope: $scope, templateUrl: 'partials/host.edit.html', show: false });

    $scope.openEdit = function () {
        $timeout.cancel(timeoutPromise);
        editModal.$promise.then(editModal.show);
    }

    $scope.closeEdit = function () {
        getData();
        editModal.$promise.then(editModal.hide);
    }

    $scope.doEdit = function () {

        $http.post(urlHostEdit, { Location: $scope.host.Location, Tags: $scope.host.TagList }, { headers: { 'Content-Type': 'application/json' } })
        .then(function (response) {
            $scope.doEditError = null;
            getData();
            editModal.$promise.then(editModal.hide);
        }, function (err) {
            console.log(err);
            $scope.doEditError = 'The server cannot be reached at the moment, please try again.';
        });
    }

    var getData = function () {
        $http.get(urlHost).then(function (host) {
            $scope.host = host.data;
            $scope.error = host.data ? null : 'Host not found.';

            if (host.data) {
                $scope.host.TagList = host.data.Tags.join(', ');
            }

        }, function (err) {
            $scope.error = 'The server cannot be reached at the moment, retying...';
        }).finally(function () {
            timeoutPromise = $timeout(getData, 5000);
        });


        $http.get(urlServices).then(function (services) {
            $scope.services = services.data;
        }, function (err) {

        });

    }

    getData();

    $scope.$on('$destroy', function () {
        if (timeoutPromise) {
            $timeout.cancel(timeoutPromise);
        }
    });
});

