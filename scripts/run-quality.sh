#!/usr/bin/env bash
# Builds, tests and packs Plan9.Utf. With --mutate, runs Stryker over the library too, and fails on
# any mutant left alive, uncovered or timed out that quality/stryker-accepted.json does not explain.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet build Plan9Utf.slnx -c Release -warnaserror
dotnet test Plan9Utf.slnx -c Release --no-build
dotnet pack Plan9.Utf/Plan9.Utf.csproj -c Release --no-build -warnaserror -o .artifacts/package

if [[ "${1:-}" == "--mutate" ]]; then
  (cd Plan9.Utf.Tests && dotnet stryker --config-file ../stryker-config.json --reporter json --reporter progress --output ../.artifacts/stryker --skip-version-check --break-on-initial-test-failure --verbosity error) || true
  report=.artifacts/stryker/reports/mutation-report.json
  unexplained=$(jq -r --slurpfile accepted quality/stryker-accepted.json '
    .files | to_entries[] | .key as $path | (.value.source | split("\n")) as $lines
    | .value.mutants[] | select(.status == "Survived" or .status == "Timeout" or .status == "NoCoverage")
    | { status, file: ($path | split("/") | last), mutator: .mutatorName, line: ($lines[.location.start.line - 1] | gsub("^\\s+|\\s+$"; "")) } as $m
    | select([$accepted[0][] | select(.status == $m.status and .file == $m.file and .mutator == $m.mutator and .line == $m.line)] | length == 0)
    | "\($m.status) \($m.file): \($m.mutator) in \($m.line)"' "$report")
  for status in Killed Survived Timeout NoCoverage; do
    printf '%s: %s\n' "$status" "$(jq "[.files[].mutants[] | select(.status == \"$status\")] | length" "$report")"
  done

  if [[ -n "$unexplained" ]]; then
    echo "Mutants not explained by quality/stryker-accepted.json:"
    echo "$unexplained"
    exit 1
  fi
fi
