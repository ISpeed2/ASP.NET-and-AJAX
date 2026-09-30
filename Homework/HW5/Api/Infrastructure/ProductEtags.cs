using Shop.Domain;

namespace Shop.Api.Infrastructure;

public static class ProductEtags
{
    public static string For(Product product) =>
        $"\"product-{product.Id:N}-v{product.Version}\"";

    public static bool MatchesIfMatch(string headerValue, string currentEtag) =>
        SplitTags(headerValue).Any(tag => tag == "*" || tag == currentEtag);

    public static bool MatchesIfNoneMatch(string headerValue, string currentEtag) =>
        SplitTags(headerValue).Any(tag =>
            tag == "*" || tag == currentEtag || tag == $"W/{currentEtag}");

    private static IEnumerable<string> SplitTags(string headerValue) =>
        headerValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
