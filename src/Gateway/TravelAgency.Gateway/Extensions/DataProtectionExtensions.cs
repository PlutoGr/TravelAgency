using Microsoft.AspNetCore.DataProtection;

namespace TravelAgency.Gateway.Extensions;

/// <summary>
/// Ключи DataProtection (шифрование cookie, antiforgery, IDataProtector).
/// Без настройки ASP.NET Core кладёт их в домашнюю папку процесса внутри контейнера,
/// и при каждом пересоздании контейнера (каждый деплой) появляется новый набор ключей.
/// </summary>
public static class DataProtectionExtensions
{
    /// <summary>Имя приложения для ключей: не зависит от пути, из которого запущен сервис.</summary>
    public const string ApplicationName = "TravelAgency.Gateway";

    /// <summary>Ключ конфигурации с каталогом ключей (в compose: DataProtection__KeysPath=/keys).</summary>
    public const string KeysPathKey = "DataProtection:KeysPath";

    public static IServiceCollection AddGatewayDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddDataProtection().SetApplicationName(ApplicationName);

        // Каталог задаётся в compose и смонтирован как volume, поэтому ключи переживают деплой.
        // Без настройки (тесты, запуск через dotnet run) остаётся поведение ASP.NET Core по умолчанию.
        var keysPath = configuration[KeysPathKey];
        if (!string.IsNullOrWhiteSpace(keysPath))
            builder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        return services;
    }
}
