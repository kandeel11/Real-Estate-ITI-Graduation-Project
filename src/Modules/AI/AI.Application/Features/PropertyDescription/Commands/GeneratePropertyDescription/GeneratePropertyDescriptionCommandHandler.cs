using AI.Application.Common.Options;
using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Application.Features.PropertyDescription.Commands.GeneratePropertyDescription
{
    public class GeneratePropertyDescriptionCommandHandler : IRequestHandler<GeneratePropertyDescriptionCommand, GeneratedDescriptionResponse>
    {
        private readonly IAiDbContext _context;
        private readonly ISemanticKernelService _semanticKernelService;
        private readonly ILogger<GeneratePropertyDescriptionCommandHandler> _logger;
        private readonly AiOptions _aiOptions;
       
        public GeneratePropertyDescriptionCommandHandler(IAiDbContext context, ISemanticKernelService semanticKernelService, ILogger<GeneratePropertyDescriptionCommandHandler> logger, IOptions<AiOptions> aiOptions)
        {
            _context = context;
            _semanticKernelService = semanticKernelService;
            _logger = logger;
            _aiOptions = aiOptions.Value;
        }
        public async Task<GeneratedDescriptionResponse> Handle(GeneratePropertyDescriptionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
            "Starting AI description generation for Property: {PropertyId} by User: {UserId}",
            request.PropertyId,
            request.RequestedById);
          
            var aiResult = await _semanticKernelService.GenerateDescriptionAsync(
            request.PropertyType,
            request.Location,
            request.Bedrooms,
            request.Area,
            request.Amenities,
            cancellationToken
        );
            _logger.LogInformation(
          "AI generation successful for Property: {PropertyId}. Tokens consumed: {Tokens}",
          request.PropertyId,
          aiResult.TokensUsed);
            var generationRecord = new AiDescriptionGeneration
            {
                PropertyId = request.PropertyId,
                RequestedById = request.RequestedById,
                PromptText = "System prompt generated in SemanticKernelService",
                GeneratedTextEn = aiResult.EnDescription,
                GeneratedTextAr = aiResult.ArDescription,
                ModelVersion = _aiOptions.ModelVersion,
                TokensUsed = aiResult.TokensUsed,
                IsAccepted = false
            };
            _context.AiDescriptionGenerations.Add(generationRecord);
            await _context.SaveChangesAsync(cancellationToken);
            return new GeneratedDescriptionResponse(
            generationRecord.Id,
            generationRecord.GeneratedTextEn,
            generationRecord.GeneratedTextAr
        );
        }
    }
}
