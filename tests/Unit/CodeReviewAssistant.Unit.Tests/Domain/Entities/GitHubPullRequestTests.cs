using System;
using AutoFixture;
using FluentAssertions;
using Xunit;
using CodeReviewAssistant.Core.Domain.Entities;

namespace CodeReviewAssistant.Unit.Tests.Domain.Entities
{
    public class GitHubPullRequestTests
    {
        private readonly IFixture _fixture;

        public GitHubPullRequestTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreatePullRequest()
        {
            // Arrange
            var pullRequestId = _fixture.Create<int>();
            var repositoryName = _fixture.Create<string>();
            var repositoryOwner = _fixture.Create<string>();
            var title = _fixture.Create<string>();
            var description = _fixture.Create<string>();
            var sourceBranch = _fixture.Create<string>();
            var targetBranch = _fixture.Create<string>();
            var headCommitSha = _fixture.Create<string>();
            var baseCommitSha = _fixture.Create<string>();
            var author = _fixture.Create<string>();
            var authorEmail = _fixture.Create<string>();
            var webhookPayload = _fixture.Create<string>();

            // Act
            var pullRequest = new GitHubPullRequest(
                pullRequestId, repositoryName, repositoryOwner, title, description,
                sourceBranch, targetBranch, headCommitSha, baseCommitSha,
                author, authorEmail, webhookPayload);

            // Assert
            pullRequest.Should().NotBeNull();
            pullRequest.PullRequestId.Should().Be(pullRequestId);
            pullRequest.RepositoryName.Should().Be(repositoryName);
            pullRequest.RepositoryOwner.Should().Be(repositoryOwner);
            pullRequest.Title.Should().Be(title);
            pullRequest.Description.Should().Be(description);
            pullRequest.SourceBranch.Should().Be(sourceBranch);
            pullRequest.TargetBranch.Should().Be(targetBranch);
            pullRequest.HeadCommitSha.Should().Be(headCommitSha);
            pullRequest.BaseCommitSha.Should().Be(baseCommitSha);
            pullRequest.Author.Should().Be(author);
            pullRequest.AuthorEmail.Should().Be(authorEmail);
            pullRequest.Status.Should().Be(PullRequestStatus.Open);
            pullRequest.IsDraft.Should().BeFalse();
            pullRequest.IsMerged.Should().BeFalse();
            pullRequest.Mergeable.Should().BeTrue();
            pullRequest.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Constructor_WithNullParameters_ShouldThrowArgumentNullException()
        {
            // Arrange
            var validParameters = new
            {
                pullRequestId = _fixture.Create<int>(),
                repositoryName = _fixture.Create<string>(),
                repositoryOwner = _fixture.Create<string>(),
                title = _fixture.Create<string>(),
                description = _fixture.Create<string>(),
                sourceBranch = _fixture.Create<string>(),
                targetBranch = _fixture.Create<string>(),
                headCommitSha = _fixture.Create<string>(),
                baseCommitSha = _fixture.Create<string>(),
                author = _fixture.Create<string>(),
                authorEmail = _fixture.Create<string>(),
                webhookPayload = _fixture.Create<string>()
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GitHubPullRequest(
                validParameters.pullRequestId, null, validParameters.repositoryOwner,
                validParameters.title, validParameters.description, validParameters.sourceBranch,
                validParameters.targetBranch, validParameters.headCommitSha, validParameters.baseCommitSha,
                validParameters.author, validParameters.authorEmail, validParameters.webhookPayload));

            Assert.Throws<ArgumentNullException>(() => new GitHubPullRequest(
                validParameters.pullRequestId, validParameters.repositoryName, null,
                validParameters.title, validParameters.description, validParameters.sourceBranch,
                validParameters.targetBranch, validParameters.headCommitSha, validParameters.baseCommitSha,
                validParameters.author, validParameters.authorEmail, validParameters.webhookPayload));
        }

        [Fact]
        public void UpdateStatus_ShouldUpdateStatusAndUpdatedAt()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();
            var originalUpdatedAt = pullRequest.UpdatedAt;
            var newStatus = PullRequestStatus.Closed;

            // Act
            pullRequest.UpdateStatus(newStatus);

            // Assert
            pullRequest.Status.Should().Be(newStatus);
            pullRequest.UpdatedAt.Should().BeAfter(originalUpdatedAt.Value);
        }

