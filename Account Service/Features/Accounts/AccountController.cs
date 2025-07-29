using BankAccounts.Features.Accounts.CreateAccount;
using BankAccounts.Features.Accounts.GetAccountById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BankAccounts.Features.Accounts;

[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
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
        return resultDto is not null ? Ok(resultDto) : NotFound();
    }
    
}