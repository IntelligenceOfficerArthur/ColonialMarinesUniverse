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

## Contract types (10 CLF-only objectives)

The Analyzer's money submission is replaced by this pool of 10 jobs, so income is not one repeated
loop. Every job is something CLF does by itself, with CLF's own people and CLF's own gear. None needs
police, GOVFOR or colonists to take part, and none target a specific person. GOVFOR only receives
the delayed alert. New content is marked "New".

Heat is how loud the job is: it shortens the GOVFOR detection delay and raises the reward.

### 1. Hidden workshop (Heat: low)
- **Basis:** Cheap weapons such as the Sten were built to be made in bulk and passed to resistance groups.
- **Job:** Set up a workshop and assemble a set number of improvised weapons or ammo boxes.
- **New:** A workshop bench and the parts it consumes.
- **Twist:** The output goes into the cell stockpile and feeds the vendors.

### 2. Airdrop recovery (Heat: high)
- **Basis:** Sten and FP-45 weapons were designed to be airdropped to resistance groups.
- **Job:** Light a beacon. After a delay a crate drops at a marked zone; recover it.
- **New:** A beacon and the drop crate.
- **Twist:** The flare is visible to everyone, so it is a race against the alert.

### 3. Tunnel dig (Heat: low, slow)
- **Basis:** The Cu Chi tunnels served as hidden fighting bases.
- **Job:** Dig a hidden tunnel linking two points.
- **New:** Tunnel entrances that connect two places for CLF only.
- **Twist:** A finished tunnel is a permanent shortcut for the cell.

### 4. Supply cache (Heat: low)
- **Basis:** Viet Cong hid weapons and food in caches across the countryside.
- **Job:** Collect supplies and bury them at marked hiding spots.
- **New:** A cache item with a one-time vendor discount.
- **Twist:** The more caches stand at the end of the round, the bigger the bonus.

### 5. Pirate broadcast (Heat: high)
- **Basis:** Insurgents used radio and propaganda to build support.
- **Job:** Build a transmitter and air a CLF message on a loop.
- **New:** A transmitter device.
- **Twist:** More points the longer it plays, but it gets easier to trace.

### 6. Booby-trap the road (Heat: medium)
- **Basis:** Viet Cong booby traps and tripwires slowed and bled patrols.
- **Job:** Set tripwires at marked choke points, using the existing Sapper tripwire.
- **Twist:** Points accrue each time a trap is triggered, not only on setup.

### 7. Safe house (Heat: medium)
- **Basis:** The Cu Chi tunnels served as hidden fighting bases.
- **Job:** Establish a hideout and hold it for a set time.
- **New:** A hideout marker with a healing and resupply point.
- **Twist:** While it stands, CLF members can heal and resupply there.

### 8. Observation post (Heat: low)
- **Job:** Hide lookouts at marked vantage points.
- **New:** A hidden post device that shows nearby movement to CLF.
- **Twist:** Several posts build a growing map of the area.

### 9. Off-grid generator (Heat: medium)
- **Job:** Build and fuel a hidden generator to power the cell's gear.
- **New:** A generator and fuel item.
- **Twist:** Keeping it running pays a trickle of points, and the cell's gear keeps working through any blackout.

### 10. Courier run (Heat: low)
- **Basis:** Viet Cong cells were linked by couriers on separate, compartmentalized routes.
- **Job:** Carry a sealed package between two CLF drop points, choosing one of three routes.
- **New:** A sealed package and two drop points.
- **Twist:** Each route trades speed for risk. A slow, quiet delivery earns a bonus.

Sources and gaps: the web search supported cheap airdropped weapons, propaganda and radio, couriers, tunnels and tripwires. Workshops, generators and observation posts were not covered by the search, so their historical basis is not claimed.

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
