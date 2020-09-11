# Principles of ESL

These are the core principles upon which ESL development is based, in no
particular order.  Note that these are principles for the product, not a code of
conduct for the community.  For that, see the
[Rust Code of Conduct](https://www.rust-lang.org/policies/code-of-conduct).

## "Black box"/"gray box" AI

In the author's experience, the decomposition of engineering problems is
enabled by treating part A as a "black box" while working on part B, and
vice-versa.  One sees this, for example, with graphics and physics engines: the
API defines the interface through which one interacts with the component, and
the internals of the component may be opaque (in the case of closed source
engines) or transparent (open source).  ESL brings this approach to AI and
generalized simulation, allowing applications to treat ESL models as "black
boxes" defined by their inputs and outputs over time (equivalent to a
generalized API).  They can also view them as a "gray box," wherein the rough
structure of the internals is known, but not the fine detail.

## 2D/3D/abstract environments

One typically envisions an ESL model as an "entity" interacting with an
"environment," though that is simply one way of framing the interaction between
the application and the ESL model(s).  With that framing, however, ESL endeavors
to support environments in a generalized way that allows any number of
spatial (or simply "non-time") dimensions.  Of specific interest are the 2D and
3D environments typically used in games, and certain "abstract" environments
where dimensions don't correspond to spatial axes (for instance, in modeling the
Prisoner's Dilemma, one axis might be "cooperation").

## Perceptually limited "body syntonic" inputs and outputs (no "cheating")

For reasons of simple practicality, game AI typically "cheats" by starting with
perfect knowledge of the environment.  For reasons of verisimilitude, ESL starts
with an assumption that there will be no "cheating": the inputs to each model
are limited by perceptual filters (e.g., ignoring distant entities or ones
outside the originating entity's field of view).  Entities also assume "body
syntonic" inputs and outputs, similar to the model used in
[Logo](https://en.wikipedia.org/wiki/Logo_(programming_language)) for the
"turtle."  For instance, instead of a command to move from absolute coordinates
(0, 0) to (1, 1), ESL/Logo would use a command to rotate to face the necessary
direction and another command to advance the required number of units.  This
makes it easier for the model author to map their own experience to that of the
"avatar," and in graphics makes it easier to create certain kinds of
symmetric/fractal patterns (such as snowflakes).

## Generalization/transfer from simple environments to more complex ones

The progressive goal of ESL is to create models for simple environments and use
them as building blocks/starting points for models that handle more complex
environments.  The assumption behind this pattern is that general intelligence
is formed by the combination and hybridization of "specific intelligence," which
is intelligence that handles single task classes.  General intelligence, then,
is something like civilization itself: a collection of specialist entities
(where "generalist" is simply another kind of specialization) working towards
common goals (but also sometimes in competition with one another, for the
purpose of creating better models).

## Hand-designed models improved by reinforcement learning/genetic programming

To flesh out the hierarchy of specific models, the ESL project will undertake a
process of building test/demonstration/example models by hand and iteratively
refinining them via reinforcement learning and "genetic programming" (that is,
using a genetic algorithms system with a representation of the ESL model as the
"genome" to be crossed over/mutated in simulations of asexual/sexual
reproduction).

## "Perfect is the enemy of [interesting/emergent/unexpected]."

The focus of ESL is not on creating "perfect" solutions to "toy problems," but
rather to create imperfect models that are interesting to observers, due at the
very least to stochastic ("random") behavior.  Generally speaking, each ESL
example is best seen as a stepping stone to the next, rather than an end in
itself (even if it incidentally involves the creation of, for instance, a
playable game).

## "Headway, not headlines."

The goal of the ESL project is always "incremental progress towards something
that could feasibly be considered 'general intelligence' or 'strong AI.'"
Notably, as compared to AI projects like the GPT series from OpenAI, ESL does
not presume general intelligence from the outset.  Instead, it represents an
ongoing collaborative discovery process that presumes only a loosely defined
and mutable set of principles: the ones in this document.  There's no need to
publicize ESL, because it won't benefit from the attention before it reaches
critical mass.

## Inspiration

* [General game playing](https://en.wikipedia.org/wiki/General_game_playing)
* [Robot Odyssey](https://en.wikipedia.org/wiki/Robot_Odyssey)
* [The Odyssey](https://en.wikipedia.org/wiki/Odyssey)
