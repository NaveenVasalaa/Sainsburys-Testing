# LSP E2E Test Suite Template

A ready-to-use E2E Test Suite template for .NET Core with Microservices Integration Platforms.

[![lsp-e2e-test-suite-template-nonprod](https://github.com/sainsburys-tech/lsp-common-project-templates/actions/workflows/lsp-e2e-tests-suite-template.yaml/badge.svg)](https://github.com/sainsburys-tech/lsp-common-project-templates/actions/workflows/lsp-e2e-tests-suite-template.yaml)

## Prerequisites

- Github Nuget Package Source
- .Net 8 SDK (or latest version of Visual Studio)

## Installation

To install this template run the following command in your desired command utility.

```
dotnet new install LSP.E2E.TestSuite
```

To update the template to latest version, run the below command.

```
dotnet new update
```

This will allow you to create new project template from anywhere.

## Creation

Clone the required github repository for the component or open the existing repository. Create a new feature branch from the master.

```
git checkout master
git pull
git checkout -b "feature-name"
```

Navigate to the e2etestsuite directory in the local folder.

```
cd e2etestsuite
```

Create new project with the template installed and pass the required testsuite name and the target lsp repository name.

```
dotnet new lsp-e2e-testsuite -n lsp-component-name --repositoryName "lsp-repository-name"
```

## Updates

- Create pipeline for the TestSuite
- Search the new solution for remarks with TODO and update all the places with required values for the new suite
- Update the test cases after the changes to make sure all test cases are passing