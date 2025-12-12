# AI-Powered Code Review Assistant API Documentation

## Overview

The AI-Powered Code Review Assistant is a comprehensive .NET 10 application that leverages Azure AI services to provide automated code review capabilities. This API documentation covers all available endpoints, authentication, and usage patterns.

## Base URL

- **Development**: `https://localhost:8080`
- **Staging**: `https://staging-api.codereview.example.com`
- **Production**: `https://api.codereview.example.com`

## Authentication

The API uses Azure Active Directory (Azure AD) for authentication. All requests must include a valid JWT bearer token in the `Authorization` header.

```http
Authorization: Bearer <your-jwt-token>
```

### Getting a Token

1. Register your application in Azure AD
2. Use OAuth 2.0 client credentials flow or authorization code flow
3. Include the token in your requests

## API Endpoints

### Code Reviews

#### Create Code Review

```http
POST /api/codereviews
Content-Type: application/json
Authorization: Bearer <token>

{
  "title": "Review new authentication feature",
  "description": "Please review the new authentication implementation",
  "repositoryUrl": "https://github.com/organization/repository",
  "branchName": "feature/authentication",
  "commitHash": "abc123def456",
  "requestedBy": "john.doe@example.com",
  "priority": "High"
}
```

**Response:**
```json
{
  "success": true,
  "codeReviewId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "Code review created successfully"
}
```

#### Get Code Review by ID

```http
GET /api/codereviews/{id}
Authorization: Bearer <token>
```

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Review new authentication feature",
  "description": "Please review the new authentication implementation",
  "repositoryUrl": "https://github.com/organization/repository",
  "branchName": "feature/authentication",
  "commitHash": "abc123def456",
  "status": "InProgress",
  "priority": "High",
  "requestedBy": "john.doe@example.com",
  "createdAt": "2024-12-12T10:30:00Z",
  "updatedAt": "2024-12-12T11:15:00Z",
  "summary": "The authentication implementation looks good overall...",
  "totalIssues": 3,
  "criticalIssues": 1,
  "majorIssues": 1,
  "minorIssues": 1
}
```

#### Get Code Reviews by User

```http
GET /api/codereviews/user/{username}?page=1&pageSize=20&status=InProgress
Authorization: Bearer <token>
```

#### Get Code Reviews by Repository

```http
GET /api/codereviews/repository?repositoryUrl=https://github.com/org/repo&page=1&pageSize=20
Authorization: Bearer <token>
```

#### Search Code Reviews

```http
GET /api/codereviews/search?searchTerm=authentication&status=Completed&priority=High&page=1&pageSize=20&sortBy=CreatedAt&sortDescending=true
Authorization: Bearer <token>
```

#### Get Code Review Metrics

```http
GET /api/codereviews/{id}/metrics
Authorization: Bearer <token>
```

**Response:**
```json
{
  "codeReviewId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "totalFiles": 15,
  "totalLinesOfCode": 1250,
  "addedLines": 450,
  "deletedLines": 120,
  "modifiedLines": 680,
  "complexityMetrics": {
    "cyclomaticComplexity": 8.5,
    "cognitiveComplexity": 12.3,
    "maintainabilityIndex": 75.2
  },
  "qualityMetrics": {
    "codeQualityScore": 85.0,
    "testCoverage": 78.5,
    "duplicationPercentage": 3.2,
    "technicalDebtRatio": 0.15
  },
  "securityMetrics": {
    "securityScore": 92.0,
    "vulnerabilitiesFound": 1,
    "securityHotspots": 3
  }
}
```

### AI Analysis

#### Get AI Analyses for Code Review

```http
GET /api/codereviews/{id}/analyses
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "codeReviewId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "analysisType": "Security",
    "model": "gpt-4",
    "status": "Completed",
    "startedAt": "2024-12-12T10:35:00Z",
    "completedAt": "2024-12-12T10:42:00Z",
    "cost": 0.0025,
    "tokensUsed": 1250,
    "insights": [
      {
        "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
        "category": "Security",
        "severity": "High",
        "title": "Potential SQL Injection Vulnerability",
        "description": "The database query is constructed using string concatenation...",
        "recommendation": "Use parameterized queries or an ORM to prevent SQL injection",
        "filePath": "/src/Services/UserService.cs",
        "lineNumber": 45,
        "confidence": 0.95
      }
    ]
  }
]
```

### Webhooks

#### Process GitHub Webhook

```http
POST /api/webhooks/github
Content-Type: application/json
X-Hub-Signature-256: sha256=<signature>
X-GitHub-Event: pull_request

{
  "action": "opened",
  "number": 123,
  "repository": {
    "name": "repository",
    "owner": {
      "login": "organization"
    }
  },
  "pull_request": {
    "title": "Add new authentication feature",
    "body": "This PR adds OAuth 2.0 authentication...",
    "head": {
      "ref": "feature/authentication",
      "sha": "abc123def456"
    },
    "base": {
      "ref": "main",
      "sha": "def456abc123"
    }
  }
}
```

## Error Handling

The API uses standard HTTP status codes and returns detailed error information:

```json
{
  "error": {
    "code": "ValidationError",
    "message": "Invalid input parameters",
    "details": [
      {
        "field": "repositoryUrl",
        "message": "Invalid GitHub repository URL format"
      }
    ]
  },
  "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2024-12-12T10:30:00Z"
}
```

### Common Error Codes

- `400 Bad Request` - Invalid input parameters
- `401 Unauthorized` - Missing or invalid authentication token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Unexpected server error

## Rate Limiting

The API implements rate limiting to ensure fair usage:

- **Standard users**: 100 requests per minute
- **Premium users**: 500 requests per minute
- **Enterprise users**: 1000 requests per minute

Rate limit headers are included in all responses:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640123456
```

## SDKs and Client Libraries

### .NET SDK

```bash
dotnet add package CodeReviewAssistant.Client
```

```csharp
using CodeReviewAssistant.Client;

var client = new CodeReviewClient(new CodeReviewClientOptions
{
    BaseUrl = "https://api.codereview.example.com",
    ApiKey = "your-api-key"
});

var codeReview = await client.CreateCodeReviewAsync(new CreateCodeReviewRequest
{
    Title = "Review new feature",
    RepositoryUrl = "https://github.com/org/repo",
    BranchName = "feature/new-feature",
    CommitHash = "abc123def456",
    RequestedBy = "user@example.com",
    Priority = Priority.High
});
```

### Python SDK

```bash
pip install codereview-assistant-client
```

```python
from codereview_assistant import CodeReviewClient

client = CodeReviewClient(
    base_url="https://api.codereview.example.com",
    api_key="your-api-key"
)

code_review = client.create_code_review(
    title="Review new feature",
    repository_url="https://github.com/org/repo",
    branch_name="feature/new-feature",
    commit_hash="abc123def456",
    requested_by="user@example.com",
    priority="High"
)
```

## Support

- **Documentation**: https://docs.codereview.example.com
- **API Status**: https://status.codereview.example.com
- **Support Email**: support@codereview.example.com
- **GitHub Issues**: https://github.com/organization/codereview-assistant/issues

## Changelog

See [CHANGELOG.md](../../CHANGELOG.md) for detailed version history and API changes.
