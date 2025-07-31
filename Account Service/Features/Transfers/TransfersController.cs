using AccountService.Features.Transfers.CreateTransfer;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Transfers;

[ApiController]
[Route("api/{controller}")]
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
    /// Операция является атомарной: создаются две транзакции (списание и пополнение),
    /// и обновляются балансы обоих счетов. Переводы возможны только между счетами в одной валюте.
    /// </remarks>
    /// <param name="command">Данные для выполнения перевода.</param>
    /// <response code="204">Перевод успешно выполнен.</response>
    /// <response code="400">Некорректные данные в запросе или нарушение бизнес-правил (например, недостаток средств).</response>
    /// <response code="404">Один из счетов не найден.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferCommand command)
    {
        await _mediator.Send(command);
        return NoContent(); // 204 No Content - идеальный ответ для успешной операции, которая не возвращает контент.
    }
}