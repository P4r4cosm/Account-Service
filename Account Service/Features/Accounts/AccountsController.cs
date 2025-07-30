using AccountService.Core.Domain;
using AccountService.Domain.Exceptions;
using AccountService.Features.Accounts.CheckOwnerHasAccounts;
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.DeleteAccount;
using AccountService.Features.Accounts.GetAccountById;
using AccountService.Features.Accounts.GetAccountById.GetAccountField;
using AccountService.Features.Accounts.GetAccounts;
using AccountService.Features.Accounts.UpdateAccount;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Accounts;

[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Создает новый банковский счёт.
    /// </summary>
    /// <remarks>
    /// ID счёта генерируется автоматически на сервере.
    /// Пример запроса:
    /// 
    ///     POST /accounts
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
    /// <returns>Информация о созданном счёте.</returns>
    /// <response code="201">Возвращает созданный счёт. В заголовке Location указан URL нового счёта.</response>
    /// <response code="400">Некорректные данные в запросе (ошибка валидации).</response>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
    {
        //отправляем команду в mediator
        var createdAccountDto = await _mediator.Send(command);
        // CreatedAtAction генерирует URL для получения только что созданного ресурса.
        return CreatedAtAction(nameof(GetAccountById),
            new { accountId = createdAccountDto.Id }, createdAccountDto);
    }

    /// <summary>
    /// Получает информацию о счёте по его ID.
    /// </summary>
    /// <param name="accountId">Идентификатор счёта (GUID).</param>
    /// <returns>Информация о счёте.</returns>
    /// <response code="200">Возвращает данные счёта.</response>
    /// <response code="404">Счёт с указанным ID не найден.</response>
    [HttpGet("{accountId:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountById([FromRoute] Guid accountId)
    {
        // 1. Создаем объект запроса с ID из URL.
        var query = new GetAccountByIdQuery(accountId);

        // 2. Отправляем запрос в MediatR.
        var resultDto = await _mediator.Send(query);

        // 3. Проверяем результат и возвращаем либо 200 OK, либо 404 Not Found.
        return resultDto is not null ? Ok(resultDto) : NotFound("Account not found");
    }
    
    /// <summary>
    /// Получает значение конкретного поля счёта.
    /// </summary>
    /// <param name="accountId">ID счёта.</param>
    /// <param name="fieldName">Имя поля (например, "balance", "currency", "ownerId"). Регистронезависимое.</param>
    /// <returns>Значение запрошенного поля.</returns>
    /// <response code="200">Возвращает значение поля. Может быть null, если поле не найдено или его значение null.</response>
    /// <response code="404">Счёт с указанным ID не найден.</response>
    [HttpGet("{accountId:guid}/{fieldName}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountField([FromRoute] Guid accountId, [FromRoute] string fieldName)
    {
        var query = new GetAccountFieldQuery(accountId, fieldName);
        var result = await _mediator.Send(query);
        
        return result is not null ? Ok(result) : NotFound("Field not found");
    }

    /// <summary>
    /// Получает постраничный список банковских счетов с фильтрацией.
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет получить список счетов с применением гибких фильтров и пагинации.
    /// 
    /// Пример запроса для получения первой страницы (20 счетов) в рублях с балансом от 1000:
    /// 
    ///     GET /api/accounts?PageNumber=1&PageSize=20&Currency=RUB&Balance_gte=1000
    /// 
    /// В ответе вы получите объект, содержащий список счетов (`items`) и метаданные пагинации (`totalCount`, `pageNumber`, `totalPages` и т.д.).
    /// </remarks>
    /// <param name="query">Объект с параметрами для фильтрации и пагинации. Все параметры опциональны.</param>
    /// <returns>Постраничный результат со списком счетов (`PagedResult<AccountDto>`).</returns>
    /// <response code="200">Запрос выполнен успешно. Возвращает объект с данными и метаинформацией о пагинации. Поле `items` может быть пустым, если по заданным фильтрам ничего не найдено.</response>
    /// <response code="400">Некорректные параметры запроса. Это может произойти, если, например, номер страницы меньше 1. Тело ответа будет содержать информацию об ошибках валидации.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts([FromQuery] GetAccountsQuery query)
    {
        var resultDto = await _mediator.Send(query);
        return Ok(resultDto);
    }


    /// <summary>
    /// Удаляет существующий банковский счёт.
    /// </summary>
    /// <param name="accountId">Идентификатор удаляемого счёта (GUID).</param>
    /// <response code="204">Счёт успешно удалён.</response>
    /// <response code="404">Счёт с указанным ID не найден.</response>
    [HttpDelete("{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount([FromRoute] Guid accountId)
    {
        var command = new DeleteAccountCommand(accountId);

        await _mediator.Send(command); // Отправляем команду

        // Если команда успешно выполнена (т.е. не бросила исключение),
        // возвращаем 204 No Content.
        return NoContent();
    }


    /// <summary>
    /// Проверяет наличие хотя бы одного счёта у клиента.
    /// </summary>
    /// <param name="ownerId">ID проверяемого клиента.</param>
    /// <returns>True, если у клиента есть счета, иначе false.</returns>
    /// <response code="200">Возвращает логическое значение.</response>
    [HttpGet("{ownerId:guid}/has-accounts")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> HasAccounts([FromRoute] Guid ownerId)
    {
        var query = new CheckOwnerHasAccountsQuery(ownerId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    /// <summary>
    /// Полностью обновляет данные счёта (идемпотентная операция).
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
    /// <response code="204">Данные счёта успешно обновлены.</response>
    /// <response code="400">Некорректные данные в запросе (ошибка валидации).</response>
    /// <response code="404">Счёт с указанным ID не найден.</response>
    [HttpPut("{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccount([FromRoute] Guid accountId, [FromBody] UpdateAccountCommand command)
    {
        // Устанавливаем ID из маршрута в команду, чтобы они были синхронизированы
        command.AccountId = accountId;
        await _mediator.Send(command);
        return NoContent();
    }
    
}