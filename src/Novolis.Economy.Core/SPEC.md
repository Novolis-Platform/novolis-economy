# Bounded Minimum Economic Model

A **bounded minimum economic model** is the smallest model that preserves the economic relationships needed for meaningful behavior while explicitly excluding operational detail that does not affect the economic question.

It is **bounded** because it declares what exists, what does not exist, and where aggregation occurs.

It is **minimum** because each concept must explain a distinct economic phenomenon. A concept is not included merely because it exists in the real world.

It remains economically rigorous by preserving:

- ownership
- production and consumption
- stocks and flows
- labor allocation
- financial claims
- liquidity and default
- regional capacity
- transport between regions
- legal and institutional distinctions

The model does not need individual workers, ships, invoices, cargo manifests, buildings, payroll systems, or detailed contracts. Those can be introduced later if the simulation question requires them.

---

## 1. The economic boundary

The minimum model contains:

```text
Legal entities
Regions
Household cohorts
Activities
Resources
Resource holdings
Shares
Loans
Credit facilities
Payment obligations
Insurance coverage
Policies
```

The central relationships are:

```text
Legal entities own resources and financial instruments.

Households supply labor and consume resources.

Firms operate productive activities.

Activities transform inputs into outputs.

Regions constrain population, production, and transport.

Shares define ownership of firms.

Loans and payment obligations define debt.

Credit facilities define borrowing capacity.

Liquidity determines whether obligations can be paid on time.

States tax, spend, transfer, and regulate.
```

This is enough to produce shortages, profits, unemployment, investment, insolvency, regional specialization, transport bottlenecks, credit crises, and ownership concentration.

---

# 2. Legal entities

A legal entity is the minimum unit capable of owning assets, owing obligations, receiving payments, and participating in economic transactions.

```csharp
public readonly record struct LegalEntityId(Guid Value);

public enum LegalEntityKind
{
    Household,
    Firm,
    Lender,
    Bank,
    Insurer,
    State
}

public sealed record LegalEntity(
    LegalEntityId Id,
    LegalEntityKind Kind,
    Money Cash);
```

The entity kinds describe economically meaningful institutional differences.

## Household

A household is an **unownable legal entity**.

It may:

- own resources
- own shares
- hold deposits
- make loans
- borrow
- consume
- supply labor
- receive wages, dividends, and transfers

It may not:

- issue shares in itself
- be acquired
- become a subsidiary
- be owned by another legal entity

Households are the terminal private beneficiaries of the ownership graph.

## Firm

A firm is an ownable legal entity that may:

- issue shares
- own resources
- operate activities
- employ labor
- borrow
- lend
- retain profit
- distribute dividends

The firm is the financial and legal container. It does not itself represent a factory, shop, carrier, or mine. Those are productive activities operated by the firm.

## Lender

A lender is a specialized legal entity that extends credit from funds it owns or has borrowed.

It does not necessarily:

- accept deposits
- provide transaction accounts
- participate directly in payment settlement
- create deposit money

## Bank

A bank is a distinct legal entity with banking privileges.

It may:

- accept deposits
- provide payment accounts
- participate in settlement
- issue loans
- create deposit liabilities when lending
- borrow from other financial institutions

A bank is therefore not merely a lender with a different label.

## Insurer

An insurer receives premiums and accepts specified risks.

Insurance transfers the financial consequences of defined losses. It does not eliminate the underlying physical or economic loss.

## State

A state is both an economic actor and a policy authority.

It may:

- collect taxes
- purchase resources and services
- employ labor
- make transfers
- borrow
- lend
- own firms
- provide insurance or guarantees
- set economic policies

---

# 3. Regions

A region is the lowest geographic resolution in the model.

A region is treated as a homogeneous economic point. Internal travel, urban structure, individual buildings, and local distribution are below the model boundary.

```csharp
public readonly record struct RegionId(Guid Value);

public sealed record Region(
    RegionId Id,
    int LivingCapacity,
    decimal ProductionCapacity,
    decimal LogisticsCapacity);
```

A region provides three principal constraints.

## Living capacity

Living capacity limits how many households may reside in the region.

```text
Resident households ≤ LivingCapacity
```

The model does not need to explain whether this capacity represents housing, life support, land, infrastructure, or administrative permission. It represents the economically relevant result: a bounded resident population.

## Production capacity

Production capacity limits the amount of productive activity that may be installed in the region.

It may represent:

- industrial land
- utility capacity
- infrastructure
- workshop space
- environmental limits
- administrative permits

