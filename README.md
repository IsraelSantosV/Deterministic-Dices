# :game_die: Deterministic Dices
A deterministically dice throwing system for synchronizing multiplayer games in **Unity Engine**

## :ticket: Introduction
In multiplayer games the values sent to each client must be obtained and passed in a deterministic way to avoid possible desynchronization between players. In a 3D game, dices are commonly created using engine physics to get the results, however, this implies getting results in a non-deterministic way, since players may receive incorrect values if they depend on the object's gravity and mass. . Therefore, this code was created thinking about making the process of rolling dice deterministic.

## :dart: Requirements
This system uses the free asset 'More Effective Coroutines' available at: (https://assetstore.unity.com/packages/tools/animation/more-effective-coroutines-free-54975)

## :black_joker: How it work
The dices are duplicated at the beginning of the code execution, separated into two groups: the visible (false dices) and the invisible (dices for obtaining the results). The invisible group are launched before the visible ones, seeking to acquire the values using Unity own physics engine. In this process, all frames of movement of the invisible dices are recorded and, when the dices stop moving, the whole process is recorded in an animation clip. After that, the fake dices are released using the previously recorded animation clip, without having to use Unity physics engine. Therefore, the server must launch the invisible dices and send the animation clip to each client for correct synchronization between them. To obtain the values of the data faces, a scalar product was used.
