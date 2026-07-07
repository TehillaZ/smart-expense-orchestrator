#!/usr/bin/env bash
#
# Pull a published ApprovalFlow image from GHCR and verify it starts locally.
# Usage:
#   ./scripts/verify-cd-image.sh ghcr.io/owner/repo/apigateway:sha-72073c5

set -euo pipefail

IMAGE_REF="${1:-}"
JWT_SIGNING_KEY="${JWT_SIGNING_KEY:-local-cd-verify-signing-key-min-32-chars}"
PORT="${PORT:-8080}"

if [ -z "${IMAGE_REF}" ]; then
  echo "Usage: $0 <image-ref>"
  echo "Example: $0 ghcr.io/tehillaz/smart-expense-orchestrator/apigateway:latest"
  exit 1
fi

if [[ "${IMAGE_REF}" == *@* ]] || [[ "${IMAGE_REF}" == *" "* ]]; then
  echo "Invalid image reference."
  exit 1
fi

container_id=""
cleanup() {
  if [ -n "${container_id}" ]; then
    docker rm -f "${container_id}" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

echo "Pulling ${IMAGE_REF}..."
docker pull "${IMAGE_REF}"

echo "Starting container..."
container_id="$(docker run -d \
  -p "${PORT}:8080" \
  -e "Jwt__SigningKey=${JWT_SIGNING_KEY}" \
  "${IMAGE_REF}")"

for attempt in $(seq 1 30); do
  if curl -fsS "http://localhost:${PORT}/health" >/dev/null; then
    echo "Health check passed for ${IMAGE_REF}."
    exit 0
  fi
  sleep 2
done

echo "Health check failed for ${IMAGE_REF}."
docker logs "${container_id}" || true
exit 1
