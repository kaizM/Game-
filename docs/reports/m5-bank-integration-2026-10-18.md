# M5 bank integration report — 2026-10-18

## Implemented

- The API creates a zero-balance bank account for each guest profile and backfills existing profiles through the `AddPlayerBanking` migration.
- Authenticated routes expose a cash/bank snapshot, recent transactions, and deposits or withdrawals. Transfers lock the same cash row used by mission rewards, lock the bank row, and update both balances plus the immutable ledger in one transaction.
- Transfers use the existing per-player idempotency store. The ledger records successful deposits, withdrawals, and job rewards. Invalid or insufficient transfers do not change balances; retrying the same key returns the recorded result.
- Unity source adds a Harbor Bank ATM, online banking calls, a balance/transfer/history panel, and an offline local bank ledger. The v9 local save migrates v8 and earlier saves with a zero starting bank balance and preserves cash and existing progress. An ambiguous online transfer retains its idempotency key so the player can retry the same amount safely.

## Verification

- `dotnet test tests/Harborlight.Api.Tests/Harborlight.Api.Tests.csproj --no-restore` passed **20/20**, 0 failed and 0 skipped, against PostgreSQL 16.14. This includes deposits/withdrawals, balance limits, insufficient funds, duplicate and conflicting keys, concurrent replay, and transaction history.
- Test fixtures use isolated schemas. A post-run query found zero `hl_test_*` schemas.
- Unity 6000.5.4f1 batch script compilation completed successfully. `Assembly-CSharp.dll` and the editor assembly compiled, and the log contains no C# compiler errors: `/tmp/Harborlight-M5-Bank-Compile-20261018.log`.
- The map source audit reconfirmed 3,600 × 2,196 m bounds (7.91 km²), 390 unique in-bounds building sites with road clearance, and 16 unique entry points on road intersections.
- The Mac reported no thermal or performance warning after the script-only compile.

## Limits

- The bank UI has not been exercised in a running player, and no Mac player or Android package was built from this source. Online Unity-to-API transfer, offline deposit/load persistence, v8 save migration, and mobile keyboard/layout need runtime verification.
- Offline cash and bank balances live in the device save; they do not merge into an online profile. Online balances remain API-authoritative.
- The latest saved map screenshot predates the 390-site/16-zone source update. That update is source-audited but not shown in a new runtime capture.
