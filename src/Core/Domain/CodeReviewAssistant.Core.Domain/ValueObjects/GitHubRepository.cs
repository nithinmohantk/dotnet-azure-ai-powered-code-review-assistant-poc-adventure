using System;
using System.Text.RegularExpressions;

namespace CodeReviewAssistant.Core.Domain.ValueObjects
{
    public class GitHubRepository
    {
        public string Owner { get; private set; }
        public string Name { get; private set; }
        public string Url { get; private set; }
        public string CloneUrl { get; private set; }
        public string ApiUrl { get; private set; }

        private GitHubRepository(string owner, string name)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Url = $"https://github.com/{owner}/{name}";
            CloneUrl = $"https://github.com/{owner}/{name}.git";
            ApiUrl = $"https://api.github.com/repos/{owner}/{name}";
        }

        public static GitHubRepository FromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("GitHub URL cannot be empty", nameof(url));

            var patterns = new[]
            {
                @"https?://github\.com/([^/]+)/([^/]+)(?:\.git)?/?$",
                @"git@github\.com:([^/]+)/([^/]+)\.git$",
                @"https?://api\.github\.com/repos/([^/]+)/([^/]+)$"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(url, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var owner = match.Groups[1].Value;
                    var name = match.Groups[2].Value.TrimEnd('/');
                    return new GitHubRepository(owner, name);
                }
            }

            throw new ArgumentException("Invalid GitHub repository URL", nameof(url));
        }

        public static GitHubRepository Create(string owner, string name)
        {
            return new GitHubRepository(owner, name);
        }

        public override string ToString()
        {
            return $"{Owner}/{Name}";
        }

        public override bool Equals(object obj)
        {
            return obj is GitHubRepository other && Owner == other.Owner && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Owner, Name);
        }

        public static bool operator ==(GitHubRepository left, GitHubRepository right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GitHubRepository left, GitHubRepository right)
        {
            return !Equals(left, right);
        }
    }
}
