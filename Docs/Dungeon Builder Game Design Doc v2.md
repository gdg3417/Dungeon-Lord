# Section 1

**High level concept**

Here is a revised and expanded version that provides clarity for publishers and developers while keeping your core fantasy centered.

**Working title**

Dungeon Builder

**Genre**

Idle dungeon management\
Base builder\
Light simulation strategy\
Fantasy economy sim

**Platform**

Mobile phones\
Portrait mode\
Optional cloud save for cross device play (later release)

**Game fantasy**

You are a living dungeon core. You pull mana from the world and shape a multi floor labyrinth. You breed monsters, craft rare loot, influence adventurers, and navigate political risk. Your dungeon becomes a living ecosystem that evolves over weeks and months.

**Core identity**

Dungeon Builder is not a clone of existing idle games. Its identity comes from

- player driven layout

- deep monster lineages

- huge loot research tree

- evolving world relationships

- long term strategy choices

**One sentence pitch**

Grow a living dungeon ecosystem, attract adventurers, and master the balance between danger, loot, and political pressure.

**Session style**

Short sessions of one to three minutes\
Long term progression through idle cycles\
Return sessions to make big choices every few hours

**Design intent**

This game aims to deliver

- the satisfaction of building

- the drama of dungeon raids

- the long term growth of prestige systems

- the creativity of designing your own monster ecosystem

- the joy of being the villain but also the world’s economy engine

If you want, the end of this section can also include a “Why this game” justification for pitching to investors.

------------------------------------------------------------------------

# Section 2

**Design pillars**

We will deepen each pillar with developer guidance, examples, and constraints.

## Pillar one

Your dungeon your way

**Meaning**\
Players should have control over

- layout

- monster selection

- monster behavior

- loot profile

- trap placement

- floor themes

- difficulty tuning

**Design constraint**\
Never make one layout or monster build the “correct” one. Multiple viable playstyles must exist.

**Example implementation**\
A player focused on loot crafting might build a safe dungeon that attracts merchants.\
A player focused on mana might build a brutal labyrinth that only elite adventurers attempt.

------------------------------------------------------------------------

## Pillar two

Smart idle depth

**Meaning**\
Idle rewards should always depend on how smartly the player designed their dungeon.

**Design constraint**\
The more optimized the dungeon, the better the passive mana per hour.

**Example**\
A maze layout with trap synergy generates more mana because adventurers use more skills inside it.\
A straight corridor dungeon generates less skill spill.

------------------------------------------------------------------------

## Pillar three

Risk versus reputation

**Meaning**\
Killing adventurers is profitable but dangerous. Letting them live is safe but slower.

**Design constraint**\
Heat must always be a pressure system that forces decisions.\
No long term no consequence farming.

------------------------------------------------------------------------

## Pillar four

Living world illusion

**Meaning**\
The world should feel alive, even if the simulation is abstract.

**Examples**

- Traders arrive when you craft exotic goods

- Mage guilds seek rare spell scrolls

- Noble families complain if their heirs die

- Kingdoms raid if heat rises

- Seasonal events change adventurer availability

------------------------------------------------------------------------

# Section 3

**Target audience and comps**

Let us define audience with more actionable clarity.

**Audience**

Primary audience

- idle game fans

- management and builder fans

- mobile fantasy economy fans

Secondary audience

- Dungeons and Dragons and LitRPG fans

- Diablo and Dungeon Keeper nostalgia players

- People who like long term progression loops

**Comparable games**

Not copying, just mechanical inspirations

- Dungeon Keeper for theme

- Idle Miner Tycoon for idle structure

- AFK Arena for long term progression pacing

- Fallout Shelter for layout management

- Dungeon Village for adventurer attraction loop

These comps help dev teams understand your vision quickly.

------------------------------------------------------------------------

# Section 4

**Core loops**

Here we deepen each loop to show optional feedback cycles.

## 4.1 Short loop expanded

Players in each session\
1 Collect mana\
2 Spend mana on layout or monster tuning\
3 Check adventurer logs\
4 Make small adjustments to optimize mana\
5 Trigger a manual challenge or wave if desired\
6 Close game and let mana accumulate

**Feedback clarity**\
Whenever a player makes a change, they should immediately see

- mana per hour effect

- heat change

- adventurer traffic prediction

This makes each small session meaningful.

------------------------------------------------------------------------

## 4.2 Long loop expanded

The long loop defines progression over days and weeks of real time.

Players over longer timeframes\
1 Unlock new floor styles and dungeon themes\
2 Expand the number of floors and overall dungeon complexity\
3 Unlock additional monster evolutions and behavior traits\
4 Develop advanced loot trees and craft rare items\
5 Create and optimize sub dungeons\
6 Navigate political heat, kingdom negotiations, and raid events\
7 Participate in seasonal events, global competitions, and leaderboard cycles

**Not included in initial build**\
Prestige cycles or full dungeon resets will not be part of the launch version. These may be added in later expansions after player progression pacing has been validated.

**Prestige idea**\
Prestige resets part of the dungeon to gain a permanent boon such as

- mana multiplier

- increase in adventurer traffic

- special cosmetic dungeon cores\
  We can design that later if you like.

------------------------------------------------------------------------

# Section 5

**Progression overview expanded**

Make this section more actionable for pacing and developer planning.

**Early game**

Duration\
First zero to four hours of play

Unlocks

- Starter monster family

- Basic traps

- Bronze tier loot

- Floor one

- Early research nodes

Goals

- Teach mana flow

- Teach layout basics

- Give players agency early without overwhelming them

------------------------------------------------------------------------

**Mid game**

Duration\
Day one through week one

Unlocks

- Multiple floors

- Monster evolution branches

- Steel and mithril tier loot

- Sub dungeons

- Heat management tools

- Elemental themes

- Dungeon identity specialization

Goals

- Show players new paths to specialize their dungeon

- Introduce political consequences

- Prepare players for raids and seasonal content

**Late game (initial release)**

Duration\
Week one through month two or three

Unlocks

