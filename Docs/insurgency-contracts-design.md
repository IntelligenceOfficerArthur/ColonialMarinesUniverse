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
loop. Rules for every contract: no target is a specific person, and each is built from existing
objective types in `Content.CMU/Shared/Round/Objectives/Type` (Fetch, Interact, Destroy, Capture,
Hotspot), items, zones and the Sapper tripwire.

Heat is how loud the job is: it shortens the GOVFOR detection delay and raises the reward.

### 1. Arms skim (Heat: medium)
- **Basis:** Mau Mau and others armed themselves from stolen government arms.
- **Job:** Take a weapons crate from a marked depot and get it back to the cell.
- **Twist:** The crate is heavy and slows whoever carries it, so CLF has to choose a route and a carrier.
- **GOVFOR counter:** Guard the depot, or catch the carrier while slowed.

### 2. Airdrop recovery (Heat: high)
- **Basis:** Sten and FP-45 weapons were designed to be airdropped to resistance groups.
- **Job:** CLF lights a beacon. After a delay a crate drops at a marked zone; recover it.
- **Twist:** The flare is visible to everyone, so the drop is a race. Once it lands, both sides know where.
- **GOVFOR counter:** Reach the zone first.

### 3. Cargo smuggle (Heat: low, slow)
- **Basis:** Ho Chi Minh Trail logistics and FLN financing networks moved goods through ordinary channels.
- **Job:** Get contraband into the colony ASRS (cargo shipping) unflagged.
- **Twist:** The longer the crate sits unsent, the higher the chance it gets inspected.
- **GOVFOR counter:** Inspect and seize crates.

### 4. Courier run (Heat: low)
- **Basis:** Viet Cong cells were linked by couriers on separate, compartmentalized routes.
- **Job:** Carry a package to a drop point, choosing one of three routes.
- **Twist:** Each route trades speed for risk. A package on a slow, quiet route earns a bonus.
- **GOVFOR counter:** Patrol the likely routes and search anyone carrying a package.

### 5. Grid sabotage (Heat: high)
- **Basis:** The French Resistance sabotaged power, transport and telecoms.
- **Job:** Disable a power or comms node in the colony.
- **Twist:** The blackout is also a distraction. Other contracts get a shorter detection delay while it lasts.
- **GOVFOR counter:** Defend and repair the node.

### 6. Clandestine radio (Heat: grows over time)
- **Basis:** Resistance radio operators transmitted from hidden sets under code names.
- **Job:** Set up a transmitter and keep it broadcasting for a set time.
- **Twist:** The signal gets easier to locate the longer it runs, so CLF can cut it early for a smaller payout.
- **GOVFOR counter:** Triangulate and raid it.

### 7. Propaganda run (Heat: low)
- **Basis:** Printing and distributing propaganda is a classic first phase of an insurgency.
- **Job:** Post material at several marked sites around the colony.
- **Twist:** Each poster that stays up pays a trickle of points. Posters torn down stop paying.
- **GOVFOR counter:** Tear them down and watch the sites.

### 8. Supply cache (Heat: low)
- **Basis:** Viet Cong hid weapons and food in caches across the countryside, moved by civilians.
- **Job:** Collect supplies and stash them at marked hiding spots.
- **Twist:** A hidden cache can be drawn on later for a one-time vendor discount, but it can be found.
- **GOVFOR counter:** Find and dig up the cache.

### 9. Booby-trap the road (Heat: medium)
- **Basis:** Viet Cong booby traps and tripwires slowed and bled patrols.
- **Job:** Set up tripwires at marked choke points, using the existing Sapper tripwire.
- **Twist:** Points accrue each time a trap is triggered, not only on setup.
- **GOVFOR counter:** Clear the traps and scout the choke points.

### 10. Safe house (Heat: medium)
- **Basis:** The Cu Chi tunnels served as hidden fighting bases.
- **Job:** Establish a hideout and hold it for a set time.
- **Twist:** While it stands, CLF members can heal and resupply there, so holding it is worth defending.
- **GOVFOR counter:** Find it and clear it.

Sources and gaps: the web search supported the general tactics (stolen arms, airdrops, couriers,
sabotage, radio, tunnels). It did not give direct detail on dead drops, forged documents, or prisoner
rescue, so those are not claimed above.

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
