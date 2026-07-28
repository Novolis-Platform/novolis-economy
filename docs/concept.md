# Concept

Working title for a future product: **Novolis Commerce** (Capitalism Plus–style economic simulation). Reusable packages live here as `Novolis.Economy.*`.

## Proposition

A business simulation where displayed numbers trace to economic processes—procurement, production, logistics, cohort demand, double-entry accounting—not opaque multipliers or gamification meters.

## This repository

Platform library family:

- Headless, deterministic, UI-independent
- `EconomyWorld` + ordered hourly phases
- Commands / events / projections
- Domain engines for production, logistics, demand, ledgers, and market observation

Vocabulary is **generic commerce** (firms, facilities, products, invoices, cohorts). Content packs (food chains, etc.) belong at the product layer.

## Deferred

- AI firm controllers / strategies
- Commerce game host / Avalonia UI
- Platform workspace and snapshot integration
- Credit markets, bankruptcy, rich P&amp;L statements
