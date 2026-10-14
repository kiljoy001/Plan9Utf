#!/usr/bin/env bash
# Builds and tests Plan9.Utf; with --mutate, runs Stryker over the library too.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet build Plan9Utf.slnx -c Release -warnaserror
dotnet test Plan9Utf.slnx -c Release --no-build

if [[ "${1:-}" == "--mutate" ]]; then
  (cd Plan9.Utf.Tests && dotnet stryker --config-file ../stryker-config.json --reporter json --reporter progress --output ../.artifacts/stryker --skip-version-check --break-on-initial-test-failure --verbosity error)
fi
