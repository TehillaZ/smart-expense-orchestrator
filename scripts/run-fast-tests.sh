#!/usr/bin/env bash
# Fast local test layers for N6 (excludes Journey).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
dotnet test "$ROOT/backend/ApprovalFlow.sln" \
  --configuration Release \
  --filter "Category!=Journey" \
  --verbosity minimal
