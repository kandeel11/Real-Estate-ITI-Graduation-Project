using Document.Application.Queries.Lease;
using Identity.Application.Queries.GetCompanyBrokersDetails;
using Identity.Application.Queries.GetCompanyDetails;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Property.Application.Features.Queries.GetPropertiesByCompany;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RealEstate.Host.Behaviors;

/// <summary>
/// MediatR pipeline behavior that caches query results in Redis.
/// Only queries listed in _registry are cached — all others pass through unchanged.
/// Cache key = query type name + MD5 of serialized parameters (collision-safe, compact).
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    // ── Cache registry ────────────────────────────────────────────────────────
    // Add any query here to make it cached. The TTL is how long the result lives
    // in Redis before the next request hits the DB again.
    private static readonly Dictionary<Type, TimeSpan> _registry = new()
    {
        // Called in 10+ controller actions on every manager dashboard request
        { typeof(GetPropertiesByCompanyQuery),   TimeSpan.FromSeconds(60)  },
        // Called in 4 controller actions — broker list rarely changes
        { typeof(GetCompanyBrokersDetailsQuery), TimeSpan.FromSeconds(120) },
        // Company profile — almost never changes
        { typeof(GetCompanyDetailsQuery),        TimeSpan.FromMinutes(5)   },
        // Lease list per company — changes on lease state transitions
        { typeof(GetLeasesByPropertyIdsQuery),   TimeSpan.FromSeconds(60)  },
        // Manager approval queue — short TTL since manager acts in real time
        { typeof(GetPendingApprovalLeasesQuery), TimeSpan.FromSeconds(30)  },
        // Awaiting tenant queue — short TTL for same reason
        { typeof(GetAwaitingTenantLeasesQuery),  TimeSpan.FromSeconds(30)  },
    };

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var type = typeof(TRequest);

        if (!_registry.TryGetValue(type, out var ttl))
            return await next();

        var key = BuildKey(type, request);

        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("[Cache HIT]  {Key}", key);
                return JsonSerializer.Deserialize<TResponse>(cached, _json)!;
            }
        }
        catch (Exception ex)
        {
            // Redis unreachable — fall through to DB, never fail the request
            _logger.LogWarning(ex, "[Cache] Redis read failed for {Key} — falling through to DB", key);
            return await next();
        }

        _logger.LogDebug("[Cache MISS] {Key} (TTL={Ttl})", key, ttl);
        var result = await next();

        try
        {
            var serialized = JsonSerializer.Serialize(result, _json);
            await _cache.SetStringAsync(key, serialized,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Redis write failure is non-fatal — the request already succeeded
            _logger.LogWarning(ex, "[Cache] Redis write failed for {Key}", key);
        }

        return result;
    }

    private static string BuildKey(Type type, TRequest request)
    {
        var paramsJson = JsonSerializer.Serialize(request, _json);
        var hash       = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(paramsJson)));
        return $"q:{type.Name}:{hash}";
    }
}
