/**
 * Production Environment Configuration
 */

export const environment = {
  production: true,
  // Backend is running locally, exposed via a Cloudflare quick tunnel — this URL
  // changes if the tunnel restarts. Update here and redeploy when that happens.
  apiUrl: 'https://metres-forever-messenger-troubleshooting.trycloudflare.com/api',
};
