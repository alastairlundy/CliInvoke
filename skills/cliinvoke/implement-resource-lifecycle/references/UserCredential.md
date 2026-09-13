# UserCredential Disposal

`UserCredential` contains a `SecureString` for passwords which must be cleared from memory.

### Recommended Pattern: `using` statement

```csharp
using var credential = new UserCredential("domain", "user", securePassword, false);
// use credential...
// disposed here
```