- Adamantine and orichalcum loot

- Boss set crafting

- Multi family monster synergy

- Advanced traps and environmental hazards

- Regular kingdom raids

- Dungeon versus dungeon seasonal ladders

Goals

- Deep strategic consideration

- High difficulty and high reward cycles

- Player driven unique dungeon identities

**End game (planned later update)**

Not included in initial release but intended for major expansions

- New monster families such as dragons, giants, demons, constructs, elementals

- Hero adventurers with named personalities and long term relationships

- Expanded adventurer economy including trade caravans and item markets

- Advanced world simulation such as faction politics, shifting borders, or guild wars

- Possibly prestige mechanic that resets parts of the dungeon for permanent bonuses

- Expanded AI behavior including personal rivalries and dungeon allegiance

- Full end game loot cycles including meteorite tier and cosmic enchantments

This creates a clear roadmap for the long term vision of the game.

# 6. Systems design (expanded)

This section defines every major system in the game using

- logical rules

- state changes

- player actions

- behind the scenes processes

We will rebuild each subsection with more detail, clarity, and technical depth.

------------------------------------------------------------------------

## 6.1 Mana system (deep expansion)

**Purpose**

Mana is the core resource that powers all progression. It must feel

- scarce early

- abundant mid game

- strategic late game

Every major action has a mana cost so that players constantly make tradeoffs.

------------------------------------------------------------------------

**Mana generation sources**

**Core passive output**

The dungeon core generates mana over time.\
Output formula is based on

- core level

- total dungeon rating

- heat level (some stages reduce or increase efficiency)

- dungeon stability

Early game\
Low base rate to encourage thoughtful planning.

Mid game\
Rate increases significantly as floors are added.

Late game\
Rate is influenced heavily by adventurer activity and monster synergy.

------------------------------------------------------------------------

**Adventurer deaths**

One of the largest active sources of mana.\
High rank adventurers yield significantly more mana.

Danger\
Killing too many adventurers increases heat.

------------------------------------------------------------------------

**Adventurer skill spill**

Whenever adventurers use combat abilities

- mana leaks into the dungeon

- monsters can absorb some spill

- the core absorbs the rest over time

This rewards

- longer dungeons

- trap synergy builds

- maze floors where adventurers use many abilities

------------------------------------------------------------------------

**Monster deaths**

Provides small amounts of mana.\
Useful

- early when few adventurers visit

- in sub dungeons designed as mana farms

------------------------------------------------------------------------

**Event boosts**

Seasonal or world events such as

- mana storm

- elemental convergence

- guild sponsorship\
  temporarily increase mana gain.

------------------------------------------------------------------------

**Mana sinks**

Mana sinks maintain long term engagement by giving players constant goals.

Mana is spent on

- floor expansion

- trap placement and upgrades

- monster summoning and leveling

- monster evolution research

- loot research

- crafting of rare items

- sub dungeon creation

- diplomacy actions

- core upgrades

------------------------------------------------------------------------

**Mana flow design principles**

This is the design philosophy for your economy.

**Scarcity phase**

Early game should force the player to choose between

- expanding the floor

- upgrading one monster

- investing in research

**Stability phase**

Mid game mana should flow steadily so

- players feel rewarded

- new monster families become realistic investments

**Surplus phase**

Late game mana allows

- big decisions

- boss gear crafting

- sub dungeon specialization

------------------------------------------------------------------------

**Idle versus active balance**

Mana gains should feel

- stable and automated through idle

- dramatically improved through player optimization

Example difference\
A poorly designed dungeon might make ten mana per minute.\
A well designed one using traps and synergy might make thirty or forty per minute.

## Updated 6.2 Dungeon structure system

Here is a refined version based on your change.

**Floor structure revision**

Floor level will **not** limit monster summon levels.

Floor level instead becomes a **soft thematic indicator**, not a mechanical limit. This gives players total freedom to create:

- A floor one death zone with level ninety monsters

- A peaceful early floor training ground with level one to twenty monsters

- A themed sub dungeon filled only with weak creatures

- A late floor that is easier than earlier floors for strategic baiting

This supports the fantasy you want\
Players can build the dungeon their way, without artificial level gating.

------------------------------------------------------------------------

**Updated roles of floor level**

Floor level now impacts:

- Dungeon rating calculation

- Adventurer expectation of difficulty

- Loot table expectations

- Heat calculation modifiers

- Environmental hazard strength

- World narrative references

Floor level does **not** modify monster maximum level.

Players who want insane difficulty on floor one can absolutely do so and will receive:

- Higher mana from kills

- Higher heat penalty

- Higher risk of rare heroes showing up to punish imbalance

Players who want a relaxed training dungeon can maintain:

- Lower heat

- Higher adventurer traffic

- Lower average mana per adventurer

This preserves design flexibility.

## 6.3 Monster system (expanded)

This section is already strong. Now we expand it into developer ready specificity.

------------------------------------------------------------------------

**Monster groups**

Each group has

- one common unit

- two evolution branches early

- four to six mid game evolutions

- one mid boss

- one late boss

Groups released at launch

- Undead

- Goblinoid

Groups planned for early expansions

- Kobolds

- Orcs

- Beasts

Later expansions

- Minotaurs

- Trolls

- Giants

- Dragons

- Constructs

- Demons

- Elementals

------------------------------------------------------------------------

**Monster evolution tree format**

Each monster species includes

**Base level**

Minimum summon level\
Indicates tier position

**Growth curve**

Scaling for

- health

- attack

- armor

- resistance

- special ability damage

**Mana summon cost**

Base cost plus multiplier for each level above base.

**Traits**

Each species has two to four traits

- regeneration

- split swarm

- stealth

- anti magic

- charge attack

- elemental affinity

**Active ability (optional)**

Higher tier monsters may have

- cleave

- fireball

- shield bash

- terror shout

**Behavior template**

Each species has default behavior

- cautious

- aggressive

- ambusher

- sentinel

Players can tweak these in monster behavior settings.

------------------------------------------------------------------------

**Monster synergy rules**

