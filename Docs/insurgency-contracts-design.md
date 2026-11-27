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

## Contract types (10 jobs, themed on historical insurgencies)

The Analyzer's money submission is replaced by this pool of 10 jobs, so income is not one repeated
loop. Each maps to an existing objective type in `Content.CMU/Shared/Round/Objectives/Type`.

| # | Contract | Historical basis | Task | Type | GOVFOR counterplay |
|---|---|---|---|---|---|
| 1 | Arms skim | Mau Mau and others armed themselves from stolen government arms | Steal a weapons crate from a depot | Fetch | Guard or audit the depot |
| 2 | Airdrop recovery | Sten and FP-45 weapons were designed to be airdropped to resistance | Reach a marked drop zone and recover the crate | Fetch + Hotspot | Race CLF to the zone |
| 3 | Cargo smuggle | Ho Chi Minh Trail logistics, FLN financing networks | Get contraband into the colony ASRS unflagged | Fetch + Interact | Inspect and seize crates |
| 4 | Courier run | Viet Cong cells linked by couriers on separate, compartmentalized routes | Carry a package to a drop through one of several routes | Fetch + Interact | Patrol routes, search carriers |
| 5 | Grid sabotage | French Resistance sabotaged power, transport and telecoms | Disable a power or comms node | Destroy | Defend and repair it |
| 6 | Clandestine radio | Resistance "pianists" transmitted from hidden sets under code names | Set up a transmitter and keep it broadcasting | Interact + Hotspot | Triangulate and raid it |
| 7 | Propaganda run | Printing and distributing propaganda is a classic insurgent first phase | Post material at several sites | Interact | Tear it down, watch the sites |
| 8 | Escape line | Resistance escape networks moved people behind enemy lines | Extract a wanted person to a safe house | Rescue survivor | Intercept the convoy |
| 9 | Levy | The FLN taxed supporters to fund the fight | Collect funds from colony ATMs or businesses | Interact (ties into Sapper) | Protect ATMs, hunt the collectors |
| 10 | Safe house | Cu Chi tunnels served as hidden fighting bases | Establish and hold a hideout | Capture / Hotspot | Find and clear it |

Sources and gaps: the web search supported the general tactics (stolen arms, airdrops, couriers,
sabotage, escape lines, radio, taxation, tunnels). It did not give direct detail on dead drops,
forged documents, or prisoner rescue, so those are not claimed above.

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
