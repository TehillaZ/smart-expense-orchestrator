/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_GATEWAY_URL?: string
  readonly VITE_NOTIFICATION_SERVICE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
