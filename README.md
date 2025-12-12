# AI-Powered Code Review Assistant
![Build Status](https://github.com/your-org/ai-code-review-assistant/workflows/CI/badge.svg)
![Coverage](https://codecov.io/gh/your-org/ai-code-review-assistant/branch/main/graph/badge.svg)
![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-.NET%2010-purple.svg)
![Cloud](https://img.shields.io/badge/cloud-Azure-blue.svg)

A production-grade, enterprise-ready AI-powered code review assistant built with .NET 10 and Azure.
## Getting Started
### Prerequisites
- .NET 10.0 SDK
- SQL Server (LocalDB, Docker, or Azure SQL)
- Azure Subscription (for deployment)
### Running Locally
1. Clone the repository
2. Set up the database:
   ```bash
   cd src/WebApi/CodeReviewAssistant.WebApi
   dotnet ef database update
   ```
3. Run the application:
   ```bash
   dotnet run 
   ```
4. Open Open https://localhost:5001/swagger to access the Swagger UI

### Project Structure

```
| src/ - Source code
|   Core/ - Core domain and application logic
|   Infrastructure/ - External concerns
|   WebApi/ - Web API project
|   Worker/ - Background worker service
|   Shared/ - Shared code
```

### Development

#### Adding a New Feature
1. Create a feature branch
2. Add your changes
3. Add/update tests
4. Submit a pull request


## License
MIT    