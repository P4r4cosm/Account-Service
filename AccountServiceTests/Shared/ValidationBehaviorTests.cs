using AccountService.Shared.Behaviors;
using AccountService.Shared.Domain;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

namespace AccountServiceTests.Shared;

// Запрос, который возвращает MbResult
public record TestRequest(string Name, string Email) : IRequest<MbResult>;

// Запрос, который возвращает MbResult<int>
public record TestRequestWithValue(int Id) : IRequest<MbResult<int>>;

// Некорректный тип результата без статического метода Failure(MbError)
public abstract class InvalidResultType(bool isSuccess, MbError? error) : MbResult(isSuccess, error)
{
    // Отсутствует необходимый метод: public static InvalidResultType Failure(MbError error)
}

// Запрос для теста с некорректным типом результата
public record TestRequestWithInvalidResult : IRequest<InvalidResultType>;

// Валидатор для TestRequest
public class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Имя не должно быть пустым.");
        RuleFor(x => x.Email).EmailAddress().WithMessage("Некорректный формат email.");
    }
}

// Еще один валидатор для TestRequest для проверки группировки ошибок
public class AnotherTestRequestValidator : AbstractValidator<TestRequest>
{
    public AnotherTestRequestValidator()
    {
        RuleFor(x => x.Name).MinimumLength(10).WithMessage("Имя должно быть длиннее.");
    }
}

// Валидатор для TestRequestWithValue
public class TestRequestWithValueValidator : AbstractValidator<TestRequestWithValue>
{
    public TestRequestWithValueValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id должен быть положительным.");
    }
}
public class ValidationBehaviorTests
{
    private readonly Mock<RequestHandlerDelegate<MbResult>> _nextHandlerMock;
    private readonly Mock<RequestHandlerDelegate<MbResult<int>>> _nextHandlerWithValueMock;

    public ValidationBehaviorTests()
    {
        _nextHandlerMock = new Mock<RequestHandlerDelegate<MbResult>>();
        _nextHandlerMock.Setup(n => n(It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult.Success());

        _nextHandlerWithValueMock = new Mock<RequestHandlerDelegate<MbResult<int>>>();
        _nextHandlerWithValueMock.Setup(n => n(It.IsAny<CancellationToken>()))
            .ReturnsAsync(MbResult<int>.Success(123));
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorsAreProvided()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestRequest, MbResult>([]);
        var request = new TestRequest("Test", "test@test.com");

        // Act
        var result = await behavior.Handle(request, _nextHandlerMock.Object, CancellationToken.None);

        // Assert
        _nextHandlerMock.Verify(next => next(It.IsAny<CancellationToken>()), Times.Once);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationSucceeds()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, MbResult>(validators);
        var request = new TestRequest("Valid Name", "valid@email.com");

        // Act
        var result = await behavior.Handle(request, _nextHandlerMock.Object, CancellationToken.None);

        // Assert
        _nextHandlerMock.Verify(next => next(It.IsAny<CancellationToken>()), Times.Once);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureResult_WhenValidationFails()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, MbResult>(validators);
        var request = new TestRequest("", "invalid-email"); // Невалидные данные

        // Act
        var result = await behavior.Handle(request, _nextHandlerMock.Object, CancellationToken.None);

        // Assert
        _nextHandlerMock.Verify(next => next(It.IsAny<CancellationToken>()), Times.Never);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("General.Validation");
        result.Error.ValidationErrors.Should().HaveCount(2);
        result.Error.ValidationErrors.Should().ContainKey("Name");
        result.Error.ValidationErrors.Should().ContainKey("Email");
        result.Error.ValidationErrors!["Name"].Should().Be("Имя не должно быть пустым.");
        result.Error.ValidationErrors!["Email"].Should().Be("Некорректный формат email.");
    }
    
    [Fact]
    public async Task Handle_ShouldReturnFailureForGenericResult_WhenValidationFails()
    {
        // Arrange
        var validators = new List<IValidator<TestRequestWithValue>> { new TestRequestWithValueValidator() };
        var behavior = new ValidationBehavior<TestRequestWithValue, MbResult<int>>(validators);
        var request = new TestRequestWithValue(0); // Невалидный Id

        // Act
        var result = await behavior.Handle(request, _nextHandlerWithValueMock.Object, CancellationToken.None);

        // Assert
        _nextHandlerWithValueMock.Verify(next => next(It.IsAny<CancellationToken>()), Times.Never);
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(0); // Значение должно быть default для типа
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("General.Validation");
        result.Error.ValidationErrors.Should().HaveCount(1);
        result.Error.ValidationErrors.Should().ContainKey("Id");
        result.Error.ValidationErrors!["Id"].Should().Be("Id должен быть положительным.");
    }
    
    [Fact]
    public async Task Handle_ShouldGroupValidationErrors_AndTakeFirstMessagePerProperty()
    {
        // Arrange
        // Два валидатора с разными правилами для одного и того же поля 'Name'
        var validators = new List<IValidator<TestRequest>>
        {
            new TestRequestValidator(), 
            new AnotherTestRequestValidator()
        };
        var behavior = new ValidationBehavior<TestRequest, MbResult>(validators);
        // Пустое имя нарушает оба правила: NotEmpty и MinimumLength
        var request = new TestRequest("", "valid@email.com");

        // Act
        var result = await behavior.Handle(request, _nextHandlerMock.Object, CancellationToken.None);

        // Assert
        _nextHandlerMock.Verify(next => next(It.IsAny<CancellationToken>()), Times.Never);
        result.IsFailure.Should().BeTrue();
        result.Error?.ValidationErrors.Should().HaveCount(1);
        result.Error?.ValidationErrors.Should().ContainKey("Name");
        // Проверяем, что взята первая ошибка из первого валидатора
        result.Error?.ValidationErrors!["Name"].Should().Be("Имя не должно быть пустым.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenResultTypeLacksStaticFailureMethod()
    {
        // Arrange
        // Используем мок валидатора, чтобы гарантировать ошибку валидации
        var mockValidator = new Mock<IValidator<TestRequestWithInvalidResult>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([
                new ValidationFailure("Property", "Error")
            ]));

        var validators = new List<IValidator<TestRequestWithInvalidResult>> { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequestWithInvalidResult, InvalidResultType>(validators);
        var request = new TestRequestWithInvalidResult();
        var nextHandler = new Mock<RequestHandlerDelegate<InvalidResultType>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, nextHandler.Object, CancellationToken.None));
        
        exception.Message.Should().Contain(nameof(InvalidResultType));
        exception.Message.Should().Contain("не имеет публичного статического метода 'Failure'");
    }
}