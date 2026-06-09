using AI.Application.Features.OwnershipVerification.Commands.AdminApproveVerification;
using AI.Application.Features.OwnershipVerification.Commands.AdminRejectVerification;
using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Queries.GetPendingAdminReviews;
using AI.Application.Features.OwnershipVerification.Queries.GetVerificationStatus;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OwnershipVerificationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public OwnershipVerificationsController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// ✅ Ownership verification is now AUTOMATIC when property is created with documents.
        /// This endpoint replaced by PropertyCreatedEventHandler in AI module.
        ///
        /// Owners upload National ID + Ownership proof during property creation.
        /// AI module automatically verifies and creates OwnershipVerificationRequest.
        /// </summary>

        /// <summary>
        /// Get verification status by request ID.
        /// </summary>
        [HttpGet("{verificationRequestId}")]
        [ProducesResponseType(typeof(VerificationStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatus(
            Guid verificationRequestId,
            CancellationToken cancellationToken
        )
        {
            var query = new GetVerificationStatusQuery(verificationRequestId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get all pending admin reviews.
        /// </summary>
        [HttpGet("admin/pending")]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<VerificationStatusDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingReviews(
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default
        )
        {
            var query = new GetPendingAdminReviewsQuery(limit);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Admin approves an ownership verification (moves to ManuallyVerified status).
        /// </summary>
        [HttpPost("{verificationRequestId}/approve")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(VerificationStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApproveVerification(
            Guid verificationRequestId,
            [FromBody] AdminReviewRequest request,
            CancellationToken cancellationToken
        )
        {
            var adminId = _currentUser.UserId;
            if (adminId is null) return Unauthorized(new { error = "Admin user not authenticated." });

            var command = new AdminApproveVerificationCommand(
                VerificationRequestId: verificationRequestId,
                AdminId: adminId.Value,
                ReviewNotes: request.ReviewNotes
            );

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Admin rejects an ownership verification.
        /// </summary>
        [HttpPost("{verificationRequestId}/reject")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(VerificationStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectVerification(
            Guid verificationRequestId,
            [FromBody] AdminReviewRequest request,
            CancellationToken cancellationToken
        )
        {
            var adminId = _currentUser.UserId;
            if (adminId is null) return Unauthorized(new { error = "Admin user not authenticated." });

            var command = new AdminRejectVerificationCommand(
                VerificationRequestId: verificationRequestId,
                AdminId: adminId.Value,
                RejectionReason: request.ReviewNotes
            );

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for admin review (approve/reject).
    /// </summary>
    public class AdminReviewRequest
    {
        public string ReviewNotes { get; set; }
    }
}
