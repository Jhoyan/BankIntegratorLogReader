namespace LeitorDeLogs.Models
{
    public class LogEntry
    {
        public required DateTimeOffset Timestamp { get; set; }

        public required string Nivel { get; set; }

        public required string Mensagem { get; set; }
    }
}
