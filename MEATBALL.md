# Meatball enemy

In Unity choose GameObject > Enemies > Meatball. Move it onto a floor in your scene, then enter Play mode. The menu creates a brown, lumpy sphere with its physics and enemy script configured. Drag it into Assets/Prefabs if you want a reusable prefab.

The enemy automatically finds SimplePlayerController or the legacy Player component. You can instead assign Target manually in its inspector. It locks its direction when the wind-up begins, squashes for 0.8 seconds, then charges straight ahead. A wall impact or the end of the charge starts a recovery period.

Three distinct thrown plates defeat the large meatball. It splits into three smaller, faster meatballs, each defeated by one plate. Minis never split. A plate can damage each enemy only once; minis inherit the parent's plate history to avoid an immediate chain kill from the final plate. Other weapons can call TakeDamage(int).

Connect the On Player Hit inspector event to your future player damage method. There is no existing player health system, so contact currently stops the charge and invokes this event without changing player health.

During Play mode, the MeatballEnemy component context menu offers Defeat (test splitting). Check wind-up, dodging, wall recovery, plate hits, splitting, and one-hit mini defeats on your level geometry. The ground check stops charges after they leave the ground; it does not provide platform navigation or guarantee avoidance of ledges.
