// Identity.Infrastructure/Email/ScribanEmailTemplateRenderer.cs

using Microsoft.Extensions.Caching.Memory;
using Scriban;

namespace Identity.Infrastructure.Email;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(string templateName, object model);
}

public sealed class ScribanEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly IMemoryCache _cache;
    private readonly string _templatesPath;

    public ScribanEmailTemplateRenderer(IMemoryCache cache)
    {
        _cache = cache;
        _templatesPath = Path.Combine(AppContext.BaseDirectory, "Email", "Templates");
    }

    public async Task<string> RenderAsync(string templateName, object model)
    {
        // 1. الـ Body
        var body = await RenderTemplateAsync($"{templateName}.html", model);

        // 2. الـ Layout
        var layout = await RenderTemplateAsync("_layout.html", new { body });

        return layout;
    }

    private async Task<string> RenderTemplateAsync(string fileName, object model)
    {
        // ✅ Template caching — نفس الـ template ما بيتفتحش من القرص كل مرة
        var template = await _cache.GetOrCreateAsync($"email-tpl-{fileName}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);

            var path = Path.Combine(_templatesPath, fileName);
            var text = await File.ReadAllTextAsync(path);
            return Template.Parse(text);
        });

        if (template!.HasErrors)
            throw new InvalidOperationException(
                $"Template {fileName} has errors: " +
                string.Join(", ", template.Messages));

        return await template.RenderAsync(model);
    }
}