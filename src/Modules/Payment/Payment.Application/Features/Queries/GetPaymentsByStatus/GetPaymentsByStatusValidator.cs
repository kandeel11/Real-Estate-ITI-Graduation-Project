using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Application.Features.Queries.GetPaymentsByStatus
{
    public sealed class GetPaymentsByStatusValidator : AbstractValidator<GetPaymentsByStatusQuery>
    {
        private static readonly string[] ValidStatuses =
            ["Pending", "Paid", "Late", "Failed", "Refunded"];

        public GetPaymentsByStatusValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("الحالة مطلوبة.")
                .Must(s => ValidStatuses.Contains(s))
                .WithMessage($"الحالة لازم تكون إحدى: {string.Join(", ", ValidStatuses)}.");
        }
    }
}
