using E_Commerce.API.DTOs;
using FluentValidation;

namespace E_Commerce.API.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .MaximumLength(100).WithMessage("User ID must not exceed 100 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item")
            .Must(items => items != null && items.Count <= 50)
                .WithMessage("Order cannot contain more than 50 items");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemDtoValidator());

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0).WithMessage("Shipping cost cannot be negative")
            .LessThan(10000).WithMessage("Shipping cost must be less than 10,000");

        RuleFor(x => x.Tax)
            .GreaterThanOrEqualTo(0).WithMessage("Tax cannot be negative")
            .LessThan(100000).WithMessage("Tax must be less than 100,000");
    }
}

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100 per item");
    }
}

public class CreateOrderFromCartDtoValidator : AbstractValidator<CreateOrderFromCartDto>
{
    public CreateOrderFromCartDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .MaximumLength(100).WithMessage("User ID must not exceed 100 characters");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0).WithMessage("Shipping cost cannot be negative")
            .LessThan(10000).WithMessage("Shipping cost must be less than 10,000");

        RuleFor(x => x.Tax)
            .GreaterThanOrEqualTo(0).WithMessage("Tax cannot be negative")
            .LessThan(100000).WithMessage("Tax must be less than 100,000");
    }
}

public class CancelOrderDtoValidator : AbstractValidator<CancelOrderDto>
{
    public CancelOrderDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required")
            .MinimumLength(5).WithMessage("Cancellation reason must be at least 5 characters")
            .MaximumLength(500).WithMessage("Cancellation reason must not exceed 500 characters");
    }
}
