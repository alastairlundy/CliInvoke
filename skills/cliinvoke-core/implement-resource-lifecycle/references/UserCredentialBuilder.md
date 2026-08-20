# UserCredentialBuilder Disposal

`UserCredentialBuilder` holds a `SecureString` sensitive password while building a credential.

### Recommended Pattern: `using` statement

```csharp
UserCredential credential;
using (var builder = new UserCredentialBuilder())
{
    credential = builder.SetUsername("user").SetPassword(securePassword).Build();
}
// builder disposed here; now dispose the resulting credential as well
using (credential)
{
    // Use credential...
}
```
