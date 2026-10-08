# Grid sorting regression checks

From `MicroservicesEcosystem`, run:

```powershell
dotnet run --project tests/AccountingInventory.GridChecks -- --local
```

`--local` reads the API's appsettings connection string and requires a localhost
PostgreSQL server. Alternatively, set `GRID_CHECK_CONNECTION_STRING` and omit
`--local`.

The checks create a temporary schema and fixture data inside a transaction,
exercise ascending and descending sorting across pages for all 16 grid query
handlers, and check multi-column sorting and invalid-field fallback. The entire
transaction is rolled back, including the temporary schema.

Run frontend checks from `react-multitenant-saas`:

```powershell
node tests/gridSorting.cjs
```