Monsters provide synergy bonuses when grouped\
Examples

- goblins get pack tactics

- undead get aura of decay

- kobolds improve trap efficiency

- minotaurs create shockwave zones

These encourage themed floors.

------------------------------------------------------------------------

**Boss units**

Bosses have

- unique ability sets

- special loot tables

- flavor text

- long research chains

- high mana cost

Boss fights create

- spikes in mana gain

- spikes in heat

## 6.4 Loot system

This section defines every component of loot design, including generation, research, rarity, economy impact, player control, and adventurer reactions.

Loot is one of the core pillars of your game because it influences

- adventurer attraction

- dungeon reputation

- dungeon heat

- dungeon identity

- world economy

Loot in Dungeon Builder is not just treasure. It is a living economy that shapes the world.

------------------------------------------------------------------------

### Loot types expanded

Loot belongs to one of several families. Each family has tiers, rarity weights, and economic impact.

#### Materials

Includes

- copper

- tin

- iron

- silver

- gold

- mithril

- adamantine

- orichalcum

- meteorite

- monster parts such as fangs, bones, claws

- refined monster essences

Uses

- crafting

- research

- enchanting

- merchant contracts

------------------------------------------------------------------------

#### Consumables

Includes

- food

- alcohol

- healing potions

- stamina draughts

- antidotes

- elemental resistance elixirs

- movement elixirs

- experience boosting elixirs

Consumables are high demand because adventurers rely on these for survival.

------------------------------------------------------------------------

#### Currency

Includes

- local kingdom coinage

- dungeon minted tokens

- rare collector coins dropped only by boss events

Currency has a simple mana replacement cost and medium impact on adventurer attraction.

------------------------------------------------------------------------

#### Utility items

Includes

- rope

- torches

- climbing gear

- backpacks

- tents

- basic magical utilities such as warm tents or everbright lanterns

- multi function magical tools

Utility items bring specialized visitors such as

- explorers

- scouts

- merchants

- caravan guards

------------------------------------------------------------------------

#### Gear and equipment

This is the most important category for attracting adventurers.

Includes

- primitive weapons such as bronze daggers

- iron weapons

- steel weapons and armor

- mithril weapons and armor

- adamantine heavy gear

- orichalcum resonance gear

- meteorite tier gear for end game

Gear can have

- one to three enchantment slots

- base stat scaling

- class tags that influence which adventurers seek it

------------------------------------------------------------------------

#### Spell scrolls

Scrolls include

- elemental spells

- protective spells

- support spells

- unique scrolls derived from adventurer drops

Scrolls strongly attract mages and scholars.

------------------------------------------------------------------------

#### Boss sets

Each monster family yields a unique themed set.\
The dungeon lord can customize them through research.

Boss sets create

- huge adventurer traffic

- prestige reputation

- mid to late game monetization interest

Examples

- goblin trickster set

- skeleton knight set

- orc warlord set

- minotaur guardian set

- dragon sovereign set

Each set has

- mandatory stats

- optional custom stats

- two piece bonus

- four piece bonus

------------------------------------------------------------------------

### Loot progression

Loot cannot be created without research. This ensures long term pacing.

Progression path\
1 absorb an item dropped by an adventurer\
2 analyze the item to unlock its blueprint\
3 research the blueprint\
4 add it to the loot pool at a mana cost\
5 optionally research enchantment variants\
6 manage loot table probabilities

------------------------------------------------------------------------

### Loot table control

Dungeon lords can set loot tables manually or select from presets.

Presets

- standard

- high profit

- prestige

- exotic trade

- boss run focused

- adventurer training focused

Each preset influences traffic patterns.

------------------------------------------------------------------------

### Loot rarity and weighting

Items have rarity classes

- common

- uncommon

- rare

- elite

- epic

- legendary

- relic tier

Drop weights are influenced by

- floor level

- dungeon difficulty

- dungeon theme

- adventurer class mix

- research unlocks

------------------------------------------------------------------------

### Loot and heat

Heat is affected by loot fairness.

Low loot versus high danger

- high heat

- noble anger

- adventurer complaints

High loot versus moderate danger

- low heat

- guild support

- merchant investment

Loot is a safety valve that helps players manage heat without weakening their monsters.

## 6.5 Adventurer and AI system

This section defines exactly how adventurers behave, how they evaluate your dungeon, how they fight, and how they influence the world.

Adventurers are the heart of the dungeon ecosystem. Their variety, motivations, and behaviors make the world feel alive.

### Adventurer classes

Adventurer classes represent distinct skill sets, behaviors, and loot preferences.

**Launch classes**

These appear from day one

- warrior

- rogue

- mage

- cleric

- ranger

These five cover the basic fantasy triangle of offense, defense, and support.

------------------------------------------------------------------------

### Future classes

These classes are intended for expansions. Many of them reflect advanced magic schools, hybrid combat styles, or specialized summoning disciplines.

**Summoner classes**

Each focuses on a different monster source

- summoner monster type

- summoner demon type

- summoner elemental type

Summoners bring extra creatures into the dungeon. Their summons have their own threat values and affect heat and mana differently.

**Martial and hybrid classes**

- monk

- paladin

- barbarian

- knight

- spellsword

- assassin

These classes have strong reactions to dungeon layouts. For example

- monks prefer light challenges and dislike overuse of traps

- paladins seek undead heavy dungeons

- assassins prefer darkness and ambush opportunities

**Magic classes**

- shaman

- warlock

- red mage

- black mage

- elemental sorcerer

- wizard single school

- wizard multi school

Magic users care heavily about

- spell scrolls

- arcane gear

- magical traps

- elemental themed floors

A single school wizard is specialized such as fire or frost\
A multi school wizard is versatile but less efficient in each school

**Support and crafting focused classes**

- bard

- artificer

- alchemist

- witch hunter

- druid

- elemental knight

These support roles add complexity to party behavior because they bring unique buffs or counters

- witch hunters excel against demons and undead

- druids excel in nature themed floors

- elemental knights chase high affinity gear

