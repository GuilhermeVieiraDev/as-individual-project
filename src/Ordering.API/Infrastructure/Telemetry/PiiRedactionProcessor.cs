using System.Diagnostics;
using OpenTelemetry;

namespace eShop.Ordering.API.Infrastructure.Telemetry;

public class PiiRedactionProcessor : BaseProcessor<Activity>
{
    private static readonly HashSet<string> _fullyRedactedTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CardNumber",
        "CardHolderName",
        "CardSecurityNumber",
        "CreditCard",
        "Email",
        "PhoneNumber",
        "Password",
        "Token",
        "AuthToken",
        "AccessToken",
        "RefreshToken",
        "Secret",
        "SSN",
        "SocialSecurityNumber",
        "TaxId",
        "AccountNumber",
        "BankAccount",
        "RoutingNumber",
        "IPAddress"
    };

    private static readonly HashSet<string> _partiallyMaskedTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "user.id",
        "user.name",
        "UserName",
        "FullName",
        "FirstName",
        "LastName",
        "ZipCode",
        "PostalCode",
        "Address",
        "Street",
        "StreetAddress",
        "City",
        "State",
        "Country",
        "DateOfBirth",
        "BirthDate"
    };

    public override void OnEnd(Activity activity)
    {
        if (activity == null) return;

        // Process all tags
        foreach (var tag in activity.TagObjects)
        {
            // Full redaction for sensitive data
            if (_fullyRedactedTagNames.Any(sensitiveTag => 
                tag.Key.Contains(sensitiveTag, StringComparison.OrdinalIgnoreCase)))
            {
                activity.SetTag(tag.Key, "[REDACTED]");
                continue;
            }

            // Partial masking for somewhat sensitive data
            if (_partiallyMaskedTagNames.Any(partialTag => 
                tag.Key.Contains(partialTag, StringComparison.OrdinalIgnoreCase)) &&
                tag.Value is string valueStr &&
                !string.IsNullOrEmpty(valueStr))
            {
                // Different masking strategies based on data length
                if (valueStr.Length > 4)
                {
                    // For longer strings, show first 2 and last 2 characters
                    activity.SetTag(tag.Key, $"{valueStr[..2]}{'*' * (valueStr.Length - 4)}{valueStr[^2..]}");
                }
                else if (valueStr.Length > 1)
                {
                    // For shorter strings, just show first and last character
                    activity.SetTag(tag.Key, $"{valueStr[0]}{'*' * (valueStr.Length - 2)}{valueStr[^1]}");
                }
            }
        }

        // Examine event names for potential PII
        if (activity.Events != null)
        {
            foreach (var activityEvent in activity.Events)
            {
                // Process event tags for PII
                foreach (var tag in activityEvent.Tags)
                {
                    if (_fullyRedactedTagNames.Any(sensitiveTag => 
                        tag.Key.Contains(sensitiveTag, StringComparison.OrdinalIgnoreCase)))
                    {
                        activity.AddTag($"{activityEvent.Name}.{tag.Key}", "[REDACTED]");
                    }
                    else if (_partiallyMaskedTagNames.Any(partialTag => 
                        tag.Key.Contains(partialTag, StringComparison.OrdinalIgnoreCase)) &&
                        tag.Value is string valueStr &&
                        !string.IsNullOrEmpty(valueStr))
                    {
                        if (valueStr.Length > 4)
                        {
                            activity.AddTag($"{activityEvent.Name}.{tag.Key}", 
                                $"{valueStr[..2]}{'*' * (valueStr.Length - 4)}{valueStr[^2..]}");
                        }
                        else if (valueStr.Length > 1)
                        {
                            activity.AddTag($"{activityEvent.Name}.{tag.Key}", 
                                $"{valueStr[0]}{'*' * (valueStr.Length - 2)}{valueStr[^1]}");
                        }
                    }
                }
            }
        }

        base.OnEnd(activity);
    }
}
