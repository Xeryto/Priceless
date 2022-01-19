let ruText = document.querySelectorAll(".ru");
let enText = document.querySelectorAll(".en");
enText.forEach(function (item) {
    item.classList.add("hidden");
});

let ruSwitch = document.querySelector("#lngSwitch1");
let enSwitch = document.querySelector("#lngSwitch2");

ruSwitch.addEventListener("click", function () {
    ruText.forEach(function (item) {
        item.classList.remove("hidden");
    });
    enText.forEach(function (item) {
        item.classList.add("hidden");
    });
});
enSwitch.addEventListener("click", function () {
    enText.forEach(function (item) {
        item.classList.remove("hidden");
    });
    ruText.forEach(function (item) {
        item.classList.add("hidden");
    });
});