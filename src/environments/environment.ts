/**
 * Development Environment Configuration
 */

export const environment = {
  production: false,
  // 7240 is the backend's HTTPS port (see launchSettings.json);
  // plain `dotnet run` uses the http profile on http://localhost:5249
  apiUrl: "https://localhost:7240/api",
};
