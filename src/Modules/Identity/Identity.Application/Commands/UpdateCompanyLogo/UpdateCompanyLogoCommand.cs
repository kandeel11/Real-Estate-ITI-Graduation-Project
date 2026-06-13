using MediatR;

namespace Identity.Application.Commands.UpdateCompanyLogo;

/// <summary>
/// Persists an already-uploaded logo URL on the company. The file upload
/// itself happens in the Host (media storage service); this only records it.
/// </summary>
public record UpdateCompanyLogoCommand(Guid CompanyId, string? LogoUrl) : IRequest;
