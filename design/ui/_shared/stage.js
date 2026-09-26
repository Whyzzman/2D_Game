/* Масштабує сцену 1920×1080 під вікно браузера, щоб прототип дивилися в
   справжніх пропорціях. Дає перемикач 16:9 / 16:10 — обидва співвідношення
   оголошені як цільові, тож обидва треба перевіряти. */
(function () {
  "use strict";

  function fit() {
    var stage = document.querySelector(".stage");
    if (!stage) { return; }

    var width = stage.classList.contains("stage--wide") ? 1920 : 1920;
    var height = stage.classList.contains("stage--wide") ? 1200 : 1080;
    var scale = Math.min(window.innerWidth / width, window.innerHeight / height);

    stage.style.transform = "scale(" + scale + ")";
    document.body.style.width = width * scale + "px";
    document.body.style.height = height * scale + "px";
    document.body.style.margin = "0 auto";

    var note = document.querySelector(".viewport-note");
    if (note) {
      note.textContent = width + "×" + height + "  ·  " +
        (stage.classList.contains("stage--wide") ? "16:10" : "16:9") +
        "  ·  масштаб " + scale.toFixed(2) + "  ·  A — змінити співвідношення";
    }
  }

  window.addEventListener("resize", fit);
  window.addEventListener("DOMContentLoaded", function () {
    fit();
    document.addEventListener("keydown", function (event) {
      if (event.key === "a" || event.key === "A" || event.key === "ф" || event.key === "Ф") {
        var stage = document.querySelector(".stage");
        if (stage) {
          stage.classList.toggle("stage--wide");
          fit();
        }
      }
    });
  });
})();
