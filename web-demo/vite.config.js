export default {
  server: {
    host: '0.0.0.0',
    port: 5173,
    hmr: {
      host: undefined,
      clientPort: 443
    },
    cors: true,
    headers: {
      "X-Frame-Options": "ALLOWALL"
    },
    // allow all hosts for preview
    allowedHosts: true
  },
  preview: {
    host: '0.0.0.0',
    port: 5173,
    cors: true
  }
}
