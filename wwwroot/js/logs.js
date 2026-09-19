(function () {
    "use strict";

    const estado = {
        pagina: 1,
        tamanhoPagina: 100,
        busca: "",
        naoContem: "",
        naoContemMultiplas: false,
        niveis: [],
        ordenarPor: "timestamp",
        ordem: "desc",
    };

    const corpoTabela = document.getElementById("corpo-tabela");
    const resumoPaginacao = document.getElementById("resumo-paginacao");
    const btnAnterior = document.getElementById("btn-anterior");
    const btnProxima = document.getElementById("btn-proxima");
    const inputBusca = document.getElementById("busca");
    const inputNaoContem = document.getElementById("nao-contem");
    const checkNaoContemMultiplas = document.getElementById("nao-contem-multiplas");
    const filtroNiveis = document.getElementById("filtro-niveis");
    const cabecalhos = document.querySelectorAll("th.ordenavel");

    let ultimoTotalFiltrado = 0;
    let debounceTimer = null;
    let debounceTimerNaoContem = null;

    function niveisAtivos() {
        return Array.from(filtroNiveis.querySelectorAll("input[type=checkbox]:checked")).map((c) => c.value);
    }

    function montarUrl() {
        const params = new URLSearchParams();
        params.set("data", DATA_LOGS);
        params.set("pagina", estado.pagina);
        params.set("tamanhoPagina", estado.tamanhoPagina);
        params.set("ordenarPor", estado.ordenarPor);
        params.set("ordem", estado.ordem);
        if (estado.busca) {
            params.set("busca", estado.busca);
        }
        if (estado.naoContem) {
            params.set("naoContem", estado.naoContem);
            params.set("naoContemMultiplas", estado.naoContemMultiplas);
        }
        estado.niveis.forEach((n) => params.append("niveis", n));
        return `/Logs/Dados?${params.toString()}`;
    }

    function classeNivel(nivel) {
        const n = (nivel || "").toLowerCase();
        return `badge-nivel nivel-${n}`;
    }

    function escapeHtml(texto) {
        const div = document.createElement("div");
        div.textContent = texto;
        return div.innerHTML;
    }

    function escapeRegExp(texto) {
        return texto.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    }

    function destacarTermo(texto, termo) {
        const textoEscapado = escapeHtml(texto);
        if (!termo) {
            return textoEscapado;
        }
        const regex = new RegExp(escapeRegExp(escapeHtml(termo)), "gi");
        return textoEscapado.replace(regex, (trecho) => `<mark>${trecho}</mark>`);
    }

    function renderizarLinhas(itens) {
        corpoTabela.innerHTML = "";

        if (itens.length === 0) {
            const tr = document.createElement("tr");
            tr.innerHTML = `<td colspan="4" class="text-center text-muted py-4">Nenhuma entrada encontrada.</td>`;
            corpoTabela.appendChild(tr);
            return;
        }

        itens.forEach((item) => {
            const linha = document.createElement("tr");
            linha.className = "linha-log";
            linha.innerHTML = `
                <td>${escapeHtml(item.data)}</td>
                <td>${escapeHtml(item.hora)}</td>
                <td><span class="badge ${classeNivel(item.nivel)}">${escapeHtml(item.nivel)}</span></td>
                <td class="coluna-mensagem">${destacarTermo(item.mensagemResumo, estado.busca)}</td>
            `;

            const linhaDetalhe = document.createElement("tr");
            linhaDetalhe.className = "linha-detalhe d-none";
            linhaDetalhe.innerHTML = `<td colspan="4"><pre>${destacarTermo(item.mensagem, estado.busca)}</pre></td>`;

            linha.addEventListener("click", () => {
                linhaDetalhe.classList.toggle("d-none");
            });

            corpoTabela.appendChild(linha);
            corpoTabela.appendChild(linhaDetalhe);
        });
    }

    function atualizarPaginacao(total, totalFiltrado) {
        ultimoTotalFiltrado = totalFiltrado;
        const inicio = totalFiltrado === 0 ? 0 : (estado.pagina - 1) * estado.tamanhoPagina + 1;
        const fim = Math.min(estado.pagina * estado.tamanhoPagina, totalFiltrado);
        resumoPaginacao.textContent = `${inicio}–${fim} de ${totalFiltrado} (total do dia: ${total})`;

        btnAnterior.disabled = estado.pagina <= 1;
        btnProxima.disabled = estado.pagina * estado.tamanhoPagina >= totalFiltrado;
    }

    function atualizarIndicadorOrdenacao() {
        cabecalhos.forEach((th) => {
            th.classList.remove("text-primary");
            const campo = th.dataset.ordenarPor;
            const marcador = th.querySelector(".marcador-ordem");
            if (marcador) {
                marcador.remove();
            }
            if (campo === estado.ordenarPor) {
                th.classList.add("text-primary");
                const span = document.createElement("span");
                span.className = "marcador-ordem";
                span.textContent = estado.ordem === "asc" ? " ▲" : " ▼";
                th.appendChild(span);
            }
        });
    }

    async function carregar() {
        corpoTabela.innerHTML = `<tr><td colspan="4" class="text-center text-muted py-4">Carregando...</td></tr>`;
        try {
            const resposta = await fetch(montarUrl());
            if (!resposta.ok) {
                throw new Error(`Erro HTTP ${resposta.status}`);
            }
            const dados = await resposta.json();
            renderizarLinhas(dados.itens);
            atualizarPaginacao(dados.total, dados.totalFiltrado);
        } catch (erro) {
            corpoTabela.innerHTML = `<tr><td colspan="4" class="text-center text-danger py-4">Falha ao carregar os logs: ${escapeHtml(erro.message)}</td></tr>`;
        }
    }

    inputBusca.addEventListener("input", () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            estado.busca = inputBusca.value.trim();
            estado.pagina = 1;
            carregar();
        }, 300);
    });

    inputNaoContem.addEventListener("input", () => {
        clearTimeout(debounceTimerNaoContem);
        debounceTimerNaoContem = setTimeout(() => {
            estado.naoContem = inputNaoContem.value.trim();
            estado.pagina = 1;
            carregar();
        }, 300);
    });

    checkNaoContemMultiplas.addEventListener("change", () => {
        estado.naoContemMultiplas = checkNaoContemMultiplas.checked;
        estado.pagina = 1;
        carregar();
    });

    filtroNiveis.addEventListener("change", () => {
        estado.niveis = niveisAtivos();
        estado.pagina = 1;
        carregar();
    });

    cabecalhos.forEach((th) => {
        th.addEventListener("click", () => {
            const campo = th.dataset.ordenarPor;
            if (estado.ordenarPor === campo) {
                estado.ordem = estado.ordem === "asc" ? "desc" : "asc";
            } else {
                estado.ordenarPor = campo;
                estado.ordem = "desc";
            }
            estado.pagina = 1;
            atualizarIndicadorOrdenacao();
            carregar();
        });
    });

    btnAnterior.addEventListener("click", () => {
        if (estado.pagina > 1) {
            estado.pagina--;
            carregar();
        }
    });

    btnProxima.addEventListener("click", () => {
        if (estado.pagina * estado.tamanhoPagina < ultimoTotalFiltrado) {
            estado.pagina++;
            carregar();
        }
    });

    estado.niveis = niveisAtivos();
    atualizarIndicadorOrdenacao();
    carregar();
})();
