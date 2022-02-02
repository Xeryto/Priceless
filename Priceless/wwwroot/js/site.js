let ruText = document.querySelectorAll(".ru");
let enText = document.querySelectorAll(".en");

let ruSwitch = document.querySelector("#lngSwitch1");
let enSwitch = document.querySelector("#lngSwitch2");
let links = document.querySelectorAll(".linkLng");

function get(name) {
    if (name = (new RegExp('[?&]' + encodeURIComponent(name) + '=([^&]*)')).exec(location.search))
        return decodeURIComponent(name[1]);
}

let curLng = get('lng');
if (curLng != null) {
    if (curLng == "en") {
        ruText.forEach(function (item) {
            item.classList.add("hidden");
        });
        links.forEach(function (link) {
            let url = new URL(link.href);
            url.searchParams.set('lng', "en")
            link.href = url;
        });
    } else {
        enText.forEach(function (item) {
            item.classList.add("hidden");
        });
        links.forEach(function (link) {
            let url = new URL(link.href);
            url.searchParams.set('lng', "ru");
            link.href = url;
        });
    }
}
else {
    enText.forEach(function (item) {
        item.classList.add("hidden");
    });
    links.forEach(function (link) {
        let url = new URL(link.href);
        url.searchParams.set('lng', "ru");
        link.href = url;
    });
}

ruSwitch.addEventListener("click", function () {
    ruText.forEach(function (item) {
        item.classList.remove("hidden");
    });
    enText.forEach(function (item) {
        item.classList.add("hidden");
    });
    links.forEach(function (link) {
        let url = new URL(link.href);
        url.searchParams.set('lng', "ru");
        link.href = url;
    });
});
enSwitch.addEventListener("click", function () {
    enText.forEach(function (item) {
        item.classList.remove("hidden");
    });
    ruText.forEach(function (item) {
        item.classList.add("hidden");
    });
    links.forEach(function (link) {
        let url = new URL(link.href);
        url.searchParams.set('lng', "en");
        link.href = url;
    });
});


