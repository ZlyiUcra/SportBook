# SportBook frontend

React SPA (Vite + TypeScript). See the repository root `README.md` for prerequisites and the full
local setup sequence.

## Architecture

Feature-Sliced Design layering under `src/`:

- `app/` - routes, layouts, providers.
- `pages/` - one folder per route (`ui/<Route>Page.tsx`).
- `features/` - one user action per slice (`ui` + `model` + `api`).
- `entities/` - domain data: types and read API calls.
- `shared/` - UI kit (`shadcn/ui`), the Axios instance, i18n setup, theme store, utilities.

## Common commands

```powershell
yarn install
yarn dev
yarn build
yarn test
yarn lint
```

The dev server expects the API at `http://localhost:5217/api` by default - see `.env.development`.
