using Microsoft.Extensions.Logging;

namespace Bazario.Email.Services
{
    /// <summary>
    /// Simple email template service for loading and rendering email templates
    /// </summary>
    public class EmailTemplateService
    {
        private readonly ILogger<EmailTemplateService> _logger;
        private readonly string _templatesPath;

        public EmailTemplateService(ILogger<EmailTemplateService> logger, string templatesPath)
        {
            _logger = logger;
            _templatesPath = templatesPath;
        }

        /// <summary>
        /// Loads and renders a template with the provided data
        /// </summary>
        /// <param name="templateName">Name of the template file (without extension)</param>
        /// <param name="data">Dictionary of key-value pairs for template replacement</param>
        /// <returns>Rendered template content</returns>
        public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> data)
        {
            try
            {
                var templatePath = Path.Combine(_templatesPath, $"{templateName}.html");
                
                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Email template not found: {TemplatePath}", templatePath);
                    throw new FileNotFoundException($"Email template not found: {templateName}");
                }

                var templateContent = await File.ReadAllTextAsync(templatePath);
                
                // Simple template replacement using {{Key}} syntax
                foreach (var kvp in data)
                {
                    templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
                }

                return templateContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render email template: {TemplateName}", templateName);
                throw;
            }
        }

        /// <summary>
        /// Renders password reset email template
        /// </summary>
        public async Task<string> RenderPasswordResetEmailAsync(string userName, string resetUrl, string resetToken)
        {
            var data = new Dictionary<string, string>
            {
                { "UserName", userName },
                { "ResetUrl", resetUrl },
                { "ResetToken", resetToken }
            };

            return await RenderTemplateAsync("PasswordResetEmail", data);
        }

        /// <summary>
        /// Renders email confirmation template
        /// </summary>
        public async Task<string> RenderEmailConfirmationAsync(string userName, string confirmationUrl, string confirmationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "UserName", userName },
                { "ConfirmationUrl", confirmationUrl },
                { "ConfirmationToken", confirmationToken }
            };

            return await RenderTemplateAsync("EmailConfirmation", data);
        }
    }
}