The specific physical cause is outside the boundary.

## Logistics capacity

Logistics capacity limits how much can be moved into or out of the region during a period.

Internal transport within the region is ignored.

The region itself acts as the inventory location. A separate warehouse or hub model is unnecessary unless local storage and distribution later become economically significant.

---

# 4. Household cohorts

At region scale, individual households usually create cost without adding useful economic behavior.

The minimum simulation unit should therefore be a **representative household cohort**.

```csharp
public readonly record struct CohortId(Guid Value);

public sealed record HouseholdCohort(
    CohortId Id,
    RegionId RegionId,
    int HouseholdCount,
    HouseholdProfile Profile,
    HouseholdLaborKind LaborKind,
    Money CashPerHousehold);
```

A cohort represents many economically similar households.

```csharp
public sealed record HouseholdProfile(
    decimal ConsumptionWeight,
    decimal SavingsPreference,
    decimal LaborQuality,
    decimal MigrationPreference);
```

The cohort may represent households sharing:

- income behavior
- consumption preferences
- labor capacity
- skill level
- savings behavior
- migration behavior

The cohort is an aggregation mechanism, not a separately ownable institution.

Economically, the cohort represents many household legal entities. Computationally, it permits them to be processed together.

## Cohort accounting

Values must be explicit about whether they are totals or per-household averages.

For example:

```text
Total cohort cash
= HouseholdCount × CashPerHousehold
```

Mixing aggregate and per-household values is one of the easiest ways to quietly corrupt the model.

---

# 5. Household labor capacity

The proposed `12 / 18 / 24` setting should be described as **labor capacity**, not productivity.

```csharp
public enum HouseholdLaborKind
{
    Common,
    Mean,
    Extreme
}

public static class HouseholdLabor
{
    public static decimal HoursPerDay(HouseholdLaborKind kind) =>
        kind switch
        {
            HouseholdLaborKind.Common => 12m,
            HouseholdLaborKind.Mean => 18m,
            HouseholdLaborKind.Extreme => 24m,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}
```

The values describe the labor-hours supplied by an average household each day.

```text
Regional labor capacity
= Σ HouseholdCount × HoursPerHousehold
```

Productivity is a separate concept.

```text
Labor capacity
= available labor-hours

Labor quality
= effective contribution per labor-hour

Production productivity
= output produced from labor and other inputs
```

This distinction allows the model to represent:

- a highly skilled but lightly working population
- a heavily working but poorly trained population
- capital-intensive production
- technological improvement
- labor shortages despite high worker productivity

A more rigorous labor calculation is:

```text
Effective labor
= household count
× available hours
× labor quality
× participation rate
```

The model may omit participation rate initially if it adds no useful behavior.

---

# 6. Activities

An activity is the minimum productive unit.

It replaces detailed physical concepts such as:

- factories
- shops
- farms
- ships
- mines
- offices
- warehouses
- transport fleets

```csharp
public readonly record struct ActivityId(Guid Value);

public sealed record Activity(
    ActivityId Id,
    LegalEntityId Operator,
    RegionId RegionId,
    ActivityRecipe Recipe,
    decimal InstalledCapacity);
```

A firm may operate many activities in many regions.

```text
Firm
├── Production activity in Region A
├── Retail activity in Region A
├── Transport activity between A and B
└── Production activity in Region B
```

The firm owns the economic operation. The activity consumes regional production capacity and performs transformation.

## Activity recipe

```csharp
public sealed record ActivityRecipe(
    IReadOnlyList<ResourceAmount> Inputs,
    IReadOnlyList<ResourceAmount> Outputs,
    decimal LaborHoursPerRun,
    decimal ProductionSpacePerRun);
```

An activity transforms inputs into outputs.

```text
Inputs + labor + production capacity
→ outputs
```

For example:

```text
2 units of raw material
+ 4 labor-hours
→ 1 unit of manufactured goods
```

The model does not need to know what machine performs the transformation.

## Actual production

Installed capacity is not necessarily actual output.

Production is constrained by the scarcest required input:

```text
Actual runs
= minimum of:

installed activity capacity
available production space
available labor
available input resources
```

This produces readable bottlenecks without arbitrary productivity penalties.

---

# 7. Resources

A resource is anything that may be owned, consumed, produced, transferred, or stored.

```csharp
public readonly record struct ResourceId(Guid Value);

public sealed record Resource(
    ResourceId Id,
    string Name,
    ResourceKind Kind);

public enum ResourceKind
{
    ConsumerGood,
    IntermediateGood,
    CapitalGood,
    Service
}
```

