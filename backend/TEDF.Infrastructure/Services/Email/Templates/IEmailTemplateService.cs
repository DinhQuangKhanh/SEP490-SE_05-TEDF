namespace TEDF.Infrastructure.Services.Email.Templates
{
    public interface IEmailTemplateService
    {
        string RenderTemplate<T>(string templateName, T model);
        bool TemplateExists(string templateName);
    }
}
