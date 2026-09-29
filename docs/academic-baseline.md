# Academic baseline

The Novolis economy is a stylized, reproducible rules-based model. It is
intended to be coherent enough for undergraduate economic reasoning, teaching,
and game systems that want explicit causal feedback. It is not an empirically
calibrated forecast and should not be presented as one.

The flagship identity is `SmallOpenRegionalTrade@small-open-regional-trade-3`.
`DeterministicBounded@deterministic-bounded-2` remains the finite,
non-stochastic teaching and regression model.

## What is explicit

- Physical quantities are held as `EconomicPosition` values and are not
  silently converted into money.
- Money is a position in the scenario unit of account.
- Claims and liabilities are represented separately from owned positions.
- Core transactions are atomic, journaled, and checked against registered
  entities, assets, and regions.
- Production, trade, consumption, and credit produce observable effects.
- Monetary closure is explicit: closed, open, or an external-sector
  counterparty.
- Interest is a per-period convention in the flagship model and is accrued at
  the declared period boundary rather than on an undocumented hourly basis.
- Rationing reports requested quantity, sold quantity, unmet demand, and the
  clearing ratio.
- Accounting is a read-only projection of Core state and transaction history.
- A model, scenario, and run have separate identities and reproducibility
  inputs.
- A full state is a versioned `economy.snapshot.v1` JSON document. A compact
  result record is a versioned `economy.run.v1` JSON document.
- Snapshots contain the typed model specification and scenario, seed, tick,
  period, command stream, observations, transition receipts, Core positions,
  claims, and append-only transaction journal.
- JSON persistence uses explicit model codecs. It never deserializes arbitrary
  CLR or assembly-qualified type names.

## SmallOpenRegionalTrade interpretation

The flagship model contains:

```text
regional producer
      ↓ produces
food and trades with households and an external sector
      ↓ requires
fuel imports, logistics capacity, and working capital
      ↓ produces observations
prices, quantities, unmet demand, cash, claims, and transaction history
```

The model is deliberately small. The point is to make causal mechanisms
inspectable:

```text
scarcity → rationing → unmet demand
imports → physical relief + external payment
exports → physical outflow + external receipt
credit → cash today + claim and interest obligation later
```

## Explicit variants

The fixed-price baseline is intentionally retained:

```text
FoodMarketMechanism.PostedPriceRationing
FoodDemandMechanism.CohortBudgetShare
CreditMechanism.BoundedWorkingCapital
```

The specification can instead select explicit alternatives:

- `SupplyDemandPriceDiscovery` changes the household food quote from declared
  supply pressure while preserving the baseline as a separate mechanism.
- `LinearExpenditure` protects a declared subsistence envelope before applying
  a discretionary food share.
- `CreditMechanism.Disabled` removes working-capital draws.
- `InterestConvention.SimplePerPeriod` and
  `InterestConvention.CompoundPerPeriod` state how interest is calculated.
- `DefaultResolution` states what happens at the end of a credit term.
- `TaxBase` and `TaxRate` are persisted model declarations; a non-zero fiscal
  mechanism should only be interpreted where the selected model documents its
  implementation.

These are model variants, not silent changes to the flagship baseline. Their
rules are included in the run manifest.

## Calibration and validation

Calibration metadata is represented by a serializable `CalibrationPlan`.
Each target records its metric, unit, model version, source, aggregation,
tolerance, and whether it is an `InternalInvariant` or `Empirical` target.
`EconomicValidationRunner` executes a seeded run and returns a JSON-capable
report.

Internal targets answer:

> Did the declared implementation rules remain internally valid?

Empirical targets answer only:

> How close was this run to an externally supplied target?

The second answer does not become an empirical claim merely because the
comparison passed.

## Reproducible run workflow

`EconomicModelRunner` is an optional generic host. A game can tick an
`IEconomicModel` directly. The same model can also be executed through:

- `Run` for one deterministic run;
- `RunSeeds` for a declared seed set;
- `RunVariants` for named specification variants;
- `Compare` for observation ranges;
- `Continue` for a suffix after a restored state.

The run manifest records model and scenario identities, seed, tick span,
initial and final fingerprints, specification hash, rule identities, actor
profiles, and initial tick. A restored suffix must produce the same final
fingerprint as an uninterrupted run when the model, commands, and seed match.

## What is not claimed

- No empirical parameter calibration is implied by the default values.
- A posted price is not a discovered general-equilibrium price.
- Aggregate household demand is not a complete household microfoundation.
- The external sector is an explicit accounting counterparty, not a complete
  international macroeconomic model.
- The model does not yet establish causal validity for a real economy.
- Reproducibility demonstrates a stable computation, not truth.

An academic use should therefore report the model version, scenario, rules,
parameters, seed, closure, command stream, snapshot/run format, validation
plan and report, observed transaction history, expected fingerprint, and
limitations. See [replication-guide.md](replication-guide.md) for the compact
checklist.