Resources are deliberately abstract.

A model may contain:

```text
Food
Basic goods
Industrial inputs
Capital goods
Transport service
Housing service
```

It does not need thousands of product types unless substitution and production-chain detail are part of the economic question.

---

# 8. Resource holdings

Ownership and location must both be explicit.

```csharp
public sealed record ResourceHolding(
    LegalEntityId Owner,
    RegionId RegionId,
    ResourceId ResourceId,
    decimal Quantity);
```

This record answers:

> Who owns how much of which resource, and where is it?

That is sufficient for:

- inventory
- household consumption
- firm inputs
- trade
- transport
- shortages
- regional price differences

The model does not require individual inventory objects.

A holding can be treated as an aggregated ledger entry keyed by:

```text
Owner × Region × Resource
```

---

# 9. Transport

Transport is an activity that changes the location of resources.

It does not require vehicles, routes, crews, cargo manifests, or shipments as physical objects.

```csharp
public sealed record TransportLane(
    RegionId Origin,
    RegionId Destination,
    int TravelPeriods,
    decimal CapacityPerPeriod);
```

A transfer moves owned resources between regions.

```csharp
public sealed record ResourceTransfer(
    LegalEntityId Owner,
    ResourceId ResourceId,
    decimal Quantity,
    RegionId Origin,
    RegionId Destination,
    int RemainingPeriods);
```

Transport preserves ownership unless a separate sale occurs.

```text
Trading:

Firm buys resource in Region A
→ firm transports owned resource
→ firm sells resource in Region B
```

```text
Carriage:

Customer owns resource in Region A
→ customer purchases transport service
→ carrier moves customer-owned resource
→ customer still owns resource in Region B
```

This preserves the distinction between trading goods and selling transport capacity without modeling actual cargo handling.

---

# 10. Shares

Shares are explicit financial instruments representing ownership in an issuing firm.

```csharp
public sealed record ShareClass(
    LegalEntityId Issuer,
    string Name,
    decimal IssuedUnits,
    decimal VotesPerUnit);

public sealed record ShareHolding(
    LegalEntityId Owner,
    LegalEntityId Issuer,
    string ShareClass,
    decimal Units);
```

A household may own shares.

A firm may own shares in another firm.

A state may own shares.

The issuing firm may not meaningfully own its own outstanding shares without explicit treasury-share treatment.

A household cannot issue shares because it is unownable.

Shares provide:

- residual economic ownership
- possible voting control
- dividend entitlement
- exposure to profit and loss

Shares are not debt. They do not promise repayment.

---

# 11. Loans

A loan is an existing debt instrument.

```csharp
public readonly record struct LoanId(Guid Value);

public sealed record Loan(
    LoanId Id,
    LegalEntityId Lender,
    LegalEntityId Borrower,
    Money PrincipalOutstanding,
    decimal InterestRatePerPeriod,
    int RemainingPeriods,
    LoanStatus Status);

public enum LoanStatus
{
    Performing,
    Delinquent,
    Defaulted,
    Repaid
}
```

The loan is:

- an asset of the lender
- a liability of the borrower

A loan changes liquidity when funds are advanced and when repayments occur.

It also changes future cash flows through interest and principal obligations.

---

# 12. Credit

Credit is the capacity to create debt, not debt that already exists.

```csharp
public readonly record struct CreditFacilityId(Guid Value);

public sealed record CreditFacility(
    CreditFacilityId Id,
    LegalEntityId Provider,
    LegalEntityId Borrower,
    Money Limit,
    Money Drawn,
    bool IsCommitted);
```

Available credit is:

```text
Limit - Drawn
```

Drawing credit creates or increases a loan.

Committed credit is contractually available if its conditions are satisfied.

Uncommitted credit is merely a lender's present willingness to lend and should not be counted as reliable liquidity.

---

# 13. Payment obligations

Economic rigor requires payment timing.

A profitable entity may still fail because its payments are due before its receipts arrive.

```csharp
public readonly record struct ObligationId(Guid Value);

public sealed record PaymentObligation(
    ObligationId Id,
    LegalEntityId Debtor,
    LegalEntityId Creditor,
    Money Amount,
    int DuePeriod,
    ObligationKind Kind,
    ObligationStatus Status);
```

