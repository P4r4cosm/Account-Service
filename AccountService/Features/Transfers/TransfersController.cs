using AccountService.Features.Transfers.CreateTransfer;
using AccountService.Shared.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Transfers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "v1")] // Группируем в отдельный раздел Swagger
public class TransfersController(IMediator mediator) : BaseApiController(mediator)
{
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
    /// <param name="correlationId">Необязательный идентификатор корреляции (из заголовка X-Correlation-ID).</param>
    /// <returns>Результат операции в формате MbResult (без значения).</returns>
    /// <response code="200">Перевод успешно выполнен. В теле ответа возвращается успешный объект MbResult.</response>
    /// <response code="400">Некорректные данные или нарушение бизнес-правил. В теле ответа возвращается объект MbResult с ошибкой.</response>
    /// <response code="404">Один из счетов не найден. В теле ответа возвращается объект MbResult с ошибкой.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferCommand command,
        [FromHeader(Name = "X-Correlation-ID")]
        Guid? correlationId)
    {
        command.CorrelationId = correlationId;
        command.CommandId= Guid.NewGuid();
        // Отправляем команду в MediatR, который вызовет CreateTransferHandler
        var result = await Mediator.Send(command);

        // Передаем результат в наш централизованный обработчик
        return HandleResult(result);
    }
}