- alchemists seek rare materials for potion crafting

------------------------------------------------------------------------

### Class property table

Each class has

- primary stat type

- secondary stat type

- armor preference

- weapon preference

- preferred loot

- preferred dungeon floor type

- disliked dungeon elements

- retreat threshold

- burst or sustain combat style

We can build a full numeric table later if you want.

------------------------------------------------------------------------

### Adventurer personality traits

These traits make adventurer behavior dynamic and unpredictable in fun ways.

Existing traits

- cautious

- reckless

- curious

- greedy

- glory seeking

- pragmatic

- vengeful

- heroic

- opportunistic

**New traits requested**

These add more depth and more economic behavior patterns.

- altruistic\
  These adventurers care about saving others. They are more willing to rescue fallen party members or retreat early to return with help.

- goal oriented\
  These adventurers ignore side loot and push deeper toward specific targets such as bosses or unique loot.

- gambler\
  High risk high reward behavior. They keep pushing even when danger is extreme, hoping for a rare payoff.

These traits interact with classes and personal goals to create unique dungeon runs.

------------------------------------------------------------------------

### Personality trait effects

Here are the mechanical effects of each new trait.

**Altruistic**

- Higher retreat chance when allies are low

- Will attempt rescue behaviors

- Will return to the dungeon sooner to retry

- Reputation bonus if they spread positive stories

**Goal oriented**

- They bypass optional rooms

- They follow the shortest path toward their objective

- They ignore most loot unless it matches their goal

- They have stable morale even when danger is high

**Gambler**

- They ignore retreat logic until very low health

- They open every loot chest

- They trigger extra traps due to risk taking

- They cause higher mana spill and danger spikes

------------------------------------------------------------------------

### Adventurer selection behavior

The expanded class roster and trait list influences how adventurers choose dungeons.

For example

- elemental sorcerers love elemental floors

- artificers chase magical utility items

- summoner demon type may avoid holy themed dungeons

- paladins aggressively target undead floors

- gamblers swarm high reward high danger dungeons

- goal oriented adventurers favor direct paths

- altruistic adventurers dislike lethal unfair dungeons

------------------------------------------------------------------------

### Party formation rules with expanded classes

Parties now include more complex compositions.

A typical mid game party might include

- one tank type such as knight

- one healer such as cleric

- one damage dealer such as black mage

- one support such as bard or artificer

- one specialist such as shaman or witch hunter

In late game

- summoners replace tanks

- spellswords replace rogues

- paladins and shamans act as hybrid defenders

- elemental sorcerers bring area control

Party synergy affects danger scoring and AI decision making.

------------------------------------------------------------------------

### In dungeon AI with expanded variety

The broader class roster creates more interesting AI behavior.

Examples

- summoners create extra damage zones

- bards buff morale so parties retreat less

- warlocks drain monster energy in unique ways

- assassins may attempt to bypass rooms

- spellswords may switch weapon or spell mid fight

- druids interact with environmental floors

- alchemists use consumables that cause secondary effects

These make dungeon runs feel alive.

------------------------------------------------------------------------

### End result

With this expanded adventurer roster and personality system

- adventurer traffic is diverse

- dungeon building decisions have long term impact

- loot crafting choices matter

- heat management becomes layered

- player stories emerge naturally

## 6.6 Political heat system

Heat is a pressure mechanic that ensures players stay balanced.

High heat means the kingdom sees your dungeon as a threat.\
Low heat means you are an asset to society.

------------------------------------------------------------------------

### Heat states

Five stages

**Peace**

Dungeon is stable and beneficial.\
Clerks and guilds love you.\
Merchant caravans come often.

**Notice**

Kingdom begins monitoring your activity.\
Guard patrols increase near your entrance.\
Nobles ask questions.

**Concern**

Complaints rise.\
Adventurer guild requests safety improvements.\
Trade penalties may be applied.

**Hostile**

Elite adventurers test your floors.\
Kingdom leaders suspect you.\
Diplomatic actions are required.

**Raid**

Full kingdom forces engage.\
Multiple waves of elite adventurers attempt to destroy your core.\
This is the most dramatic event in the game.

------------------------------------------------------------------------

### Heat modifiers

Heat increases due to

- adventurer deaths

- noble deaths

- monster overflows

- unfair loot

- imbalance between difficulty and reward

Heat decreases due to

- high quality loot

- training events

- diplomacy quests

- merchant contracts

------------------------------------------------------------------------

### Raid event structure

Raids occur in waves

- scouts

- elite squads

- heroes

- final assault

Victory resets heat and grants

- rare loot

- research unlocks

- political reputation bonuses

Failure triggers penalties such as

- loss of a floor

- loss of elite monsters

- loss of research progress

- mana generation penalties

------------------------------------------------------------------------

## 6.7 Sub dungeon system

Sub dungeons are optional specialized facilities that support your main dungeon.

------------------------------------------------------------------------

### Sub dungeon types

**Mana farm**

Cheap units\
Many traps\
Small floors\
Goal is pure mana income

**Elemental node**

One elemental theme\
Elementally aligned monsters\
Loot focused on elemental crafting

**Training grounds**

Low danger\
Low loot\
High adventurer traffic\
Reduces heat

**Challenge arena**

Special events\
Boss fights\
Tournament style cycles\
Huge mana bursts and prestige rewards

Sub dungeons allow players to diversify strategy without changing their main dungeon identity.

------------------------------------------------------------------------

## 6.8 Research system

We already partially expanded this, now we finalize it into a full system.

------------------------------------------------------------------------

### Research slots

Players begin with one slot.\
Additional slots unlocked through premium currency.

Slots allow

- parallel research

- long term planning

- faster growth

Maximum slots determined by future balancing.

------------------------------------------------------------------------

### Research cost

All research costs mana.

Mana cost is scaled by

- research tier

- material tier

- tree depth

- world events

------------------------------------------------------------------------

### Research time

Research durations cover

- minutes early game

- hours mid game

- days late game

Time creates anticipation and monetization opportunity.

