# Meatball enemy

Choose GameObject > Enemies > Meatball to create one. Place its root on a floor. Keep its visual child centered on the root, at scale 1, with no enabled collider. Assign Target, or let the enemy find a player controller/PlayerInput. PhilippeScene already has a configured enemy and player.

Combat: three distinct flying plates defeat a large meatball. Resting, landed and very slow plates do no damage. Surviving a hit briefly stuns the enemy. Defeat spawns three faster one-hit minis which never split; they remember the killing plate and ignore other meatball colliders to prevent spawning collisions. Unsafe fragment offsets fall back to the parent's position.

Movement: the target direction locks at the start of the visible wind-up. The enemy charges straight, then recovers. Walls stop the charge. Ground detection uses a sphere probe, slope filtering and self/player exclusion. Three forward probes check for floor before moving, including enough distance for the next physics step. Missing support or an excessive drop stops the charge before the edge. Ground Layers, Max Slope, Ground Tolerance and Ledge Look Ahead tune platform behavior. Keep Ground Tolerance small for strict edge safety. This is charge behavior, not pathfinding: it waits rather than navigating around a gap or wall.

PlayerHealth is attached to PhilippeScene's player: five health, one second of invulnerability after a hit, respawn at the starting position on death. Assign Respawn Point to override that position. Other scenes need PlayerHealth added to their player. Death, damage and respawn events support UI/audio. Meatball events support player contact, damage, defeat and charge audio/effects. CharacterController contacts are forwarded by SimplePlayerController so charging damage works with this movement setup.

Select the enemy in Play mode to see Movement Status in the inspector. Yellow gizmos show detection range; green/red forward probes indicate safe/unsafe ground. The component context menu can defeat it for split testing.

Tools > Validate Meatball Ground Safety checks ground support, a pre-edge stopping position, self filtering, missing floor, player damage, invulnerability, death and respawn. Play mode testing should also cover slopes, moving platforms, walls, firing plates, simultaneous enemies and splitting near ledges. Finite probes cannot guarantee safety on arbitrary discontinuous or moving geometry.