```csharp
public enum ObligationKind
{
    Trade,
    Wage,
    Tax,
    Interest,
    Principal,
    Dividend,
    InsurancePremium,
    InsuranceClaim
}

public enum ObligationStatus
{
    Pending,
    Paid,
    Delinquent,
    Defaulted
}
```

Detailed invoices and payroll records are not required.

A wage obligation may simply represent the total wages owed by an activity or firm for the current period.

---

# 14. Liquidity

Liquidity is an entity's ability to meet obligations when they become due.

Liquidity should generally be derived rather than stored as an arbitrary score.

```csharp
public sealed record LiquidityPosition(
    Money Cash,
    Money AccessibleDeposits,
    Money UndrawnCommittedCredit,
    Money DueNow)
{
    public Money Available =>
        Cash + AccessibleDeposits + UndrawnCommittedCredit;

    public Money Surplus =>
        Available - DueNow;
}
```

Liquidity differs from solvency.

```text
Liquidity:
Can the entity pay now?

Solvency:
Are the entity's recoverable assets worth more than its liabilities?
```

An entity may be:

| Position | Meaning |
|---|---|
| Solvent and liquid | Financially healthy |
| Solvent but illiquid | Valuable assets, insufficient immediate payment capacity |
| Insolvent but liquid | Can continue temporarily despite structural failure |
| Insolvent and illiquid | Immediate and structural failure |

The minimum model may calculate solvency using simplified asset values rather than simulating asset-sale markets.

---

# 15. Banks and deposits

A bank deposit is a financial claim against the bank.

```csharp
public sealed record Deposit(
    LegalEntityId Depositor,
    LegalEntityId Bank,
    Money Balance);
```

For the depositor:

```text
Deposit = asset
```

For the bank:

```text
Deposit = liability
```

When a non-bank lender issues a loan, it normally transfers existing liquidity.

When a bank issues a loan, it may create a corresponding deposit liability.

```text
Bank assets:
+ loan

Bank liabilities:
+ borrower deposit
```

This distinction is sufficient to separate bank lending from ordinary lending without building a complete monetary system.

---

# 16. Insurance

Insurance is a contract that converts uncertain losses into premiums and conditional claims.

```csharp
public sealed record InsuranceCoverage(
    LegalEntityId Insurer,
    LegalEntityId Insured,
    RiskKind Risk,
    decimal CoveredFraction,
    Money Deductible,
    Money PremiumPerPeriod);

public enum RiskKind
{
    ProductionLoss,
    TransportLoss,
    LiabilityLoss,
    CreditLoss
}
```

At minimum resolution:

```text
Insured entity pays premium each period.

A loss event occurs according to model rules.

The insured entity bears the deductible and uncovered portion.

The insurer owes the covered portion.
```

Insurance therefore affects:

- expected losses
- liquidity volatility
- insurer exposure
- operating costs
- survivability after shocks

Claims processing, legal disputes, and billing are below the boundary.

---

# 17. State policy

Policies should alter rules and flows rather than act as unexplained modifiers.

```csharp
public sealed record StatePolicy(
    decimal HouseholdTaxRate,
    decimal FirmTaxRate,
    Money TransferPerHousehold,
    decimal DepositReserveRequirement,
    decimal InsuranceCapitalRequirement);
```

The state may create:

- tax obligations
- transfer payments
- public demand
- guarantees
- capacity investment
- regulatory limits

For example:

```text
Household tax
→ household liquidity decreases
→ state liquidity increases

State transfer
→ state liquidity decreases
→ household liquidity increases
```

This preserves conservation of money within the model boundary.

---

# 18. Stocks and flows

The model must distinguish stocks from flows.

## Stocks

Stocks exist at a point in time:

- cash
- deposits
- debt outstanding
- resource holdings
- shares
- resident households
- installed activity capacity

## Flows

Flows occur during a period:

- production
- consumption
- wages
- taxes
- interest
- dividends
- transport
- migration
- loan issuance
- repayment

A stock evolves through flows:

```text
Closing holding
= opening holding
+ production
+ purchases
+ imports
- consumption
- sales
- exports
- losses
```

Every simulated period should reconcile opening stocks, flows, and closing stocks.

---

# 19. Core invariants

Economic rigor is largely achieved through invariants rather than detail.

## Ownership conservation

A resource transfer must either:

- preserve ownership and change location, or
- change ownership through an exchange

Resources should not silently change owner.

## Resource conservation

Except where a production recipe explicitly creates, consumes, or loses resources:

```text
Total resource before
= total resource after
```

## Financial conservation

