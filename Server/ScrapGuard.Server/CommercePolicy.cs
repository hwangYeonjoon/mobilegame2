using ScrapGuard.Core;
namespace ScrapGuard.Server;

// All modes remain off unless explicitly configured on the server.
public sealed class CommercePolicy
{
    public string Mode { get; }
    public bool Enabled => Mode != "disabled";
    public bool LicenseTest => Mode == "license_test";
    public bool SalesEnabled { get; }
    public string[] SaleProducts { get; }
    public CommercePolicy(IConfiguration config)
    {
        Mode = config["COMMERCE_MODE"] ?? "disabled";
        if (Mode != "disabled" && Mode != "license_test" && Mode != "production")
            throw new InvalidOperationException("Unknown COMMERCE_MODE.");
        if (Enabled && string.IsNullOrWhiteSpace(config["PLAY_PACKAGE_NAME"]))
            throw new InvalidOperationException("PLAY_PACKAGE_NAME is required for commerce.");
        if (Mode == "production" && !config.GetValue("COMMERCE_RELEASE_READY", false))
            throw new InvalidOperationException("Production commerce requires COMMERCE_RELEASE_READY after release verification.");
        SalesEnabled = Enabled && config.GetValue("COMMERCE_SALES_ENABLED", false);
        var disabled = (config["COMMERCE_DISABLED_PRODUCTS"] ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (disabled.Any(id => CommerceCatalog.Find(id) == null)) throw new InvalidOperationException("Unknown disabled product.");
        SaleProducts = SalesEnabled ? CommerceCatalog.Offers.Where(o => !disabled.Contains(o.Id)).Select(o => o.Id).ToArray() : [];
    }
}
