using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.BuildingBlocks.Web;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiceRoller.BuildingBlocks.Tests.Web;

public sealed record Order(string? Name, int Quantity);

public sealed record Item(int Id, string Name);

/// <summary>Name must be non-empty and at least 3 characters; quantity must be even.</summary>
public sealed class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(order => order.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(order => order.Name).MinimumLength(3).WithMessage("Name must be at least 3 characters.");
        RuleFor(order => order.Quantity).Must(quantity => quantity % 2 == 0).WithMessage("Quantity must be even.");
    }
}

/// <summary>A second validator for the same type, to prove every registered validator runs.</summary>
public sealed class OrderLimitValidator : AbstractValidator<Order>
{
    public OrderLimitValidator()
    {
        RuleFor(order => order.Quantity).LessThan(1000).WithMessage("Quantity must be less than 1000.");
    }
}

[ApiController]
[Route("test")]
public sealed class TestController : ControllerBase
{
    public const string FailureMessage = "The operation failed.";
    public const string SecretMessage = "connection string with password";

    [HttpGet("failure/{type}")]
    public IActionResult Failure(ErrorType type) =>
        Result.Failure(new Error("Test.Failed", FailureMessage, type)).ToActionResult();

    [HttpGet("value")]
    public IActionResult Value() => Result.Success(new Item(1, "dice")).ToActionResult();

    [HttpGet("value-failure")]
    public IActionResult ValueFailure() =>
        Result.Failure<Item>(Error.NotFound("Item.NotFound", "Item not found.")).ToActionResult();

    [HttpDelete("value")]
    public IActionResult Delete() => Result.Success().ToActionResult();

    [HttpPost("items")]
    public IActionResult Create() => Result.Success(new Item(5, "dice")).ToCreatedResult("GetItem", new { id = 5 });

    [HttpPost("items-no-body")]
    public IActionResult CreateWithoutBody() => Result.Success().ToCreatedResult("GetItem", new { id = 7 });

    [HttpPost("items-conflict")]
    public IActionResult CreateConflict() =>
        Result.Failure<Item>(Error.Conflict("Item.Exists", "Item exists.")).ToCreatedResult("GetItem", new { id = 5 });

    [HttpGet("items/{id:int}", Name = "GetItem")]
    public IActionResult GetItem(int id) => Ok(new Item(id, "dice"));

    [HttpGet("domain")]
    public IActionResult Domain() => throw new DomainException(Error.Conflict("Test.Conflict", "Domain rule broken."));

    [HttpGet("unexpected")]
    public IActionResult Unexpected() => throw new InvalidOperationException(SecretMessage);

    [HttpPost("orders")]
    public IActionResult CreateOrder(Order order) => Ok(order);

    [HttpGet("paged")]
    public IActionResult Paged([FromQuery] PagedQuery query) => Ok(query);

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new { subject = User.FindFirst(JwtClaimNames.Subject)?.Value });

    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public IActionResult Admin() => Ok();
}
