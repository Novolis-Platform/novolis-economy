# Novolis.Economy.Agents

**Economic agents** — heuristic decision-makers that observe `EconomyWorld` and enqueue commands. Not LLMs / ML.

Includes Extractive / Manufacturing / Retail / Carrier / Treasury / **Household** (`HouseholdFirmAgent` — invest/lend only above comfort using cohort `BudgetRemaining`).

Typical pulse: `AgentScheduler.TickAll(agents, context)` then `await sim.AdvanceAsync(1h)`.
