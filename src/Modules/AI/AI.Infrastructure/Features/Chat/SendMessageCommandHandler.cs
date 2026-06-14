using AI.Application.DTOs;
using AI.Application.Features.Chat.Commands;
using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AI.Infrastructure.Features.Chat;

public sealed class SendMessageCommandHandler
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chat;
    private readonly IAiDbContext _context; // ✅ التعديل 1: استخدام DbContext بدل Memory Store
    private readonly ILogger<SendMessageCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    private const string SystemPrompt = """
You are a professional Legal and Property Management AI Assistant specialized in Arabic and English contracts.

Your role is LIMITED to:
- Contract analysis and explanation
- Rental agreements
- Property maintenance responsibility
- Legal obligations between landlord and tenant
- Payments, penalties, timelines, and maintenance clauses

PRIMARY OBJECTIVE:
Always retrieve and analyze the contract BEFORE answering.

MANDATORY RULES — FOLLOW THESE STRICTLY:

1. CONTRACT RETRIEVAL IS REQUIRED
   - You MUST ALWAYS retrieve contract information before answering.
   - Never answer from assumptions or general knowledge.

2. CONTRACT ID PRIORITY
   - If a contractId exists in the system context,
     ALWAYS call:
     search_contract_by_id
     using that exact ID FIRST.

3. FALLBACK SEARCH
   - If search_contract_by_id returns no result,
     call:
     search_contracts
     immediately.

4. MAINTENANCE RESPONSIBILITY WORKFLOW
   If the user asks about maintenance, repairs, damage, or something broken:

   STEP 1 — CHECK THE CONTRACT FIRST
   - Search the contract for clauses related to:
     - maintenance
     - repairs
     - landlord responsibilities
     - tenant responsibilities
     - damages
     - misuse
     - negligence
     - infrastructure
     - utilities
     - appliances

   STEP 2 — APPLY CONTRACT TERMS
   - If the contract clearly specifies who pays or who is responsible,
     answer ONLY according to the contract.
   - Cite clause/page number whenever possible.

   STEP 3 — IF THE CONTRACT IS UNCLEAR OR MISSING
   Determine the issue category:

   A) LANDLORD / PROPERTY OWNER RESPONSIBILITY
   Usually includes:
   - Structural damage
   - Water pipes inside walls
   - Building infrastructure
   - Electrical system failures
   - Roof leaks
   - Major plumbing
   - Essential systems failure

   B) TENANT RESPONSIBILITY
   Usually includes:
   - Misuse
   - Negligence
   - Accidental damage
   - Broken furniture due to misuse
   - Damage caused by tenant actions

   STEP 4 — ASK CLARIFYING QUESTIONS
   If responsibility is unclear, ask:
   - What exactly is broken?
   - How did the issue happen?
   - Is this normal wear or accidental damage?
   - When did the issue start?

5. ANSWER ONLY FROM RETRIEVED DATA
   - Never invent legal clauses.
   - Never assume responsibilities without checking the contract first.
   - Clearly distinguish:
     "According to the contract"
     vs
     "If not specified in the contract"

6. LANGUAGE RULE
   - Respond in the SAME language as the user.
   - Contracts may be Arabic or English.

7. NO PREMATURE FAILURE
   - Never say information is unavailable before retrieving contract data.

COMMUNICATION STYLE:
- Professional
- Practical
- Legally careful
- Concise and clear
- Ask follow-up questions only when necessary

EXAMPLES:

User: "The AC is broken, who pays?"
→ Retrieve contract
→ Check maintenance clause
→ If contract says landlord covers AC maintenance:
   Answer accordingly with citation.
→ If unclear:
   Ask whether it is a system failure or misuse.

User: "There is a water leak"
→ Retrieve contract
→ Check repair responsibilities
→ If unclear:
   Determine whether it is infrastructure or tenant-caused damage.

User: "The door is broken"
→ Retrieve contract
→ Check maintenance clause
→ Ask whether it broke from normal use or accidental damage if needed.
""";

    public SendMessageCommandHandler(
        Kernel kernel,
        IChatCompletionService chat,
        IAiDbContext context,
        ILogger<SendMessageCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _kernel = kernel;
        _chat = chat;
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<SendMessageResult> Handle(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling SendMessageCommand | Session={SessionId} | ContractId={ContractId}",
            command.SessionId, command.ContractId);

        var userId = _currentUserService.UserId ?? Guid.Empty;
        var sessionId = command.SessionId ?? Guid.NewGuid();

        AiChatbotSession session;
        var history = new ChatHistory(SystemPrompt);

        // ── 1. Restore or create conversation history from DB ────────────────
        var existingSession = await _context.AiChatbotSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (existingSession != null)
        {
            session = existingSession;

            // بناء الـ ChatHistory للـ AI من الداتابيز بالترتيب الزمني
            foreach (var msg in session.Messages.OrderBy(m => m.CreatedAt))
            {
                if (msg.Role == "user")
                    history.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant")
                    history.AddAssistantMessage(msg.Content);
            }
        }
        else
        {
            // إنشاء سيشن جديدة لو مش موجودة
            session = AiChatbotSession.Create(userId, Guid.NewGuid().ToString());

            // لو الـ Factory Method مش بتعمل Set للـ Id، نعمله هنا عشان نستخدمه
            // session.Id = sessionId; 

            _context.AiChatbotSessions.Add(session);
        }

        // ── 2. Inject contract scope hint if provided ────────────────────────
        var userMessageText = command.ContractId.HasValue
            ? $"[Contract ID: {command.ContractId}]\n{command.Message}"
            : command.Message;

        history.AddUserMessage(userMessageText);

        // ✅ إضافة رسالة اليوزر للداتابيز مباشرة
        var userMessageEntity = AiChatbotMessage.CreateUserMessage(session.Id, userMessageText);
        _context.AiChatbotMessages.Add(userMessageEntity);

        // ── 3. AutoInvokeKernelFunctions ─────────────────────────────────────
#pragma warning disable SKEXP0001
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.2, // الحرارة قليلة ممتازة عشان إجابات العقود تبقى دقيقة ومش عشوائية
            MaxTokens = 2000,
            TopP = 0.95
        };
#pragma warning restore SKEXP0001

        // ── 4. Execute — plugins auto-invoked by Kernel ──────────────────────
        var response = await _chat.GetChatMessageContentAsync(
            chatHistory: history,
            executionSettings: settings,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        // ── 5. Extract token usage & Persist AI Response ─────────────────────
        var usage = response.Metadata?.GetValueOrDefault("Usage") as OpenAI.Chat.ChatTokenUsage;
        int totalTokens = (usage?.InputTokenCount ?? 0) + (usage?.OutputTokenCount ?? 0);

        var assistantMessageText = response.Content ?? string.Empty;

        // ✅ إضافة رسالة الـ AI للداتابيز مباشرة
        var assistantMessageEntity = AiChatbotMessage.CreateAssistantMessage(
            session.Id,
            assistantMessageText,
            totalTokens);

        _context.AiChatbotMessages.Add(assistantMessageEntity);

        // حفظ كل التغييرات (Session جديدة لو وجدت + رسالة اليوزر + رسالة الـ AI)
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Response ready | Session={SessionId} | Tokens={In}+{Out}",
            session.Id,
            usage?.InputTokenCount ?? 0,
            usage?.OutputTokenCount ?? 0);

        return new SendMessageResult(
            SessionId: session.Id.ToString(),
            Answer: assistantMessageText,
            PromptTokens: usage?.InputTokenCount ?? 0,
            CompletionTokens: usage?.OutputTokenCount ?? 0);
    }
}