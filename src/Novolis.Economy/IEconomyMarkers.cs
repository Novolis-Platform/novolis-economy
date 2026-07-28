namespace Novolis.Economy;

/// <summary>Marker for player or AI decisions applied to the simulation.</summary>
public interface IEconomyCommand;

/// <summary>Marker for facts that occurred during simulation (diagnostics and reporting).</summary>
public interface IEconomyEvent;

/// <summary>Marker for read models answering UI or tooling queries.</summary>
public interface IEconomyProjection;
