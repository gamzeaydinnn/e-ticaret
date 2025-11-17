using FluentValidation;
using ECommerce.Core.DTOs.Order;

namespace ECommerce.API.Validators
{
    public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
    {
        public OrderItemDtoValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("ProductId must be greater than 0.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
            RuleFor(x => x.UnitPrice).GreaterThan(0).WithMessage("UnitPrice must be greater than 0.");
        }
    }
}
