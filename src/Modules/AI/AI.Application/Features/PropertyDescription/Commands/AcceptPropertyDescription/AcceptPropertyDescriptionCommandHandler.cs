using AI.Application.Interfaces.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var generationRecord = await _context.AiDescriptionGenerations.FindAsync(
                new object[] { request.GenerationId },
                cancellationToken);

            if (generationRecord is null)
            {
                _logger.LogWarning("AI description generation with ID: {GenerationId} was not found.", request.GenerationId);
                return false;
            }

            if (generationRecord.IsAccepted)
            {
                _logger.LogInformation("AI description generation with ID: {GenerationId} is already accepted.", request.GenerationId);
                return true; 
            }

            generationRecord.IsAccepted = true;


            await _context.SaveChangesAsync(cancellationToken);

            var integrationEvent = new PropertyDescriptionAcceptedEvent(
                generationRecord.PropertyId,
                generationRecord.GeneratedTextEn,
                generationRecord.GeneratedTextAr
            );
            
            await _eventPublisher.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation("Successfully accepted AI description generation with ID: {GenerationId}", request.GenerationId);

            return true;
        }
    }
}
