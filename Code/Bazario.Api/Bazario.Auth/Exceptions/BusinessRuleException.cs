using System;

namespace Bazario.Auth.Exceptions
{
    /// <summary>
    /// Exception for business rule violations
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public string? RuleName { get; }
        public object? RuleContext { get; }

        public BusinessRuleException(string message) : base(message) 
        {
            RuleName = null;
            RuleContext = null;
        }

        public BusinessRuleException(string message, string ruleName) : base(message)
        {
            RuleName = ruleName;
            RuleContext = null;
        }

        public BusinessRuleException(string message, string ruleName, object ruleContext) : base(message)
        {
            RuleName = ruleName;
            RuleContext = ruleContext;
        }

        public BusinessRuleException(string message, Exception innerException) : base(message, innerException) 
        {
            RuleName = null;
            RuleContext = null;
        }
    }
}
