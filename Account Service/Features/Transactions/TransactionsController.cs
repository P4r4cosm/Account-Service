using AccountService.Features.Transactions.GetAccountStatement;
using AccountService.Features.Transactions.GetTransactionById;
using AccountService.Features.Transactions.RegisterTransaction;
using AccountService.Shared.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Transactions;

/// <summary>
/// Управляет транзакциями и выписками по счетам.
/// </summary>
[ApiController]
[Route("api/accounts/{accountId:guid}/transactions")]
[Produces("application/json")]
public class TransactionsController(IMediator mediator) : BaseApiController(mediator) // Наследуемся от BaseApiController
{
    /// <summary>
    /// Регистрирует новую транзакцию (пополнение или списание) для указанного счёта.
    /// </summary>
    /// <remarks>
    /// Эта операция изменяет баланс счёта. Используется для операций, не являющихся переводом,
    /// например, пополнение наличными в кассе или списание комиссии.
    /// 
    /// **Пример запроса на пополнение счёта на 1000 рублей:**
    /// 
    ///     POST /api/accounts/3fa85f64-5717-4562-b3fc-2c963f66afa6/transactions
    ///     {
    ///        "amount": 1000,
    ///        "type": "Credit", // "Credit" для пополнения, "Debit" для списания
    ///        "description": "Пополнение наличными через кассу"
    ///     }
    /// 
    /// </remarks>
    /// <param name="accountId">ID счёта, по которому проводится транзакция.</param>
    /// <param name="command">Данные для создания транзакции.</param>
    /// <returns>Результат операции в формате MbResult. При успехе 'value' содержит данные созданной транзакции.</returns>
    /// <response code="201">Транзакция успешно создана. В теле ответа возвращается объект MbResult с данными транзакции.</response>
    /// <response code="400">Некорректные данные или нарушение бизнес-правил. Возвращает MbResult с ошибкой.</response>
    /// <response code="404">Счёт не найден. Возвращает MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MbResult<TransactionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterTransaction([FromRoute] Guid accountId, [FromBody] RegisterTransactionCommand command)
    {
        command.AccountId = accountId;
        var result = await Mediator.Send(command);

        // Проверяем, что результат успешный и содержит значение, прежде чем обращаться к result.Value
        return !result.IsSuccess ?
            // Если произошла ошибка (например, валидации), используем стандартный обработчик
            HandleResult(result) : HandleCreationResult(result, nameof(GetTransactionById), new { accountId = result.Value!.AccountId, transactionId = result.Value.Id });
    }

    /// <summary>
    /// Получает выписку по счёту за указанный период.
    /// </summary>
    /// <remarks>
    /// Возвращает объект, содержащий как информацию о самом счёте (текущий баланс, валюта и т.д.),
    /// так и пагинированный список транзакций за выбранный период.
    /// 
    /// **Пример запроса на получение первой страницы выписки (20 транзакций):**
    /// <code>
    /// <![CDATA[
    ///     GET /api/accounts/3fa85f64-5717-4562-b3fc-2c963f66afa6/transactions?pageNumber=1&pageSize=20
    /// ]]>
    /// </code>
    /// **Пример запроса за конкретный период:**
    /// <code>
    /// <![CDATA[
    ///     GET /api/accounts/3fa85f64-5717-4562-b3fc-2c963f66afa6/transactions?startDate=2025-07-01&endDate=2025-07-31
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="accountId">ID счёта, для которого запрашивается выписка.</param>
    /// <param name="query">Параметры для фильтрации по дате и пагинации.</param>
    /// <returns>Результат операции в формате MbResult. При успехе 'value' содержит полную выписку по счёту.</returns>
    /// <response code="200">Запрос выполнен. Возвращает объект MbResult с данными выписки.</response>
    /// <response code="404">Счёт не найден. Возвращает MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet]
    [ProducesResponseType(typeof(MbResult<AccountStatementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountStatement([FromRoute] Guid accountId, [FromQuery] GetAccountStatementQuery query)
    {
        query.AccountId = accountId;
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Получает одну транзакцию по её ID в контексте указанного счёта.
    /// </summary>
    /// <param name="accountId">ID счёта, к которому относится транзакция.</param>
    /// <param name="transactionId">ID искомой транзакции.</param>
    /// <returns>Результат операции в формате MbResult. При успехе 'value' содержит данные транзакции.</returns>
    /// <response code="200">Транзакция найдена. Возвращает MbResult с данными транзакции.</response>
    /// <response code="404">Счёт или транзакция не найдены. Возвращает MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpGet("{transactionId:guid}", Name = "GetTransactionById")]
    [ProducesResponseType(typeof(MbResult<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactionById([FromRoute] Guid accountId, [FromRoute] Guid transactionId)
    {
        var query = new GetTransactionByIdQuery(accountId, transactionId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}