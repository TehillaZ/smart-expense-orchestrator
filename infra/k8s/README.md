# Kubernetes manifests (B3 bonus)

Deploys the same system as `infra/docker-compose.yml`, to a local Kubernetes cluster,
using Dapr's native Kubernetes mode instead of the `network_mode: "service:X"` sidecar
trick Docker Compose needed. Dapr injects a sidecar into any pod annotated
`dapr.io/enabled: "true"` automatically — no sidecar containers are defined by hand here.

## Prerequisites

- A local cluster: Docker Desktop's built-in Kubernetes (Settings → Kubernetes → Enable
  Kubernetes), or `minikube start`, or `kind create cluster`.
- [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/), then install the
  Dapr control plane onto the cluster once: `dapr init -k`
- Images built locally and visible to the cluster. With Docker Desktop's Kubernetes,
  images already built via `docker compose -f infra/docker-compose.yml build` are
  immediately usable (same image store) — no registry push needed. For minikube/kind,
  load them explicitly, e.g. `minikube image load approvalservice:latest` per image, or
  build directly against the cluster's own daemon (`eval $(minikube docker-env)` first).

## Deploy

```powershell
# 1. Secrets — copy the template and fill in real values first
cp infra/k8s/02-secret.example.yaml infra/k8s/02-secret.yaml
# edit infra/k8s/02-secret.yaml — Jwt signing key, and the LLM provider keys in the
# embedded secrets.json block (Stub needs no real key at all)

# 2. Apply everything
kubectl apply -f infra/k8s/00-namespace.yaml
kubectl apply -f infra/k8s/01-configmap.yaml
kubectl apply -f infra/k8s/02-secret.yaml
kubectl apply -f infra/k8s/03-dapr-components.yaml
kubectl apply -f infra/k8s/10-redis.yaml
kubectl apply -f infra/k8s/20-intakeservice.yaml
kubectl apply -f infra/k8s/21-approvalservice.yaml
kubectl apply -f infra/k8s/22-paymentservice.yaml
kubectl apply -f infra/k8s/23-notificationservice.yaml
kubectl apply -f infra/k8s/30-apigateway.yaml
kubectl apply -f infra/k8s/40-frontend.yaml

# 3. Watch it come up
kubectl get pods -n approvalflow -w
```

Each app pod should show `2/2` containers ready — your service plus its Dapr sidecar,
injected automatically. `apigateway` and `redis` show `1/1` (no sidecar).

## Access it

```powershell
# UI
http://localhost:30173

# API Gateway directly (Scalar docs, curl, etc.)
http://localhost:30080/scalar
```

(NodePorts are fixed at 30080/30173 in the manifests above; adjust if those are taken.)

## Verify

```powershell
kubectl get pods -n approvalflow
kubectl logs -n approvalflow deployment/approvalservice -c approvalservice
kubectl logs -n approvalflow deployment/approvalservice -c daprd   # the injected sidecar
```

## Differences from Docker Compose worth knowing

- **No `network_mode: "service:X"` sidecar trick.** Kubernetes pods share one network
  namespace by design (every container in a pod already shares `localhost`), so Dapr's
  Kubernetes injector just adds the sidecar as a second container in the same pod — the
  workaround Compose needed doesn't exist as a problem here.
- **Secret store stays `secretstores.local.file`, not the "more idiomatic"
  `secretstores.kubernetes`.** The app queries secrets by colon-path
  (`"LlmProvider:ApiKey"`), which only the local-file store's nested-key support handles;
  Dapr's native Kubernetes secret store treats the whole query as a K8s object name,
  which can't contain colons. Solved by mounting a Secret-backed volume into the sidecar
  via `dapr.io/volume-mounts-readonly`, so the file-based component works unchanged.
- **No `--scale` limitation.** This was the actual reason Docker Compose couldn't scale
  cleanly — Kubernetes gives every replica its own pod, and therefore its own sidecar,
  automatically. `kubectl scale deployment/approvalservice --replicas=3 -n approvalflow`
  just works.
