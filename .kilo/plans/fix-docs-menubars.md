# Plan: Fix missing menubars in Lunet docs

## Goal
Restore the missing left-hand navigation (menubars) for the "Guides" and "Migration Guides" sections of the documentation site.

## Analysis
In Lunet, when a menu item is marked as `folder: true` in a `menu.yml` file, the engine looks for a corresponding `menu.yml` file within that directory to populate the sidebar for pages in that section.

Currently, `site/docs/menu.yml` defines:
- `guides/readme.md` as a folder.
- `migration-guides/readme.md` as a folder.

However, neither `site/docs/guides/` nor `site/docs/migration-guides/` contains a `menu.yml` file, resulting in the missing sidebars.

## Steps

### 1. Create Guides Menu
Create `site/docs/guides/menu.yml` to define the navigation for the Guides section.
- Path: `site/docs/guides/menu.yml`
- Content should include links to:
    - `readme.md` (as the section overview)
    - `architecture.md`
    - `configuration.md`
    - `troubleshooting.md`

### 2. Create Migration Guides Menu
Create `site/docs/migration-guides/menu.yml` to define the navigation for the Migration Guides section.
- Path: `site/docs/migration-guides/menu.yml`
- Content should include links to:
    - `readme.md` (as the section overview)
    - `v1-to-v2/migration-v1-to-v2-Method-Signature-Changes.md`
    - `v1-to-v2/migration-v1-to-v2-Removed-Classes.md`
    - `v1-to-v2/migration-v1-to-v2-Removed-Methods.md`

## Validation
- Since the docs are generated and published via GitHub Actions (`.github/workflows/docs-publish.yml`), validation will require checking the rendered site at `lunet.io/docs` (or a local preview if available) after the changes are pushed.
- Verify that the "Guides" and "Migration Guides" pages now display a left-hand navigation menu with the specified links.
