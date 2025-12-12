// src/SharedKernel/CodeReviewAssistant.SharedKernel/Common/BaseEntity.cs
using System;
namespace CodeReviewAssistant.SharedKernel.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ModifiedAt { get; protected set; }
    }
}