        [Fact]
        public void MarkAsDraft_ShouldSetIsDraftToTrue()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();

            // Act
            pullRequest.MarkAsDraft();

            // Assert
            pullRequest.IsDraft.Should().BeTrue();
        }

        [Fact]
        public void MarkAsReadyForReview_ShouldSetIsDraftToFalse()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();
            pullRequest.MarkAsDraft();

            // Act
            pullRequest.MarkAsReadyForReview();

            // Assert
            pullRequest.IsDraft.Should().BeFalse();
        }

        [Fact]
        public void MarkAsMerged_ShouldSetMergedProperties()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();
            var mergeCommitSha = _fixture.Create<string>();

            // Act
            pullRequest.MarkAsMerged(mergeCommitSha);

            // Assert
            pullRequest.IsMerged.Should().BeTrue();
            pullRequest.Status.Should().Be(PullRequestStatus.Merged);
            pullRequest.MergeCommitSha.Should().Be(mergeCommitSha);
            pullRequest.MergedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Close_ShouldSetStatusToClosed()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();

            // Act
            pullRequest.Close();

            // Assert
            pullRequest.Status.Should().Be(PullRequestStatus.Closed);
            pullRequest.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void UpdateMergeability_ShouldUpdateMergeableProperty()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();

            // Act
            pullRequest.UpdateMergeability(false);

            // Assert
            pullRequest.Mergeable.Should().BeFalse();
        }

        [Fact]
        public void UpdateStatistics_ShouldUpdateLineCounts()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();
            var addedLines = _fixture.Create<int>();
            var deletedLines = _fixture.Create<int>();
            var changedFiles = _fixture.Create<int>();

            // Act
            pullRequest.UpdateStatistics(addedLines, deletedLines, changedFiles);

            // Assert
            pullRequest.AddedLines.Should().Be(addedLines);
            pullRequest.DeletedLines.Should().Be(deletedLines);
            pullRequest.ChangedFiles.Should().Be(changedFiles);
        }

        [Fact]
        public void AddFile_ShouldAddFileToCollection()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();
            var file = CreateMockReviewFile();

            // Act
            pullRequest.AddFile(file);

            // Assert
            pullRequest.Files.Should().HaveCount(1);
            pullRequest.Files.Should().Contain(file);
        }

        [Fact]
        public void AddComment_ShouldAddCommentToCollection()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();
            var comment = CreateMockReviewComment();

            // Act
            pullRequest.AddComment(comment);

            // Assert
            pullRequest.Comments.Should().HaveCount(1);
            pullRequest.Comments.Should().Contain(comment);
        }

        [Fact]
        public void AddFile_WithNullFile_ShouldThrowArgumentNullException()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => pullRequest.AddFile(null));
        }

        [Fact]
        public void AddComment_WithNullComment_ShouldThrowArgumentNullException()
        {
            // Arrange
            var pullRequest = CreateValidPullRequest();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => pullRequest.AddComment(null));
        }

        private GitHubPullRequest CreateValidPullRequest()
        {
            return new GitHubPullRequest(
                _fixture.Create<int>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>());
        }

        private ReviewFile CreateMockReviewFile()
        {
            // This would be a mock or stub of ReviewFile
            // For now, return null as this is just for testing the collection behavior
            return null;
        }

        private ReviewComment CreateMockReviewComment()
        {
            // This would be a mock or stub of ReviewComment
            // For now, return null as this is just for testing the collection behavior
            return null;
        }
    }
}
