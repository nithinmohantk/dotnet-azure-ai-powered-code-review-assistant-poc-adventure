using System;
using CodeReviewAssistant.Core.Domain.Common;

namespace CodeReviewAssistant.Core.Domain.Entities
{
    public class ReviewComment : AuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid CodeReviewId { get; private set; }
        public string Content { get; private set; }
        public string Author { get; private set; }
        public Guid? ParentCommentId { get; private set; }
        public bool IsEdited { get; private set; }
        public DateTime? EditedAt { get; private set; }
        public int Upvotes { get; private set; }
        public int Downvotes { get; private set; }

        protected ReviewComment()
        {
        }

        public ReviewComment(Guid codeReviewId, string content, string author, Guid? parentCommentId = null)
        {
            Id = Guid.NewGuid();
            CodeReviewId = codeReviewId;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Author = author ?? throw new ArgumentNullException(nameof(author));
            ParentCommentId = parentCommentId;
            IsEdited = false;
            Upvotes = 0;
            Downvotes = 0;
            Created = DateTime.UtcNow;
            CreatedBy = author;
        }

        public void EditContent(string newContent, string editedBy)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Content cannot be empty", nameof(newContent));

            Content = newContent;
            IsEdited = true;
            EditedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
            LastModifiedBy = editedBy;
        }

        public void Upvote()
        {
            Upvotes++;
            LastModified = DateTime.UtcNow;
        }

        public void Downvote()
        {
            Downvotes++;
            LastModified = DateTime.UtcNow;
        }

        public void RemoveUpvote()
        {
            if (Upvotes > 0)
            {
                Upvotes--;
                LastModified = DateTime.UtcNow;
            }
        }

        public void RemoveDownvote()
        {
            if (Downvotes > 0)
            {
                Downvotes--;
                LastModified = DateTime.UtcNow;
            }
        }
    }
}
