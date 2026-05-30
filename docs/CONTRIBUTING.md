# Contributing / Git Workflow

This project follows **GitHub Flow** — `main` is always deployable, all work lives on short-lived branches, and every change goes through a PR.

## Branch naming

```
feature/short-description
fix/short-description
chore/short-description
docs/short-description
```

## Day-to-day workflow

```
main ──────────────────────────────────────────────► main
         │                                ▲
         └── feature/my-change ──────────┘
              (branch + develop + push)   (squash merge via PR)
```

1. **Branch** off `main`:
   ```bash
   git switch -c feature/my-change
   ```

2. **Develop locally** — see [Aspire Local Dev](ASPIRE_LOCAL_DEV.md) for startup.

3. **Push and open a PR** against `main` on GitHub.

4. **CI runs automatically** on the PR — build, unit tests, Aspire-driven integration tests, Bicep lint, SonarQube analysis (main branch only).

5. **Get a review** — at least one approval required.

6. **Merge when ready** — the branch must be up-to-date with `main` before merging. GitHub enforces squash merge: each PR lands as one commit on `main`.

## Required checks before merge

| Check | Enforced by |
| ----- | ----------- |
| CI pipeline green (build + unit tests + integration tests) | Azure Pipelines status check on GitHub |
| At least 1 reviewer approval | GitHub branch protection |
| Branch up-to-date with `main` | GitHub branch protection |
| Squash merge only (no merge commits) | GitHub repo settings |

> SonarQube analysis runs only on the `main` branch (Community Edition limitation — no branch analysis). PRs still get build and test feedback; SonarQube results appear after merge.

## What happens after merge to `main`

Merging a PR to `main` triggers `.azure-pipelines/main-ci-cd.yml` automatically when one of these paths changed: `src/`, `tests/`, `Dockerfile`, `infra/main/`, `Directory.Packages.props`, `MusicAlbumsApi.slnx`, or `.azure-pipelines/main-ci-cd.yml` itself.

```
Build ──► Test ──► Push image ──► (approval) ──► Deploy to dev
```

The **Deploy Application** stage targets the Azure DevOps `Development` environment, which has a manual approval gate. The pipeline pauses there and resumes once approved.

## Deploying to production

Production is always triggered manually — there is no automatic deployment to prod on merge.

1. Go to **Azure Pipelines → Music Albums API → Run pipeline**
2. Set `targetEnvironment = prod`
3. The same stages run (Build → Test → Push → Deploy), now targeting the `Production` environment gate

## Infrastructure operations

Infra changes (Bicep) are always manual, regardless of environment:

| Goal | How |
| ---- | --- |
| Preview infra changes | Run pipeline manually with `deployInfra = true` — runs What-If analysis, then pauses for approval before applying |
| Deploy/update infra | Same as above — approve the What-If stage to proceed to `DeployInfra` |
| Destroy an environment | Run pipeline manually with `destroyInfra = true` — all other stages are skipped, only `DestroyInfra` runs |

> Destroy is irreversible. Re-deploy from scratch with `deployInfra = true` after destroy.

See [CI/CD](CI_CD.md) for pipeline parameters, variable groups, and manual `az` commands as an alternative to the pipeline.
