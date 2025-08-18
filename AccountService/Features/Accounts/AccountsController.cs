using AccountService.Features.Accounts.CheckOwnerHasAccounts;
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.DeleteAccount;
using AccountService.Features.Accounts.GetAccountById;
using AccountService.Features.Accounts.GetAccountById.GetAccountField;
using AccountService.Features.Accounts.GetAccounts;
using AccountService.Features.Accounts.PatchAccount;
using AccountService.Features.Accounts.UpdateAccount;
using AccountService.Shared.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Accounts;

/// <summary>
/// Контроллер для управления банковскими счетами.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "v1")] // Группируем в отдельный раздел Swagger
public class AccountsController(IMediator mediator) : BaseApiController(mediator)
{
    // --- CREATE ---

    /// <summary>
    /// Создает новый банковский счёт.
    /// </summary>
    /// <remarks>
    /// ID счёта генерируется автоматически на сервере.
    /// Пример запроса:
    /// 
    ///     POST /api/accounts
    ///     {
    ///        "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "accountType": "Checking", // или "Deposit", "Credit"
    ///        "currency": "RUB",
    ///        "interestRate": null // или, например, 3.5 для вклада
    ///     }
    /// 
    /// Правила валидации:
    /// - ownerId: обязательное поле, должен быть валидным GUID
    /// - accountType: обязательное поле, допустимые значения: Checking, Deposit, Credit
    /// - currency: обязательное поле, должен быть валидным кодом валюты ISO 4217
    /// - interestRate: опционально, но если указано, должно быть >= 0
    /// </remarks>
    /// <param name="command">Данные для создания счёта.</param>
    /// <param name="correlationId">Необязательный идентификатор корреляции (из заголовка X-Correlation-ID).</param>
    /// <returns>Результат операции в формате MbResult. При успехе поле 'value' содержит данные созданного счёта.</returns>
    /// <response code="201">Счёт успешно создан. В теле ответа возвращается объект MbResult с данными счёта.</response>
    /// <response code="400">Некорректные данные в запросе. В теле ответа возвращается объект MbResult с деталями ошибки.</response>
    /// <response code="404">Клиент с указанным ownerId не найден. В теле ответа возвращается объект MbResult с деталями ошибки.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MbResult<AccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command,
        [FromHeader(Name = "X-Correlation-ID")]
        Guid? correlationId)
    {
        command.CorrelationId = correlationId;
        command.CommandId = Guid.NewGuid();
        var result = await Mediator.Send(command);

        // Проверяем, что результат успешный и содержит значение, прежде чем обращаться к result.Value
        return !result.IsSuccess
            ?
            // Если произошла ошибка (например, валидации), используем стандартный обработчик
            HandleResult(result)
            : HandleCreationResult(result, nameof(GetAccountById), new { accountId = result.Value!.Id });
    }

    // --- READ ---

    /// <summary>
    /// Получает постраничный список банковских счетов с фильтрацией.
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет получить список счетов с применением гибких фильтров и пагинации.
    /// 
    /// Пример запроса для получения первой страницы (20 счетов) в рублях с балансом от 1000:
    /// 
    /// <code>
    /// <![CDATA[
    /// GET /api/accounts?PageNumber=1&PageSize=20&Currency=RUB&Balance_gte=1000
    /// ]]>
    /// </code>
    /// 
    /// В ответе вы получите объект, содержащий список счетов (`items`) и метаданные пагинации (`totalCount`, `pageNumber`, `totalPages` и т.д.).
    /// </remarks>
    /// <param name="query">Параметры для фильтрации и пагинации.</param>
    /// <returns>Результат операции в формате MbResult. При успехе 'value' содержит постраничный список счетов.</returns>
    /// <response code="200">Запрос выполнен. В теле ответа возвращается объект MbResult с данными.</response>
    /// <response code="400">Некорректные параметры запроса. В теле ответа возвращается объект MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet]
    [ProducesResponseType(typeof(MbResult<PagedResult<AccountDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccounts([FromQuery] GetAccountsQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Получает информацию о счёте по его ID.
    /// </summary>
    /// <param name="accountId">Идентификатор счёта (GUID).</param>
    /// <returns>Результат операции в формате MbResult. При успехе 'value' содержит данные счёта.</returns>
    /// <response code="200">Возвращает объект MbResult с данными счёта.</response>
    /// <response code="404">Счёт не найден. Возвращает объект MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("{accountId:guid}", Name = "GetAccountById")]
    [ProducesResponseType(typeof(MbResult<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountById([FromRoute] Guid accountId)
    {
        var query = new GetAccountByIdQuery { AccountId = accountId };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }


    /// <summary>
    /// Получает значение конкретного поля счёта.
    /// </summary>
    /// <param name="accountId">ID счёта.</param>
    /// <param name="fieldName">Имя поля (например, "balance", "currency", "ownerId"). Регистронезависимое.</param>
    /// <returns>Значение запрошенного поля.</returns>
    /// <response code="200">Возвращает значение поля. Может быть null, если поле не найдено или его значение null.</response>
    /// <response code="404">Счёт не найден. Возвращает объект MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("{accountId:guid}/{fieldName}")]
    [ProducesResponseType(typeof(MbResult<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountField([FromRoute] Guid accountId, [FromRoute] string fieldName)
    {
        var query = new GetAccountFieldQuery(accountId, fieldName);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Проверяет наличие хотя бы одного счёта у клиента.
    /// </summary>
    /// <param name="ownerId">ID проверяемого клиента.</param>
    /// <returns>Результат операции в формате MbResult. При успехе 'value' содержит true или false.</returns>
    /// <response code="200">Возвращает MbResult с логическим значением.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("owner/{ownerId:guid}/has-accounts")]
    [ProducesResponseType(typeof(MbResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckOwnerHasAccounts([FromRoute] Guid ownerId)
    {
        var query = new CheckOwnerHasAccountsQuery(ownerId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    // --- UPDATE ---

    /// <summary>
    /// Полностью обновляет данные счёта (идемпотентная операция PUT).
    /// </summary>
    /// <remarks>
    /// Этот метод заменяет все изменяемые данные счёта на те, что переданы в теле запроса.
    /// Неизменяемые поля (ID, баланс, дата открытия и т.д.) игнорируются.
    /// 
    /// Пример запроса:
    /// 
    ///     PUT /api/accounts/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     {
    ///         "ownerId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
    ///         "interestRate": 4.2
    ///     }
    /// 
    /// </remarks>
    /// <param name="accountId">ID счёта для обновления.</param>
    /// <param name="command">Данные для обновления.</param>
    /// <param name="correlationId">Необязательный идентификатор корреляции (из заголовка X-Correlation-ID).</param>
    /// <returns>Результат операции в формате MbResult (без значения).</returns>
    /// <response code="200">Данные успешно обновлены. Возвращает успешный MbResult.</response>
    /// <response code="400">Некорректные данные. Возвращает MbResult с ошибкой.</response>
    /// <response code="404">Счёт не найден. Возвращает MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpPut("{accountId:guid}")]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAccount([FromRoute] Guid accountId, [FromBody] UpdateAccountCommand command,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId)

    {
        command.CorrelationId = correlationId;
        command.CommandId = Guid.NewGuid();
        command.AccountId = accountId;
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Частично обновляет данные счёта (PATCH).
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет обновить одно или несколько полей счёта.
    /// Передавайте в теле запроса только те поля, которые необходимо изменить.
    ///
    /// Пример запроса на смену владельца:
    ///
    ///     PATCH /api/accounts/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     {
    ///        "ownerId": "a1b2c3d4-e5f6-7890-1234-567890abcdef"
    ///     }
    /// </remarks>
    /// <param name="accountId">ID счёта для обновления.</param>
    /// <param name="command">Данные для обновления.</param>
    /// <param name="correlationId">Необязательный идентификатор корреляции (из заголовка X-Correlation-ID).</param>
    /// <returns>Результат операции в формате MbResult (без значения).</returns>
    /// <response code="200">Данные успешно обновлены. Возвращает успешный MbResult.</response>
    /// <response code="400">Некорректные данные. Возвращает MbResult с ошибкой.</response>
    /// <response code="404">Счёт не найден. Возвращает MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpPatch("{accountId:guid}")]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PatchAccount([FromRoute] Guid accountId, [FromBody] PatchAccountCommand command
        ,   [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId)
    {
        command.CorrelationId = correlationId;
        command.CommandId = Guid.NewGuid();
        command.AccountId = accountId;
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // --- DELETE ---

    /// <summary>
    /// Удаляет существующий банковский счёт.
    /// </summary>
    /// <param name="accountId">Идентификатор удаляемого счёта.</param>
    /// <param name="correlationId">Необязательный идентификатор корреляции (из заголовка X-Correlation-ID).</param>
    /// <returns>Результат операции в формате MbResult (без значения).</returns>
    /// <response code="200">Счёт успешно удалён. Возвращает успешный MbResult.</response>
    /// <response code="404">Счёт не найден. Возвращает MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpDelete("{accountId:guid}")]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromRoute] Guid accountId,
        [FromHeader(Name = "X-Correlation-ID")]
        Guid? correlationId)
    {
        var command = new DeleteAccountCommand
        {
            AccountId = accountId,
            CorrelationId = correlationId,
            CommandId = Guid.NewGuid()
        };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}