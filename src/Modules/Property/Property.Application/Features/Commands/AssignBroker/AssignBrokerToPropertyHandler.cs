using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.AssignBroker;

public class AssignBrokerToPropertyHandler : IRequestHandler<AssignBrokerToPropertyCommand>
{
    private readonly IPropertyRepository _repository;

    public AssignBrokerToPropertyHandler(IPropertyRepository repository)
        => _repository = repository;

    public async Task Handle(AssignBrokerToPropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new InvalidOperationException("Property not found.");

        // Only the property owner or someone from the same company can assign a broker
        bool isOwner = property.OwnerId == request.RequestingUserId;
        bool isSameCompany = request.RequestingCompanyId.HasValue
            && property.CompanyId == request.RequestingCompanyId;

        if (!isOwner && !isSameCompany)
            throw new UnauthorizedAccessException("You do not have permission to assign a broker to this property.");

        // Property must belong to a company to have a broker assigned
        if (!property.CompanyId.HasValue)
            throw new InvalidOperationException("Only company-listed properties can have a broker assigned.");

        property.AssignBroker(request.BrokerId);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
