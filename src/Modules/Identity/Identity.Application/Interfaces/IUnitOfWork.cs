// Identity.Application/Interfaces/Common/IUnitOfWork.cs

namespace Identity.Application.Interfaces.Common;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
}