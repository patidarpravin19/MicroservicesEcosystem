# Accounting and inventory product readiness

This review targets the existing INR, CGST/SGST, serialized-product application.
It does not certify a complete accounting product or statutory compliance.

## Implemented safeguards

- Tenant business endpoints require a token identifying the same tenant as the header,
  an active registry record, and an active user in that tenant. Refresh and password
  recovery check current tenant/user status instead of trusting stale token state.
- Tenant listing, suspension and reactivation require the operator `Tenants.Manage`
  permission. Ordinary tenant registration/login does not issue that permission.
  Configure a separately secured operator identity before using those operations.
- Core commands run in a serializable database transaction. Stock/balance checks,
  source writes, journals and audit entries commit together. Concurrent conflicts
  return HTTP 409 with no partial writes; the client must refresh before retrying.
  Login, refresh and control-plane provisioning are excluded because they switch schemas.
- Staff require `catalog.manage`, `purchases.manage`, `sales.manage`, `inventory.manage`
  or `accounting.manage` for the corresponding writes. Existing specialized permissions
  remain required for approvals, dimensions, documents, assets and budgets. The earliest
  tenant user retains the existing owner policy and can grant permissions in Accounting
  Controls. Existing staff must receive grants before continuing these writes.
- Anonymous user registration is restricted to an empty tenant. Once populated,
  additional registration requires the owner. Username/email comparisons are normalized;
  registration now stores email and mobile in the correct fields.
- Purchases validate active references within the tenant and the model's brand/type.
  Invoice units share dates and terms; case variants of bill numbers form one invoice; paid bills cannot gain or change units.
  Serial collision checks are case insensitive across both serial fields.
- Sold, written-off, paid or ledger-posted products cannot be edited. Referenced
  catalog records cannot be deleted, including references in archived source rows.
  Models used by inventory cannot change their brand/type association.
- Sales use recorded inventory cost and invoice year for numbering. Sales cannot
  precede purchases, and payments/receipts cannot precede invoices. Invoice price/discount and receipt precision
  are validated before posting. Sales sorting uses the tax-inclusive invoice total.
- Manual journals cannot impersonate automatic sources. Manual reversals preserve
  dimensions, cannot precede the original entry, and cannot be duplicated. Automatic
  source journals cannot be reversed independently of their operational records.
- Year-end closing cannot run twice. The balance sheet includes nominal closing
  lines, preventing closed earnings from being added twice; operating P&L excludes them.
- Tenant migrations repair invoice/ledger columns and unambiguously swapped user contacts.

## Remaining release requirements

These are substantive missing or incomplete workflows, not features delivered by this patch.

| Area | Required follow-up |
| --- | --- |
| Returns and corrections | Source-linked sales/purchase returns, credit/debit notes, cancellation, refund allocation, tax reversal and stock disposition in one transaction. Current immutable invoice safeguards deliberately reject unsafe direct corrections. |
| Invoice model | Multi-line invoices, invoice snapshots for historical customer/vendor/product details, and tenant fiscal-year numbering settings. Current sales represent individual serialized units. |
| Onboarding and staff | Verified owner bootstrap/invitation flow, persistent owner identity and recovery, staff administration UI, permission-aware navigation and configurable read permissions. Knowing an empty tenant's identifier remains insufficient proof of ownership; public onboarding needs verification before launch. |
| Tax scope | Confirm the jurisdiction and required tax types, business/product identifiers, export formats and external reporting integrations. Current CGST/SGST reports alone are not a full tax product. |
| Inventory scope | Warehouses/locations, transfers, counts and count reconciliation, units of measure, reorder thresholds and nonserialized quantities where required. |
| Subledger reconciliation | Vendor/customer statements, advances, payment allocation/refunds, bank import deduplication and opening subledger/stock reconciliation against the GL. |
| Operations | Production secret configuration, backup and tested restore, monitoring, tenant export/offboarding, migration rollback rehearsal, capacity testing and incident recovery. |
| Validation | API-level tenant-isolation tests, full UI purchase-to-payment/sale-to-receipt tests, refund/return tests as implemented, accountancy review and business acceptance testing. |

## Verification and deployment

Run from `MicroservicesEcosystem`:

```powershell
dotnet run --project tests/AccountingInventory.GridChecks -m:1 -- --local
dotnet run --project tests/AccountingInventory.SchemaChecks -- --local
dotnet build src/AccountingInventory/AccountingInventory.Api --no-restore -m:1
```

The grid/integrity suite uses rolled-back temporary fixtures; its concurrency probe
uses a separate scratch schema removed after testing. Production tenant data is not
used as test fixtures. If a running API locks DLLs, use a separate `OutDir` for build verification.

Run `npm run build` and `node tests/gridSorting.cjs` from `react-multitenant-saas`.
Rebuild and restart the API to apply pending migrations to active tenant schemas.
Suspended tenants receive pending migrations when reactivated through provisioning.
Take a backup and rehearse the upgrade on a database copy before production deployment.
Review ambiguous historic user contacts and invoice/subledger differences rather than
rewriting financial history automatically. The changed authorization requires staff
permission grants and an operator identity for tenant administration.
