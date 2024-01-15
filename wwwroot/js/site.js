function pack_dd(data, id, value, defaultSelect, defaultValue) {
    var result = [];

    if (defaultSelect === true) {
        if (typeof defaultValue !== "undefined") {
            result.push({ value: defaultValue, text: "" });
        } else {
            result.push({ value: null, text: "" });
        }
    }

    if (data.length > 0) {
        $.each(data, function(i, v) {
            result.push({ value: v[id], text: v[value] });
        });
    }

    return result;
}

function call_ajax(type, url, data) {
    if (typeof data === "undefined") {
        data = {};
    }

    return $.ajax({
        type: type,
        url: url,
        data: data,
        dataType: "json",
        cache: false,
    });
}