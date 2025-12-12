using System;
using System.Collections.Generic;
using CodeReviewAssistant.Core.Domain.Common;
using CodeReviewAssistant.Core.Domain.ValueObjects;

namespace CodeReviewAssistant.Core.Domain.Entities
{
    public class User : AuditableEntity
    {
        public Guid Id { get; private set; }
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string DisplayName { get; private set; }
        public string AvatarUrl { get; private set; }
        public string GitHubUsername { get; private set; }
        public string AzureAdObjectId { get; private set; }
        public UserRole Role { get; private set; }
        public UserStatus Status { get; private set; }
        public DateTime LastLoginAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsEmailVerified { get; private set; }
        public string Preferences { get; private set; }
        public int ReviewCount { get; private set; }
        public int ReviewRequestCount { get; private set; }

        private readonly List<UserPermission> _permissions = new();
        public IReadOnlyCollection<UserPermission> Permissions => _permissions.AsReadOnly();

        protected User()
        {
        }

        public User(
            string username,
            string email,
            string displayName,
            UserRole role = UserRole.Developer)
        {
            Id = Guid.NewGuid();
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Role = role;
            Status = UserStatus.Active;
            CreatedAt = DateTime.UtcNow;
            LastLoginAt = DateTime.UtcNow;
            IsEmailVerified = false;
            ReviewCount = 0;
            ReviewRequestCount = 0;
            Preferences = "{}";
        }

        public void UpdateProfile(string displayName, string avatarUrl = null)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                AvatarUrl = avatarUrl;
            }
        }

        public void LinkGitHub(string githubUsername)
        {
            GitHubUsername = githubUsername ?? throw new ArgumentNullException(nameof(githubUsername));
        }

        public void LinkAzureAd(string azureAdObjectId)
        {
            AzureAdObjectId = azureAdObjectId ?? throw new ArgumentNullException(nameof(azureAdObjectId));
        }

        public void VerifyEmail()
        {
            IsEmailVerified = true;
        }

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        public void ChangeRole(UserRole newRole)
        {
            Role = newRole;
        }

        public void Activate()
        {
            Status = UserStatus.Active;
        }

        public void Deactivate()
        {
            Status = UserStatus.Inactive;
        }

        public void Suspend()
        {
            Status = UserStatus.Suspended;
        }

        public void IncrementReviewCount()
        {
            ReviewCount++;
        }

        public void IncrementReviewRequestCount()
        {
            ReviewRequestCount++;
        }

        public void UpdatePreferences(string preferences)
        {
            Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        }

        public void AddPermission(UserPermission permission)
        {
            if (permission == null) throw new ArgumentNullException(nameof(permission));
            _permissions.Add(permission);
        }

        public bool HasPermission(string permission)
        {
            return _permissions.Exists(p => p.Name.Equals(permission, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class UserPermission : AuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public DateTime GrantedAt { get; private set; }
        public DateTime? ExpiresAt { get; private set; }
        public bool IsActive { get; private set; }

        protected UserPermission()
        {
        }

        public UserPermission(Guid userId, string name, string description = null)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            GrantedAt = DateTime.UtcNow;
            IsActive = true;
        }

        public void Revoke()
        {
            IsActive = false;
        }

        public void SetExpiration(DateTime expiresAt)
        {
            ExpiresAt = expiresAt;
        }

        public bool IsExpired()
        {
            return ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
        }
    }

    public enum UserRole
    {
        Developer,
        CodeReviewer,
        TeamLead,
        Admin,
        SystemAdministrator
    }

    public enum UserStatus
    {
        Active,
        Inactive,
        Suspended,
        PendingVerification
    }
}