------------------------------------------------------------------------

### Premium currency usage

Premium currency can

- unlock additional research slots

- accelerate research by removing time

- complete research early with proportional cost

Premium currency never

- unlocks content that free players cannot reach

- bypasses prerequisites

- grants power directly

It is purely a time acceleration system.

------------------------------------------------------------------------

### Research tree branches

The full research system contains six branches:

**Core tech**

Mana generation\
Core durability\
Dungeon wide bonuses

**Monsterology**

New monster families\
Evolutions\
Behavior traits\
Boss unlocks

**Arcanology**

Loot recipes\
Enchantments\
Spell scrolls\
Boss sets\
Forging tiers

**Trapcraft**

Traps\
Environmental hazards\
Trap synergy

**Architecture**

Floor styles\
Room upgrades\
Efficiency bonuses

**Diplomacy**

Heat reduction\
Contracts\
Tax reductions\
Kingdom influence

Each tree is deep enough to support months of progression.

# 7. Economy and monetization

The goal of this section is to clearly define every in game currency, how players earn and spend them, and how monetization supports the game without blocking free players.

Dungeon Builder is designed as a fair, long term idle management game where money speeds up progress but never replaces it.

------------------------------------------------------------------------

### 7.1 Currency system

The game has two currencies

- mana

- premium crystals or premium currency

A third non spendable metric, dungeon rating, is referenced but not a currency. It affects leaderboards and adventurer matchmaking.

**Mana**

Mana is the main soft currency.

Players earn mana from

- passive dungeon core generation

- adventurer skill spill

- adventurer deaths

- monster deaths

- event boosts

- sub dungeon yields

- raids won against kingdoms

Players spend mana on

- monsters

- traps

- rooms

- loot restocking

- crafting

- research

- sub dungeon creation

- diplomacy

Mana is the most used resource and must always feel relevant at every stage of the game.

**Characteristics**

- Players accumulate mana both online and offline

- Mana is abundant late game

- No hard caps are placed on mana

- Mana is not directly monetized

------------------------------------------------------------------------

**Premium crystals**

Premium crystals are the main monetization currency.

They are used for

- unlocking extra research slots

- speeding up research

- purchasing cosmetic packs

- purchasing optional mana bundles

- unlocking cosmetic dungeon cores

- cosmetic monster skins

- cosmetic floor themes

- optional quality of life boosts

**Characteristics**

- Premium crystals are never required

- All content can be reached by free players

- Premium crystals reduce time, not difficulty

- Players earn some crystals through

  - daily missions

  - weekly challenges

  - special events

  - ads if enabled

This keeps monetization fair while still giving players reasons to engage.

------------------------------------------------------------------------

### 7.2 Monetization philosophy

Dungeon Builder’s monetization follows four key rules.

**Rule one**

Never sell power that free players cannot achieve.

**Rule two**

Selling time is allowed but must feel optional.

**Rule three**

Cosmetics should always be high value but never required.

**Rule four**

Progression speed boosts cannot bypass critical gameplay mechanics.

These rules ensure longevity, fairness, and positive player sentiment.

------------------------------------------------------------------------

### 7.3 Monetization features

Below are all monetization features. None are mandatory.

#### 7.3.1 Research slot unlocks

The most important monetization driver.

Players begin with one research slot.\
They can unlock more via premium currency.

Benefits

- parallel research

- faster progression

- long term account power growth

Design intent

- new players do not need extra slots

- mid game players value slot two

- late game players desire slot three or four

Slots do not grant unique content, only speed.

#### 7.3.2 Research speed ups

Players may use premium crystals to complete research immediately.

Design

- cost equals remaining time removed

- price scales linearly

- research must have started

- cannot skip prerequisites

This feels fair because it simply saves time.

------------------------------------------------------------------------

#### 7.3.3 Mana bundles

Optional small mana bundles.

These are not required and are intended for

- impatient players

- casual players

- people experimenting with layouts

Mana bundles do not scale infinitely to prevent imbalance.\
Mana cannot be used to skip research.

------------------------------------------------------------------------

#### 7.3.4 Cosmetic dungeon cores

These skins change

- visual effects

- core glow

- mana beam color

- idle animations

Cosmetics have no mechanical advantage.

Examples

- volcanic core

- shadow core

- crystalline core

- celestial core

------------------------------------------------------------------------

#### 7.3.5 Cosmetic monster skins

Monster skins include

- skeleton knight alternate armor

- goblin shaman ceremonial robes

- minotaur warpaint

Skins can be grouped into

- seasonal themes

- faction themes

- event themes

Players love cosmetic expression in management games.

------------------------------------------------------------------------

#### 7.3.6 Floor themes and cosmetic decorations

Players can buy

- cosmetic floor skins

- visual trap effects

- animated decorations

- themed lighting packages

Examples

- winter frost cavern

- deep forest grove

- lava arena

- starfall cosmic chamber

These appear in replay videos and help social sharing.

------------------------------------------------------------------------

#### 7.3.7 Timed mana boosters

Premium boosters

- increase mana gain by a percent

- last a limited number of hours

- have daily caps

- cannot stack infinitely

These are careful because they touch power, but constraints prevent pay to win.

------------------------------------------------------------------------

#### 7.3.8 Event passes

Optional seasonal passes include

- cosmetics

- small crystal rewards

- special missions

- exclusive dungeon core skins

Passes do not unlock gameplay advantages the free track does not also reach.

------------------------------------------------------------------------

#### 7.4 Anti whale and anti exploit rules

To prevent imbalance

- no infinite mana purchase

- no infinite booster stacking

- no instant unlock of entire research tree

- no ability to bypass heat penalties

- no ability to bypass dungeon rating rules

This ensures the game remains competitive and fun.

### 7.5 Free player experience

Free players must feel welcomed and able to compete.

Free players receive

- full access to all gameplay

- one research slot

- daily and weekly crystal rewards

- event participation

- ability to craft every type of loot

- ability to unlock every monster family

Free progression is slower but not blocked.

------------------------------------------------------------------------

