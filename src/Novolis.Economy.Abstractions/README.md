# Novolis.Economy.Abstractions

Model-neutral contracts for composing economic behavior.

This package contains replaceable rule contracts, actor boundary records, and
deterministic randomness interfaces. It does not own economic state, model
implementations, simulation clocks, or settlement.

`Novolis.Economy.Core` remains the authority for positions, claims,
transactions, and invariants. `Novolis.Economy.Simulation` selects concrete
rules and executes them over time.