An ordinary payment reduces the payer's liquidity and increases the receiver's liquidity by the same amount.

```text
Payer cash change = -payment
Receiver cash change = +payment
```

Bank credit creation is an explicit exception because it simultaneously creates:

- a loan asset
- a deposit liability

## Claim symmetry

Every debt has two sides.

```text
Borrower's liability
= lender's corresponding asset
```

## Share consistency

For each share class:

```text
Σ externally held units
+ treasury units
= issued units
```

## Capacity limits

```text
Resident households ≤ living capacity

Installed activities ≤ production capacity

Transfers per period ≤ logistics capacity
```

## Household ownership rule

```text
Households may own.

Households may not be owned.
```

---

# 20. Period execution

A deterministic period may execute in this order:

```text
1. Apply policies and opening conditions.

2. Calculate household labor supply.

3. Allocate regional labor to activities.

4. Determine activity production from:
   - installed capacity
   - labor
   - inputs
   - regional capacity

5. Add produced resources to owner holdings.

6. Resolve household and firm demand.

7. Match buyers and sellers.

8. Transfer ownership and payments.

9. Start and complete interregional transfers.

10. Create wage, tax, interest, and insurance obligations.

11. Settle obligations according to liquidity and priority.

12. Draw committed credit where permitted.

13. Mark delinquency and default.

14. Distribute dividends or retain profit.

15. Apply household consumption and migration.

16. Reconcile stocks, claims, and ownership.
```

The exact sequence is part of the economic model. Changing it may alter results, especially under liquidity constraints.

---

# 21. Minimum aggregate state

The entire simulation may be represented by a compact immutable state.

```csharp
public sealed record EconomyState(
    int Period,
    IReadOnlyDictionary<RegionId, Region> Regions,
    IReadOnlyDictionary<LegalEntityId, LegalEntity> Entities,
    IReadOnlyDictionary<CohortId, HouseholdCohort> Cohorts,
    IReadOnlyDictionary<ActivityId, Activity> Activities,
    IReadOnlyList<ResourceHolding> Holdings,
    IReadOnlyList<ResourceTransfer> Transfers,
    IReadOnlyList<ShareHolding> Shares,
    IReadOnlyList<Loan> Loans,
    IReadOnlyList<CreditFacility> CreditFacilities,
    IReadOnlyList<Deposit> Deposits,
    IReadOnlyList<PaymentObligation> Obligations,
    IReadOnlyList<InsuranceCoverage> Insurance,
    StatePolicy Policy);
```

The state transition is conceptually:

```csharp
public interface IEconomyStep
{
    EconomyState Execute(EconomyState current);
}
```

Each economic mechanism may be implemented as a separate step:

```csharp
public sealed record EconomyEngine(
    IReadOnlyList<IEconomyStep> Steps)
{
    public EconomyState Advance(EconomyState state) =>
        Steps.Aggregate(state, static (current, step) => step.Execute(current));
}
```

The record types explain the model. They do not require the implementation to become an inheritance-heavy object simulation.

---

# 22. What the model deliberately excludes

The bounded minimum excludes:

- individual natural persons
- employment contracts
- employee schedules
- professional certifications
- individual vehicles
- buildings
- cargo manifests
- invoices
- billing systems
- bank-account transaction histories
- detailed contract law
- individual insurance claims handling
- intraregional logistics
- physical production machinery
- detailed securities markets
- individual consumer product selection

These may later be introduced where a specific economic question requires them.

For example:

- Individual workers become necessary if qualifications constrain production.
- Physical carriers become necessary if maintenance and fleet composition matter.
- Detailed contracts become necessary if breach, negotiation, or legal enforcement matters.
- Market order books become necessary if price formation itself is under study.

Until then, those concepts are outside the model boundary.

---

# 23. Resulting economic grammar

The bounded minimum can be summarized as:

> Legal entities own resources and financial instruments. Household cohorts supply labor and consume. Firms operate activities that transform resources. Regions constrain residence, production, and movement. Shares define ownership. Loans define existing debt. Credit defines borrowing capacity. Payment obligations introduce time. Liquidity determines whether promises can be honored. States alter flows through policy. Insurance redistributes specified risks.

This is small enough to remain understandable, but complete enough to model economic causality rather than merely applying bonuses and penalties.

Its rigor comes from preserving the relationships that cannot be removed without changing the meaning of the economy:

```text
ownership
location
capacity
transformation
payment
time
risk
claims
liquidity
```

Everything else is optional detail.
