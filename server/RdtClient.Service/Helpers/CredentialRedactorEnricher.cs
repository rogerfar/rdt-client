using Serilog.Core;
using Serilog.Events;
using RdtClient.Service.Services;

namespace RdtClient.Service.Helpers;

public class CredentialRedactorEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var sensitiveValues = new List<String>();

        var apiKey = Settings.Get.Provider.ApiKey;
        if (!String.IsNullOrWhiteSpace(apiKey) && apiKey.Length > 5)
        {
            sensitiveValues.Add(apiKey);
        }

        var aria2Secret = Settings.Get.DownloadClient.Aria2cSecret;
        if (!String.IsNullOrWhiteSpace(aria2Secret) && aria2Secret.Length > 5)
        {
            sensitiveValues.Add(aria2Secret);
        }

        var dsPassword = Settings.Get.DownloadClient.DownloadStationPassword;
        if (!String.IsNullOrWhiteSpace(dsPassword) && dsPassword.Length > 5)
        {
            sensitiveValues.Add(dsPassword);
        }

        if (sensitiveValues.Count == 0)
        {
            return;
        }

        foreach (var sensitiveValue in sensitiveValues)
        {
            // Redact in the message template
            if (logEvent.MessageTemplate.Text.Contains(sensitiveValue))
            {
                var newText = logEvent.MessageTemplate.Text.Replace(sensitiveValue, "*****");
                
                var field = typeof(MessageTemplate).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                                   .FirstOrDefault(f => f.FieldType == typeof(String));
                
                field?.SetValue(logEvent.MessageTemplate, newText);
            }

            // Redact in properties
            var propertiesToUpdate = new List<LogEventProperty>();
            foreach (var property in logEvent.Properties)
            {
                if (property.Value is ScalarValue scalarValue && scalarValue.Value is String stringValue && stringValue.Contains(sensitiveValue))
                {
                    var newValue = stringValue.Replace(sensitiveValue, "*****");
                    propertiesToUpdate.Add(new LogEventProperty(property.Key, new ScalarValue(newValue)));
                }
            }

            foreach (var property in propertiesToUpdate)
            {
                logEvent.AddOrUpdateProperty(property);
            }
        }
    }
}
