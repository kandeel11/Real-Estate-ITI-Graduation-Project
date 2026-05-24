using AI.Application.DTOs;
using Microsoft.SemanticKernel.ChatCompletion;
namespace AI.Application.Interfaces.Common
{
    public interface ISemanticKernelService
    {
        Task<(string EnDescription, string ArDescription, int TokensUsed)> GenerateDescriptionAsync(
        string propertyType,
        string location,
        int bedrooms,
        decimal area,
        List<string> amenities,
        CancellationToken cancellationToken);

        Task<ChatbotAiResponse> SendChatMessageAsync(ChatHistory chatHistory, CancellationToken cancellationToken);
    }
}
