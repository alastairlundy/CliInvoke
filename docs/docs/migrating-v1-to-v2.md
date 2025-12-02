# CliInvoke v1 to v2 Migration Guide.

CliInvoke v2 contains a number of breaking changes from CliInvoke v1. This document aims to detail alternatives to removed interfaces and classes.

## v2 Overview
CliInvoke version 2, hereafter v2, is a redesign of CliInvoke that vastly simplifies the API surface, improves the ergonomic experience of using CliInvoke, and adds support for Process Exit Configuration and additional features.

Explicit support for .NET Standard 2.1 has been removed and is now provided through .NET Standard 2.0 Support

### Supported Target Frameworks
CliInvoke v2 supports the following target frameworks:
* .NET Standard 2.0
* .NET 8
* .NET 9

There is implicit support for .NET 10 but explicit support will come in a future update.

## Breaking Changes Summary

* [Removals](#removals)
* [Method Signature Changes](#method-signature-changes)


### Removals
See [Removed classes](/docs/docs/migration-guides/v1-to-v2/Removed-Classes.md) for all interfaces and classes removed in v2 and their replacements.

See [Removed methods](/docs/docs/migration-guides/v1-to-v2/Removed-Methods.md) for all methods removed in v2 and their replacements.

### Method Signature Changes
See [Method Signature Changes](/docs/docs/migration-guides/v1-to-v2/Method-Signature-Changes.md) for all method signature changes for retained methods from v1 to v2.


## Migration Steps
For CliInvoke v1 users not using the latest version of v1, update to the latest version of CliInvoke v1.

1. Ensure your project works and builds successfully with CliInvoke v1.
2. Create a new branch for updating to CliInvoke v2.
3. Replace ``ICliCommandInvoker`` usage with ``IProcessInvoker`` and ``CliCommandConfiguration`` usage with ``ProcessConfiguration``.
4. Replace usage of all other deprecated code with newer alternatives or replacements where available.
5. Update to CliInvoke v2.
6. Address breaking changes in method signatures and elsewhere.
7. Resolve all remaining errors.
8. Update your project's testing code as needed to work with v2.
8. Test that the project still works and builds successfully (this time with v2).
9. Merge into your project's main branch if successful.