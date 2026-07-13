# Interaction Runtime Contract

## Authority

This document is authoritative for explicit interaction intent, eligibility, deterministic target selection, interaction resolution, `BeginInteractionCommand`, and `interaction.started`.

It does not define downstream dialogue, inventory, quest, harvesting, combat, or other interaction-domain behavior.

## Interaction capability

Stable component ID:

```text
component.interactable
```

Required semantic values:

- stable interaction kind;
- finite non-negative range;
- allowed interactor tags;
- optional required interactor component type IDs.

Initial interaction kind:

```text
interaction.talk
```

The interaction kind is routing data, not an arbitrary callback.

## Interaction intent

Required intent:

```text
InteractIntent
```

Fields:

- stable intent ID;
- interactor entity ID;
- optional explicit target entity ID;
- optional requested interaction kind;
- behavior assignment ID;
- deterministic ordering key.

Behavior code emits the intent and does not mutate interaction state.

## Eligibility

A target is eligible when:

- target exists;
- target has `component.interactable`;
- interactor and target have compatible world/spatial membership;
- distance is within target interaction range;
- interactor satisfies required tags/components;
- requested interaction kind matches when provided.

Line of sight and facing are not considered.

## Target selection

Selection order:

1. valid explicit target when supplied;
2. otherwise nearest eligible target;
3. entity ID ordinal tie-break.

Candidate collection uses the radius query contract.

## Resolution

Accepted resolution contains:

```text
BeginInteractionCommand
```

The command records participants and interaction kind, then emits:

```text
interaction.started
```

Required event payload:

- interaction kind;
- interactor entity ID;
- target entity ID;
- intent ID;
- tick.

No downstream domain execution occurs.

## Rejection reasons

Required stable reasons:

```text
interactor-not-found
interactor-spatial-state-missing
explicit-target-not-found
explicit-target-ineligible
no-eligible-target-in-range
interaction-kind-mismatch
```

Rejected interaction emits no `interaction.started`.

Rejection is a normal domain outcome.

## Determinism

Equivalent snapshot, intent, and query results must produce equivalent candidates, distances, filter outcomes, selected target, selection reason, command, event, rejection reason, and diagnostics.
