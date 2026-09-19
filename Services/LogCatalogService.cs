using System.Globalization;
using System.Text.RegularExpressions;
using LeitorDeLogs.Models;
using Microsoft.Extensions.Caching.Memory;

namespace LeitorDeLogs.Services
{
    public partial class LogCatalogService : ILogCatalogService
    {
        private readonly string _pastaLogs;
        private readonly IMemoryCache _cache;

        [GeneratedRegex(@"-(\d{8})(?:_\d+)?\.txt$", RegexOptions.IgnoreCase)]
        private static partial Regex NomeArquivoRegex();

        public LogCatalogService(IWebHostEnvironment ambiente, IMemoryCache cache)
        {
            _pastaLogs = Path.Combine(ambiente.ContentRootPath, "logs");
            _cache = cache;
        }

        private IEnumerable<(string Caminho, DateOnly Data)> ListarArquivosDeLog()
        {
            if (!Directory.Exists(_pastaLogs))
            {
                yield break;
            }

            foreach (var caminho in Directory.EnumerateFiles(_pastaLogs, "*.txt"))
            {
                var match = NomeArquivoRegex().Match(Path.GetFileName(caminho));
                if (!match.Success)
                {
                    continue;
                }

                var data = DateOnly.ParseExact(match.Groups[1].Value, "yyyyMMdd", CultureInfo.InvariantCulture);
                yield return (caminho, data);
            }
        }

        public List<LogDiaResumo> ListarDias()
        {
            return ListarArquivosDeLog()
                .GroupBy(a => a.Data)
                .Select(g =>
                {
                    var arquivos = g.Select(a => a.Caminho).OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
                    var tamanhoBytes = arquivos.Sum(a => new FileInfo(a).Length);
                    return new LogDiaResumo
                    {
                        Data = g.Key,
                        Arquivos = arquivos.Select(Path.GetFileName).Select(n => n!).ToList(),
                        TamanhoBytes = tamanhoBytes,
                        QuantidadeEntradas = arquivos.Sum(LogFileParser.ContarEntradas),
                    };
                })
                .OrderByDescending(d => d.Data)
                .ToList();
        }

        public List<LogEntry> ObterEntradas(DateOnly data)
        {
            var arquivos = ListarArquivosDeLog()
                .Where(a => a.Data == data)
                .Select(a => a.Caminho)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (arquivos.Count == 0)
            {
                return new List<LogEntry>();
            }

            var ultimaEscrita = arquivos.Max(a => File.GetLastWriteTimeUtc(a));
            var chaveCache = $"logs:{data:yyyyMMdd}:{ultimaEscrita.Ticks}";

            return _cache.GetOrCreate(chaveCache, entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));

                var entradas = new List<LogEntry>();
                foreach (var arquivo in arquivos)
                {
                    entradas.AddRange(LogFileParser.Parse(arquivo));
                }

                return entradas.OrderBy(e => e.Timestamp).ToList();
            }) ?? new List<LogEntry>();
        }
    }
}
