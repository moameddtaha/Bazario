using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bazario.Email.ServiceContracts
{
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email using SMTP
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
    }
}
