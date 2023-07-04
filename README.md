# Rowbot

Rowbot is a data pipeline framework for the .NET developer. It provides a simple, fluent api to extract, transform, and load data.

Rowbot includes a builder that ensures pipelines are authored correctly and consistently, and a runner that is responsible for executing them in the correct order. The framework encourages the creation of many small data pipelines which then become the building blocks for more complicated ones. 

Rowbot is designed to be extensible; custom-built components plug into the pipeline builder exactly the same way as built-in components do. Extensions like custom data source connectors are simple to build and integrate into the api.

> Rowbot was built as an educational tool for interested C# developers. It is not intended to be a competitor to full-featured data pipeline orchestration frameworks.

## Get Started
This project includes two types of documentation. Each has a different purpose and is designed to provide a different understanding of Rowbot.

### User Documentation
The user documentation provides how-to guides and code examples to get started building a project on top of the Rowbot framework.

[User -> Get Started](docs/User/Get%20Started.md)

### Contributor Documentation
The contributor documentation provides explanations of the major abstractions that power Rowbot. It attempts to contextualise design decisions and describe the pros and cons of the approach taken. Contributor documentation is intentionally not reference documentatation. It is designed to be simple and concise so that a contributor can quickly build an understanding of Rowbot's internals.

[Contributor -> Get Started](docs/Contributor/Get%20Started.md)
