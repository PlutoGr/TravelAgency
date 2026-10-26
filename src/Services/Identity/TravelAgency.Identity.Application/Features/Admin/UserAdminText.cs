namespace TravelAgency.Identity.Application.Features.Admin;

public static class UserAdminText
{
    public const string LastAdmin = "Нельзя: в системе должен остаться хотя бы один администратор";
    public const string SelfBlock = "Нельзя заблокировать собственный аккаунт";
    public const string SelfDowngrade = "Нельзя понизить собственную роль";
    public const string AlreadyBlocked = "Пользователь уже заблокирован";
    public const string NotBlocked = "Пользователь не заблокирован";
    public const string UnknownRole = "Неизвестная роль";
    public const string UnknownStatus = "Неизвестный статус";
    public const string QueryTooLong = "Запрос не длиннее 100 символов";
    public const string InvalidPage = "Неверный номер страницы";
    public const string RoleChanged = "Роль изменена";
    public const string RoleUnchanged = "Роль не изменилась";
    public const string UserBlocked = "Пользователь заблокирован";
    public const string UserUnblocked = "Пользователь разблокирован";
    public const string ActorNotAdmin = "Недостаточно прав";
    public const string UserNotFound = "Пользователь не найден";
    public const string ReasonRequired = "Укажите причину блокировки";
    public const string ReasonTooLong = "Причина не длиннее 500 символов";
    public const string ActiveStatus = "Active";
    public const string BlockedStatus = "Blocked";

    public const int PageSize = 20;
    public const int MaxQueryLength = 100;
    public const int AuditTake = 50;
}
