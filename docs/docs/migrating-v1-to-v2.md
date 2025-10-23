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

#### ``IProcessFactory`` and ``ProcessFactory``
**Rationale**:  Removed due to exposing behaviour and functionality that is antithetical to the goals of CliInvoke. Safe process running and avoiding the pitfalls of the ``Process`` class are easily done with other invokers and ``IProcessFactory``'s interface methods made it easier to run into some of these pitfalls.

**Replacement**: 
* ``IProcessConfigurationFactory`` and ``ProcessConfigurationFactory`` for simple ``ProcessConfiguration`` creation.
* ``IProcessInvoker`` and ``ProcessInvoker`` for running ``ProcessConfiguration`` objects.

#### ``ICliCommandInvoker`` and ``CliCommandInvoker``
**Rationale**: 

**Replacement**: 
* ``IProcessInvoker`` and ``ProcessInvoker``


####  ``CliCommandConfiguration``

**Rationale**: 

**Replacement**:

### Method Signature Changes

## Detailed Migration Steps