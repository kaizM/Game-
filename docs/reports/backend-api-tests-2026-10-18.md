# Backend API test report — 2026-10-18

## Result — original run

- Test project: `../harborlight-services/tests/Harborlight.Api.Tests/Harborlight.Api.Tests.csproj`
- Runtime: .NET 10; PostgreSQL 16.14 (local Homebrew server)
- Result: 11 passed, 0 failed, 0 skipped; test duration 2 seconds
- Command: `dotnet test tests/Harborlight.Api.Tests/Harborlight.Api.Tests.csproj --no-restore --logger 'console;verbosity=minimal'` with `HARBORLIGHT_TEST_CONNECTION` set to a fresh temporary database (`harborlight_codex_test_20261018_grounding`) on the local PostgreSQL 16.14 instance.

The suite exercised guest-token hashing, atomic/idempotent cosmetic spending, duplicate store notification handling, fail-closed purchase verification, allowlisted/idempotent/audited admin actions, game registration and typed allowlists, identity-link configuration behavior, and the restricted runtime database role.

## Database isolation

Docker could not connect to the Colima daemon. The existing local PostgreSQL server was reachable. The uniquely named database `harborlight_codex_test_20261018_grounding` was used only for this run. The tests created per-test schemas and temporary runtime roles; post-run inspection found no leftover schemas or runtime roles. The test database was then dropped, and a post-cleanup query confirmed it no longer exists.

## Limits

This proves the current service and database integration tests pass against PostgreSQL 16.14. It does not prove live HTTP endpoint behavior, real Apple/Google purchase validation, configured identity-provider verification, production OIDC/MFA, database recovery, or multiplayer behavior.

## Rerun — 2026-10-17

- Re-ran the current test project against PostgreSQL 16.14 with `HARBORLIGHT_TEST_CONNECTION='Host=127.0.0.1;Port=5432;Database=postgres;Username=admin'`.
- Result: **16 passed, 0 failed, 0 skipped**, 3 seconds.
- Added cases cover server-authoritative beacon/courier/inspection transitions, reward ordering, idempotent replay, conflicting request-ID reuse, concurrent replay, and single-use realtime tickets.
- Test fixtures create and dispose unique schemas and runtime roles.
- The suite validates API services and PostgreSQL transactions, not the Unity-to-API transport. See `m5-mission-persistence-e2e-2026-10-17.md` for separate HTTP and Unity loopback checks.

## Rerun — 2026-10-18, banking

- Ran the current test project with .NET SDK 10.0.400 against the existing local PostgreSQL 16.14 service. The suite passed **20 tests, 0 failed, 0 skipped** in 12 seconds.
- Added bank coverage for guest account initialization, atomic deposit/withdraw and ledger history, invalid and insufficient transfers, conflicting idempotency-key reuse, and concurrent duplicate replay.
- Test schemas were created in the `postgres` test database and removed by the fixtures. A post-run query found zero `hl_test_*` schemas.
- Exact bank/API/Unity-source evidence and limits are in `m5-bank-integration-2026-10-18.md`. Unity script compilation passed, but this is not a player build or bank UI runtime test.
