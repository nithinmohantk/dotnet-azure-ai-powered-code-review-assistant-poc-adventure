using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CodeReviewAssistant.Core.Application.Interfaces;
using CodeReviewAssistant.Core.Domain.ValueObjects;
using CodeReviewAssistant.Core.Domain.Events;

namespace CodeReviewAssistant.Infrastructure.ExternalServices
{
    public interface IAzureOpenAIService
    {
        Task<CodeAnalysisResult> AnalyzeCodeAsync(string code, string filePath, CancellationToken cancellationToken = default);
        Task<CodeAnalysisResult> AnalyzeRepositoryAsync(string repositoryContent, Dictionary<string, string> files, CancellationToken cancellationToken = default);
        Task<string> GenerateReviewSummaryAsync(List<AnalysisIssue> issues, CancellationToken cancellationToken = default);
        Task<string> GenerateSuggestionAsync(AnalysisIssue issue, CancellationToken cancellationToken = default);
    }

    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly OpenAIClient _openAIClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly string _deploymentName;

        public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var key = _configuration["AzureOpenAI:Key"];
            _deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException("Azure OpenAI endpoint not configured");

            if (!string.IsNullOrEmpty(key))
            {
                // Set environment variable and use default credential
                Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", key);
                _openAIClient = new OpenAIClient(endpoint);
            }
            else
            {
                // Use default Azure credential
                _openAIClient = new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
            }
        }

        public async Task<CodeAnalysisResult> AnalyzeCodeAsync(string code, string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var language = DetermineLanguage(filePath);
                var prompt = GenerateCodeAnalysisPrompt(code, filePath, language);

                var messages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage("You are an expert code reviewer. Provide detailed, constructive feedback on code quality, security, performance, and best practices."),
                    new ChatRequestUserMessage(prompt)
                };

                var response = await _openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions(_deploymentName, messages)
                {
                    Temperature = 0.3f,
                    MaxTokens = 2000,
                    ChoiceCount = 1
                }, cancellationToken);

                var analysis = response.Value.Choices[0].Message.Content;
                var issues = ParseAnalysisResponse(analysis, filePath);

                return new CodeAnalysisResult(
                    $"Analysis completed for {filePath}",
                    issues,
                    new List<string> { "Review the identified issues and implement the suggested improvements." }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze code for file {FilePath}", filePath);
                throw new InvalidOperationException($"Code analysis failed for {filePath}", ex);
            }
        }

        public async Task<CodeAnalysisResult> AnalyzeRepositoryAsync(string repositoryContent, Dictionary<string, string> files, CancellationToken cancellationToken = default)
        {
            try
            {
                var allIssues = new List<AnalysisIssue>();
                var recommendations = new List<string>();

                foreach (var file in files.Take(10)) // Limit to 10 files to avoid token limits
                {
                    var fileIssues = await AnalyzeCodeAsync(file.Value, file.Key, cancellationToken);
                    allIssues.AddRange(fileIssues.Issues);
                }

                // Generate repository-level analysis
                var repositoryPrompt = GenerateRepositoryAnalysisPrompt(repositoryContent, files.Keys.ToList());
                var messages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage("You are an expert software architect. Analyze the overall repository structure and provide architectural insights."),
                    new ChatRequestUserMessage(repositoryPrompt)
                };

                var response = await _openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions(_deploymentName, messages)
                {
                    Temperature = 0.3f,
                    MaxTokens = 1500,
                    ChoiceCount = 1
                }, cancellationToken);

                var repositoryAnalysis = response.Value.Choices[0].Message.Content;
                recommendations.Add(repositoryAnalysis);

                return new CodeAnalysisResult(
                    "Repository analysis completed",
                    allIssues,
                    recommendations
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze repository");
                throw new InvalidOperationException("Repository analysis failed", ex);
            }
        }

        public async Task<string> GenerateReviewSummaryAsync(List<AnalysisIssue> issues, CancellationToken cancellationToken = default)
        {
            try
            {
                var prompt = GenerateSummaryPrompt(issues);
                var messages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage("You are a technical lead summarizing code review findings. Provide a concise, actionable summary."),
                    new ChatRequestUserMessage(prompt)
                };

                var response = await _openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions(_deploymentName, messages)
                {
                    Temperature = 0.2f,
                    MaxTokens = 1000,
                    ChoiceCount = 1
                }, cancellationToken);

                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate review summary");
                throw new InvalidOperationException("Failed to generate review summary", ex);
            }
        }

        public async Task<string> GenerateSuggestionAsync(AnalysisIssue issue, CancellationToken cancellationToken = default)
        {
            try
            {
                var prompt = GenerateSuggestionPrompt(issue);
                var messages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage("You are a senior developer providing specific code improvement suggestions."),
                    new ChatRequestUserMessage(prompt)
                };

                var response = await _openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions(_deploymentName, messages)
                {
                    Temperature = 0.3f,
                    MaxTokens = 500,
                    ChoiceCount = 1
                }, cancellationToken);

                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate suggestion for issue {IssueTitle}", issue.Title);
                throw new InvalidOperationException("Failed to generate suggestion", ex);
            }
        }

        private string DetermineLanguage(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".cs" => "C#",
                ".js" => "JavaScript",
                ".ts" => "TypeScript",
                ".py" => "Python",
                ".java" => "Java",
                ".cpp" => "C++",
                ".c" => "C",
                ".go" => "Go",
                ".rs" => "Rust",
                ".php" => "PHP",
                ".rb" => "Ruby",
                ".swift" => "Swift",
                ".kt" => "Kotlin",
                ".scala" => "Scala",
                _ => "Unknown"
            };
        }

        private string GenerateCodeAnalysisPrompt(string code, string filePath, string language)
        {
            return $"""
                Please analyze the following {language} code from file '{filePath}' and provide detailed feedback:

                ```{language}
                {code}
                ```

                Focus on:
                1. Code quality and readability
                2. Security vulnerabilities
                3. Performance issues
                4. Best practices violations
                5. Potential bugs or edge cases
                6. Maintainability concerns

                Format your response as a structured analysis with:
                - Overall assessment
                - Specific issues with severity levels (Critical, Major, Minor)
                - Line numbers where applicable
                - Specific suggestions for improvement
                """;
        }

        private string GenerateRepositoryAnalysisPrompt(string repositoryContent, List<string> filePaths)
        {
            return $"""
                Please analyze this repository structure and provide architectural insights:

                Repository files:
                {string.Join("\n", filePaths)}

                Repository overview:
                {repositoryContent}

                Focus on:
                1. Overall architecture patterns
                2. Code organization and structure
                3. Dependency management
                4. Design patterns usage
                5. Scalability considerations
                6. Security architecture
                7. Testing strategy
                8. Documentation quality

                Provide actionable recommendations for improvement.
                """;
        }

        private string GenerateSummaryPrompt(List<AnalysisIssue> issues)
        {
            var issuesBySeverity = issues.GroupBy(i => i.Severity)
                .ToDictionary(g => g.Key, g => g.Count());

            var issuesByCategory = issues.GroupBy(i => i.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            return $"""
                Please provide a concise summary of the following code review findings:

                Total Issues: {issues.Count}
                Critical: {issuesBySeverity.GetValueOrDefault(Severity.Critical, 0)}
                Major: {issuesBySeverity.GetValueOrDefault(Severity.Major, 0)}
                Minor: {issuesBySeverity.GetValueOrDefault(Severity.Minor, 0)}
                Info: {issuesBySeverity.GetValueOrDefault(Severity.Info, 0)}

                Issues by category:
                {string.Join("\n", issuesByCategory.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}

                Key issues:
                {string.Join("\n", issues.Where(i => i.Severity >= Severity.Major).Take(5).Select(i => $"- {i.Title}: {i.Description}"))}

                Provide a summary that includes:
                1. Overall code quality assessment
                2. Priority areas for improvement
                3. Recommended next steps
                4. Risk assessment
                """;
        }

        private string GenerateSuggestionPrompt(AnalysisIssue issue)
        {
            return $"""
                Please provide a specific, actionable suggestion to fix the following issue:

                Issue: {issue.Title}
                Description: {issue.Description}
                File: {issue.FilePath}
                Line: {issue.LineNumber}
                Severity: {issue.Severity}
                Category: {issue.Category}

                Provide:
                1. Specific code changes needed
                2. Explanation of why this fixes the issue
                3. Any alternative approaches
                4. Best practices to follow
                """;
        }

        private List<AnalysisIssue> ParseAnalysisResponse(string analysis, string filePath)
        {
            var issues = new List<AnalysisIssue>();
            var lines = analysis.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Parse the AI response to extract structured issues
                // This is a simplified parser - in production, you'd use more sophisticated parsing
                if (line.Contains("Critical") || line.Contains("Major") || line.Contains("Minor"))
                {
                    var severity = line.Contains("Critical") ? Severity.Critical :
                                  line.Contains("Major") ? Severity.Major : Severity.Minor;

                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var title = parts[0].Trim();
                        var description = parts[1].Trim();

                        issues.Add(new AnalysisIssue(
                            title,
                            description,
                            filePath,
                            severity,
                            DetermineCategory(description)
                        ));
                    }
                }
            }

            return issues;
        }

        private IssueCategory DetermineCategory(string description)
        {
            var desc = description.ToLowerInvariant();
            if (desc.Contains("security") || desc.Contains("auth") || desc.Contains("token"))
                return IssueCategory.Security;
            if (desc.Contains("performance") || desc.Contains("slow") || desc.Contains("memory"))
                return IssueCategory.Performance;
            if (desc.Contains("test") || desc.Contains("spec") || desc.Contains("unit"))
                return IssueCategory.Testing;
            if (desc.Contains("doc") || desc.Contains("comment") || desc.Contains("readme"))
                return IssueCategory.Documentation;
            if (desc.Contains("error") || desc.Contains("exception") || desc.Contains("handle"))
                return IssueCategory.ErrorHandling;
            if (desc.Contains("design") || desc.Contains("pattern") || desc.Contains("arch"))
                return IssueCategory.Design;
            
            return IssueCategory.CodeQuality;
        }
    }
}
