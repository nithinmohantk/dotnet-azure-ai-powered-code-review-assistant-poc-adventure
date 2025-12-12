using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CodeReviewAssistant.Core.Application.Interfaces;
using CodeReviewAssistant.Core.Domain.ValueObjects;
using DomainGitHubRepository = CodeReviewAssistant.Core.Domain.ValueObjects.GitHubRepository;
using InterfaceGitHubRepository = CodeReviewAssistant.Core.Application.Interfaces.GitHubRepository;

namespace CodeReviewAssistant.Infrastructure.ExternalServices
{
    public class GitHubService : IGitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(HttpClient httpClient, IConfiguration configuration, ILogger<GitHubService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var token = _configuration["GitHub:Token"];
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
            }

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CodeReviewAssistant/1.0");
        }

        public async Task ValidateRepositoryAccessAsync(InterfaceGitHubRepository repository, CancellationToken cancellationToken = default)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{repository.FullName}";
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Successfully validated access to repository {Repository}", repository);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to validate repository access for {Repository}", repository);
                throw new InvalidOperationException($"Cannot access repository: {repository}", ex);
            }
        }

        public async Task<string> GetFileContentAsync(InterfaceGitHubRepository repository, string branch, string filePath, CancellationToken cancellationToken = default)
        {
            var apiUrl = $"https://api.github.com/repos/{repository.FullName}/contents/{filePath}?ref={branch}";
            
            try
            {
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var fileData = JsonSerializer.Deserialize<GitHubApiResponse>(content);

                if (fileData?.Encoding == "base64")
                {
                    var bytes = Convert.FromBase64String(fileData.Content);
                    return System.Text.Encoding.UTF8.GetString(bytes);
                }

                return fileData?.Content ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to get file content for {FilePath} in {Repository}", filePath, repository);
                throw new InvalidOperationException($"Cannot get file content: {filePath}", ex);
            }
        }

        public async Task<IEnumerable<GitHubFile>> GetRepositoryFilesAsync(InterfaceGitHubRepository repository, string branch, CancellationToken cancellationToken = default)
        {
            var apiUrl = $"https://api.github.com/repos/{repository.FullName}/git/trees/{branch}?recursive=1";
            
            try
            {
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var treeData = JsonSerializer.Deserialize<GitHubTreeResponse>(content);

                var files = new List<GitHubFile>();
                foreach (var item in treeData.Tree.Where(t => t.Type == "blob"))
                {
                    files.Add(new GitHubFile
                    {
                        Path = item.Path,
                        Sha = item.Sha,
                        Size = item.Size,
                        Type = item.Type,
                        Url = item.Url,
                        HtmlUrl = item.HtmlUrl,
                        GitUrl = item.GitUrl,
                        DownloadUrl = item.DownloadUrl
                    });
                }

                return files;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to get repository files for {Repository}", repository);
                throw new InvalidOperationException($"Cannot get repository files", ex);
            }
        }

        public async Task<GitHubCommit> GetCommitDetailsAsync(InterfaceGitHubRepository repository, string commitHash, CancellationToken cancellationToken = default)
        {
            var apiUrl = $"https://api.github.com/repos/{repository.FullName}/commits/{commitHash}";
            
            try
            {
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var commitData = JsonSerializer.Deserialize<GitHubCommitResponse>(content);

                return new GitHubCommit
                {
                    Sha = commitData.Sha,
                    Message = commitData.Message,
                    Author = new GitHubAuthor
                    {
                        Name = commitData.Author.Name,
                        Email = commitData.Author.Email,
                        Date = commitData.Author.Date,
                        Username = commitData.Author.Login
                    },
                    Committer = new GitHubAuthor
                    {
                        Name = commitData.Committer.Name,
                        Email = commitData.Committer.Email,
                        Date = commitData.Committer.Date,
                        Username = commitData.Committer.Login
                    },
                    TreeSha = commitData.Tree.Sha,
                    CommitDate = commitData.Author.Date,
                    ParentShas = commitData.Parents?.Select(p => p.Sha).ToList() ?? new List<string>(),
                    Files = commitData.Files?.Select(f => new GitHubCommitFile
                    {
                        Sha = f.Sha,
                        Filename = f.Filename,
                        Status = f.Status,
                        Additions = f.Additions,
                        Deletions = f.Deletions,
                        Changes = f.Changes,
                        Patch = f.Patch,
                        BlobUrl = f.BlobUrl,
                        RawUrl = f.RawUrl,
                        ContentsUrl = f.ContentsUrl
                    }).ToList() ?? new List<GitHubCommitFile>(),
                    Stats = new GitHubStats
                    {
                        Additions = commitData.Stats.Additions,
                        Deletions = commitData.Stats.Deletions,
                        Total = commitData.Stats.Total
                    }
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to get commit details for {CommitHash} in {Repository}", commitHash, repository);
                throw new InvalidOperationException($"Cannot get commit details: {commitHash}", ex);
            }
        }

        public async Task<GitHubPullRequest> GetPullRequestDetailsAsync(InterfaceGitHubRepository repository, int pullRequestNumber, CancellationToken cancellationToken = default)
        {
            var apiUrl = $"https://api.github.com/repos/{repository.FullName}/pulls/{pullRequestNumber}";
            
            try
            {
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var prData = JsonSerializer.Deserialize<GitHubPullRequestResponse>(content);

                return new GitHubPullRequest
                {
                    Number = prData.Number,
                    Title = prData.Title,
                    Body = prData.Body,
                    State = prData.State,
                    Draft = prData.Draft,
                    User = new GitHubUser
                    {
                        Login = prData.User.Login,
                        Id = prData.User.Id,
                        AvatarUrl = prData.User.AvatarUrl,
                        HtmlUrl = prData.User.HtmlUrl,
                        Type = prData.User.Type,
                        SiteAdmin = prData.User.SiteAdmin
                    },
                    HeadSha = prData.Head.Sha,
                    HeadRef = prData.Head.Ref,
                    BaseSha = prData.Base.Sha,
                    BaseRef = prData.Base.Ref,
                    CreatedAt = prData.CreatedAt,
                    UpdatedAt = prData.UpdatedAt,
                    MergedAt = prData.MergedAt,
                    MergeCommitSha = prData.MergeCommitSha,
                    MergedBy = prData.MergedBy != null ? new GitHubUser
                    {
                        Login = prData.MergedBy.Login,
                        Id = prData.MergedBy.Id,
                        AvatarUrl = prData.MergedBy.AvatarUrl,
                        HtmlUrl = prData.MergedBy.HtmlUrl,
                        Type = prData.MergedBy.Type,
                        SiteAdmin = prData.MergedBy.SiteAdmin
                    } : null,
                    Repository = new InterfaceGitHubRepository
                    {
                        Id = prData.Base.Repo.Id,
                        Name = prData.Base.Repo.Name,
                        FullName = prData.Base.Repo.FullName,
                        Private = prData.Base.Repo.Private,
                        Owner = new GitHubUser
                        {
                            Login = prData.Base.Repo.Owner.Login,
                            Id = prData.Base.Repo.Owner.Id,
                            AvatarUrl = prData.Base.Repo.Owner.AvatarUrl,
                            HtmlUrl = prData.Base.Repo.Owner.HtmlUrl,
                            Type = prData.Base.Repo.Owner.Type,
                            SiteAdmin = prData.Base.Repo.Owner.SiteAdmin
                        },
                        HtmlUrl = prData.Base.Repo.HtmlUrl,
                        Description = prData.Base.Repo.Description,
                        Fork = prData.Base.Repo.Fork,
                        DefaultBranch = prData.Base.Repo.DefaultBranch
                    }
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to get pull request details for #{PullRequestNumber} in {Repository}", pullRequestNumber, repository);
                throw new InvalidOperationException($"Cannot get pull request details: #{pullRequestNumber}", ex);
            }
        }

        public async Task<IEnumerable<GitHubPullRequest>> GetPullRequestsAsync(InterfaceGitHubRepository repository, string branch = null, CancellationToken cancellationToken = default)
        {
            var apiUrl = $"https://api.github.com/repos/{repository.FullName}/pulls";
            if (!string.IsNullOrEmpty(branch))
            {
                apiUrl += $"?base={branch}";
            }
            
            try
            {
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var prDataList = JsonSerializer.Deserialize<List<GitHubPullRequestResponse>>(content);

                return prDataList.Select(pr => new GitHubPullRequest
                {
                    Number = pr.Number,
                    Title = pr.Title,
                    Body = pr.Body,
                    State = pr.State,
                    Draft = pr.Draft,
                    User = new GitHubUser
                    {
                        Login = pr.User.Login,
                        Id = pr.User.Id,
                        AvatarUrl = pr.User.AvatarUrl,
                        HtmlUrl = pr.User.HtmlUrl,
                        Type = pr.User.Type,
                        SiteAdmin = pr.User.SiteAdmin
                    },
                    HeadSha = pr.Head.Sha,
                    HeadRef = pr.Head.Ref,
                    BaseSha = pr.Base.Sha,
                    BaseRef = pr.Base.Ref,
                    CreatedAt = pr.CreatedAt,
                    UpdatedAt = pr.UpdatedAt,
                    MergedAt = pr.MergedAt,
                    MergeCommitSha = pr.MergeCommitSha
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to get pull requests for {Repository}", repository);
                throw new InvalidOperationException("Cannot get pull requests", ex);
            }
        }

        public async Task<string> CreatePullRequestCommentAsync(InterfaceGitHubRepository repository, int pullRequestNumber, string body, CancellationToken cancellationToken = default)
        {
            var apiUrl = $"https://api.github.com/repos/{repository.FullName}/issues/{pullRequestNumber}/comments";
            var payload = new { body };
            
            try
            {
                var content = JsonSerializer.Serialize(payload);
                var response = await _httpClient.PostAsync(apiUrl, new StringContent(content, System.Text.Encoding.UTF8, "application/json"), cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var commentData = JsonSerializer.Deserialize<GitHubCommentResponse>(responseContent);

                return commentData.HtmlUrl;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to create pull request comment for #{PullRequestNumber} in {Repository}", pullRequestNumber, repository);
                throw new InvalidOperationException($"Cannot create pull request comment: #{pullRequestNumber}", ex);
            }
        }

        public async Task<string> CreateCommitCommentAsync(InterfaceGitHubRepository repository, string commitHash, string body, string filePath = null, int? line = null, CancellationToken cancellationToken = default)
        {
            var apiUrl = $"https://api.github.com/repos/{repository.FullName}/commits/{commitHash}/comments";
            var payload = new { body, path = filePath, line };
            
            try
            {
                var content = JsonSerializer.Serialize(payload);
                var response = await _httpClient.PostAsync(apiUrl, new StringContent(content, System.Text.Encoding.UTF8, "application/json"), cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var commentData = JsonSerializer.Deserialize<GitHubCommentResponse>(responseContent);

                return commentData.HtmlUrl;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to create commit comment for {CommitHash} in {Repository}", commitHash, repository);
                throw new InvalidOperationException($"Cannot create commit comment: {commitHash}", ex);
            }
        }

        public async Task<GitHubRepositoryInfo> GetRepositoryInfoAsync(InterfaceGitHubRepository repository, CancellationToken cancellationToken = default)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{repository.FullName}";
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var repoData = JsonSerializer.Deserialize<GitHubRepositoryResponse>(content);

                // Get languages
                var languagesUrl = $"https://api.github.com/repos/{repository.FullName}/languages";
                var languagesResponse = await _httpClient.GetAsync(languagesUrl, cancellationToken);
                var languagesContent = await languagesResponse.Content.ReadAsStringAsync(cancellationToken);
                var languagesData = JsonSerializer.Deserialize<Dictionary<string, int>>(languagesContent);

                return new GitHubRepositoryInfo
                {
                    Name = repoData.Name,
                    FullName = repoData.FullName,
                    Description = repoData.Description,
                    Private = repoData.Private,
                    DefaultBranch = repoData.DefaultBranch,
                    Stats = new GitHubRepositoryStats
                    {
                        Forks = repoData.ForksCount,
                        OpenIssues = repoData.OpenIssuesCount,
                        Watchers = repoData.WatchersCount,
                        Stargazers = repoData.StargazersCount,
                        Size = (int)repoData.Size
                    },
                    Languages = languagesData?.Keys.ToList() ?? new List<string>(),
                    CreatedAt = repoData.CreatedAt,
                    UpdatedAt = repoData.UpdatedAt,
                    PushedAt = repoData.PushedAt
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to get repository info for {Repository}", repository);
                throw new InvalidOperationException($"Cannot get repository info", ex);
            }
        }

        public bool ValidateWebhookSignature(string requestBody, string signature)
        {
            try
            {
                var secret = _configuration["GitHub:WebhookSecret"];
                if (string.IsNullOrEmpty(secret))
                {
                    _logger.LogWarning("GitHub webhook secret not configured");
                    return false;
                }

                var expectedSignature = $"sha256={ComputeHmacSha256(secret, requestBody)}";
                return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate webhook signature");
                return false;
            }
        }

        private string ComputeHmacSha256(string secret, string payload)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(secret);
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
            
            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    // Internal response models for JSON deserialization
    internal class GitHubApiResponse
    {
        public string Content { get; set; }
        public string Encoding { get; set; }
    }

    internal class GitHubTreeResponse
    {
        public List<GitHubTreeItem> Tree { get; set; }
    }

    internal class GitHubTreeItem
    {
        public string Path { get; set; }
        public string Sha { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string GitUrl { get; set; }
        public string DownloadUrl { get; set; }
    }

    internal class GitHubCommitResponse
    {
        public string Sha { get; set; }
        public string Message { get; set; }
        public GitHubAuthorInfo Author { get; set; }
        public GitHubAuthorInfo Committer { get; set; }
        public GitHubTreeInfo Tree { get; set; }
        public List<GitHubParentInfo> Parents { get; set; }
        public List<GitHubCommitFileResponse> Files { get; set; }
        public GitHubStatsInfo Stats { get; set; }
    }

    internal class GitHubAuthorInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
        public string Login { get; set; }
    }

    internal class GitHubTreeInfo
    {
        public string Sha { get; set; }
    }

    internal class GitHubParentInfo
    {
        public string Sha { get; set; }
    }

    internal class GitHubCommitFileResponse
    {
        public string Sha { get; set; }
        public string Filename { get; set; }
        public string Status { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int Changes { get; set; }
        public string Patch { get; set; }
        public string BlobUrl { get; set; }
        public string RawUrl { get; set; }
        public string ContentsUrl { get; set; }
    }

    internal class GitHubStatsInfo
    {
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int Total { get; set; }
    }

    internal class GitHubPullRequestResponse
    {
        public int Number { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string State { get; set; }
        public bool Draft { get; set; }
        public GitHubUserInfo User { get; set; }
        public GitHubPullRequestBranch Head { get; set; }
        public GitHubPullRequestBranch Base { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? MergedAt { get; set; }
        public string MergeCommitSha { get; set; }
        public GitHubUserInfo MergedBy { get; set; }
    }

    internal class GitHubUserInfo
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string AvatarUrl { get; set; }
        public string HtmlUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
    }

    internal class GitHubPullRequestBranch
    {
        public string Sha { get; set; }
        public string Ref { get; set; }
        public GitHubRepositoryInfo Repo { get; set; }
    }

    internal class GitHubCommentResponse
    {
        public string HtmlUrl { get; set; }
    }

    internal class GitHubRepositoryResponse
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public bool Private { get; set; }
        public GitHubUserInfo Owner { get; set; }
        public string HtmlUrl { get; set; }
        public bool Fork { get; set; }
        public string DefaultBranch { get; set; }
        public int ForksCount { get; set; }
        public int OpenIssuesCount { get; set; }
        public int WatchersCount { get; set; }
        public int StargazersCount { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PushedAt { get; set; }
    }
}
