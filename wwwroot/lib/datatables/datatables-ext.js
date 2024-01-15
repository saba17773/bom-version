// @ts-nocheck

function loadGrid(o) {
  $(o.el).DataTable(o);

  $(o.el + " thead tr:eq(1) th").each(function(i) {
    $(this).html('<input type="text" style="width: 100%;" />');
    $("input", this).on("keyup change", function(e) {
      if (
        $(o.el)
          .DataTable()
          .column(i)
          .search() !== this.value
      ) {
        if (e.which === 13 || this.value === "") {
          $(o.el)
            .DataTable()
            .column(i)
            .search(this.value)
            .draw();
        }
      }
    });
  });
}

function getRowsSelected(selector) {
  var data = [];

  var row_selected = $(selector)
    .DataTable()
    .rows(".selected", { selected: true })
    .data();

  if (typeof row_selected !== "undefined" && row_selected.length > 0) {
    $.each(row_selected, function(i, v) {
      data.push(v);
    });

    return data;
  } else {
    return [];
  }
}

function reloadGrid(selector, resetPage) {
  if (typeof resetPage != "undefined") {
    $(selector)
      .DataTable()
      .ajax.reload(null, false);
  } else {
    $(selector)
      .DataTable()
      .ajax.reload();
  }
}
