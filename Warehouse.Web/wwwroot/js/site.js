// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {
    $('.selectpicker').selectpicker();
    $('input[id="daterange"]').daterangepicker({
        "locale": {
            "format": "DD-MM-YYYY"
        },
        opens: 'right'
    }, function (start, end, label) {
        console.log("A new date selection was made: " + start.format('YYYY-MM-DD') + ' to ' + end.format('YYYY-MM-DD'));
    });
    $('input[id="datepicker"]').daterangepicker({
        "locale": {
            "format": "YYYY-MM-DD"
        },
        singleDatePicker: true,
        showDropdowns: true,
        minYear: 1970,
        maxYear: parseInt(moment().format('YYYY'), 10)
    });
});

