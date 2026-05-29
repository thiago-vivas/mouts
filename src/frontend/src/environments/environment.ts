/**
 * API base URL. Adjust to match where the backend runs:
 *  - docker-compose: http://localhost:8080/api
 *  - local `dotnet run`: see src/backend/.../launchSettings.json (e.g. http://localhost:5000/api)
 */
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:8080/api',
};
