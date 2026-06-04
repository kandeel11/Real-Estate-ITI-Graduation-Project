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

            // PRO TIP: If you want to track user edits to improve AI prompts later, 
            // you can save the edited text to new columns in your database here:
            // generationRecord.EditedTextEn = request.FinalTextEn;
            // generationRecord.EditedTextAr = request.FinalTextAr;

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Publish integration event with the FINAL EDITED TEXT, not the original AI text
            // The Property Module will listen to this event and save these exact strings.
            var integrationEvent = new PropertyDescriptionAcceptedEvent(
                generationRecord.PropertyId,
                request.FinalTextEn, // Passing the user's updated English text
                request.FinalTextAr  // Passing the user's updated Arabic text
            );

            await _eventPublisher.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation("Successfully accepted AI description generation with ID: {GenerationId}", request.GenerationId);

            return true;
        }
    }
}