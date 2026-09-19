using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LeitorDeLogs.Models;

namespace LeitorDeLogs.Services
{
    public static partial class LogFileParser
    {
        [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[(\w{2,5})\] (.*)$")]
        private static partial Regex LinhaDeEntradaRegex();

        public static List<LogEntry> Parse(string caminhoArquivo)
        {
            var entradas = new List<LogEntry>();
            var regex = LinhaDeEntradaRegex();

            DateTimeOffset? timestampAtual = null;
            string? nivelAtual = null;
            StringBuilder? mensagemAtual = null;

            void FinalizarEntradaAtual()
            {
                if (timestampAtual is null || nivelAtual is null || mensagemAtual is null)
                {
                    return;
                }

                entradas.Add(new LogEntry
                {
                    Timestamp = timestampAtual.Value,
                    Nivel = nivelAtual,
                    Mensagem = mensagemAtual.ToString(),
                });
            }

            foreach (var linha in File.ReadLines(caminhoArquivo, Encoding.UTF8))
            {
                var match = regex.Match(linha);
                if (match.Success)
                {
                    FinalizarEntradaAtual();

                    timestampAtual = DateTimeOffset.ParseExact(
                        match.Groups[1].Value,
                        "yyyy-MM-dd HH:mm:ss.fff zzz",
                        CultureInfo.InvariantCulture);
                    nivelAtual = match.Groups[2].Value;
                    mensagemAtual = new StringBuilder(match.Groups[3].Value);
                }
                else if (mensagemAtual is not null)
                {
                    mensagemAtual.Append('\n').Append(linha);
                }
            }

            FinalizarEntradaAtual();

            return entradas;
        }

        public static int ContarEntradas(string caminhoArquivo)
        {
            var regex = LinhaDeEntradaRegex();
            var quantidade = 0;

            foreach (var linha in File.ReadLines(caminhoArquivo, Encoding.UTF8))
            {
                if (regex.IsMatch(linha))
                {
                    quantidade++;
                }
            }

            return quantidade;
        }
    }
}
