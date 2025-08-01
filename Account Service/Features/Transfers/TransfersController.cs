using AccountService.Features.Transfers.CreateTransfer;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Transfers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransfersController: ControllerBase
{
    private readonly IMediator _mediator;

    public TransfersController(IMediator mediator) 
    { 
        _mediator = mediator; 
    }

    /// <summary>
    /// Выполняет перевод средств между двумя счетами.
    /// </summary>
    /// <remarks>
    /// Эта операция является атомарной: в рамках одного действия создаются две транзакции
    /// (дебетовая для счёта-отправителя и кредитовая для счёта-получателя)
    /// и обновляются балансы обоих счетов.
    /// 
    /// **Бизнес-правила:**
    /// - Переводы возможны только между счетами в одной валюте.
    /// - Нельзя выполнять переводы с/на закрытые счета.
    /// - На счёте списания должно быть достаточно средств.
    /// 
    /// **Пример запроса:**
    /// 
    ///     POST /api/transfers
    ///     {
    ///        "fromAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "toAccountId": "acde070d-8c4c-4f0d-9d8a-162843c10333",
    ///        "amount": 200,
    ///        "description": "Пополнение вклада"
    ///     }
    /// 
    /// </remarks>
    /// <param name="command">Данные для выполнения перевода.</param>
    /// <response code="204">Перевод успешно выполнен.</response>
    /// <response code="400">Некорректные данные в запросе или нарушение бизнес-правил (например, недостаток средств).</response>
    /// <response code="404">Один из счетов (отправителя или получателя) не найден.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferCommand command)
    {
        await _mediator.Send(command);
        return NoContent(); // 204 No Content - идеальный ответ для успешной операции, которая не возвращает тело.
    }
}