# Contributing to AI-Powered Code Review Assistant

Thank you for your interest in contributing to our AI-Powered Code Review Assistant! This document provides guidelines and information for contributors.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Contribution Guidelines](#contribution-guidelines)
- [Code of Conduct](#code-of-conduct)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)
- [Release Process](#release-process)

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Docker Desktop
- Azure Subscription (for development resources)
- Git
- Visual Studio 2022 or VS Code

### Development Setup

1. Fork the repository
2. Clone your fork locally
3. Create a feature branch
4. Set up your development environment

## Contribution Guidelines

### Code Style

- Follow Microsoft C# coding conventions
- Use EditorConfig for consistent formatting
- Write meaningful commit messages
- Keep pull requests focused and atomic

### Branching Strategy

- `main`: Production-ready code
- `develop`: Integration branch
- `feature/*`: New features
- `hotfix/*`: Critical fixes

## Pull Request Process

1. Update README.md with details of changes
2. Update CHANGELOG.md
3. Ensure all tests pass
4. Request code review
5. Address feedback promptly

## Testing Guidelines

- Unit tests: 80%+ coverage required
- Integration tests for external services
- E2E tests for critical user flows
- Performance tests for API endpoints

## Documentation

- Update API documentation
- Add code comments for complex logic
- Update architecture diagrams if needed
- Create/update runbooks for operations

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