### 7.6 Player value loop

When designing economy systems, the player value loop must stay positive.

Players

- explore

- build

- get mana

- invest mana

- earn loot

- attract adventurers

- unlock more research

- grow their dungeon

Monetization should never break this loop. It should only smooth and speed it.

------------------------------------------------------------------------

### 7.7 Balancing the economy

Key economy balance goals include

- mana always matters

- research is the main long term gate

- loot costs scale with dungeon growth

- heat is the safety valve that prevents abuse

- no currency becomes worthless

- late game loops stay rewarding

This creates a clean and fair economy structure.

# 8. User experience and interface

Dungeon Builder is designed for mobile portrait play with one hand. The UX focuses on clarity, instant feedback, and short meaningful interactions.

The UI supports

- fast check ins

- deep planning sessions

- satisfying visual feedback

- clear measurements of progress

- intuitive navigation

------------------------------------------------------------------------

## 8.1 UX philosophy

**One hand ease**

All primary actions must be reachable with the thumb on modern phones.

**Zero clutter**

Only the most relevant information appears at any given moment. Deep info is available in expandable menus.

**Immediate feedback**

If a player places a monster or trap or loot node the game immediately shows

- mana per hour change

- predicted adventurer flow

- predicted heat impact\
  This feedback loop is essential.

**Idle friendly**

Players should be able to log in for only one minute and still feel progress.

**Long session depth**

Players should be able to spend fifteen or thirty minutes experimenting with layouts without UI friction.

**Smooth onboarding**

New players should see tutorials that guide them through

- building a room

- summoning a monster

- attracting adventurers

- researching an upgrade

**Persistent clarity**

The UI always communicates

- heat status

- mana flow

- research progress

- dungeon safety

------------------------------------------------------------------------

## 8.2 Core UI layout

The game has a simple but flexible structure.

**Top bar**

Always visible

- mana total

- mana per hour

- heat indicator

- premium crystals

- core health if under raid

The top bar uses icons with numeric labels to reduce text clutter.

------------------------------------------------------------------------

### Home screen

The Home screen shows a wide angle view of the dungeon core and a compact view of

- heat bar

- mana income

- active research

- adventurer activity

- recommended actions

Buttons on this screen

- go to floors

- go to research

- go to monster management

- go to loot crafting

- go to diplomacy

- go to sub dungeons

- open events

This screen is the control center of the game.

------------------------------------------------------------------------

### Floor screen

The most interactive screen in the game.

Players can

- zoom and pan

- drag rooms into place

- connect corridors

- drop traps

- summon monsters

- create loot nodes

- view danger maps

- preview adventurer pathing

UI elements

- grid or node layout

- icons for rooms and traps

- monster portraits on their tiles

- small floating bars for monster levels

Toggle options

- show trap ranges

- show monster patrol routes

- show adventurer path predictions

- show synergy highlights

This screen must feel fast and responsive.

------------------------------------------------------------------------

### Monster management

This screen displays the entire monster roster.

Layout

- top level tabs for each monster group

- scrollable list of unlocked monsters

- evolution tree button

- stat panel

- ability panel

- behavior panel

Players can

- level monsters

- evolve them

- edit behavior traits

- assign them to floors

- view synergy bonuses

A monster comparison view helps players choose between two similar options.

------------------------------------------------------------------------

### Loot crafting and loot tables

This screen allows players to craft loot and manage drop tables.

Sections

- material inventory

- blueprint library

- enchantment library

- loot crafting panel

- loot pool editor

Players can

- craft gear

- add or remove items from the loot table

- tag items as rare drops

- adjust weighted probabilities

- view adventurer attraction predictions

Visuals show

- rarity glow

- enchantment icons

- potential adventurer classes attracted

------------------------------------------------------------------------

### Research screen

Research is the backbone of long term play. This screen must be clean.

Layout

- tabs for each research branch

- tree view or card based progression

- timer displayed clearly

- button to speed using premium currency

- option to queue next research if unlocked

For players with multiple slots each research project shows

- slot number

- progress bar

- time remaining

The player should be able to see at a glance where they are in each research tree.

------------------------------------------------------------------------

### Adventurer log

This area contains reports and replays.

Sections

- recent adventurer runs

- adventurer names classes levels

- loot gained by adventurers

- monsters defeated

- death or escape outcome

- heat changes

Replay function

- simplified top down animation

- timeline scrub bar

- event markers for fights and trap triggers

Analysis tab

- survival rate by floor

- most dangerous rooms

- most effective trap combos

- which adventurer classes died the most

This screen teaches players how to optimize.

------------------------------------------------------------------------

### Diplomacy screen

This screen visualizes the current political situation.

UI elements

- heat bar with five stages

- kingdom alert panels

- noble complaint logs

- merchant contracts

- tribute requests

Players can

- send offers

- run training events

- process diplomatic quests

- temporarily lower heat through negotiations

The diplomacy screen must communicate consequence clearly.

------------------------------------------------------------------------

### Sub dungeon screen

Each sub dungeon has its own miniature layout screen or specialty panel.

Players can

- configure sub dungeon layout

- assign monsters

- choose sub dungeon role

- assign loot

- collect mana

- run sub dungeon events

Sub dungeons are smaller and simpler than main floors so the UI is streamlined.

------------------------------------------------------------------------

### Events and seasonal UI

Events use a compact banner on the home screen.

Event screen shows

- rewards

- challenges

- timers

- cosmetic previews

Events should look special and exciting.

------------------------------------------------------------------------

## 8.3 Navigation flow

Navigation should feel effortless.

Primary flow\
Home\
Floors\
Monster management\
Research\
Loot crafting\
Diplomacy\
Sub dungeons\
Events

Back navigation always returns to the previous screen without losing progress.

------------------------------------------------------------------------

## 8.4 Accessibility

The game should be playable by a wide range of players.

Accessibility options

- colorblind mode for trap and monster indicators

- larger text option

- slower animations mode

- reduced flashing effects

- simplified interface mode

These options increase usability for all players.

------------------------------------------------------------------------

