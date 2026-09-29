# Economy replication guide

This guide describes the minimum record needed to reproduce one
`SmallOpenRegionalTrade` result.

## Required manifest

Record these values with the result:

1. Model identity:
   `SmallOpenRegionalTrade@small-open-regional-trade-3`.
2. Scenario identity:
   `three-region-baseline@small-open-regional-trade-scenario-1`, or the
   complete custom scenario record.
3. The complete serialized `SmallOpenRegionalTradeSpecification`, including
   market, demand, credit, interest, default, fiscal, and closure choices.
4. The rule identities and actor profile identifiers from the run manifest.
5. Root seed, tick count, period length, and the absolute command stream.
6. Initial and final state fingerprints.
7. Snapshot format `economy.snapshot.v1` when a resumable state is supplied.
8. Run format `economy.run.v1` when only observations and receipts are
   supplied.
9. Calibration plan, target sources, validation report, and any external
   data transformation.

## Minimal execution shape

```csharp
var request = new EconomicModelRunRequest(
    new SmallOpenRegionalTradeModel(specification),
    scenario,
    Seed: 42,
    Ticks: 24,
    PeriodLengthTicks: 24,
    Commands: commands);

var result = EconomicModelRunner.Run(request);
var json = EconomicRunStore.Serialize(result);
```

The model itself owns one tick. `EconomicModelRunner` supplies a convenient
clock and run manifest; it is not part of the model's economic meaning.

## Snapshot and resume

Capture a full snapshot with the model's registered
`SmallOpenRegionalTradeJsonCodec`. The snapshot contains the authoritative Core
positions, claims, transaction journal, model state, command stream,
observations, and receipts. On restore:

1. Resolve the codec by explicit model identity and version.
2. Validate the format and model/scenario envelope.
3. Validate Core registrations, positions, claims, projections, journal
   structure, and the state fingerprint.
4. Continue from the restored absolute tick with the same seed and commands.

The restored suffix is a replication failure if its final fingerprint or
journal order differs from a fresh uninterrupted run.

## Claims and limitations

This workflow supports reproducible computational experiments and transparent
undergraduate-level mechanism studies. It does not, by itself, establish
empirical validity, parameter identification, welfare conclusions, or
general-equilibrium results. Report which observations are internal
invariants, which are stylized mechanism outputs, and which are comparisons
against external data.
