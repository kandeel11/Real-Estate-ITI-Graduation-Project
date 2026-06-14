using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record AiEvaluationResultDto(string ResponsibleParty, string Category, int ConfidenceScore, string Reasoning);
}
