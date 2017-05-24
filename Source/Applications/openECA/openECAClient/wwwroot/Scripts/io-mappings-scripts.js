"use strict";

var udts = [];
var mappings = [];
var parentDevice = [];
var deviceData = [];
var measurementDetails = [];
var errorList = [];
var re = /^[A-Za-z_][A-Za-z_0-9]*$/;

$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();
});

$(function () {
    // Client function called from the dataHub when meta data gets recieved
    dataHubClient.metaDataReceived = function () {
        $(window).trigger("metaDataReceived");
    }

});

$('#identifier').on('keyup', function (e) {
    if (re.test($('#identifier').val())) {
        $('#identifier').css('border-color', '');
        $('#saveBtn').removeAttr('disabled');
    } else {
        $('#identifier').css('border-color', 'red');
        $('#saveBtn').attr('disabled', 'disabled');
    }
});

var userDefinedMappings = angular.module('UserDefinedMappings', []);
var MappingsCtrl = userDefinedMappings.controller('MappingsCtrl', function ($scope, $location) {
    $scope.output = true; // true for output, false for input mappings
    $scope.filelocation = "";
    $scope.rawData;
    $scope.metaDataRecieved = false;
    $scope.sortBy = 'Type';
    $scope.reverse = false;
    $scope.sortDirection = true;  // true for ascending and false for descending
    $scope.Header = ['glyphicon glyphicon-chevron-up', '', '', '', '', ''];
    $scope.searchText = '';
    $scope.pageSize = 10;
    $scope.currentPage = 1;
    $scope.pageCount;
    $scope.pagedMappings = [];
    $scope.type;
    $scope.mapping = {
        Identifier: '',
        Type: null,
        FieldMappings: []
    }
    $scope.deviceList = [];
    $scope.stats = [];
    $scope.iocheckbox = $location.absUrl().includes('Output');
    $scope.ioString = $scope.iocheckbox ? 'Output' : 'Input';

    $(window).on("hubConnected", function (event) {
        $scope.initialize();
    });

    $(window).on('metaDataReceived', function () {
        $scope.deviceList = [];
        dataHub.getDeviceDetails().done(function (dd) {
            deviceData = dd;
            var parents = ($.unique($.map(deviceData, function (n) { return n.ParentAcronym }))).filter(function (x, i) { return ($.unique($.map(deviceData, function (n) { return n.ParentAcronym }))).indexOf(x) === i });
            $.each(parents, function (key, device) {
                $scope.deviceList.push({
                    ParentAcronym: (device == '' ? 'Unknown' : device),
                    Devices: [],
                    Statistics: []
                });
            });

            $.each(dd, function (key, device) {
                var index = $scope.deviceList.findIndex(function (elem, ind) { return elem.ParentAcronym === (device.ParentAcronym == '' ? 'Unknown' : device.ParentAcronym) })
                if ($scope.deviceList[index].Devices.indexOf(device.Acronym) === -1) {
                    if (device.Enabled)
                        $scope.deviceList[index].Devices.push({ Acronym: device.Acronym, Measurements: [], Statistics: [] });
                }
            });

            dataHub.getMeasurementDetails().done(function (md) {
                measurementDetails = md;
                $.each(md, function (key, measurement) {
                    if (measurement.DeviceAcronym == null)
                        measurement.DeviceAcronym = "Unknown";

                    if ($scope.deviceList.map(function (a) { return a.ParentAcronym }).indexOf(measurement.DeviceAcronym) != -1) {
                        var index = $scope.deviceList.findIndex(function (elm, i) { return elm.ParentAcronym == measurement.DeviceAcronym; });
                        $scope.deviceList[index].Statistics.push(measurement);
                        return;
                    }

                    var pa = '';
                    try {
                        pa = $.grep($scope.deviceList, function (d) { return $.grep(d.Devices, function (dd) { return dd.Acronym == measurement.DeviceAcronym }).length > 0 })[0].ParentAcronym;
                    }
                    catch (ex) {
                        pa = '';
                    }

                    pa = (pa == '' ? 'Unknown' : pa);

                    try {
                        var index = $scope.deviceList.findIndex(function (elm, i) { return elm.ParentAcronym == pa; });
                        var index2 = $scope.deviceList[index].Devices.findIndex(function (elm, i) { return elm.Acronym == measurement.DeviceAcronym });

                        if (measurement.SignalAcronym.indexOf('STAT') < 0)
                            $scope.deviceList[index].Devices[index2].Measurements.push(measurement);
                        else
                            $scope.deviceList[index].Devices[index2].Statistics.push(measurement);
                    }
                    catch (ex) {

                    }
                });
                $scope.metaDataRecieved = true;
                $scope.$apply();

            }).fail(function (error) {
                showErrorMessage(error);
            });
        }).fail(function (error) {
            showErrorMessage(error);
        });




    });

    $scope.iocheckboxChange = function (e) {
        $scope.ioString = $scope.iocheckbox ? 'Output' : 'Input';
        $scope.initialize($scope.iocheckbox);
    }

    $scope.initialize = function (output) {
        output = (typeof output !== 'undefined') ? output : $scope.iocheckbox;
        $scope.getData(output);

        dataHub.initializeSubscriptions().fail(function (error) {
            showErrorMessage(error);
        });

        dataHub.getMappingFileDirectory($scope.iocheckbox).done(function (directory) {
            $scope.filelocation = 'File location: ' + directory;
        });

    }

    $scope.getData = function (output) {
        output = (typeof output !== 'undefined') ? output : true;
        udts = [];

        dataHub.getDefinedTypes().done(function (types) {
            $scope.type = angular.copy($.grep(types, function (d) { return d.IsUserDefined }));
            udts = angular.copy($scope.type);
            $scope.mapping.Type = $scope.type[0];
            $.each($scope.mapping.Type.Fields, function (i, d) {
                $scope.mapping.FieldMappings.push({ 'Field': d, 'Expression': '', 'TimeWindowExpression': '' })
            });
            $scope.mapping.TypeIndex = '0';
            $scope.$apply();
            dataHub.getDefinedMappings(output).done(function (data) {
                $scope.rawData = data;
                $scope.pageCount = Math.ceil(data.length / $scope.pageSize);
                $scope.setPages($scope.rawData);
                $scope.$apply();
                $('#recordCount').text(data.length);

            });
            dataHub.getMappingCompilerErrors(output).done(function (data) {
                errorList = data;

                if (errorList.length == 0) {
                    if ($('#error-count').length)
                        hideErrorMessage();

                    $('#modal-errors').modal('hide');
                } else if (errorList.length > 0) {
                    var anchor = $('<a href="#" id="error-count">');

                    if (data.length == 1)
                        anchor.append('1 error');
                    else
                        anchor.append(data.length + ' errors');

                    UpdateErrorModal();
                    showErrorMessage(anchor.prop('outerHTML') + ' occurred during mapping compilation.');

                    $('#error-count').click(function (e) {
                        $('#modal-errors').modal('show');
                        return false;
                    });
                }
            }).fail(function (error) {
                showErrorMessage(error);
            });

        });

    }

    $scope.getExpressions = function (item) {
        var fieldString = "";
        $.each(item.FieldMappings, function (i, fieldMapping) {
            fieldString += fieldMapping.Field.Identifier + ' to ' + fieldMapping.Expression + (fieldMapping.TimeWindowExpression != null ? ' ' + fieldMapping.TimeWindowExpression : '');
            if (i < item.FieldMappings.length - 1)
                fieldString += ', ';
        });
        return fieldString;
    }

    $scope.fixAcronyms = function (string) {
        return string.replace(/[^a-zA-Z0-9]/g, '')
    }

    $scope.showAddSignals = function () {
        return $scope.mapping.Type && !($scope.mapping.Type.Fields[0].Type.IsUserDefined || $scope.mapping.Type.Fields[0].Type.IsArray);
    }

    $scope.addMapping = function () {
        $('#updateBtn').hide();
        $('#saveBtn').show();
        $('#addSignalsBtn').show();
        $scope.mapping.Type = $scope.type[0];
        $scope.mapping.TypeIndex = '0';
        $scope.mapping.Identifier = '';
        $scope.mapping.FieldMappings = [];
        $.each($scope.mapping.Type.Fields, function (i, d) {
            $scope.mapping.FieldMappings.push({ 'Field': d, 'Expression': '', 'TimeWindowExpression': '' })
        });


    }

    $scope.editMapping = function () {
        $('#mappingDialog').modal('hide');
        dataHub.editMapping($scope.mapping, $scope.iocheckbox).done(function () {
            $scope.getData();
        }).fail(function (error) {
            showErrorMessage(error);
        });
    }

    $scope.saveMapping = function () {
        $('#mappingDialog').modal('hide');
        dataHub.addMapping($scope.mapping, $scope.iocheckbox).done(function () {
            $scope.getData();
        }).fail(function (error) {
            showErrorMessage(error);
        });
    }

    $scope.addToExpression = function (x, thecontrol) {
        var string = "";
        $.each(thecontrol.multiselectData, function (i, d) {
            string += d + ';';
        });

        x.Expression = string;
    }

    $scope.updateTypeMappings = function () {
        $scope.mapping.Type = udts[$scope.mapping.TypeIndex];
        $scope.mapping.FieldMappings = [];
        $.each($scope.mapping.Type.Fields, function (i, d) {
            $scope.mapping.FieldMappings.push({ 'Field': d, 'Expression': '', 'TimeWindowExpression': '' })
        });

    }

    $scope.openSignalDialog = function () {
        $scope.mapping.Type = udts[$scope.mapping.TypeIndex];
        $('#signalDialog').modal('show');
    }

    $scope.addNewSignals = function () {
        $.each($scope.mapping.Type.Fields, function (i, f) {
            // TODO: Must load existing DeviceID and SignalID in order to "update" existing records - may need a new hub function to do this...
            var ms = {
                'AnalyticProjectName': $('#proj' + f.Identifier).val(),
                'AnalyticInstanceName': $('#inst' + f.Identifier).val(),
                'DeviceID': '00000000-0000-0000-0000-000000000000',
                'RuntimeID': 0,
                'SignalID': '00000000-0000-0000-0000-000000000000',
                'PointTag': $('#point' + f.Identifier).val(),
                'SignalType': $('#type' + f.Identifier).val(),
                'Description': $('#desc' + f.Identifier).val()
            }
            $scope.mapping.FieldMappings[i].Expression = $('#point' + f.Identifier).val();

            //$('#input' + f.Identifier).val($('#point' + f.Identifier).val());
            //setTimeout(function () { dataHub.metaSignalCommand(ms); }, i * 500);
            dataHub.metaSignalCommand(ms);
        });

        $('#signalDialog').modal('hide');
    }

    $scope.cancelSignalDialog = function () {
        $('#signalDialog').modal('hide');
    }

    $scope.saveWindow = function (acronym) {
        $('#timeWindow' + acronym).val(angular.copy($scope.timeWindowText.trim()));
    }

    $scope.clearFields = function () {
        $scope.timeWindowField = null;
        $scope.relativeTimeSelect = 99;
        $scope.timeWindowText = '';
    }

    $scope.openTimeWindow = function (data) {
        $('#timeWindowSave').off('click').on('click', function () {
            data.TimeWindowExpression = angular.copy($scope.timeWindowText);
            $scope.$apply();
            $scope.clearFields();
        });
        $('#timeWindowDialog').modal('show');
    }

    $scope.openTimeWindow2 = function (data) {
        $('#timeWindowSave2').off('click').on('click', function () {
            data.TimeWindowExpression = angular.copy($scope.timeWindowText);
            $scope.$apply();
            $scope.clearFields();
        });
        $('#timeWindowDialog2').modal('show');
    }


    $scope.updateTimeWindowText = function () {
        $scope.timeWindowText = "";
        $.each($('#secondMaprt').children().children(), function (i, object) {
            if ($(object).is('select'))
                $scope.timeWindowText += $(object).children(':selected').text() + ' ';
            else if ($(object).is('input'))
                $scope.timeWindowText += $(object).val() + ' ';
            else
                $scope.timeWindowText += $(object).text() + ' ';
        });

    }

    $scope.updateTimeWindowText2 = function () {
        $scope.timeWindowText = "";
        $.each($('#secondMaptw').children().children(), function (i, object) {
            if ($(object).is('select'))
                $scope.timeWindowText += $(object).children(':selected').text() + ' ';
            else if ($(object).is('input'))
                $scope.timeWindowText += $(object).val() + ' ';
            else
                $scope.timeWindowText += $(object).text() + ' ';
        });

    }


    $scope.setPages = function (data) {
        var page = 0;
        $scope.pagedMappings = [];
        $scope.pagedMappings.push([]);
        $.each(data, function (index, data) {
            if (index !== 0 && index % $scope.pageSize === 0) {
                $scope.pagedMappings.push([]);
                page++
            }
            $scope.pagedMappings[page].push(data);
        });
        $scope.pageCount = page + 1;
        $scope.currentPage = 1;
    };

    $scope.removeMapping = function (item) {
        dataHub.removeMapping(item, $scope.iocheckbox).done(function () {
            $scope.getData();
        }).fail(function (error) {
            showErrorMessage(error);
        });
    }

    $scope.updateMapping = function (item) {
        var index = 0;

        $.each(udts, function (i, d) {
            if (d.Identifier == item.Type.Identifier && d.Category == item.Type.Category)
                index = i;
        });

        $scope.mapping.Identifier = item.Identifier;
        $scope.mapping.TypeIndex = index.toString();
        $scope.mapping.Type = udts[index];
        $scope.mapping.FieldMappings = [];
        $.each(item.FieldMappings, function (i, d) {
            $scope.mapping.FieldMappings.push(angular.copy(d))
        });
        $('#updateBtn').show();
        $('#saveBtn').hide();
        $('#addSignalsBtn').hide();

        $('#mappingDialog').modal('show');
    }

    $scope.cancelDeviceList = function () {
        $('#deviceList input[type=checkbox]').removeAttr('checked');
        $('#deviceList').modal('hide');
    }

    $scope.addOneSignal = function (x) {
        $('#addSignalsButton').hide();

        $('#deviceList input[type=checkbox]').each(function () {
            if ($(this).val() == x.Expression) {
                $(this).prop('checked', true);
            }
            $(this).on('change', function () {
                x.Expression = $(this).val();
                $('#deviceList input[type=checkbox]').removeAttr('checked');
                $('#deviceList input[type=checkbox]').off('change')
                $('#deviceList').modal('hide');
                $scope.$apply();
            });
        });

        $('#deviceList').modal('show');
    }

    $scope.addMultipleSignals = function (x) {
        $('#addSignalsButton').show();
        var things = x.Expression.split(';');
        $('#deviceList input[type=checkbox]').each(function () {

            if (things.indexOf($(this).val()) > -1) {
                $(this).prop('checked', true);
            }
        });
        $('#addSignalsButton').off('click').on('click', function () {
            x.Expression = "";
            $('#deviceList input[type=checkbox]').each(function () {
                if ($(this).prop('checked')) {
                    x.Expression += $(this).val() + ';';
                }

            });
            $('#deviceList input[type=checkbox]').removeAttr('checked');
            $('#deviceList').modal('hide');
            $scope.$apply();

        });

        $('#deviceList').modal('show');

    }

    $scope.showTimeWindow = function (x) {
        return x.Expression.split(';').length > 2;
    }

    $scope.loadSelectBox = function () {
        if ($('.multiselect').filter('button').length > 0)
            $('.multiselect').multiselect('destroy');

        $('.multiselect').multiselect({
            buttonClass: 'btn btn-default',
            delimiter: ';',
        });


        return true;
    }

    $scope.setPageSize = function (size) {
        $scope.pageSize = size;
        $scope.setPages($scope.rawData);
    };

    $scope.setPage = function (pageNumber) {
        $scope.currentPage = pageNumber;
    };

    $scope.firstPage = function () {
        $scope.currentPage = 1;
    };

    $scope.lastPage = function () {
        $scope.currentPage = $scope.pageCount;
    };

    $scope.plusPage = function () {
        $scope.currentPage++;
    }
    $scope.minusPage = function () {
        $scope.currentPage--;
    }

    $scope.sort = function (sortBy) {
        if (sortBy === $scope.sortBy) {
            $scope.reverse = !$scope.reverse;
        }
        $scope.sortBy = sortBy;
        $scope.Header = ['', '', ''];

        var iconName;

        if ($scope.reverse)
            iconName = 'glyphicon glyphicon-chevron-down';
        else
            iconName = 'glyphicon glyphicon-chevron-up';

        if (sortBy === 'Type') {
            $scope.Header[0] = iconName;
            $scope.rawData.sort(function (a, b) {
                if (!$scope.reverse) {
                    if ((a.Type.Category + ' ' + a.Type.Identifier) < (b.Type.Category + ' ' + b.Type.Identifier)) return -1;
                    if ((a.Type.Category + ' ' + a.Type.Identifier) > (b.Type.Category + ' ' + b.Type.Identifier)) return 1;
                    return 0;
                }
                else {
                    if ((b.Type.Category + ' ' + b.Type.Identifier) < (a.Type.Category + ' ' + a.Type.Identifier)) return -1;
                    if ((b.Type.Category + ' ' + b.Type.Identifier) > (a.Type.Category + ' ' + a.Type.Identifier)) return 1;
                    return 0;
                }
            });
        }
        else if (sortBy === 'Identifier') {
            $scope.Header[1] = iconName;
            $scope.rawData.sort(function (a, b) {
                if (!$scope.reverse) {
                    if (a.Identifier < b.Identifier) return -1;
                    if (a.Identifier > b.Identifier) return 1;
                    return 0;
                }
                else {
                    if (b.Identifier < a.Identifier) return -1;
                    if (b.Identifier > a.Identifier) return 1;
                    return 0;
                }
            });
        } else if (sortBy === 'Mappings') {
            $scope.Header[2] = iconName;
            $scope.rawData.sort(function (a, b) {
                if (!$scope.reverse) {
                    if ($scope.getExpressions(a) < $scope.getExpressions(b)) return -1;
                    if ($scope.getExpressions(a) > $scope.getExpressions(b)) return 1;
                    return 0;
                }
                else {
                    if ($scope.getExpressions(b) < $scope.getExpressions(a)) return -1;
                    if ($scope.getExpressions(b) > $scope.getExpressions(a)) return 1;
                    return 0;
                }
            });
        }

        $scope.setPages($scope.rawData);

    };

    $scope.search = function () {
        var array;
        if ($scope.searchText !== "") {
            array = $.grep($scope.rawData, function (a, i) {
                return (a.Type.Category + ' ' + a.Type.Identifier).toLowerCase().indexOf($scope.searchText.toLowerCase()) >= 0 || a.Identifier.toLowerCase().indexOf($scope.searchText.toLowerCase()) >= 0 || $scope.getExpressions(a).toLowerCase().indexOf($scope.searchText.toLowerCase()) >= 0;
            });
        }
        else {
            array = $scope.rawData;
        }

        $scope.setPages(array);
    };

    $scope.saveBtnEnabler = function () {
        return $scope.mapping.Identifier.trim().length > 0;
    }

});

