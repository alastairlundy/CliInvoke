# CliInvoke Context

This document provides a glossary and architectural mapping of the CliInvoke domain. It is intended to ensure consistency across the codebase and provide unambiguous definitions for agents and developers.

## Core Concepts

### Resource-Owning Type
A type within the library that manages unmanaged OS resources (such as pipes, file handles, or process threads) or contains sensitive data in memory (such as `SecureString`). Because these resources are not managed by the .NET Garbage Collector, they require explicit lifecycle management.

## Architectural Patterns

For detailed definitions, target audiences, and usage examples of the architectural patterns used in CliInvoke, refer to **[PATTERNS.md](PATTERNS.md)**.
