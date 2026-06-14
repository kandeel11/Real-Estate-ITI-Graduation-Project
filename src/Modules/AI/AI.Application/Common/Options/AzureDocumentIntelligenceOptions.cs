using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Common.Options
{
    public sealed class AzureDocumentIntelligenceOptions
    {
        public const string SectionName = "AzureDocumentIntelligence";

        [Required]
        public string Endpoint { get; init; } = string.Empty;

        [Required]
        public string ApiKey { get; init; } = string.Empty;
    }
}
