using FluentValidation.TestHelper;
using Xunit;
using CodeReviewAssistant.Core.Application.Commands;
using CodeReviewAssistant.Core.Application.Validators;

namespace CodeReviewAssistant.Unit.Tests.Application.Validators
{
    public class CreateCodeReviewCommandValidatorTests
    {
        private readonly CreateCodeReviewCommandValidator _validator;

        public CreateCodeReviewCommandValidatorTests()
        {
            _validator = new CreateCodeReviewCommandValidator();
        }

        [Fact]
        public void Validator_WithValidCommand_ShouldNotHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://github.com/user/repo",
                "feature-branch",
                "abc123def456",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validator_WithEmptyTitle_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "",
                "Test Description",
                "https://github.com/user/repo",
                "feature-branch",
                "abc123def456",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validator_WithTitleExceedingMaxLength_ShouldHaveValidationError()
        {
            // Arrange
            var longTitle = new string('a', 201);
            var command = new CreateCodeReviewCommand(
                longTitle,
                "Test Description",
                "https://github.com/user/repo",
                "feature-branch",
                "abc123def456",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validator_WithEmptyDescription_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "",
                "https://github.com/user/repo",
                "feature-branch",
                "abc123def456",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validator_WithInvalidGitHubUrl_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://gitlab.com/user/repo",
                "feature-branch",
                "abc123def456",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RepositoryUrl);
        }

        [Fact]
        public void Validator_WithEmptyBranchName_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://github.com/user/repo",
                "",
                "abc123def456",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BranchName);
        }

        [Fact]
        public void Validator_WithInvalidBranchNameCharacters_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://github.com/user/repo",
                "branch@invalid",
                "abc123def456",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BranchName);
        }

        [Fact]
        public void Validator_WithEmptyCommitHash_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://github.com/user/repo",
                "feature-branch",
                "",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CommitHash);
        }

        [Fact]
        public void Validator_WithInvalidCommitHashLength_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://github.com/user/repo",
                "feature-branch",
                "abc",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CommitHash);
        }

        [Fact]
        public void Validator_WithInvalidCommitHashCharacters_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://github.com/user/repo",
                "feature-branch",
                "xyz123def456",
                "testuser",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CommitHash);
        }

        [Fact]
        public void Validator_WithEmptyRequestedBy_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://github.com/user/repo",
                "feature-branch",
                "abc123def456",
                "",
                Priority.Medium);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RequestedBy);
        }

        [Fact]
        public void Validator_WithInvalidPriority_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateCodeReviewCommand(
                "Test Review",
                "Test Description",
                "https://github.com/user/repo",
                "feature-branch",
                "abc123def456",
                "testuser",
                (Priority)999);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Priority);
        }
    }
}
