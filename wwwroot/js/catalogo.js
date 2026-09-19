(function () {
    "use strict";

    const container = document.getElementById("catalogo-meses");
    if (!container) {
        return;
    }

    const btnExpandirTudo = document.getElementById("btn-expandir-tudo");
    const btnRecolherTudo = document.getElementById("btn-recolher-tudo");

    container.querySelectorAll(".collapse").forEach((el) => {
        el.addEventListener("show.bs.collapse", () => atualizarSeta(el, true));
        el.addEventListener("hide.bs.collapse", () => atualizarSeta(el, false));
    });

    function atualizarSeta(elColapsavel, expandido) {
        const botao = container.querySelector(`button[data-bs-target="#${elColapsavel.id}"]`);
        const seta = botao?.querySelector(".seta-mes");
        if (seta) {
            seta.textContent = expandido ? "▾" : "▸";
        }
    }

    function alternarTodos(expandir) {
        container.querySelectorAll(".collapse").forEach((el) => {
            const instancia = bootstrap.Collapse.getOrCreateInstance(el, { toggle: false });
            if (expandir) {
                instancia.show();
            } else {
                instancia.hide();
            }
        });
    }

    btnExpandirTudo?.addEventListener("click", () => alternarTodos(true));
    btnRecolherTudo?.addEventListener("click", () => alternarTodos(false));
})();
