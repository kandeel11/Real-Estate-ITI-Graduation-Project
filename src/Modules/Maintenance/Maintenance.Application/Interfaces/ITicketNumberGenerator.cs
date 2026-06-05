using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.Interfaces
{
    public interface ITicketNumberGenerator
    {
        Task<string> GenerateAsync(CancellationToken ct = default);
    }
}
