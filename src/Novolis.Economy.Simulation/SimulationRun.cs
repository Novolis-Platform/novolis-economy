using Novolis.Economy.Logistics;

namespace Novolis.Economy.Simulation;

/// <summary>Request for one reproducible Simulation-owned model run.</summary>
public sealed record SimulationRunRequest(
  SimulationModelDefinition Model,
  ulong Seed,
  SimulationDuration Duration,
  bool ComputeFinalHash = true);

/// <summary>Read-only metrics captured from a completed simulation run.</summary>
public sealed record SimulationMetricSnapshot(
  IReadOnlyDictionary<string, decimal> Values)
{
  /// <summary>Gets a metric, or zero when the metric is not present.</summary>
  public decimal Get(string name) => Values.GetValueOrDefault(name);
}

/// <summary>Completed model run with its manifest, state, result, and metrics.</summary>
public sealed record SimulationRunResult(
  SimulationState State,
  SimulationResult Advance,
  SimulationMetricSnapshot Metrics)
{
  /// <summary>Reproducibility manifest captured at run creation.</summary>
  public SimulationRunManifest Manifest => State.Manifest;
}

/// <summary>Executes model-selected runs without introducing a separate experiment package.</summary>
public static class SimulationRunner
{
  /// <summary>Runs one selected model for the requested duration.</summary>
  public static async ValueTask<SimulationRunResult> RunAsync(
    SimulationRunRequest request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    var simulation = EconomySimulation.FromModel(request.Seed, request.Model);
    var advance = await simulation.AdvanceAsync(
      request.Duration,
      request.ComputeFinalHash,
      cancellationToken).ConfigureAwait(false);
    return new SimulationRunResult(
      simulation.State,
      advance,
      SimulationMetrics.Capture(simulation.State));
  }
}

/// <summary>Common scalar observations for run comparison and teaching output.</summary>
public static class SimulationMetrics
{
  /// <summary>Captures deterministic metrics without changing simulation state.</summary>
  public static SimulationMetricSnapshot Capture(SimulationState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    var world = state.World;
    return new SimulationMetricSnapshot(
      new Dictionary<string, decimal>
      {
        ["hours"] = state.Clock.HourIndex,
        ["period"] = world.CoreState.Period,
        ["state-hash"] = state.Hash,
        ["world-fingerprint"] = world.Fingerprint(),
        ["external-imports-paid"] = world.ExternalTrade.ImportsPaid.Amount,
        ["external-exports-received"] = world.ExternalTrade.ExportsReceived.Amount,
        ["external-trade-balance"] = world.ExternalTrade.TradeBalance.Amount,
        ["physical-production"] =
          world.CoreState.Flows.ProductionQuantities.Values.Sum(),
        ["in-transit-shipments"] =
          world.Shipments.Count(shipment => shipment.Status == ShipmentStatus.InTransit),
        ["core-transactions"] = world.CoreState.Journal.Count
      });
  }
}

/// <summary>Runs a stable ordered collection of model-run requests.</summary>
public static class SimulationSweep
{
  /// <summary>Executes requests in input order for deterministic result ordering.</summary>
  public static async ValueTask<IReadOnlyList<SimulationRunResult>> RunAsync(
    IEnumerable<SimulationRunRequest> requests,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(requests);
    var results = new List<SimulationRunResult>();
    foreach (var request in requests)
    {
      cancellationToken.ThrowIfCancellationRequested();
      results.Add(await SimulationRunner.RunAsync(request, cancellationToken)
        .ConfigureAwait(false));
    }

    return results;
  }
}
