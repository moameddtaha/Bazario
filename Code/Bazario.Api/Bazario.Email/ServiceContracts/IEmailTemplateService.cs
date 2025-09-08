using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bazario.Email.ServiceContracts
{
    public interface IEmailTemplateService
    {
        /// <summary>
        /// Loads and renders a template with the provided data
        /// </summary>
        /// <param name="templateName">Name of the template file (without extension)</param>
        /// <param name="data">Dictionary of key-value pairs for template replacement</param>
        /// <returns>Rendered template content</returns>
        public Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> data);

        /// <summary>
        /// Renders password reset email template
        /// </summary>
        public Task<string> RenderPasswordResetEmailAsync(string userName, string resetUrl, string resetToken);

        /// <summary>
        /// Renders email confirmation template
        /// </summary>
        public Task<string> RenderEmailConfirmationAsync(string userName, string confirmationUrl, string confirmationToken);
    }
}
