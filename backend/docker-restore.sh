#!/usr/bin/env bash
# Retries NuGet restore to tolerate transient Docker Desktop DNS failures (NU1301).
set -euo pipefail

# BuildKit's embedded DNS (127.0.0.11) often cannot resolve api.nuget.org on Docker Desktop.
if [ -w /etc/resolv.conf ]; then
  printf 'nameserver 8.8.8.8\nnameserver 1.1.1.1\n' > /etc/resolv.conf || true
fi

project="${1:?project path required}"
max_attempts="${2:-6}"

for attempt in $(seq 1 "${max_attempts}"); do
  if dotnet restore "${project}"; then
    exit 0
  fi

  if [ "${attempt}" -eq "${max_attempts}" ]; then
    echo "dotnet restore failed after ${max_attempts} attempts for ${project}" >&2
    exit 1
  fi

  delay=$((attempt * 5))
  echo "dotnet restore failed (attempt ${attempt}/${max_attempts}); retrying in ${delay}s..." >&2
  sleep "${delay}"
done
