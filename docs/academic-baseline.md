# Academic baseline

The Novolis economy is a stylized, reproducible rules-based model. It is
intended to be coherent enough for undergraduate economic reasoning, teaching,
and game systems that want explicit causal feedback. It is not an empirically
calibrated forecast and should not be presented as one.

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

## What is not claimed

- No empirical parameter calibration is implied by the default values.
- A posted price is not a discovered general-equilibrium price.
- Aggregate household demand is not a complete household microfoundation.
- The external sector is an explicit accounting counterparty, not a complete
  international macroeconomic model.
- The model does not yet establish causal validity for a real economy.
- Reproducibility demonstrates a stable computation, not truth.

An academic use should therefore report the model version, scenario, rules,
parameters, seed, closure, observed transaction history, and limitations.
