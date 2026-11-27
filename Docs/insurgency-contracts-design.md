# Insurgency Intel Rework: CLF Contracts (design proposal)

Status: proposal, no code written yet. Needs sign-off on the open questions at the bottom.

## Problem

Today CLF earns intel points by submitting money, documents and data at the Analyzer
(`IntelSystem.CMUAnalyzer.cs`, `FactionEconomy` in `FactionDefinition.cs`). Money converts at a flat
rate into the cell's shared pool, and vendors spend it. It is a passive submit-and-wait loop, and
GOVFOR has no part in it.

## Goal

CLF gets a **Contracts console**. CLF picks a contract and starts it. The start is a deliberate
risk: **GOVFOR receives a notification after a delay**. That gives both sides a clock to play
against.

## Core loop

1. A CLF member opens the console and sees a rotating board of 3 to 4 offered contracts. Each shows
   its reward, risk and time limit.
2. CLF accepts one. It becomes active, and only one or two can be active at once.
3. **Detection delay.** After a delay GOVFOR gets a radio and console alert. The delay depends on
   the contract and its Heat (see Risk). Detail level also depends on how long the contract has run.
   - Early: "suspicious activity".
   - Late: a rough area or an objective type.
4. CLF completes the contract before the timer runs out. GOVFOR tries to find it and shut it down.
5. Rewards pay out as intel points. The existing Analyzer path stays, with a reduced role.

## Contract types

Smuggling is the first one you asked for. The rest reuse existing objective components in
`Content.CMU/Shared/Round/Objectives/Type` (Fetch, Interact, Destroy, Capture, Hotspot):

| Contract | Task | GOVFOR counterplay |
|---|---|---|
| Cargo smuggle | Get contraband crates into the colony ASRS, or onto an outbound shipment, unflagged | Inspect and seize crates, and catch whoever sends them |
| Dead drop | Deliver an item to a marked location | Stake out the drop location after the late alert |
| Signal jam / sabotage | Interact with or destroy a target | Defend or repair it |
| Data theft | Fetch a disk from a site and bring it to the Analyzer | Intercept the carrier |
| Hold the point | Capture and hold a zone (Hotspot) | Contest the zone |

## Risk and Heat

- Each contract has a **Heat** value. Higher Heat means a shorter detection delay and a more
  detailed alert, and also a bigger reward.
- Failure or expiry costs CLF points, or locks the board for a while. A GOVFOR counter-win pays GOVFOR
  objective points.
- This keeps risk and reward tied together: CLF chooses between slow and safe or fast and loud.

## GOVFOR engagement

- It is not only a notification. A shutdown pays GOVFOR objective points, which gives them a reason
  to respond.
- The alert goes to the existing GOVFOR objectives console and radio.
- Optional later: GOVFOR gets a small "counter-intel" board of its own, such as seize contraband.

## Implementation sketch

Mostly reuse of what exists:

- **Console**: copy `ObjectivesConsoleComponent` and `ObjectiveConsoleSystem` (CMU/Server/Round/Objectives)
  as a CLF Contracts console. This is the same UI plumbing with a faction filter.
- **Contracts**: new prototype type, or extend `CMUObjectiveComponent`, with `Reward`, `Heat`,
  `TimeLimit` and `DetectionDelay`. Completion reuses the existing objective type systems.
- **Detection**: a timer on the active contract that fires a GOVFOR alert event. This is new code.
- **Smuggling**: hook the ASRS export path (`RequisitionsSystem.*` in CMU/Server/Requisitions).
  Mark the contraband crate and detect it on delivery. I have not read this code in detail yet.
- **Rewards**: add to the cell's tech tree points via `IntelSystem.AddPoints`, same as the Analyzer.
- **Faction editor**: add contract toggles and reward scaling to `FactionEconomy`, so custom
  factions can tune or disable them.
- **Tests**: integration tests in `Content.IntegrationTests/_CMU14/Insurgency`, covering contract
  completion, detection timing and failure.

## Open questions

1. **Analyzer**: keep submitting money as a smaller secondary income, or remove it entirely?
2. **Detection**: should GOVFOR get an exact location eventually, or only a vague area? An exact
   location is better for GOVFOR but may make CLF's job too hard.
3. **Smuggling**: how should the cargo contract work? For example, CLF orders a real crate and GOVFOR
   can inspect it, or CLF slips contraband into the colony's own shipments.
4. **Scope of v1**: just smuggling plus the alert, or smuggling plus two or three other contract
   types?
5. **Failure penalty**: lose points, or only miss the reward and lock the board?
