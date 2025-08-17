using AccountService.Shared.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Events;

[ApiController]
[Route("api/events-documentation")]
[Produces("application/json")]
[AllowAnonymous]
[ApiExplorerSettings(GroupName = "events-v1")] // Имя группы, совпадающее с определением в Swagger
public class EventsController : ControllerBase
{
    // --- Публикуемые события (Published Events) ---

    #region Published Events

    /// <summary>
    /// AccountOpened: Публикуется при успешном открытии нового счёта.
    /// </summary>
    /// <remarks>
    /// Это событие информирует другие системы (например, CRM) о появлении нового счёта клиента.
    /// Публикуется в рамках транзакции с созданием счёта через механизм Transactional Outbox.
    ///
    /// **Routing Key:** `account.opened`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "b5f3a7f6-2f4e-4b1a-9f3a-2b0c1e7c1a11",
    ///   "occurredAt": "2025-08-12T21:00:00Z",
    ///   "payload": {
    ///     "accountId": "9c3f3f5d-7c2e-4a1a-9f5a-1b3a7e9d2f11",
    ///     "ownerId": "2a7e9d2f-9f5a-4a1a-7c2e-9c3f3f5d1b3a",
    ///     "currency": "RUB",
    ///     "type": "Checking"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "11111111-1111-1111-1111-111111111111",
    ///     "causationId": "22222222-2222-2222-2222-222222222222"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/account-opened")]
    [ProducesResponseType(typeof(EventEnvelope<AccountOpenedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetAccountOpenedEventExample() => Ok("This endpoint is for documentation purposes only.");

    /// <summary>
    /// MoneyCredited: Публикуется при пополнении счёта.
    /// </summary>
    /// <remarks>
    /// Используется для информирования сервиса уведомлений о зачислении средств.
    ///
    /// **Routing Key:** `money.credited`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "c1d3e2b5-9f5a-4e8b-8a7f-1b2c3d4e5f6a",
    ///   "occurredAt": "2025-08-13T10:30:00Z",
    ///   "payload": {
    ///     "accountId": "9c3f3f5d-7c2e-4a1a-9f5a-1b3a7e9d2f11",
    ///     "amount": 1000.00,
    ///     "currency": "RUB",
    ///     "operationId": "a1b2c3d4-e5f6-a7b8-c9d0-e1f2a3b4c5d6"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "33333333-3333-3333-3333-333333333333",
    ///     "causationId": "44444444-4444-4444-4444-444444444444"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/money-credited")]
    [ProducesResponseType(typeof(EventEnvelope<MoneyCreditedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetMoneyCreditedEventExample() => Ok("This endpoint is for documentation purposes only.");
    
    /// <summary>
    /// MoneyDebited: Публикуется при списании средств со счёта.
    /// </summary>
    /// <remarks>
    /// Информирует заинтересованные системы (например, аудита) о факте списания.
    ///
    /// **Routing Key:** `money.debited`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "f0e9d8c7-6b5a-4c3d-2b1a-0f9e8d7c6b5a",
    ///   "occurredAt": "2025-08-14T11:00:00Z",
    ///   "payload": {
    ///     "accountId": "9c3f3f5d-7c2e-4a1a-9f5a-1b3a7e9d2f11",
    ///     "amount": 200.00,
    ///     "currency": "RUB",
    ///     "operationId": "b2c3d4e5-f6a7-b8c9-d0e1-f2a3b4c5d6e7",
    ///     "reason": "Transfer"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "55555555-5555-5555-5555-555555555555",
    ///     "causationId": "66666666-6666-6666-6666-666666666666"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/money-debited")]
    [ProducesResponseType(typeof(EventEnvelope<MoneyDebitedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetMoneyDebitedEventExample() =>Ok("This endpoint is for documentation purposes only.");

    /// <summary>
    /// TransferCompleted: Публикуется при успешном завершении перевода между счетами.
    /// </summary>
    /// <remarks>
    /// Гарантированно публикуется после коммита транзакции списания и зачисления.
    ///
    /// **Routing Key:** `money.transfer.completed`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "a1b2c3d4-e5f6-a7b8-c9d0-e1f2a3b4c5d6",
    ///   "occurredAt": "2025-08-14T11:00:05Z",
    ///   "payload": {
    ///     "sourceAccountId": "9c3f3f5d-7c2e-4a1a-9f5a-1b3a7e9d2f11",
    ///     "destinationAccountId": "d2c1b0a9-8f7e-6d5c-4b3a-2f1e0d9c8b7a",
    ///     "amount": 200.00,
    ///     "currency": "RUB",
    ///     "transferId": "c3d4e5f6-a7b8-c9d0-e1f2-a3b4c5d6e7f8"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "55555555-5555-5555-5555-555555555555",
    ///     "causationId": "66666666-6666-6666-6666-666666666666"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/transfer-completed")]
    [ProducesResponseType(typeof(EventEnvelope<TransferCompletedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetTransferCompletedEventExample() => Ok("This endpoint is for documentation purposes only.");
    
    /// <summary>
    /// InterestAccrued: Публикуется после начисления процентов по вкладу.
    /// </summary>
    /// <remarks>
    /// Публикуется по завершении фоновой задачи (Hangfire job) по начислению процентов.
    ///
    /// **Routing Key:** `money.interest.accrued`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "b9c8d7e6-f5a4-b3c2-d1e0-f9a8b7c6d5e4",
    ///   "occurredAt": "2025-08-15T00:05:00Z",
    ///   "payload": {
    ///     "accountId": "d2c1b0a9-8f7e-6d5c-4b3a-2f1e0d9c8b7a",
    ///     "amount": 24.66,
    ///     "periodFrom": "2025-08-14T00:00:00Z",
    ///     "periodTo": "2025-08-15T00:00:00Z"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "77777777-7777-7777-7777-777777777777",
    ///     "causationId": "88888888-8888-8888-8888-888888888888"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/interest-accrued")]
    [ProducesResponseType(typeof(EventEnvelope<InterestAccruedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetInterestAccruedEventExample() => Ok("This endpoint is for documentation purposes only.");
    
    
    /// <summary>
    /// AccountOwnerChanged: Публикуется при смене владельца счёта.
    /// </summary>
    /// <remarks>
    /// Используется в редких случаях, например, при наследовании.
    ///
    /// **Routing Key:** `account.ownerChanged`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "32f5a4e2-9b3c-4d8e-9f1a-2b3c4d5e6f7a",
    ///   "occurredAt": "2026-03-05T16:20:00Z",
    ///   "payload": {
    ///     "accountId": "d2c1b0a9-8f7e-6d5c-4b3a-2f1e0d9c8b7a",
    ///     "oldOwnerId": "2a7e9d2f-9f5a-4a1a-7c2e-9c3f3f5d1b3a",
    ///     "newOwnerId": "5e8d7c6b-5a4f-3e2d-1c0b-9a8f7e6d5c4b"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "1b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d6e",
    ///     "causationId": "7a8b9c0d-1e2f-3a4b-5c6d-7e8f9a0b1c2d"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/account-owner-changed")]
    [ProducesResponseType(typeof(EventEnvelope<AccountOwnerChangedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetAccountOwnerChangedEventExample() => Ok("This endpoint is for documentation purposes only.");
    
    /// <summary>
    /// AccountInterestRateChanged: Публикуется при изменении процентной ставки по вкладу.
    /// </summary>
    /// <remarks>
    /// Информирует системы (например, систему уведомлений) об изменении условий по вкладу.
    ///
    /// **Routing Key:** `account.rateChanged`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "45a8b7c1-9d2e-4f6a-8b1c-3d5e7f9a2b4d",
    ///   "occurredAt": "2025-09-01T00:00:00Z",
    ///   "payload": {
    ///     "accountId": "d2c1b0a9-8f7e-6d5c-4b3a-2f1e0d9c8b7a",
    ///     "oldRate": 3.0,
    ///     "newRate": 3.5
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "k1k1k1k1-l2l2-m3m3-n4n4-o5o5o5o5o5o5",
    ///     "causationId": "p6p6p6p6-q7q7-r8r8-s9s9-t0t0t0t0t0t0"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/account-rate-changed")]
    [ProducesResponseType(typeof(EventEnvelope<AccountInterestRateChangedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetAccountInterestRateChangedEventExample() => Ok("This endpoint is for documentation purposes only.");
    
    /// <summary>
    /// AccountClosed: Публикуется при закрытии счёта.
    /// </summary>
    /// <remarks>
    /// Информирует системы о том, что счёт более неактивен.
    ///
    /// **Routing Key:** `account.closed`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "18a2b1c3-4d5e-6f7a-8b9c-0d1e2f3a4b5c",
    ///   "occurredAt": "2026-01-20T14:00:00Z",
    ///   "payload": {
    ///     "accountId": "9c3f3f5d-7c2e-4a1a-9f5a-1b3a7e9d2f11",
    ///     "ownerId": "2a7e9d2f-9f5a-4a1a-7c2e-9c3f3f5d1b3a",
    ///     "closedAt": "2026-01-20T13:59:55Z"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    ///     "causationId": "ffffffff-gggg-hhhh-iiii-jjjjjjjjjjjj"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/account-closed")]
    [ProducesResponseType(typeof(EventEnvelope<AccountClosedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetAccountClosedEventExample() => Ok("This endpoint is for documentation purposes only.");

    /// <summary>
    /// AccountReopened: Публикуется при повторном открытии ранее закрытого счёта.
    /// </summary>
    /// <remarks>
    /// Сигнализирует о возобновлении операций по счёту.
    ///
    /// **Routing Key:** `account.reopened`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "21d1e4c5-8f2a-4b6c-8d9e-1f2a3b4c5d6e",
    ///   "occurredAt": "2026-02-10T11:30:00Z",
    ///   "payload": {
    ///     "accountId": "9c3f3f5d-7c2e-4a1a-9f5a-1b3a7e9d2f11",
    ///     "ownerId": "2a7e9d2f-9f5a-4a1a-7c2e-9c3f3f5d1b3a"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "account-service",
    ///     "correlationId": "1a1a1a1a-2b2b-3c3c-4d4d-5e5e5e5e5e5e",
    ///     "causationId": "6f6f6f6f-7g7g-8h8h-9i9i-0j0j0j0j0j0j"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("published/account-reopened")]
    [ProducesResponseType(typeof(EventEnvelope<AccountReopenedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetAccountReopenedEventExample() => Ok("This endpoint is for documentation purposes only.");

    #endregion

    // --- Потребляемые события (Consumed Events) ---

    #region Consumed Events

    /// <summary>
    /// ClientBlocked: Потребляется от сервиса Antifraud для блокировки счетов клиента.
    /// </summary>
    /// <remarks>
    /// При получении этого события сервис находит все счета клиента и устанавливает им флаг "Frozen",
    /// запрещая расходные операции. Обработка идемпотентна благодаря механизму Inbox.
    ///
    /// **Routing Key:** `client.blocked`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "d2e1f0a9-b8c7-d6e5-f4a3-b2c1d0e9f8a7",
    ///   "occurredAt": "2025-08-16T15:00:00Z",
    ///   "payload": {
    ///     "clientId": "2a7e9d2f-9f5a-4a1a-7c2e-9c3f3f5d1b3a"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "antifraud-service",
    ///     "correlationId": "99999999-9999-9999-9999-999999999999",
    ///     "causationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("consumed/client-blocked")]
    [ProducesResponseType(typeof(EventEnvelope<ClientBlockedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetClientBlockedEventExample() => Ok("This endpoint is for documentation purposes only.");

    /// <summary>
    /// ClientUnblocked: Потребляется для снятия блокировки со счетов клиента.
    /// </summary>
    /// <remarks>
    /// При получении этого события сервис находит все счета клиента и снимает флаг "Frozen",
    /// разрешая расходные операции.
    ///
    /// **Routing Key:** `client.unblocked`
    ///
    /// ### Пример JSON-сообщения:
    /// ```json
    /// {
    ///   "eventId": "e3f2a1b0-c9d8-e7f6-a5b4-c3d2e1f0a9b8",
    ///   "occurredAt": "2025-08-17T18:00:00Z",
    ///   "payload": {
    ///     "clientId": "2a7e9d2f-9f5a-4a1a-7c2e-9c3f3f5d1b3a"
    ///   },
    ///   "meta": {
    ///     "version": "v1",
    ///     "source": "antifraud-service",
    ///     "correlationId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    ///     "causationId": "cccccccc-cccc-cccc-cccc-cccccccccccc"
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("consumed/client-unblocked")]
    [ProducesResponseType(typeof(EventEnvelope<ClientUnblockedEvent>), StatusCodes.Status200OK)]
    public IActionResult GetClientUnblockedEventExample() => Ok("This endpoint is for documentation purposes only.");

    #endregion
}