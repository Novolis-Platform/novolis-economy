# Novolis.Economy.Agents

**Economic agents** — heuristic decision-makers that observe `EconomyWorld` and enqueue commands. Not LLMs / ML.

Typical pulse: `AgentScheduler.TickAll(agents, context)` then `await sim.AdvanceAsync(1h)`.
