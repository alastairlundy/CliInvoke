# Automated API Reference Generation (Issue #340)

Summary

This document collects key details from Lunet's `api.dotnet` plugin to help automate .NET API reference generation for this repository.

Quick start (config.scriban)

```
with api.dotnet
  title = "MyProject API Reference"
  path = "/api"
  menu_name = "api"
  menu_title = "API Reference"
  config = "Release"
  properties = { TargetFramework: "net10.0" }
  projects = [
    { name: "CliInvoke", path: "../src/CliInvoke/CliInvoke.csproj" }
  ]
end
```

Key points

- `api.dotnet` scans one or more .csproj files and generates API pages under `path` (default `/api`).
- Generated pages: root (`api-dotnet`), namespace pages (`api-dotnet-namespace`), and member pages (`api-dotnet-member`).
- You can merge hand-authored Markdown into generated models by adding files under `<project>/apidocs/**/*.md` with a `uid: <API uid>` frontmatter.
- Configure `api.dotnet.properties` / per-project `properties` for MSBuild properties (e.g., `TargetFramework`).
- Use `api.dotnet.references` to include referenced assemblies (NuGet packages); XML docs attached to those assemblies will be used when available.
- External cross-site xrefs can be configured with `api.dotnet.external_apis`.

Templates & customization

- Default templates live under Lunet's shared layouts and includes. Copy/override these into `site/.lunet/layouts` or `site/.lunet/includes` to customize output.
- Generated menu tree is available as `site.menu.<menu_name>` and can be referenced from `menu.yml` using a `folder: true` entry.

Linking & xref

- Use `xref:Full.Type.Name` inline in Markdown or DocFX `<xref href="Full.Type.Name"></xref>` to link to generated API pages.
- Scriban helpers like `{{ "My.Namespace.Type" | xref }}` are available in templates.

Next steps (suggested)

1. Add an `api.dotnet` block to `site/config.scriban` referencing the repository csproj(s). Prefer enabling only in `dev`/docs builds initially.
2. Add `apidocs/` Markdown files under any project that needs richer namespace/type remarks.
3. Ensure CI docs job builds the projects (with XML docs enabled) before running Lunet so summaries/remarks are available.

References

Source: https://lunet.io/docs/plugins/api-dotnet/
