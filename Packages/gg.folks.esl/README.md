# Esl
The Excellent Simulation Library is a Unity package that functions as an "AI
engine": it aims to prevent or limit the need for the bespoke rule-based AI
systems that power most games.  It is based on the idea that cognition and
simulation are inseparable--or rather, that cognition subsumes simulation (as
well as computation).  Here, "simulation" is used in a broad sense that includes
both physical simulation (as performed by physics engines) and more general
discrete event simulation ("logical" simulation).  The simulation that ESL
provides is fully "parameterized": that is, rather than being driven by a fixed
algorithm (such as those used in Havok or The Sims), each ESL instance can have
its rules change over time, typically as driven by a reinforcement learning
process (as animals learn in real life).  This training process can take place
either/both offline (before the game is shipped) and online (for each player
  while they're playing).

ESL consists of a set of Unity components (the primary Simulation component and
assorted additional helpers) and a set of test/example scenes demonstrating the
use of those components to provide "intelligent" simulated behavior in different
types of environments/games.  Here, "intelligence" is defined modestly as
"understandable/reasonable, but not (necessarily) predictable."  To use ESL in
their own game, a developer would first peruse the examples to find the model
that most closely matches their requirements.  If necessary, they may have to
create a model from scratch either in C# or in a graph-based editor.  If the
developer starts with an off-the-shelf model, they will likely have to
customize/alter the model slightly to fit the exact needs of their game.

After choosing or creating a base model and instantiating it within the new
game, the process of offline training can begin.  ESL will provide tools to
automate (and perhaps distribute) this process, allowing the model to improve
steadily over many iterations.  The parameters and hyperparameters (i.e., the
things that change with training and the things that don't) of the model can be
saved in simple generic formats, making it easy to store snapshots of
interesting behaviors that appear in training/testing and use them as the basis
for further evolution.

Development on ESL has just started, so there's nothing to see yet.  Watch this
space!
