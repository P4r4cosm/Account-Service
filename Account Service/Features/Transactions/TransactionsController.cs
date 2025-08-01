using AccountService.Features.Transactions.GetAccountStatement;
using AccountService.Features.Transactions.GetTransactionById;
using AccountService.Features.Transactions.RegisterTransaction;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Transactions;

/// <summary>
/// Управляет транзакциями и выписками по счетам.
/// </summary>
[ApiController]
[Route("api/accounts/{accountId:guid}/transactions")]
[Produces("application/json")]
public class TransactionsController(IMediator mediator) : ControllerBase
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
    /// <returns>Информация о созданной транзакции.</returns>
    /// <response code="201">Транзакция успешно создана и баланс обновлен. Возвращает созданную транзакцию.</response>
    /// <response code="400">Некорректные данные в запросе или нарушение бизнес-правил (например, списание с недостаточным балансом).</response>
    /// <response code="404">Счёт с указанным ID не найден.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterTransaction([FromRoute] Guid accountId, [FromBody] RegisterTransactionCommand command)
    {
        command.AccountId = accountId;
        var transactionDto = await mediator.Send(command);
        
        // Теперь эта ссылка полностью корректна и ведёт на работающий эндпоинт
        return CreatedAtAction(
            nameof(GetTransactionById), 
            new { accountId = transactionDto.AccountId, transactionId = transactionDto.Id }, 
            transactionDto);
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
    /// <returns>Полная выписка по счёту.</returns>
    /// <response code="200">Запрос выполнен успешно. Возвращает объект выписки.</response>
    /// <response code="404">Счёт с указанным ID не найден.</response>
    [HttpGet]
    [ProducesResponseType(typeof(AccountStatementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountStatement([FromRoute] Guid accountId, [FromQuery] GetAccountStatementQuery query)
    {
        query.AccountId = accountId;
        var result = await mediator.Send(query);
        return Ok(result);
    }
    /// <summary>
    /// Получает одну транзакцию по её ID в контексте указанного счёта.
    /// </summary>
    /// <param name="accountId">ID счёта, к которому относится транзакция.</param>
    /// <param name="transactionId">ID искомой транзакции.</param>
    /// <returns>Данные одной транзакции.</returns>
    /// <response code="200">Транзакция найдена и возвращена.</response>
    /// <response code="404">Счёт или транзакция не найдены.</response>
    [HttpGet("{transactionId:guid}", Name = "GetTransactionById")] // <-- Name = "GetTransactionById" важен для CreatedAtAction
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById([FromRoute] Guid accountId, [FromRoute] Guid transactionId)
    {
        var query = new GetTransactionByIdQuery(accountId, transactionId);
        var transaction = await mediator.Send(query);
        return Ok(transaction);
    }
}