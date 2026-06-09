using AI.Application.Interfaces.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AI.Application.Features.PropertyDescription.Commands.AcceptPropertyDescription
{
    public class AcceptPropertyDescriptionCommandHandler : IRequestHandler<AcceptPropertyDescriptionCommand, bool>
    {
        private readonly IAiDbContext _context;
        private readonly ILogger<AcceptPropertyDescriptionCommandHandler> _logger;
        private readonly IPublisher _eventPublisher;

        public AcceptPropertyDescriptionCommandHandler(
            IAiDbContext context,
            ILogger<AcceptPropertyDescriptionCommandHandler> logger,
            IPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        public async Task<bool> Handle(AcceptPropertyDescriptionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to accept AI description generation with ID: {GenerationId}", request.GenerationId);

            // 1. Fetch the generated draft
            var generationRecord = await _context.AiDescriptionGenerations.FindAsync(
                new object[] { request.GenerationId },
                cancellationToken);

            if (generationRecord is null)
            {
                _logger.LogWarning("AI description generation with ID: {GenerationId} was not found.", request.GenerationId);
                return false;
            }

            // 2. Idempotency check: Skip if already accepted
            if (generationRecord.IsAccepted)
            {
                return true;
            }

            // 3. Mark as accepted in the AI database
            generationRecord.IsAccepted = true;

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Use the owner's edited text if they provided any; otherwise accept the
            //    stored AI draft as-is (no need to resend the text just to accept it).
            var finalEn = string.IsNullOrWhiteSpace(request.FinalTextEn)
                ? generationRecord.GeneratedTextEn
                : request.FinalTextEn;
            var finalAr = string.IsNullOrWhiteSpace(request.FinalTextAr)
                ? generationRecord.GeneratedTextAr
                : request.FinalTextAr;

            // The Property Module listens to this event and saves these exact strings.
            var integrationEvent = new PropertyDescriptionAcceptedEvent(
                generationRecord.PropertyId,
                finalEn,
                finalAr
            );

            await _eventPublisher.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation("Successfully accepted AI description generation with ID: {GenerationId}", request.GenerationId);

            return true;
        }
    }
}