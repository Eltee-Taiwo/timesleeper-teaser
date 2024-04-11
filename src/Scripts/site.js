$(document).scroll(function () {
    SetHeaderAndScrollVisibility();
});

$(document).ready(function () {
    SetHeaderAndScrollVisibility();
});

$(window).resize(function () {
    SetHeaderAndScrollVisibility();
});

function SetHeaderAndScrollVisibility() {
    let $nav = $(".navbar-fixed-top");
    let $scrollIndicator = $("#scrollIndicatorDiv");

    $nav.toggleClass('scrolled', $(this).scrollTop() > $nav.height() + 5);
    $scrollIndicator.toggleClass('clear', $(this).scrollTop() > $nav.height() + 5);
}