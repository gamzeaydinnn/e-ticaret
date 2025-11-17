using FluentValidation;
using ECommerce.Core.DTOs.Order;

namespace ECommerce.API.Validators
{
    public class OrderCreateDtoValidator : AbstractValidator<OrderCreateDto>
    {
        public OrderCreateDtoValidator()
        {
            RuleFor(x => x.ShippingAddress).NotEmpty().WithMessage("Adres zorunludur.");
            RuleFor(x => x.ShippingCity).NotEmpty().WithMessage("Şehir zorunludur.");

            RuleFor(x => x.OrderItems)
                .NotNull().WithMessage("OrderItems cannot be null.")
                .NotEmpty().WithMessage("Order must contain at least one item.");

            RuleForEach(x => x.OrderItems).SetValidator(new OrderItemDtoValidator());
        }
    }
}
