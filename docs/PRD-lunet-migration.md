# PRD: Migrate Documentation Site from DocFX to Lunet

## Problem Statement

The current documentation site powered by DocFX is being replaced to provide a better authoring experience, a more modern visual aesthetic, and an opportunity to reorganize the information architecture for better discoverability.

## Solution

Replace the existing DocFX documentation infrastructure with Lunet (lunet.io), a Markdown-centric static site generator. All conceptual guides and manual documentation will be migrated to Markdown, while API references will be automatically generated from the C# source code via a Lunet plugin. The site will be deployed as a static site via GitHub Actions.

## User Stories

1. As a library user, I want a modern, responsive documentation site, so that I can easily read guides on different devices.
2. As a developer, I want the API reference to be automatically updated from the source code, so that the documentation never drifts from the actual implementation.
3. As a new user, I want a reorganized information architecture, so that I can find the "Getting Started" and "Migration" guides more intuitively.
4. As a contributor, I want to write documentation in simple Markdown, so that I can contribute updates without needing to learn the complex DocFX configuration.
5. As a user, I want to be able to search the documentation efficiently, so that I can quickly find specific classes or methods.
6. As a developer, I want the site to build and deploy automatically on every push to the main branch, so that the latest changes are always live.
7. As a user, I want a consistent visual identity (logo, colors), so that the documentation feels like an official part of the CliInvoke project.
8. As a user, I want to see the most up-to-date documentation by default, so that I don't accidentally follow instructions for an obsolete version.
9. As a user, I want a clear path to legacy documentation (e.g., v1 to v2 migration), so that I can upgrade my existing project.
10. As a developer, I want to maintain a minimal set of configuration files (`config.scriban`), so that the build process remains transparent and easy to maintain.

## Implementation Decisions

### Modules and Components
- **Lunet Build Engine:** The core static site generator responsible for transforming Markdown and templates into HTML.
- **C# API Reference Plugin:** A specialized Lunet plugin that parses the CliInvoke projects to generate the API documentation pages.
- **CI/CD Pipeline (GitHub Actions):** A workflow responsible for installing the Lunet toolchain, executing the build, and publishing the output to the hosting provider.
- **Scriban Configuration:** A central configuration file (`config.scriban`) used to define site metadata, branding, and global variables.
- **Information Architecture (IA) Map:** A new structure for the navigation menu that reorganizes conceptual guides and the API reference.

### Technical Decisions
- **Content Format:** All non-API documentation will be stored as Markdown files.
- **API Generation:** The API reference will not be manually written; it will be derived from source code using the Lunet plugin.
- **Deployment Strategy:** Static site deployment targeting the `main` branch.
- **Versioning:** "Latest by Default" approach. Legacy documentation will be stored as static Markdown within the site structure rather than through separate versioned builds.
- **Styling:** A tiered approach starting with the default Lunet theme, customized via `config.scriban` for core branding, with custom CSS/Templates deferred to a future phase.

## Testing Decisions

### Validation Criteria
- **Link Integrity:** All internal links within the reorganized IA must resolve correctly (no 404s).
- **Build Success:** The GitHub Action must successfully compile the site and deploy it without manual intervention.
- **API Coverage:** The generated API reference must include all public classes and methods defined in the `src/` directory.
- **Visual Regression:** The site must render correctly across major browsers (Chrome, Firefox, Safari).

### Testing Approach
- **Manual Audit:** A comprehensive walk-through of the la a new IA map to verify discoverability.
- **CI Validation:** The GitHub Action itself serves as the primary integration test for the build process.

## Out of Scope

- Support for multiple concurrent versioned documentation sub-sites (e.g., `/v1/`, `/v2/` as separate build artifacts).
- Extensive custom CSS/JS framework development (deferred to future iterations).
- Migration of non-Markdown content formats other than the source code API.

## Further Notes

The reorganization of the Information Architecture should be informed by an audit of the current `docs/menu.yml` and actual user navigation patterns if available.
