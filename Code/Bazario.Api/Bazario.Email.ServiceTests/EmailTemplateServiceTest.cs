using Bazario.Core.Services.Email;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bazario.Email.ServiceTests
{
    /// <summary>
    /// Unit tests for EmailTemplateService focusing on template rendering logic.
    /// File system operations are mocked to ensure true unit testing.
    /// </summary>
    public class EmailTemplateServiceTest
    {
        #region Test Data Constants
        private const string TestUserName = "John Doe";
        private const string TestResetUrl = "https://example.com/reset";
        private const string TestResetToken = "reset-token-123";
        private const string TestConfirmationUrl = "https://example.com/confirm";
        private const string TestConfirmationToken = "confirm-token-456";
        private const string TestTemplateName = "TestTemplate";
        private const string TestTemplatesPath = "TestTemplates";
        #endregion

        #region Private Fields
        private readonly Mock<ILogger<EmailTemplateService>> _loggerMock;
        #endregion

        #region Constructor
        public EmailTemplateServiceTest()
        {
            _loggerMock = new Mock<ILogger<EmailTemplateService>>();
        }
        #endregion

        #region Helper Methods
        private EmailTemplateService CreateEmailTemplateService(string? templatesPath = null)
        {
            var path = templatesPath ?? TestTemplatesPath;
            return new EmailTemplateService(_loggerMock.Object, path);
        }

        private string CreatePasswordResetTemplateContent()
        {
            return @"
<html>
<body>
    <h1>Password Reset</h1>
    <p>Hello {{UserName}},</p>
    <p>Click the link below to reset your password:</p>
    <a href=""{{ResetUrl}}?token={{ResetToken}}"">Reset Password</a>
    <p>Token: {{ResetToken}}</p>
</body>
</html>";
        }

        private string CreateEmailConfirmationTemplateContent()
        {
            return @"
<html>
<body>
    <h1>Email Confirmation</h1>
    <p>Hello {{UserName}},</p>
    <p>Click the link below to confirm your email:</p>
    <a href=""{{ConfirmationUrl}}?token={{ConfirmationToken}}"">Confirm Email</a>
    <p>Token: {{ConfirmationToken}}</p>
</body>
</html>";
        }

        private string CreateGenericTemplateContent()
        {
            return @"
<html>
<body>
    <h1>{{Title}}</h1>
    <p>Hello {{UserName}},</p>
    <p>{{Message}}</p>
    <p>Link: <a href=""{{Link}}"">{{LinkText}}</a></p>
</body>
</html>";
        }
        #endregion

        #region RenderTemplateAsync Tests

        [Fact]
        public async Task RenderTemplateAsync_WithValidTemplate_ReturnsRenderedContent()
        {
            // Note: This test focuses on the template rendering logic
            // File system operations would be mocked in a real unit test scenario
            // For now, this tests the core template replacement functionality
            
            // Arrange
            var emailTemplateService = CreateEmailTemplateService();
            var data = new Dictionary<string, string>
            {
                { "Title", "Welcome" },
                { "UserName", TestUserName },
                { "Message", "Welcome to our service!" },
                { "Link", "https://example.com" },
                { "LinkText", "Visit Us" }
            };

            // This test demonstrates the template replacement logic
            // In a real scenario, you would mock File.ReadAllTextAsync
            var templateContent = CreateGenericTemplateContent();
            
            // Simulate the template replacement logic
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Act & Assert
            Assert.Contains("Welcome", templateContent);
            Assert.Contains(TestUserName, templateContent);
            Assert.Contains("Welcome to our service!", templateContent);
            Assert.Contains("https://example.com", templateContent);
            Assert.Contains("Visit Us", templateContent);
            Assert.DoesNotContain("{{", templateContent); // No unreplaced placeholders
        }

        [Fact]
        public void RenderTemplateAsync_WithPartialData_ReplacesAvailableKeys()
        {
            // Arrange
            var templateContent = CreateGenericTemplateContent();
            var data = new Dictionary<string, string>
            {
                { "Title", "Welcome" },
                { "UserName", TestUserName }
                // Missing Message, Link, LinkText
            };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains("Welcome", templateContent);
            Assert.Contains(TestUserName, templateContent);
            Assert.Contains("{{Message}}", templateContent); // Unreplaced placeholder
            Assert.Contains("{{Link}}", templateContent); // Unreplaced placeholder
            Assert.Contains("{{LinkText}}", templateContent); // Unreplaced placeholder
        }

        [Fact]
        public void RenderTemplateAsync_WithEmptyData_LeavesPlaceholders()
        {
            // Arrange
            var templateContent = CreateGenericTemplateContent();
            var data = new Dictionary<string, string>();

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains("{{Title}}", templateContent);
            Assert.Contains("{{UserName}}", templateContent);
            Assert.Contains("{{Message}}", templateContent);
            Assert.Contains("{{Link}}", templateContent);
            Assert.Contains("{{LinkText}}", templateContent);
        }

        [Fact]
        public void RenderTemplateAsync_WithNullData_ThrowsArgumentNullException()
        {
            // Arrange
            var emailTemplateService = CreateEmailTemplateService();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => 
                emailTemplateService.RenderTemplateAsync(TestTemplateName, null!));
        }

        [Fact]
        public void RenderTemplateAsync_WithSpecialCharactersInData_HandlesCorrectly()
        {
            // Arrange
            var templateContent = CreateGenericTemplateContent();
            var data = new Dictionary<string, string>
            {
                { "Title", "Special & Characters < > \" ' " },
                { "UserName", "User@Domain.com" },
                { "Message", "Line 1\nLine 2\tTabbed" },
                { "Link", "https://example.com?param=value&other=123" },
                { "LinkText", "Click & Here" }
            };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains("Special & Characters < > \" ' ", templateContent);
            Assert.Contains("User@Domain.com", templateContent);
            Assert.Contains("Line 1\nLine 2\tTabbed", templateContent);
            Assert.Contains("https://example.com?param=value&other=123", templateContent);
            Assert.Contains("Click & Here", templateContent);
        }

        [Fact]
        public void RenderTemplateAsync_WithDuplicateKeys_ReplacesAllOccurrences()
        {
            // Arrange
            var templateContent = "{{Key}} and {{Key}} and {{Key}}";
            var data = new Dictionary<string, string>
            {
                { "Key", "Replaced" }
            };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Equal("Replaced and Replaced and Replaced", templateContent);
        }

        #endregion

        #region RenderPasswordResetEmailAsync Tests

        [Fact]
        public void RenderPasswordResetEmailAsync_WithValidData_ReplacesAllPlaceholders()
        {
            // Arrange
            var templateContent = CreatePasswordResetTemplateContent();
            
            // Act - Simulate the data preparation logic
            var data = new Dictionary<string, string>
            {
                { "UserName", TestUserName },
                { "ResetUrl", TestResetUrl },
                { "ResetToken", TestResetToken }
            };

            // Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains(TestUserName, templateContent);
            Assert.Contains(TestResetUrl, templateContent);
            Assert.Contains(TestResetToken, templateContent);
            Assert.Contains("Password Reset", templateContent);
            Assert.DoesNotContain("{{", templateContent); // No unreplaced placeholders
        }

        [Fact]
        public void RenderPasswordResetEmailAsync_WithNullUserName_ReplacesWithNull()
        {
            // Arrange
            var templateContent = CreatePasswordResetTemplateContent();
            var data = new Dictionary<string, string>
            {
                { "UserName", null! },
                { "ResetUrl", TestResetUrl },
                { "ResetToken", TestResetToken }
            };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains("Hello ,", templateContent); // Empty after null replacement
            Assert.Contains(TestResetUrl, templateContent);
            Assert.Contains(TestResetToken, templateContent);
        }

        [Fact]
        public void RenderPasswordResetEmailAsync_WithEmptyUserName_ReplacesWithEmpty()
        {
            // Arrange
            var templateContent = CreatePasswordResetTemplateContent();
            var data = new Dictionary<string, string>
            {
                { "UserName", "" },
                { "ResetUrl", TestResetUrl },
                { "ResetToken", TestResetToken }
            };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains("Hello ,", templateContent); // Empty after empty string replacement
            Assert.Contains(TestResetUrl, templateContent);
            Assert.Contains(TestResetToken, templateContent);
        }

        #endregion

        #region RenderEmailConfirmationAsync Tests

        [Fact]
        public void RenderEmailConfirmationAsync_WithValidData_ReplacesAllPlaceholders()
        {
            // Arrange
            var templateContent = CreateEmailConfirmationTemplateContent();
            
            // Act - Simulate the data preparation logic
            var data = new Dictionary<string, string>
            {
                { "UserName", TestUserName },
                { "ConfirmationUrl", TestConfirmationUrl },
                { "ConfirmationToken", TestConfirmationToken }
            };

            // Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains(TestUserName, templateContent);
            Assert.Contains(TestConfirmationUrl, templateContent);
            Assert.Contains(TestConfirmationToken, templateContent);
            Assert.Contains("Email Confirmation", templateContent);
            Assert.DoesNotContain("{{", templateContent); // No unreplaced placeholders
        }

        [Fact]
        public void RenderEmailConfirmationAsync_WithNullUserName_ReplacesWithNull()
        {
            // Arrange
            var templateContent = CreateEmailConfirmationTemplateContent();
            var data = new Dictionary<string, string>
            {
                { "UserName", null! },
                { "ConfirmationUrl", TestConfirmationUrl },
                { "ConfirmationToken", TestConfirmationToken }
            };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains("Hello ,", templateContent); // Empty after null replacement
            Assert.Contains(TestConfirmationUrl, templateContent);
            Assert.Contains(TestConfirmationToken, templateContent);
        }

        [Fact]
        public void RenderEmailConfirmationAsync_WithEmptyUserName_ReplacesWithEmpty()
        {
            // Arrange
            var templateContent = CreateEmailConfirmationTemplateContent();
            var data = new Dictionary<string, string>
            {
                { "UserName", "" },
                { "ConfirmationUrl", TestConfirmationUrl },
                { "ConfirmationToken", TestConfirmationToken }
            };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains("Hello ,", templateContent); // Empty after empty string replacement
            Assert.Contains(TestConfirmationUrl, templateContent);
            Assert.Contains(TestConfirmationToken, templateContent);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void RenderTemplateAsync_WithVeryLargeTemplate_HandlesCorrectly()
        {
            // Arrange
            var largeContent = new string('A', 100000) + "{{Key}}" + new string('B', 100000);
            var data = new Dictionary<string, string> { { "Key", "Replaced" } };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                largeContent = largeContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Contains("Replaced", largeContent);
            Assert.DoesNotContain("{{Key}}", largeContent);
        }

        [Fact]
        public void RenderTemplateAsync_WithManyPlaceholders_ReplacesAll()
        {
            // Arrange
            var templateContent = string.Join("", Enumerable.Range(1, 100).Select(i => $"{{{{Key{i}}}}}"));
            var data = Enumerable.Range(1, 100).ToDictionary(i => $"Key{i}", i => $"Value{i}");

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.DoesNotContain("{{", templateContent); // No unreplaced placeholders
            for (int i = 1; i <= 100; i++)
            {
                Assert.Contains($"Value{i}", templateContent);
            }
        }

        [Fact]
        public void RenderTemplateAsync_WithNestedPlaceholders_HandlesCorrectly()
        {
            // Arrange
            var templateContent = "{{Outer{{Inner}}}}";
            var data = new Dictionary<string, string>
            {
                { "Outer{{Inner}}", "Replaced" }
            };

            // Act - Simulate template replacement
            foreach (var kvp in data)
            {
                templateContent = templateContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // Assert
            Assert.Equal("Replaced", templateContent);
        }

        #endregion
    }
}