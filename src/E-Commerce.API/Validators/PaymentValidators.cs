using E_Commerce.API.DTOs;
using FluentValidation;

namespace E_Commerce.API.Validators;

public class ProcessPaymentDtoValidator : AbstractValidator<ProcessPaymentDto>
{
    private static readonly string[] ValidPaymentMethods =
    {
        "CreditCard",
        "DebitCard",
        "PayPal",
        "Stripe",
        "Cash",
        "FailCard" // For testing
    };

    public ProcessPaymentDtoValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required")
            .Must(method => ValidPaymentMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Payment method must be one of: {string.Join(", ", ValidPaymentMethods)}");
    }
}
