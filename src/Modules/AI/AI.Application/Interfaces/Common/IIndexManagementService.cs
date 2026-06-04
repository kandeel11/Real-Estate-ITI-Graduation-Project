using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Interfaces.Common
{
    public interface IIndexManagementService
    {
        Task EnsureIndexesAsync(
            string collectionName,
            CancellationToken cancellationToken = default);
    }
}
