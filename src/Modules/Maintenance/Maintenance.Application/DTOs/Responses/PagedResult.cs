using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    }
}
