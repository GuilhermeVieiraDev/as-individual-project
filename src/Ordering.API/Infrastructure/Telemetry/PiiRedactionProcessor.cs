using System.Diagnostics;
using OpenTelemetry;

namespace eShop.Ordering.API.Infrastructure.Telemetry;

public class PiiRedactionProcessor : BaseProcessor<Activity>
{
    private static readonly HashSet<string> _sensitiveTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CardNumber",
        "CardHolderName",
        "CardSecurityNumber",
        "Email",
        "PhoneNumber"
    };

    private static readonly HashSet<string> _partiallyMaskedTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "UserName",
        "ZipCode"
    };

    public override void OnEnd(Activity activity)
    {
        if (activity == null) return;

        // Scrub sensitive tags completely
        foreach (var tag in activity.TagObjects)
        {
            foreach (var sensitiveTag in _sensitiveTagNames)
            {
                if (tag.Key.Contains(sensitiveTag, StringComparison.OrdinalIgnoreCase))
                {
                    activity.SetTag(tag.Key, "[REDACTED]");
                }
            }

            // Partially mask some tags
            foreach (var partialTag in _partiallyMaskedTagNames)
            {
                if (tag.Key.Contains(partialTag, StringComparison.OrdinalIgnoreCase) && 
                    tag.Value is string valueStr &&
                    !string.IsNullOrEmpty(valueStr))
                {
                    if (valueStr.Length > 2)
                    {
                        activity.SetTag(tag.Key, valueStr[0] + new string('*', valueStr.Length - 2) + valueStr[valueStr.Length - 1]);
                    }
                }
            }
        }

        base.OnEnd(activity);
    }
}
