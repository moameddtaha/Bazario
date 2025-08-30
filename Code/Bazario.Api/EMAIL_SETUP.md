# Email Setup Guide for Bazario API

## Overview
This guide explains how to set up email functionality in your Bazario API using Gmail SMTP.

## Prerequisites

### 1. Gmail Account Setup
1. **Enable 2-Factor Authentication** on your Gmail account
2. **Generate an App Password**:
   - Go to Google Account settings
   - Security → 2-Step Verification → App passwords
   - Generate a new app password for "Mail"
   - Use this password in your configuration (NOT your regular Gmail password)

### 2. Required NuGet Packages
The following packages are already included:
- `MailKit` - For SMTP email functionality
- `DotNetEnv` - For environment variable management

## Configuration

### Environment Variables
Create a `.env` file in your project root with the following variables:

```bash
# Gmail SMTP Settings
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=your-app-password
ENABLE_SSL=true
FROM_EMAIL=your-email@gmail.com
FROM_NAME=Bazario Team

# App Settings
EMAIL_CONFIRMATION_URL=https://your-domain.com/confirm-email
PASSWORD_RESET_URL=https://your-domain.com/reset-password
```

### AppSettings Configuration
The email settings are automatically bound to the `EmailSettings` model from your configuration.

## Features

### 1. Password Reset Emails
- Sends HTML-formatted password reset emails
- Includes secure reset links with expiration
- Professional styling and branding

### 2. Email Confirmation
- Sends welcome emails with confirmation links
- 24-hour expiration for security
- Responsive HTML design

### 3. Security Features
- SSL/TLS encryption
- App password authentication
- Token-based verification
- Configurable timeouts

## Production Considerations

### 1. Email Service Providers
For production, consider using dedicated email services:
- **SendGrid** - High deliverability, analytics
- **Mailgun** - Developer-friendly, good pricing
- **Amazon SES** - Cost-effective for high volume
- **Postmark** - Transactional email specialist

### 2. Environment-Specific Configuration
- Use different email accounts for dev/staging/production
- Implement email templates for different environments
- Set up monitoring and alerting for email failures

### 3. Security Best Practices
- Never commit `.env` files to source control
- Use strong, unique app passwords
- Implement rate limiting for email sending
- Monitor for suspicious email activity

### 4. Monitoring and Logging
- All email operations are logged with Serilog
- Track email delivery success/failure rates
- Set up alerts for email service issues

## Testing

### 1. Development Testing
- Use Gmail SMTP for development
- Test with real email addresses
- Verify email delivery and formatting

### 2. Production Testing
- Test with production email configuration
- Verify deliverability to major email providers
- Check spam folder placement

## Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Verify 2FA is enabled
   - Use app password, not regular password
   - Check username format (email address)

2. **Connection Timeout**
   - Verify SMTP server and port
   - Check firewall settings
   - Ensure SSL/TLS is properly configured

3. **Emails Not Delivered**
   - Check spam/junk folders
   - Verify sender email address
   - Check Gmail sending limits

### Debug Mode
Enable debug logging in development to troubleshoot SMTP issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Bazario.Core.Services.EmailService": "Debug"
    }
  }
}
```

## Support

For additional help:
1. Check the application logs
2. Verify environment variable configuration
3. Test SMTP connection manually
4. Review Gmail account settings
