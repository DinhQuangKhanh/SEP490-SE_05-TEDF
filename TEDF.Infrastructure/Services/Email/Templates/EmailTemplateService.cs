namespace TEDF.Infrastructure.Services.Email.Templates
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IReadOnlyDictionary<string, string> _templates = EmailTemplates.GetAllTemplates();

        public string RenderTemplate<T>(string templateName, T model)
        {
            if (!_templates.TryGetValue(templateName, out var template))
                throw new ArgumentException($"Template '{templateName}' not found.");

            // Simple placeholder replacement
            var result = template;
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(model)?.ToString() ?? string.Empty;
                result = result.Replace($"{{{{{prop.Name}}}}}", value);
            }

            return result;
        }

        public bool TemplateExists(string templateName) => _templates.ContainsKey(templateName);
    }
}
