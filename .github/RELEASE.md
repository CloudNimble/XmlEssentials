# Release pipeline

GitHub Actions ships NuGet packages the same way EasyAF, ClaudeEssentials, and McpEssentials do.

| Workflow | When | Version | NuGet | GitHub Release | Increments |
|---|---|---|---|---|---|
| `pr-validation.yml` | PRs into `main` / `dev` | `1.0.0-CI-<utc>` | Artifact only | No | No |
| `build-and-deploy.yml` | Push to `main` | `VERSION_MAJOR.MINOR.PATCH` (first 1.0.0) | Yes | Yes (`v1.0.0`) | `VERSION_PATCH` += 1 |
| `build-and-deploy.yml` | Push to `dev` | `DEV_VERSION_*-preview.N` | Yes | No | `DEV_VERSION_PREVIEW_SUFFIX` += 1 |
| `publish-ci-build.yml` | Manual | The CI packages from a prior run | Yes | No | No |

`main` is the 1.0 line. `dev` is the next preview line. A push to `main` that succeeds is a 1.0.x NuGet publish plus a GitHub release.

PR validation and the deploy build also run `coverage-gate.cs` (100% line and branch on `CloudNimble.XmlEssentials`) and Native AOT (`XmlAotPublishTests` on net11.0 plus the `xmlessentials` CLI sample).

## Repository variables

Set these under **Settings → Secrets and variables → Actions → Variables**. The first 1.0.0 release uses the values below.

| Variable | 1.0 value | Used on | After a successful deploy |
|---|---|---|---|
| `VERSION_MAJOR` | `1` | `main` | Leave at `1` until a breaking major |
| `VERSION_MINOR` | `0` | `main` | Bump by hand when you start 1.1.0 |
| `VERSION_PATCH` | `0` | `main` | Pipeline sets this to `1` after 1.0.0, then `2`, … |
| `DEV_VERSION_MAJOR` | `1` | `dev`, PRs into `dev` | Leave at `1` until the next major |
| `DEV_VERSION_MINOR` | `0` | `dev`, PRs into `dev` | Bump by hand when `dev` moves to 1.1 |
| `DEV_VERSION_PATCH` | `0` | `dev` | Leave at `0` unless you want `1.1.1-preview.N` |
| `DEV_VERSION_PREVIEW_SUFFIX` | `0` | `dev` | Pipeline sets this to `1` after the first `1.0.0-preview.1`, then `2`, … |

How the numbers actually increment:

1. Keep `VERSION_PATCH` at `0` until you want the first `main` push to publish **1.0.0**.
2. That deploy then runs `gh variable set VERSION_PATCH --body 1`. The next `main` push is **1.0.1**.
3. Keep `DEV_VERSION_PREVIEW_SUFFIX` at `0`. The first `dev` push publishes **1.0.0-preview.1** and sets the suffix to `1`.
4. To start **1.1.0** on `main`, set `VERSION_MINOR=1` and `VERSION_PATCH=0` by hand (and usually `DEV_VERSION_MINOR=1`, `DEV_VERSION_PREVIEW_SUFFIX=0`).

PRs never increment anything. They stamp `1.0.0-CI-yyyyMMdd-HHmmss` from the target branch’s major/minor.

## Secrets

Set these under **Settings → Secrets and variables → Actions → Secrets**.

| Secret | What it is |
|---|---|
| `NUGET_USER` | Your NuGet.org username. Used by `nuget/login` Trusted Publishing (`id-token: write`). |
| `REPO_ACCESS_TOKEN` | A PAT (or fine-grained token) with permission to **set repository Actions variables**. The default `GITHUB_TOKEN` cannot update `VERSION_PATCH` / `DEV_VERSION_PREVIEW_SUFFIX`. |

Without `REPO_ACCESS_TOKEN`, packages still publish; the patch/preview numbers stay put and the next run would try to republish the same version (`--skip-duplicate` then no-ops).

## GitHub environment

Create a repository environment named **`production`**. The deploy and “publish CI build” jobs target it so you can add required reviewers later. An empty environment is enough for the first 1.0.0.

## NuGet.org Trusted Publishing

On [nuget.org](https://www.nuget.org) → your account → **Trusted Publishing**, add GitHub Actions for this repository. Package id to register before the first push: `XmlEssentials`.

Workflow: `.github/workflows/build-and-deploy.yml`  
Repository: `CloudNimble/XmlEssentials`

## Manual 1.0.0

1. Variables and secrets above are in place.
2. `production` environment exists.
3. Trusted Publishing is registered for `XmlEssentials`.
4. Merge to `main` (or run **Build and Deploy to NuGet** on `main` with `deploy_to_nuget=true`).
5. Confirm NuGet shows 1.0.0, GitHub has `v1.0.0`, and `VERSION_PATCH` is now `1`.
