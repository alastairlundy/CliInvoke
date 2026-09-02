# Good First Issue Guidelines

This document outlines the criteria for labeling an issue as `good first issue` in the CliInvoke repository. The goal is to provide welcoming, low-friction entry points for new contributors that help them learn the codebase without feeling overwhelmed.

## Criteria for a Good First Issue

An issue should be marked as a `good first issue` if it meets most of the following criteria:

### 1. Atomic and Focused
The task is a single, well-defined change. It should not require:
- Modifying multiple core components.
- Changing the public API surface (unless it's a trivial addition).
- Massive refactoring of existing logic.

### 2. Clearly Defined Goal
The "Definition of Done" must be explicit. A contributor should be able to read the issue and know exactly what success looks like without needing extensive back-and-forth communication.

### 3. Low Architectural Risk
The change is isolated. It should be unlikely to introduce regressions in critical paths or require deep knowledge of the entire system's internal state or complex design patterns.

### 4. Easy to Verify
The change can be verified quickly. Ideally:
- It can be proven with a new TUnit test.
- There is a provided "repro" case (for bugs).
- The verification steps are simple and documented in the issue.

### 5. Low Barrier to Entry
It does not require specialized environment setup, third-party tools, or platform-specific configurations beyond the standard .NET 10 SDK and the project's basic build instructions.

### 6. Educational Value
Completing the task introduces the contributor to a key part of the codebase. For example:
- Adding a test helps them learn the test framework and project structure.
- Implementing a helper in `Extensions` shows them how the library is extended.
- Fixing a bug in `Core` introduces them to the basic models.

---

## Examples

### ✅ Good First Issues
- **Documentation:** Fixing typos, improving clarity in the README, or adding a missing example to the docs.
- **Testing:** Adding edge-case tests to an existing suite to increase coverage.
- **Small Features:** Implementing a requested helper method in `CliInvoke.Extensions`.
- **Validation:** Adding a simple null or range check to a configuration model.
- **Bug Fixes:** Fixing a bug that is easily reproducible with a provided failing test case.

### ❌ NOT Good First Issues
- **Architectural Changes:** "Refactor the `ProcessInvoker` to use a different concurrency model."
- **Complex Features:** "Implement a new platform specialization from scratch."
- **Optimization:** "Optimize the overall memory allocation of the library."
- **Deep Bugs:** "Fix an intermittent race condition in the asynchronous I/O pipeline."
