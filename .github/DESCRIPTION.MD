# GitHub Metadata

This folder is reserved for GitHub-specific configuration for the MontanaMesh website repository, such as workflows, issue templates, or repository automation.

## Current State

`workflows/ci.yml` runs Website CI for pull requests and pushes to main. A GitHub-hosted runner runs the updater regression tests and builds the production Dockerfile with read-only repository permissions and no deployment credentials. Production deployment is manually controlled by the private master-control repository after advancing its pinned website commit.

## Documentation

Whenever GitHub automation or repository metadata changes in this folder, keep this service-local note current in the same change.
