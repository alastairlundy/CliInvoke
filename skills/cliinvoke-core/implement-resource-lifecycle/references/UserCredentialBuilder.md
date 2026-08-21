# UserCredentialBuilder Disposal (CliInvoke 2.x — current released API)

> On **CliInvoke 3.0** use `UserCredentialSpec` instead (see the `UserCredentialSpec` section in the skill body). This page documents the current 2.x builder API.

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
