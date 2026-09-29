using System.Reflection;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Agents;
using Novolis.Economy.Core;
using Novolis.Economy.Finance;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Unit.Architecture;

public sealed class LayerBoundaryTests
{
  [Test]
  public async Task Abstractions_ReferencesOnlyPrimitivesWithinEconomy()
  {
    var references = EconomyReferences(typeof(IEconomicRule<,>));

    await Assert.That(references).IsEquivalentTo(
      new[] { "Novolis.Economy.Primitives" });
  }

  [Test]
  public async Task Agents_DoesNotReferenceSimulation()
  {
    var references = EconomyReferences(typeof(IEconomicAgent));

    await Assert.That(
        references.Contains("Novolis.Economy.Simulation", StringComparer.Ordinal))
      .IsFalse();
  }

  [Test]
  public async Task Finance_DoesNotReferenceAccounting()
  {
    var references = EconomyReferences(typeof(LoanEngine));

    await Assert.That(
        references.Contains("Novolis.Economy.Accounting", StringComparer.Ordinal))
      .IsFalse();
  }

  [Test]
  public async Task Core_DoesNotReferenceSimulation()
  {
    var references = EconomyReferences(typeof(EconomyState));

    await Assert.That(
        references.Contains("Novolis.Economy.Simulation", StringComparer.Ordinal))
      .IsFalse();
  }

  [Test]
  public async Task NoExperimentsAssemblyIsPresent()
  {
    var loaded = AppDomain.CurrentDomain.GetAssemblies()
      .Select(assembly => assembly.GetName().Name)
      .Where(name => name is not null)
      .ToArray();

    await Assert.That(
        loaded.Any(name => name!.Equals(
          "Novolis.Economy.Experiments",
          StringComparison.Ordinal)))
      .IsFalse();
  }

  private static IReadOnlyList<string> EconomyReferences(Type type) =>
    type.Assembly
      .GetReferencedAssemblies()
      .Select(reference => reference.Name)
      .Where(name => name?.StartsWith(
        "Novolis.Economy.",
        StringComparison.Ordinal) == true)
      .Cast<string>()
      .OrderBy(name => name, StringComparer.Ordinal)
      .ToArray();
}
