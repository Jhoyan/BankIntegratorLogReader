using LeitorDeLogs.Models;

namespace LeitorDeLogs.Services
{
    public interface ILogCatalogService
    {
        List<LogDiaResumo> ListarDias();

        List<LogEntry> ObterEntradas(DateOnly data);
    }
}
