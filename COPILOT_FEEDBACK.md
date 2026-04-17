# CliInvoke API Usability & Adoption Analysis

**Document**: Comprehensive feedback on friction points, pain points, and adoption barriers  
**Date**: 2026-04-16  
**Focus**: API surface, discoverability, convenience, and developer experience

---

## Executive Summary

CliInvoke's **separation-of-concerns architecture is theoretically sound but pragmatically undermines adoption**. The core issue:

**You have two powerful abstractions (`ProcessConfiguration`, `ProcessTimeoutPolicy`) that look authoritative but feel cumbersome for 90% of real-world use cases.**

New users hit this friction immediately:
- Can't discover the simplest path to "run a command"
- When they find `ProcessConfiguration.Create()`, it still requires configuration + invoker ceremony
- Setting a timeout requires nesting 3+ objects for something that should be one parameter
- Documentation emphasizes the hard way (builders), not the fast way (static helpers)

**The real problem**: ProcessConfiguration and ProcessTimeoutPolicy are *overdesigned for the 90% case and underserved for discoverability*. They need a frictionless entry point that doesn't feel like you're cheating the API.

**What's needed**: A **static helper class** (`CliCommand` or `ProcessRunner`) that provides the fast path. This:
- ✅ Makes simple tasks trivial (1-2 liners)
- ✅ Doesn't undermine ProcessConfiguration/ProcessTimeoutPolicy—it delegates to them
- ✅ Guides users toward the rich API once they outgrow simple cases
- ✅ Avoids polluting `IProcessInvoker` interface with convenience overloads (which I incorrectly recommended)

---

## Detailed Pain Points


### 5. **ProcessConfiguration Conflates Data Model and Factory Logic** ⚠️ Medium Priority

**Problem**: ProcessConfiguration is trying to be two things:

```csharp
// As a data model: ✅ makes sense
public class ProcessConfiguration : IEquatable<ProcessConfiguration>, IDisposable
{
    public string TargetFilePath { get; set; }
    public string Arguments { get; protected set; }
    public bool WindowCreation { get; }
    // ... more properties
}

// But also has static factory:
public static ProcessConfiguration Create(string targetFilePath, params string[] arguments)
    => ...; // This feels wrong on a data class
```

**Why it's confusing**:
- Users don't expect factories on data classes
- Violates single responsibility: it's both a DTO and a factory
- Buries the factory method in Intellisense under a class full of properties

**Better design**:
- Keep ProcessConfiguration as a pure data model
- Move `.Create()` to a dedicated static factory or extension helper
- Make it visible as a top-level concept (not a property of the model)

