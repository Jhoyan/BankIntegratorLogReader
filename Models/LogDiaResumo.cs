namespace LeitorDeLogs.Models
{
    public class LogDiaResumo
    {
        public required DateOnly Data { get; set; }

        public int QuantidadeEntradas { get; set; }

        public long TamanhoBytes { get; set; }

        public List<string> Arquivos { get; set; } = new();
    }
}
