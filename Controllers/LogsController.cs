using System.Diagnostics;
using LeitorDeLogs.Models;
using LeitorDeLogs.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeitorDeLogs.Controllers
{
    public class LogsController : Controller
    {
        private readonly ILogCatalogService _catalogo;

        public LogsController(ILogCatalogService catalogo)
        {
            _catalogo = catalogo;
        }

        public IActionResult Index()
        {
            var dias = _catalogo.ListarDias();
            return View(dias);
        }

        [HttpGet("Logs/Dia/{data}")]
        public IActionResult Dia(DateOnly data)
        {
            ViewData["Data"] = data;
            return View(data);
        }

        [HttpGet("Logs/Dados/{data?}")]
        public IActionResult Dados(
            DateOnly data,
            int pagina = 1,
            int tamanhoPagina = 100,
            string? busca = null,
            string? naoContem = null,
            bool naoContemMultiplas = false,
            [FromQuery] string[]? niveis = null,
            string ordenarPor = "timestamp",
            string ordem = "desc",
            TimeOnly? horaInicio = null,
            TimeOnly? horaFim = null)
        {
            if (pagina < 1)
            {
                pagina = 1;
            }

            tamanhoPagina = Math.Clamp(tamanhoPagina, 10, 1000);

            if (horaInicio.HasValue && horaFim.HasValue && horaInicio.Value > horaFim.Value)
            {
                return BadRequest(new { erro = "A hora inicial deve ser menor ou igual à hora final." });
            }

            var entradas = _catalogo.ObterEntradas(data).AsEnumerable();

            if (horaInicio.HasValue || horaFim.HasValue)
            {
                // Compara na precisão de segundos (a mesma do campo de hora da tela),
                // com os dois limites inclusivos: "até 10:30:15" inclui 10:30:15.999.
                var inicio = horaInicio ?? TimeOnly.MinValue;
                var fim = horaFim ?? TimeOnly.MaxValue;
                entradas = entradas.Where(e =>
                {
                    var hora = TimeOnly.FromDateTime(e.Timestamp.DateTime);
                    var horaSegundos = new TimeOnly(hora.Hour, hora.Minute, hora.Second);
                    return horaSegundos >= inicio && horaSegundos <= fim;
                });
            }

            if (niveis is { Length: > 0 })
            {
                var niveisSet = niveis.ToHashSet(StringComparer.OrdinalIgnoreCase);
                entradas = entradas.Where(e => niveisSet.Contains(e.Nivel));
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                entradas = entradas.Where(e => e.Mensagem.Contains(busca, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(naoContem))
            {
                if (naoContemMultiplas)
                {
                    var termos = naoContem
                        .Split(',')
                        .Select(t => t.Trim())
                        .Where(t => t.Length > 0)
                        .ToList();

                    if (termos.Count > 0)
                    {
                        entradas = entradas.Where(e => !termos.Any(t => e.Mensagem.Contains(t, StringComparison.OrdinalIgnoreCase)));
                    }
                }
                else
                {
                    entradas = entradas.Where(e => !e.Mensagem.Contains(naoContem, StringComparison.OrdinalIgnoreCase));
                }
            }

            var ascendente = string.Equals(ordem, "asc", StringComparison.OrdinalIgnoreCase);

            entradas = ordenarPor.ToLowerInvariant() switch
            {
                "nivel" => ascendente
                    ? entradas.OrderBy(e => e.Nivel).ThenBy(e => e.Timestamp)
                    : entradas.OrderByDescending(e => e.Nivel).ThenByDescending(e => e.Timestamp),
                _ => ascendente
                    ? entradas.OrderBy(e => e.Timestamp)
                    : entradas.OrderByDescending(e => e.Timestamp),
            };

            var listaFiltrada = entradas.ToList();
            var totalFiltrado = listaFiltrada.Count;

            var itensPagina = listaFiltrada
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(e =>
                {
                    var mensagemUmaLinha = e.Mensagem.Replace('\n', ' ').Replace('\r', ' ');
                    return new
                    {
                        data = e.Timestamp.ToString("yyyy-MM-dd"),
                        hora = e.Timestamp.ToString("HH:mm:ss.fff"),
                        nivel = e.Nivel,
                        mensagem = e.Mensagem,
                        mensagemResumo = mensagemUmaLinha.Length > 80
                            ? string.Concat(mensagemUmaLinha.AsSpan(0, 80), "…")
                            : mensagemUmaLinha,
                    };
                })
                .ToList();

            return Json(new
            {
                total = _catalogo.ObterEntradas(data).Count,
                totalFiltrado,
                itens = itensPagina,
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
