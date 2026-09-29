# Novolis.Economy.Models.SmallOpenRegionalTrade

Host-neutral small open regional trade model for game, simulation, and research
consumers.

The model owns economic rules, parameters, named scenarios, and one-tick
transitions. It does not own a clock or require `Novolis.Economy.Simulation`.
Consumers drive `IEconomicModel.Advance` with their own clock and commands.