function UpdateErrorModal() {
    var content = $('<pre>');

    $.each(errorList, function (key, error) {
        content.append($('<a id="error-' + key + '" href="#">').text(error.Message));
        content.append('\n');
    });

    $('#modal-errors').find('.modal-body').empty().append(
        $('<div style="max-height: 250px; overflow-y: auto">').append(content),
        $('<div id="input-label-errors">').text('No file being edited'),
        $('<textarea id="input-errors" type="text" cols="80" rows="10">').attr('disabled', 'disabled')
    );

    $.each(errorList, function (key, error) {
        $('#error-' + key).click(function (e) {
            var filePath = error.FilePath;

            if (filePath.length > 80)
                filePath = "..." + filePath.substr(filePath.length - 77, 77);

            $('#error-' + key).parent().children().css('text-decoration', '');
            $('#error-' + key).css('text-decoration', 'underline');
            $('#input-label-errors').text(filePath);
            $('#input-errors').removeAttr('disabled').val(error.FileContents);

            $('#save-errors').off('click.errors').on('click.errors', function (e) {
                var contents = $('#input-errors').val();

                $('#save-errors')
                    .off('click.Errors')
                    .attr('disabled', 'disabled');

                $('#input-label-errors').val('No file being edited');
                $('#input-errors').val('').attr('disabled', 'disabled');

                dataHub.fixMapping(error.FilePath, contents, $scope.iocheckbox).done(function () {
                    angular.element('[ng-controller=MappingsCtrl]').scope().getData();
                }).fail(function (error) {
                    showErrorMessage(error);
                });
            }).removeAttr('disabled');

            return false;
        });
    });
}