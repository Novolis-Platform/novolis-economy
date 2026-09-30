using Novolis.Economy;
using Novolis.Economy.Logistics;

using System.Reflection;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>Runs agents in order; caller advances the simulation.</summary>
public static class AgentScheduler
{
  /// <summary>Ticks each agent once.</summary>
  public static void TickAll(IEnumerable<IEconomicAgent> agents, AgentContext context)
  {
    ArgumentNullException.ThrowIfNull(agents);
    ArgumentNullException.ThrowIfNull(context);
    foreach (var agent in agents)
    {
      agent.Tick(context);
    }
  }
}
