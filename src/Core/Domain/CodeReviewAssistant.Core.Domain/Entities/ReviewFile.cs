using System;
using CodeReviewAssistant.Core.Domain.Common;

namespace CodeReviewAssistant.Core.Domain.Entities
{
    public class ReviewFile : AuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid CodeReviewId { get; private set; }
        public string FilePath { get; private set; }
        public string Content { get; private set; }
        public string FileType { get; private set; }
        public long SizeInBytes { get; private set; }
        public int LinesOfCode { get; private set; }
        public string Language { get; private set; }
        public bool IsBinary { get; private set; }
        public string FileHash { get; private set; }
        public DateTime? LastModifiedAt { get; private set; }

        protected ReviewFile()
        {
        }

        public ReviewFile(Guid codeReviewId, string filePath, string content, string fileType)
        {
            Id = Guid.NewGuid();
            CodeReviewId = codeReviewId;
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            FileType = fileType ?? throw new ArgumentNullException(nameof(fileType));
            
            SetContent(content);
            DetermineLanguage(filePath, fileType);
            Created = DateTime.UtcNow;
            CreatedBy = "system";
        }

        private void SetContent(string content)
        {
            Content = content ?? string.Empty;
            SizeInBytes = System.Text.Encoding.UTF8.GetByteCount(Content);
            LinesOfCode = string.IsNullOrEmpty(Content) ? 0 : Content.Split('\n').Length;
            IsBinary = IsBinaryFile(Content);
            FileHash = ComputeFileHash(Content);
        }

        private void DetermineLanguage(string filePath, string fileType)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            Language = extension switch
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
                ".sh" => "Shell",
                ".sql" => "SQL",
                ".html" => "HTML",
                ".css" => "CSS",
                ".scss" => "SCSS",
                ".less" => "Less",
                ".json" => "JSON",
                ".xml" => "XML",
                ".yaml" => "YAML",
                ".yml" => "YAML",
                ".md" => "Markdown",
                ".txt" => "Text",
                _ => "Unknown"
            };
        }

        private static bool IsBinaryFile(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            // Check for null bytes in first 1000 characters
            var checkLength = Math.Min(1000, content.Length);
            for (int i = 0; i < checkLength; i++)
            {
                if (content[i] == '\0')
                    return true;
            }

            return false;
        }

        private static string ComputeFileHash(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public void UpdateContent(string newContent, DateTime? lastModifiedAt = null)
        {
            SetContent(newContent);
            LastModifiedAt = lastModifiedAt;
            LastModified = DateTime.UtcNow;
            LastModifiedBy = "system";
        }

        public void UpdateLanguage(string newLanguage)
        {
            Language = newLanguage ?? throw new ArgumentNullException(nameof(newLanguage));
            LastModified = DateTime.UtcNow;
            LastModifiedBy = "system";
        }
    }
}