## 8.5 UX sound design

Sound cues support the experience without overwhelming it.

Examples

- soft chime when mana collected

- deep hum near the dungeon core

- trap click when placed

- soft growl when monster summoned

- magical shimmer when research completes

- alarm tone when heat increases

No sound should be harsh or exhausting for long idle play.

------------------------------------------------------------------------

## 8.6 UX feedback rules

Every player action must create feedback.

Examples\
Placing a monster

- monster appears with small animation

- mana cost floats upward

- synergy glow appears briefly

- projected mana per hour appears

Adding loot

- loot icon sparkles

- adventurer attraction preview updates

Completing research

- screen shakes slightly

- core pulses

- new content highlighted

------------------------------------------------------------------------

## 8.7 Tutorial and onboarding

The tutorial unfolds naturally during play rather than in a single forced sequence.

Tutorial steps\
1 place your first room\
2 place your first monster\
3 check adventurer log\
4 research a new monster evolution\
5 craft your first loot item\
6 adjust your loot table\
7 respond to your first heat warning\
8 unlock a sub dungeon

Each step is short and interactive.

# 9. Content plan and live operations

Dungeon Builder is designed as a long term service game. This section outlines the content roadmap, live event structure, cadence of updates, and the systems needed to keep players engaged for months or years.

------------------------------------------------------------------------

## 9.1 Launch content scope

The launch version must feel complete but not overloaded. It should introduce core systems and give players at least one to two months of progression.

Launch content includes

- two monster families\
  undead and goblinoid

- three to four floor types\
  maze\
  room based\
  environmental\
  basic arena

- core loot tiers up to steel and mithril

- basic enchantments

- full mana system

- one sub dungeon unlocked through research

- heat system including first three heat states\
  peace\
  notice\
  concern

- one type of kingdom raid

- full adventurer AI for five launch classes

- research trees covering early to mid game

- daily and weekly quests

- first event framework

- home screen and dungeon UI fully functional

This gives players enough to engage deeply without overwhelming them.

------------------------------------------------------------------------

## 9.2 Three month post launch roadmap

At this point players are entering late mid game. Expansion must introduce new goals and new motivations.

Quarter one update themes

- new monster family such as kobolds

- new loot tiers up to adamantine

- new enchantment path such as elemental resonance

- new boss set

- new diplomacy contracts

- expanded heat interactions

- new sub dungeon type

- first hero adventurers\
  elite individuals who appear rarely

- first seasonal event\
  themed content such as winter frost or fire festival

This expansion encourages returning engagement and reactivates players who slowed down.

------------------------------------------------------------------------

## 9.3 Six to twelve month roadmap

These updates shift the game toward broad late game systems.

Themes

- large content drops tied to story arcs

- new monster families such as orcs or trolls

- orichalcum loot tier

- new high tier environmental floors

- expanded adventurer class roster

- new raid types

- increased dungeon versus dungeon functionality

- guild or clan features for social play

- cosmetic shop expansion

- first large world event\
  entire kingdoms react to dungeon outbreaks or magical anomalies

Only one or two pieces release every patch to ensure pacing.

------------------------------------------------------------------------

## 9.4 Seasonal events

Seasonal events in Dungeon Builder are large limited time competitions where all players enter a parallel event dungeon that exists separate from their main dungeon. These event dungeons allow rapid experimentation, reward creative design, and create short term competitive goals without affecting long term progression.

Seasonal event dungeons last two to four weeks per event.

------------------------------------------------------------------------

### Seasonal event concept

Each event creates a brand new dungeon instance for every player. This event dungeon

- starts at floor zero with no rooms unlocked

- begins with a unique monster family or unique floor theme

- provides boosted mana generation

- drastically reduces research times

- prevents spending premium currency

- prevents transferring main dungeon resources

- includes exclusive seasonal challenges

The goal is to allow all players to experiment with entirely new strategies without touching or resetting their main dungeon.

------------------------------------------------------------------------

### Core rules of seasonal event dungeons

These rules ensure fairness and creativity.

**One**

Every player starts from scratch within the event dungeon\
No advantage from main account monster unlocks\
No advantage from premium currency

**Two**

Mana generation is significantly accelerated\
Up to ten to twenty times faster depending on event theme

This allows players to try builds they normally would not risk.

**Three**

Research time is dramatically reduced\
A research that normally takes twelve hours might take thirty minutes

This creates rapid iteration and the joy of experimenting.

**Four**

Premium currency is disabled\
You cannot

- speed research

- purchase mana

- buy boosts

- unlock extra slots

This creates a level playing field based entirely on strategy and participation.

**Five**

Event exclusive mechanics may be introduced\
These could include

- elemental storms that change monster synergy

- random floor modifiers

- special adventurer types

- event only loot

------------------------------------------------------------------------

### Seasonal event goals and competitions

Each event tracks multiple leaderboards and achievement paths.

**Deepest floor reached**

Players compete to push the largest number of floors with the event rules.

**Highest challenge rating**

Rating is calculated based solely on the event dungeon.\
Players who focus on monster synergy or extreme danger layouts excel here.

**Most adventurers visited**

Rewards players who create fun or easy dungeons that attract non stop traffic.

**Most adventurer deaths**

Rewards players who create brutal or high risk dungeons.

**Highest daily mana generation**

Focuses on efficiency and long term strategy.

**Most unique loot crafted**

Rewards crafting heavy players.

Because the event dungeon is separate from the main one, players can chase leaderboard categories they normally would not.

### Seasonal rewards

At the end of each event, players earn rewards in their main dungeon based on participation and placement.

Rewards may include

- premium crystals

- exclusive monster skins

- exclusive dungeon core skins

- unique floor themes

- special research blueprints

- exclusive loot cosmetics

- event medals visible on player profile

Event rewards are cosmetic or utility focused but never grant competitive advantage.

------------------------------------------------------------------------

### Seasonal event structure

A typical event follows this structure.

**Day zero**

Event dungeon unlocked\
Players claim their base core and starting monster family

**Week one**

