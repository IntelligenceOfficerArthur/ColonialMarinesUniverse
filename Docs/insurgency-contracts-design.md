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

## Contract types (10 new objectives)

The Analyzer's money submission is replaced by this pool of 10 jobs, so income is not one repeated
loop. These are designed fresh and may need new content (marked "New"). Rules for every contract: no
target is a specific person, and each is a place, an item, a device or a meter.

Heat is how loud the job is: it shortens the GOVFOR detection delay and raises the reward.

### 1. Pirate broadcast (Heat: high)
- **Basis:** Insurgents used radio and propaganda to build support.
- **Job:** Tap the colony intercom and air a CLF message.
- **New:** A broadcast hijack device that plays across all colony intercoms.
- **Twist:** The longer the message plays, the more points, but the source gets easier to trace.
- **Counter:** Find and cut the device.

### 2. Black market stall (Heat: low)
- **Job:** Set up a hidden stall that sells to colonists and earns points per sale.
- **New:** A stall device and a colonist purchase loop.
- **Twist:** Better goods sell more but raise Heat.
- **Counter:** Police find and raid the stall.

### 3. Forged papers (Heat: medium)
- **Job:** Produce forged colony permits at a hidden bench and deliver them to a drop.
- **New:** A forgery bench and a permit item.
- **Twist:** A delivered permit lets CLF pass one checkpoint unsearched.
- **Counter:** Police check permits against the colony record.

### 4. Tunnel dig (Heat: low, slow)
- **Basis:** The Cu Chi tunnels served as hidden fighting bases.
- **Job:** Dig a hidden tunnel linking two points inside the colony.
- **New:** Tunnel entrances that connect two places for CLF only.
- **Twist:** A finished tunnel is a permanent shortcut, but a discovered one collapses.
- **Counter:** Spot the entrances and seal them.

### 5. Diversion (Heat: high)
- **Job:** Set off decoy alarms and fake fires across the colony to pull police and GOVFOR away.
- **New:** A decoy device that triggers a fake alert.
- **Twist:** Lasts only as long as responders believe the alerts, and each used decoy makes the next less convincing.
- **Counter:** Verify before responding.

### 6. Evidence raid (Heat: high)
- **Job:** Break into the police evidence locker and recover seized contraband.
- **New:** An evidence locker that holds items seized from CLF.
- **Twist:** It holds whatever police actually seized, so the reward depends on how well they have been doing.
- **Counter:** Guard the locker.

### 7. Wiretap (Heat: low)
- **Basis:** Intelligence collection is a standard underground function.
- **Job:** Plant a bug in a police or GOVFOR comms point and let it listen.
- **New:** A bug device that relays intercepted chatter to CLF.
- **Twist:** While it runs, CLF gets early warning of GOVFOR alerts, so detection delay on other contracts grows.
- **Counter:** Sweep for bugs.

### 8. Toll road (Heat: medium)
- **Basis:** The FLN taxed supporters to fund the fight.
- **Job:** Set up a toll point on a route that collects from passing traffic.
- **New:** A toll barrier that takes a cut from vehicles and people passing through.
- **Twist:** Higher tolls pay more but make people avoid the route.
- **Counter:** Clear the barrier, or reroute traffic.

### 9. General strike (Heat: medium)
- **Job:** Shut down colony workplaces by cutting their breaker boxes, until a number of sites stay dark for a set time.
- **New:** A strike counter per site.
- **Twist:** Each shut site costs the colony income, which hurts GOVFOR indirectly.
- **Counter:** Restore the breakers.

### 10. Observation post (Heat: low)
- **Job:** Hide a lookout at marked vantage points.
- **New:** A hidden post device that shows nearby movement to CLF.
- **Twist:** Several posts give a growing map of patrol routes.
- **Counter:** Find and remove the posts.

Sources and gaps: the web search supported insurgent radio and propaganda, intelligence collection, taxation and tunnels. Black markets, forged papers, decoy diversions and strikes were not covered by the search, so their historical basis is not claimed.

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
