using System;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using Moq;
using Xunit;
using CodeReviewAssistant.Core.Domain.Entities;
using CodeReviewAssistant.Core.Domain.ValueObjects;

namespace CodeReviewAssistant.Unit.Tests.Domain.Entities
{
    public class CodeReviewTests
    {
        private readonly IFixture _fixture;

        public CodeReviewTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateCodeReview()
        {
            // Arrange
            var title = _fixture.Create<string>();
            var description = _fixture.Create<string>();
            var repositoryUrl = "https://github.com/user/repo";
            var branchName = _fixture.Create<string>();
            var commitHash = _fixture.Create<string>();
            var requestedBy = _fixture.Create<string>();
            var priority = Priority.Medium;

            // Act
            var codeReview = new CodeReview(title, description, repositoryUrl, branchName, commitHash, requestedBy, priority);

            // Assert
            codeReview.Should().NotBeNull();
            codeReview.Title.Should().Be(title);
            codeReview.Description.Should().Be(description);
            codeReview.RepositoryUrl.Should().Be(repositoryUrl);
            codeReview.BranchName.Should().Be(branchName);
            codeReview.CommitHash.Should().Be(commitHash);
            codeReview.RequestedBy.Should().Be(requestedBy);
            codeReview.Priority.Should().Be(priority);
            codeReview.Status.Should().Be(ReviewStatus.Pending);
            codeReview.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            codeReview.Issues.Should().BeEmpty();
            codeReview.Comments.Should().BeEmpty();
            codeReview.Files.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null, "description", "repository", "branch", "commit", "user")]
        [InlineData("title", null, "repository", "branch", "commit", "user")]
        [InlineData("title", "description", null, "branch", "commit", "user")]
        [InlineData("title", "description", "repository", null, "commit", "user")]
        [InlineData("title", "description", "repository", "branch", null, "user")]
        [InlineData("title", "description", "repository", "branch", "commit", null)]
        public void Constructor_WithNullParameters_ShouldThrowArgumentNullException(
            string title, string description, string repositoryUrl, string branchName, string commitHash, string requestedBy)
        {
            // Act & Assert
            Action act = () => new CodeReview(title, description, repositoryUrl, branchName, commitHash, requestedBy);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void StartReview_WhenStatusIsPending_ShouldChangeToInProgress()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();

            // Act
            codeReview.StartReview();

            // Assert
            codeReview.Status.Should().Be(ReviewStatus.InProgress);
            codeReview.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void StartReview_WhenStatusIsNotPending_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            codeReview.StartReview(); // Change to InProgress

            // Act & Assert
            Action act = () => codeReview.StartReview();
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Review can only be started from pending status");
        }

        [Fact]
        public void CompleteReview_WhenStatusIsInProgress_ShouldCompleteSuccessfully()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            codeReview.StartReview();
            var summary = _fixture.Create<string>();
            var issues = CreateTestIssues();

            // Act
            codeReview.CompleteReview(summary, issues);

            // Assert
            codeReview.Status.Should().Be(ReviewStatus.Completed);
            codeReview.Summary.Should().Be(summary);
            codeReview.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            codeReview.Issues.Should().HaveCount(issues.Count);
            codeReview.TotalIssues.Should().Be(issues.Count);
            codeReview.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void CompleteReview_WhenStatusIsNotInProgress_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            var summary = _fixture.Create<string>();
            var issues = CreateTestIssues();

            // Act & Assert
            Action act = () => codeReview.CompleteReview(summary, issues);
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Review can only be completed from in-progress status");
        }

        [Fact]
        public void CompleteReview_WithNullSummary_ShouldThrowArgumentNullException()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            codeReview.StartReview();
            var issues = CreateTestIssues();

            // Act & Assert
            Action act = () => codeReview.CompleteReview(null, issues);
            act.Should().Throw<ArgumentNullException>().WithParameterName("summary");
        }

        [Fact]
        public void FailReview_WhenStatusIsInProgress_ShouldFailSuccessfully()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            codeReview.StartReview();
            var reason = _fixture.Create<string>();

            // Act
            codeReview.FailReview(reason);

            // Assert
            codeReview.Status.Should().Be(ReviewStatus.Failed);
            codeReview.Summary.Should().Be(reason);
            codeReview.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            codeReview.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void AddComment_WithValidParameters_ShouldAddComment()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            var content = _fixture.Create<string>();
            var author = _fixture.Create<string>();

            // Act
            codeReview.AddComment(content, author);

            // Assert
            codeReview.Comments.Should().HaveCount(1);
            var comment = codeReview.Comments.First();
            comment.Content.Should().Be(content);
            comment.Author.Should().Be(author);
            comment.CodeReviewId.Should().Be(codeReview.Id);
            codeReview.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void AddComment_WithEmptyContent_ShouldThrowArgumentException()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            var author = _fixture.Create<string>();

            // Act & Assert
            Action act = () => codeReview.AddComment("", author);
            act.Should().Throw<ArgumentException>().WithParameterName("content");
        }

        [Fact]
        public void AddFile_WithValidParameters_ShouldAddFile()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            var filePath = _fixture.Create<string>();
            var content = _fixture.Create<string>();
            var fileType = ".cs";

            // Act
            codeReview.AddFile(filePath, content, fileType);

            // Assert
            codeReview.Files.Should().HaveCount(1);
            var file = codeReview.Files.First();
            file.FilePath.Should().Be(filePath);
            file.Content.Should().Be(content);
            file.FileType.Should().Be(fileType);
            file.CodeReviewId.Should().Be(codeReview.Id);
            codeReview.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void CalculateIssueCounts_ShouldCalculateCorrectly()
        {
            // Arrange
            var codeReview = CreateValidCodeReview();
            codeReview.StartReview();
            
            var issues = new List<ReviewIssue>
            {
                new ReviewIssue(codeReview.Id, "Critical Issue", "Description", "file.cs", Severity.Critical, IssueCategory.Security),
                new ReviewIssue(codeReview.Id, "Major Issue", "Description", "file.cs", Severity.Major, IssueCategory.Performance),
                new ReviewIssue(codeReview.Id, "Minor Issue", "Description", "file.cs", Severity.Minor, IssueCategory.CodeQuality)
            };

            // Act
            codeReview.CompleteReview("Test summary", issues);

            // Assert
            codeReview.TotalIssues.Should().Be(3);
            codeReview.CriticalIssues.Should().Be(1);
            codeReview.MajorIssues.Should().Be(1);
            codeReview.MinorIssues.Should().Be(1);
        }

        private CodeReview CreateValidCodeReview()
        {
            return new CodeReview(
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                "https://github.com/user/repo",
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                Priority.Medium);
        }

        private List<ReviewIssue> CreateTestIssues()
        {
            var codeReviewId = Guid.NewGuid();
            return new List<ReviewIssue>
            {
                new ReviewIssue(codeReviewId, "Test Issue 1", "Description 1", "file1.cs", Severity.Major, IssueCategory.Security),
                new ReviewIssue(codeReviewId, "Test Issue 2", "Description 2", "file2.cs", Severity.Minor, IssueCategory.CodeQuality)
            };
        }
    }
}