Players expand rapidly\
Leaderboards begin stabilizing\
Event missions unlock

**Week two**

Mid event modifiers appear such as

- elemental storms

- merchant caravans

- special adventurers

These keep the event dynamic.

**Final days**

Events intensify\
Adventurers get stronger\
Floor modifiers intensify\
Leaderboard races tighten

**Event end**

Dungeons freeze\
Scores recorded\
Rewards delivered to main account\
Event archive logs available for replay

------------------------------------------------------------------------

### Benefits to long term retention

Seasonal events

- encourage creative freedom

- bring inactive players back

- let players try new monster families before permanent release

- reduce risk of experimentation

- give high engagement players new goals

- create community excitement

This solves a major issue in idle games where players fear reconfiguring their main dungeon.

------------------------------------------------------------------------

## 9.5 Weekly and daily content

Daily and weekly goals drive short sessions.

Daily missions

- collect mana

- research one node

- place or upgrade a monster

- review adventurer log

Weekly missions

- complete one raid

- craft higher tier loot

- run three diplomacy tasks

- reconfigure one floor

Rewards

- mana bundles

- crafting materials

- premium crystals

- cosmetic fragments

Daily content teaches new players. Weekly content drives engagement for veteran players.

------------------------------------------------------------------------

## 9.6 Special hero adventurers

Hero adventurers are late game content.

Characteristics

- unique names and lore

- stronger stats

- special abilities

- special loot drops

- unique interactions with floors

- the ability to form grudges or rivalries with your dungeon

Heroes increase world immersion and deepen adventurer AI complexity.

------------------------------------------------------------------------

## 9.7 World events and live narrative

Every few months the world changes temporarily.

Examples

- war breaks out between regions

- plague forces clerics to your dungeon

- mana storm enhances magical floors

- seasonal migrations of beasts or elementals

These events shift

- adventurer class distribution

- heat behavior

- loot economy

- mana generation

World events help the game feel alive.

------------------------------------------------------------------------

## 9.8 Cosmetic releases

Cosmetics can release

- monthly

- tied to events

- tied to major content patches

Cosmetics include

- dungeon cores

- monster skins

- floor themes

- trap effects

- room decorations

These can be monetized without affecting gameplay.

# 10. Technical implementation

Section Ten describes the technical foundation. This is what engineers, producers, and architects need to scope and build the game without ambiguity.

------------------------------------------------------------------------

## 10.1 Engine and tools

Dungeon Builder is designed for mobile development.

Recommended engines

- Unity

- Unreal Engine Mobile branch

- Godot Mobile

Unity is most practical due to

- wide mobile support

- well tested idle game frameworks

- lightweight asset streaming

- easy animation pipeline

------------------------------------------------------------------------

## 10.2 Data structures

Core systems require clear data modeling.

### Monster data objects

Each monster has

- monster ID

- base level

- stat curves

- traits

- abilities

- affinity tags

- animation set

- sound references

- spawn cost

- AI profile

### Loot data objects

Each loot item has

- item ID

- rarity

- material tier

- enchantment slots

- enchantment list

- value rating

- adventurer attraction rating

- mana replacement cost

- blueprint reference

### Research data objects

Each research node has

- research ID

- category

- mana cost

- time cost

- prerequisite IDs

- unlock effects

- slot compatibility

- premium time conversion formula

### Floor and room objects

Each object includes

- room ID or tile ID

- room or tile type

- connected paths

- trap sockets

- environmental tags

- monster allocation

- loot nodes

These objects interact through pathfinding and synergy scoring.

------------------------------------------------------------------------

## 10.3 Server and client architecture

Dungeon Builder requires

- persistent save functionality

- anti manipulation checks

- cloud enabled accounts

**Hybrid model**

Client handles

- rendering

- dungeon layout

- adventurer simulation

- simple AI and pathing

Server handles

- account storage

- premium currency

- event timing

- leaderboard updates

- anti cheat logic

This keeps server cost reasonable while preventing exploits.

------------------------------------------------------------------------

## 10.4 Idle progression architecture

Idle games require careful handling of offline time.

System requirements

- last login timestamp stored server side

- offline mana generation calculated on login

- offline adventurer simulation simplified

- upper limit protections against fake time jumps

- event timers tracked server side

------------------------------------------------------------------------

## 10.5 AI architecture

AI needs to be modular.

Modules

- adventurer decision module

- combat resolution module

- pathing and rerouting

- synergy detector

- personality behavior tree

- retreat logic

- loot evaluation logic

Using modular components allows engineers to add new classes easily.

------------------------------------------------------------------------

## 10.6 Performance considerations

Dungeon Builder must run well on low to mid tier phones.

Optimization strategies

- pooled monster animations

- simplified pathfinding during offline mode

- baked lighting for static floors

- simple particle effects for traps

- capped replay FPS

- dynamic resolution scaling for older devices

------------------------------------------------------------------------

## 10.7 Asset streaming

To reduce download size

- monster skins load on demand

- cosmetic floor themes stream as needed

- cutscenes or animations cached after use

This improves performance and storage space.

------------------------------------------------------------------------

## 10.8 Security and anti cheating

Critical systems such as

- premium currency

- research completion

- mana rewards\
  must be validated server side.

Anti exploit checks

- time jump detection

- excessive mana gain

- research skip attempts

- repeated offline collect attempts

------------------------------------------------------------------------

## 10.9 Testing and QA plan

Dungeon Builder requires

- unit tests for AI logic

- pathfinding tests

- stress tests for late game monster swarms

- device compatibility tests

- balance passes on loot tables

- heat system edge case tests

- raid encounter simulation tests

QA cycles should run\
pre alpha\
alpha\
beta\
soft launch\
global launch

------------------------------------------------------------------------

## 10.10 Analytics

Analytics help balance the game.

Tracked metrics include

- player retention

- research bottleneck points

- mana generation averages

- adventurer death ratios

- loot table popularity

- heat spikes and raid triggers

- premium currency purchase patterns

Analytics inform future content and monetization tuning.
