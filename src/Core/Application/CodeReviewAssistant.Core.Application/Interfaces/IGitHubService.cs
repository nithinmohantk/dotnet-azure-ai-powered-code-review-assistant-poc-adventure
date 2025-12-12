using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHubRepository = CodeReviewAssistant.Core.Domain.ValueObjects.GitHubRepository;

namespace CodeReviewAssistant.Core.Application.Interfaces
{
    public interface IGitHubService
    {
        Task ValidateRepositoryAccessAsync(GitHubRepository repository, CancellationToken cancellationToken = default);
        Task<string> GetFileContentAsync(GitHubRepository repository, string branch, string filePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<GitHubFile>> GetRepositoryFilesAsync(GitHubRepository repository, string branch, CancellationToken cancellationToken = default);
        Task<GitHubCommit> GetCommitDetailsAsync(GitHubRepository repository, string commitHash, CancellationToken cancellationToken = default);
        Task<GitHubPullRequest> GetPullRequestDetailsAsync(GitHubRepository repository, int pullRequestNumber, CancellationToken cancellationToken = default);
        Task<IEnumerable<GitHubPullRequest>> GetPullRequestsAsync(GitHubRepository repository, string branch = null, CancellationToken cancellationToken = default);
        Task<string> CreatePullRequestCommentAsync(GitHubRepository repository, int pullRequestNumber, string body, CancellationToken cancellationToken = default);
        Task<string> CreateCommitCommentAsync(GitHubRepository repository, string commitHash, string body, string filePath = null, int? line = null, CancellationToken cancellationToken = default);
        Task<GitHubRepositoryInfo> GetRepositoryInfoAsync(GitHubRepository repository, CancellationToken cancellationToken = default);
        bool ValidateWebhookSignature(string requestBody, string signature);
    }

    public class GitHubFile
    {
        public string Path { get; set; }
        public string Sha { get; set; }
        public long Size { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public string Encoding { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string GitUrl { get; set; }
        public string DownloadUrl { get; set; }
    }

    public class GitHubCommit
    {
        public string Sha { get; set; }
        public string Message { get; set; }
        public GitHubAuthor Author { get; set; }
        public GitHubAuthor Committer { get; set; }
        public string TreeSha { get; set; }
        public DateTime CommitDate { get; set; }
        public List<string> ParentShas { get; set; }
        public List<GitHubCommitFile> Files { get; set; }
        public GitHubStats Stats { get; set; }
    }

    public class GitHubAuthor
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
        public string Username { get; set; }
    }

    public class GitHubCommitFile
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

    public class GitHubStats
    {
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int Total { get; set; }
    }

    public class GitHubPullRequest
    {
        public int Number { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string State { get; set; }
        public bool Draft { get; set; }
        public GitHubUser User { get; set; }
        public GitHubUser Assignee { get; set; }
        public List<GitHubUser> Assignees { get; set; }
        public GitHubUser RequestedReviewer { get; set; }
        public List<GitHubUser> RequestedReviewers { get; set; }
        public string HeadSha { get; set; }
        public string HeadRef { get; set; }
        public string BaseSha { get; set; }
        public string BaseRef { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? MergedAt { get; set; }
        public string MergeCommitSha { get; set; }
        public GitHubUser MergedBy { get; set; }
        public GitHubRepository Repository { get; set; }
        public List<GitHubCommitFile> Files { get; set; }
        public GitHubPullRequestStats Stats { get; set; }
    }

    public class GitHubUser
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string AvatarUrl { get; set; }
        public string HtmlUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Blog { get; set; }
        public string Location { get; set; }
        public string Email { get; set; }
        public bool Hireable { get; set; }
        public string Bio { get; set; }
        public int Followers { get; set; }
        public int Following { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class GitHubRepository
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool Private { get; set; }
        public GitHubUser Owner { get; set; }
        public string HtmlUrl { get; set; }
        public string Description { get; set; }
        public bool Fork { get; set; }
        public string DefaultBranch { get; set; }
        public GitHubRepositoryStats Stats { get; set; }
    }

    public class GitHubRepositoryStats
    {
        public int Forks { get; set; }
        public int OpenIssues { get; set; }
        public int Watchers { get; set; }
        public int Stargazers { get; set; }
        public int Size { get; set; }
    }

    public class GitHubPullRequestStats
    {
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int ChangedFiles { get; set; }
        public int Commits { get; set; }
    }

    public class GitHubRepositoryInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public bool Private { get; set; }
        public GitHubUser Owner { get; set; }
        public string HtmlUrl { get; set; }
        public bool Fork { get; set; }
        public string DefaultBranch { get; set; }
        public GitHubRepositoryStats Stats { get; set; }
        public List<string> Languages { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PushedAt { get; set; }
    }
}
