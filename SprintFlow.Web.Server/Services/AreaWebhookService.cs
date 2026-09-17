using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

public class AreaWebhookService
{
    public const int MaxUrlLength = 2048;

    private readonly BoardRepository boardRepository;
    private readonly WebhookOptions options;

    public AreaWebhookService(BoardRepository boardRepository, IOptions<WebhookOptions> options)
    {
        this.boardRepository = boardRepository;
        this.options = options.Value;
    }

    public Task<AreaWebhookTarget?> GetTargetAsync(int areaId, CancellationToken cancellationToken)
        => boardRepository.GetAreaWebhookTargetAsync(areaId, cancellationToken);

    public async Task<SetAreaWebhookOutcome> SetWebhookUrlAsync(
        int areaId,
        string? webhookUrl,
        CancellationToken cancellationToken)
    {
        var normalised = string.IsNullOrWhiteSpace(webhookUrl) ? null : webhookUrl.Trim();

        if (normalised is not null && !IsAcceptableUrl(normalised))
        {
            return SetAreaWebhookOutcome.InvalidUrl;
        }

        var area = await boardRepository.GetAreaForUpdateAsync(areaId, cancellationToken);

        if (area is null)
        {
            return SetAreaWebhookOutcome.AreaNotFound;
        }

        area.WebhookUrl = normalised;

        await boardRepository.SaveChangesAsync(cancellationToken);

        return SetAreaWebhookOutcome.Saved;
    }

    public Task<IReadOnlyList<AreaWebhookTarget>> GetTargetsAsync(CancellationToken cancellationToken)
        => boardRepository.GetAreaWebhookTargetsAsync(cancellationToken);

    private bool IsAcceptableUrl(string value)
    {
        if (value.Length > MaxUrlLength
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return options.AllowHttpLocalhost || !IsInternalHost(uri);
        }

        return uri.Scheme == Uri.UriSchemeHttp && options.AllowHttpLocalhost && uri.IsLoopback;
    }

    private static bool IsInternalHost(Uri uri)
    {
        if (uri.IsLoopback || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (uri.HostNameType is not (UriHostNameType.IPv4 or UriHostNameType.IPv6)
            || !IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address))
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal || address.IsIPv6SiteLocal;
        }

        var bytes = address.GetAddressBytes();

        return bytes[0] == 10
            || bytes[0] == 127
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 169 && bytes[1] == 254)
            || bytes[0] == 0;
    }
}
