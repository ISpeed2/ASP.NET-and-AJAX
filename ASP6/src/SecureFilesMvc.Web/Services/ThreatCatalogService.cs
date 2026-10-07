using SecureFilesMvc.Web.Models.ThreatModel;

namespace SecureFilesMvc.Web.Services;

public class ThreatCatalogService : IThreatCatalogService
{
    private static readonly IReadOnlyList<TrustBoundary> Boundaries = new List<TrustBoundary>
    {
        new()
        {
            Id = "TB-01", Name = "Браузер → Web API (HTTP)",
            CrossingData = "HTTP-запросы: JSON, multipart/form-data",
            Controls = new[] { "HTTPS + HSTS", "Аутентификация по API-ключу", "Серверная валидация", "Rate limiting на прокси" }
        },
        new()
        {
            Id = "TB-02", Name = "Web API → Файловое хранилище",
            CrossingData = "Бинарные файлы",
            Controls = new[] { "GUID-имена", "Проверка magic bytes", "Хранение вне webroot", "Ограничение размера" }
        },
        new()
        {
            Id = "TB-03", Name = "Web API → БД (в будущем)", CrossingData = "SQL-запросы",
            Controls = new[] { "Параметризованные запросы", "Минимальные права пользователя БД" }
        }
    };

    private static readonly IReadOnlyList<Threat> Threats = new List<Threat>
    {
        new() { Id = "TH-001", Category = StrideCategory.Spoofing, Title = "Подмена клиента при загрузке файла", Description = "Злоумышленник отправляет файл без валидного API-ключа или с чужим ключом.", TrustBoundaryId = "TB-01", RiskScore = 7.5, Mitigations = new[] { "API-key auth", "Отказ 401", "Логирование попыток" }, BacklogItemIds = new[] { "SB-001" } },
        new() { Id = "TH-002", Category = StrideCategory.Tampering, Title = "Path Traversal при скачивании", Description = "Клиент пытается запросить файл вне каталога хранилища.", TrustBoundaryId = "TB-02", RiskScore = 9.0, Mitigations = new[] { "GUID-имена", "Path.GetFullPath и проверка префикса" }, BacklogItemIds = new[] { "SB-002" } },
        new() { Id = "TH-003", Category = StrideCategory.InformationDisclosure, Title = "Утечка файлов через предсказуемые URL", Description = "Предсказуемые имена позволяют перебирать чужие документы.", TrustBoundaryId = "TB-02", RiskScore = 6.5, Mitigations = new[] { "GUID-имена", "Авторизация каждого запроса", "Нет листинга" }, BacklogItemIds = new[] { "SB-003" } },
        new() { Id = "TH-004", Category = StrideCategory.DenialOfService, Title = "DoS через большие или множественные загрузки", Description = "Отправка больших файлов или множества параллельных запросов.", TrustBoundaryId = "TB-01", RiskScore = 7.0, Mitigations = new[] { "MaxRequestBodySize", "MultipartBodyLengthLimit", "Rate limiting" }, BacklogItemIds = new[] { "SB-004" } },
        new() { Id = "TH-005", Category = StrideCategory.ElevationOfPrivilege, Title = "Загрузка исполняемых файлов", Description = "Пользователь загружает содержимое, которое может исполниться на сервере или в браузере.", TrustBoundaryId = "TB-02", RiskScore = 8.5, Mitigations = new[] { "Whitelist расширений", "Проверка magic bytes", "Content-Disposition: attachment" }, BacklogItemIds = new[] { "SB-005" } },
        new() { Id = "TH-006", Category = StrideCategory.Repudiation, Title = "Отсутствие следов загрузки и скачивания", Description = "Нельзя доказать, кто и когда выполнил операцию.", TrustBoundaryId = "TB-01", RiskScore = 4.0, Mitigations = new[] { "Correlation ID", "Структурированное журналирование" }, BacklogItemIds = new[] { "SB-006" } }
    };

    private static readonly IReadOnlyList<SecurityBacklogItem> Backlog = new List<SecurityBacklogItem>
    {
        new() { Id = "SB-001", ThreatId = "TH-001", Title = "Внедрить ApiKeyAuthenticationHandler", Priority = BacklogPriority.Critical, Status = BacklogStatus.Done, MitigationSummary = "Реализован кастомный authentication handler", Owner = "backend" },
        new() { Id = "SB-002", ThreatId = "TH-002", Title = "Защита от path traversal", Priority = BacklogPriority.Critical, Status = BacklogStatus.Done, MitigationSummary = "GUID-имена и проверка абсолютного пути", Owner = "backend" },
        new() { Id = "SB-003", ThreatId = "TH-003", Title = "Авторизация на каждый файл", Priority = BacklogPriority.High, Status = BacklogStatus.InProgress, MitigationSummary = "Все операции требуют API-ключ; ownership запланирован", Owner = "backend" },
        new() { Id = "SB-004", ThreatId = "TH-004", Title = "Лимиты размера и rate limiting", Priority = BacklogPriority.High, Status = BacklogStatus.Done, MitigationSummary = "Установлен лимит 10 МБ; rate limiting оставлен прокси", Owner = "devops" },
        new() { Id = "SB-005", ThreatId = "TH-005", Title = "Whitelist MIME и magic bytes", Priority = BacklogPriority.Critical, Status = BacklogStatus.Done, MitigationSummary = "Расширение и сигнатура проверяются сервисом", Owner = "backend" },
        new() { Id = "SB-006", ThreatId = "TH-006", Title = "Structured logging с correlation ID", Priority = BacklogPriority.Medium, Status = BacklogStatus.InProgress, MitigationSummary = "RequestIdMiddleware добавляет область логирования", Owner = "backend" }
    };

    public IReadOnlyList<TrustBoundary> GetTrustBoundaries() => Boundaries;
    public IReadOnlyList<Threat> GetThreats() => Threats;
    public IReadOnlyList<SecurityBacklogItem> GetBacklog() => Backlog;
}
