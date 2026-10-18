# M5 inventory API slice — 2026-10-18

## Implemented

- Authenticated catalog and inventory snapshot endpoints.
- Server-authoritative cash purchases for seven original supply/equipment items, with a 10 kg capacity, per-item stack limits, a cash ledger entry, and an inventory history entry.
- Equip/unequip for owned equipment, with one equipped item per slot.
- UUID idempotency keys on purchase and equip actions. Per-player row locking serializes inventory purchases with jobs and bank transfers; replayed requests return their original result.
- Unity source for a bag panel, catalog, inventory snapshot, purchase/equip actions, and a locally persisted retry key for an ambiguous network result.

## Verification

Ran the complete `Harborlight.Api.Tests` suite against local PostgreSQL after the migration and inventory code were in place. Result: 24 passed, 0 failed, 0 skipped. Coverage includes cash debit and persistence after loading through a second database context, same-key replay and conflicting-key rejection, quantity/stack/capacity limits, equipment ownership and slot replacement, and concurrent same-key requests through separate contexts.

`git diff --check` passed for the service and Unity repositories. The current Unity source compiled in a macOS development QA build (`harborlight-inventory-ui-build-2026-10-18.log`). A short player smoke used an isolated loopback API and disposable PostgreSQL database: it opened the bag before guest sign-in completed, loaded all seven catalog items at 125 credits, bought Dockhand Gloves for 35 credits, equipped them, and rendered the saved owned/equipped state at 90 credits. The final database query found exactly one purchase row and one equip row for that QA player. The first capture exposed a stale offline feedback message after reconnect; the panel now refreshes on an offline-to-online transition, and the corrected capture shows “Inventory updated.” Runtime output and screenshot are `harborlight-inventory-ui-runtime-2026-10-18.log` and `screenshots/Harborlight-Inventory-UI-Authenticated-2026-10-18.png`.

## Remaining scope

The smoke called the same `HarborlightBackendClient` methods wired to the BUY/EQUIP controls, but did not synthesize button input. Button hit targets, Android touch, network-loss/ambiguous-response retry in the player, sales/trading, durability/use effects, visible appearance changes, offline inventory, and production commerce remain open. This slice does not make the broader M5 economy milestone complete.
