using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Harmony;
using UnityEngine;


namespace ArmorPiercingProjectiles.src
{
    public class IngressPoint
    {

        public static bool Debug = false;

        private static void DebugPrint(String contents)
        {
            if (IngressPoint.Debug)
            {
                Console.WriteLine("[AP-P] " + contents);
            }
        }

        // Do the high damage penetration bits
        [HarmonyPatch(typeof(Projectile), "OnCollisionEnter")]
        public static class PatchProjectile
        {
            private static readonly FieldInfo m_DamageType = typeof(Projectile).GetField("m_DamageType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_Stuck = typeof(Projectile).GetField("m_Stuck", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_SingleImpact = typeof(Projectile).GetField("m_SingleImpact", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_HasSetCollisionDeathDelay = typeof(Projectile).GetField("m_HasSetCollisionDeathDelay", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_Weapon = typeof(Projectile).GetField("m_Weapon", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_StickOnContact = typeof(Projectile).GetField("m_StickOnContact", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_ExplodeOnStick = typeof(Projectile).GetField("m_ExplodeOnStick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_VisibleStuckTo = typeof(Projectile).GetField("m_VisibleStuckTo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_Smoke = typeof(Projectile).GetField("m_Smoke", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_ExplodeOnTerrain = typeof(Projectile).GetField("m_ExplodeOnTerrain", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_StickOnTerrain = typeof(Projectile).GetField("m_StickOnTerrain", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo OnParentDestroyed = typeof(Projectile).GetField("OnParentDestroyed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_StickImpactEffect = typeof(Projectile).GetField("m_StickImpactEffect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly FieldInfo m_ImpactSFXType = typeof(Projectile).GetField("m_ImpactSFXType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            private static readonly MethodInfo IsProjectileArmed = typeof(Projectile).GetMethod("IsProjectileArmed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly MethodInfo SpawnExplosion = typeof(Projectile).GetMethod("SpawnExplosion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly MethodInfo SpawnStickImpactEffect = typeof(Projectile).GetMethod("SpawnStickImpactEffect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly MethodInfo SpawnTerrainHitEffect = typeof(Projectile).GetMethod("SpawnTerrainHitEffect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly MethodInfo SetStuck = typeof(Projectile).GetMethod("SetStuck", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly MethodInfo GetDeathDelay = typeof(Projectile).GetMethod("GetDeathDelay", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly MethodInfo OnDelayedDeathSet = typeof(Projectile).GetMethod("OnDelayedDeathSet", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly MethodInfo SetProjectileForDelayedDestruction = typeof(Projectile).GetMethod("SetProjectileForDelayedDestruction", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            private static bool IntersectsCell(Vector3 point, Vector3 direction, IntVector3 cell)
            {
                float distance = Vector3.Cross(cell - point, cell - (point + direction)).magnitude / direction.magnitude;
                return distance < Mathf.Sqrt(2) / 2;
            }

            private static int HandleCollision(Projectile __instance, ArmorPiercing armorPiercing, Damageable damageable, Vector3 hitPoint, Vector3 impactVector, Collider otherCollider, bool ForceDestroy)
            {
                int retVal = 0;
                try
                {
                    if (!((Component)__instance).gameObject.activeInHierarchy)
                    {
                        return 0;
                    }
                    if ((bool)PatchProjectile.m_Stuck.GetValue(__instance))
                    {
                        return 0;
                    }
                    bool singleImpact = (bool)PatchProjectile.m_SingleImpact.GetValue(__instance);
                    bool hasHitTerrain = false;

                    bool stickOnContact = (bool)PatchProjectile.m_StickOnContact.GetValue(__instance);
                    float deathDelay = (float)PatchProjectile.GetDeathDelay.Invoke(__instance, null);

                    DebugPrint($"Handle Collision for projectile {__instance.name}");

                    // handle damage calculations and explosions
                    if (damageable)
                    {
                        float damage = armorPiercing.remainingDamage;
                        TankBlock targetBlock = damageable.Block;

                        d.Assert(targetBlock != null, "TARGET BLOCK IS NOT NULL");

                        // Armor pierce works as follows:
                        // Deal full damage to the first block we hit. After that, (1 - pierce) * dealt damage is subtracted from remaining damage (if any)

                        ManDamage.DamageInfo damageInfo = new ManDamage.DamageInfo(damage, (ManDamage.DamageType)PatchProjectile.m_DamageType.GetValue(__instance), (ModuleWeapon)PatchProjectile.m_Weapon.GetValue(__instance), __instance.Shooter, hitPoint, __instance.rbody.velocity, 0f, 0f);
                        float fracDamageRemaining = Singleton.Manager<ManDamage>.inst.DealDamage(damageInfo, damageable);

                        float damageDealt = fracDamageRemaining > 0.0f ? damage * (1 - fracDamageRemaining) : damage;
                        DebugPrint($"Stage 4a - Just dealt {damageDealt} damage to block {targetBlock.name} ({damageable.Health}), original shell has damage {damage}");

                        // block was destroyed, damage potentially leftover
                        if (fracDamageRemaining > 0.0f)
                        {
                            retVal = (int)Mathf.Max(0.0f, (damage * fracDamageRemaining) - (damageDealt * (1.0f - armorPiercing.armorPierce)));
                            DebugPrint($"Killed block {targetBlock.name}, SHELL DMG {damage} ==[REMAINING DMG]=> {retVal}");
                        }
                        else
                        {
                            DebugPrint($"Failed to kill block {targetBlock.name}, SHELL DMG {damage} ==[REMAINING DMG]=> 0");
                        }
                        // no damage leftover cases:
                        if (retVal == 0)
                        {
                            if (deathDelay != 0.0f && !stickOnContact)
                            {
                                // penetration fuse, but failed to kill = flattened, spawn the explosion now
                                deathDelay = 0.0f;
                                PatchProjectile.SpawnExplosion.Invoke(__instance, new object[] { hitPoint, damageable });
                            }
                            else if ((bool)PatchProjectile.IsProjectileArmed.Invoke(__instance, null) && !stickOnContact)
                            {
                                // no penetration fuse, check if armed and not stick on contact - stick on contact explosions are done later
                                PatchProjectile.SpawnExplosion.Invoke(__instance, new object[] { hitPoint, damageable });
                            }
                        }
                    }
                    else if (otherCollider is TerrainCollider || otherCollider.gameObject.layer == Globals.inst.layerLandmark || otherCollider.GetComponentInParents<TerrainObject>(true))
                    {
                        DebugPrint("Stage 4b");
                        hasHitTerrain = true;
                        PatchProjectile.SpawnTerrainHitEffect.Invoke(__instance, new object[] { hitPoint });
                        DebugPrint("Stage 4bb");

                        // if explode on terrain, explode and end, no matter death delay
                        if ((bool)PatchProjectile.m_ExplodeOnTerrain.GetValue(__instance) && (bool)PatchProjectile.IsProjectileArmed.Invoke(__instance, null))
                        {
                            PatchProjectile.SpawnExplosion.Invoke(__instance, new object[] { hitPoint, null });

                            // if default single impact behavior, explode on terrain, then die.
                            // else, keep the bouncing explosions
                            if (singleImpact)
                            {
                                __instance.Recycle(false);
                                return 0;
                            }
                        }
                    }

                    DebugPrint("Stage 5 - play sfx, handle recycle");
                    Singleton.Manager<ManSFX>.inst.PlayImpactSFX(__instance.Shooter, (ManSFX.WeaponImpactSfxType)PatchProjectile.m_ImpactSFXType.GetValue(__instance), damageable, hitPoint, otherCollider);

                    DebugPrint("Stage 6");
                    // if here, then no stick on contact, and no damage is leftover, so start destruction sequence
                    if (ForceDestroy)   // if projectile hits a shield, always destroy
                    {
                        __instance.Recycle(false);
                    }
                    else if (deathDelay == 0f)
                    {
                        // If hasn't hit terrain, and still damage left, return here - don't recycle
                        if (!hasHitTerrain && retVal > 0)
                        {
                            return retVal;
                        }
                        __instance.Recycle(false);
                    }
                    else if (!(bool)PatchProjectile.m_HasSetCollisionDeathDelay.GetValue(__instance))
                    {
                        PatchProjectile.m_HasSetCollisionDeathDelay.SetValue(__instance, true);
                        PatchProjectile.SetProjectileForDelayedDestruction.Invoke(__instance, new object[] { deathDelay });
                        if (__instance.SeekingProjectile)
                        {
                            __instance.SeekingProjectile.enabled = false;
                        }
                        PatchProjectile.OnDelayedDeathSet.Invoke(__instance, null);
                    }

                    DebugPrint("Stage 7 - handle stick on terrain");
                    bool stickOnTerrain = (bool)PatchProjectile.m_StickOnTerrain.GetValue(__instance);
                    DebugPrint("HUH");
                    if (stickOnContact && (stickOnTerrain || !hasHitTerrain))
                    {
                        DebugPrint("WHAT");
                        GameObject test3 = otherCollider.gameObject;
                        DebugPrint("WHAT 1");
                        Transform trans = test3.transform;
                        DebugPrint("WHAT 2");
                        Vector3 scale = trans.lossyScale;
                        DebugPrint("WHAT 3");
                        if (otherCollider.gameObject.transform.lossyScale.Approximately(Vector3.one, 0.001f))
                        {
                            DebugPrint("Stage 7a");
                            ((Component)__instance).transform.SetParent(otherCollider.gameObject.transform);
                            PatchProjectile.SetStuck.Invoke(__instance, new object[] { true });
                            SmokeTrail smoke = (SmokeTrail)PatchProjectile.m_Smoke.GetValue(__instance);
                            if (smoke)
                            {
                                smoke.enabled = false;
                                smoke.Reset();
                            }

                            DebugPrint("Stage 7b");
                            Visible stuckTo = Singleton.Manager<ManVisible>.inst.FindVisible(otherCollider);
                            PatchProjectile.m_VisibleStuckTo.SetValue(__instance, stuckTo);
                            if (stuckTo.IsNotNull())
                            {
                                stuckTo.RecycledEvent.Subscribe(new Action<Visible>((Action<Visible>)PatchProjectile.OnParentDestroyed.GetValue(__instance)));
                            }
                            DebugPrint("Stage 7c");
                            if ((bool)PatchProjectile.m_ExplodeOnStick.GetValue(__instance))
                            {
                                Visible visible = (Visible)PatchProjectile.m_VisibleStuckTo.GetValue(__instance);
                                Damageable directHitTarget = visible.IsNotNull() ? visible.damageable : null;
                                PatchProjectile.SpawnExplosion.Invoke(__instance, new object[] { hitPoint, directHitTarget });
                            }
                            DebugPrint("Stage 7d");
                            if (((Transform)PatchProjectile.m_StickImpactEffect.GetValue(__instance)).IsNotNull())
                            {
                                PatchProjectile.SpawnStickImpactEffect.Invoke(__instance, new object[] { hitPoint });
                            }
                        }
                        else
                        {
                            d.LogWarning(string.Concat(new string[]
                            {
                            "Won't attach projectile ",
                            __instance.name,
                            " to ",
                            otherCollider.name,
                            ", as scale is not one"
                            }));
                        }
                    }
                    DebugPrint("FINAL");
                }
                catch (Exception e)
                {
                    Console.WriteLine("[AP-P] EXCEPTION IN HANDLE COLLISION:");
                    Console.WriteLine(e.Message);
                    if (__instance)
                    {
                        __instance.Recycle(false);
                    }
                }
                return retVal;
            }

            public static bool Prefix(ref Projectile __instance, ref Collision collision)
            {
                ArmorPiercing armorPiercing = __instance.GetComponent<ArmorPiercing>();
                if (!armorPiercing || __instance.GetType() != typeof(Projectile) || __instance.GetType().IsSubclassOf(typeof(Projectile)))
                {
                    return true;
                }

                ContactPoint[] contacts = collision.contacts;
                if (contacts.Length == 0)
                {
                    return false;
                }
                ContactPoint contactPoint = contacts[0];

                Vector3 relativeVelocity = collision.relativeVelocity;
                Rigidbody targetRigidbody = collision.collider.attachedRigidbody;
                Vector3 targetVelocity = Vector3.zero;
                if (targetRigidbody)
                {
                    targetVelocity = targetRigidbody.velocity;
                }
                Vector3 originalVelocity = targetVelocity - relativeVelocity;
                int remainderDamage = PatchProjectile.HandleCollision(__instance, armorPiercing, contactPoint.otherCollider.GetComponentInParents<Damageable>(true), contactPoint.point, originalVelocity, collision.collider, false);
                
                // if returns 0, then standard behavior, has hit the limit.
                if (remainderDamage > 0)
                {
                    // else, block is destroyed. Decrease damage accordingly, reset relative velocity
                    armorPiercing.remainingDamage = remainderDamage;
                    DebugPrint(relativeVelocity.ToString());
                    DebugPrint(targetVelocity.ToString());
                    DebugPrint(__instance.rbody.velocity.ToString());
                    __instance.rbody.velocity = originalVelocity;
                }
                return false;
            }
        }

        // Setup projectiles on pool w/ new component
        [HarmonyPatch(typeof(Projectile), "OnPool")]
        public static class PatchProjectilePool
        {
            private static readonly FieldInfo m_Damage = typeof(Projectile).GetField("m_Damage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            public static void Postfix(ref Projectile __instance)
            {
                ArmorPiercing armorPiercing = __instance.GetComponent<ArmorPiercing>();
                if (armorPiercing == null)
                {
                    armorPiercing = __instance.gameObject.AddComponent<ArmorPiercing>();
                    armorPiercing.armorPierce = 0.5f;
                }

                armorPiercing.remainingDamage = (float) (int) m_Damage.GetValue(__instance);
            }
        }

        // Reset damage on recycle
        [HarmonyPatch(typeof(Projectile), "OnRecycle")]
        public static class PatchProjectileRecycle
        {
            private static readonly FieldInfo m_Damage = typeof(Projectile).GetField("m_Damage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            public static void Postfix(ref Projectile __instance)
            {
                ArmorPiercing armorPiercing = __instance.GetComponent<ArmorPiercing>();
                if (armorPiercing == null)
                {
                    armorPiercing = __instance.gameObject.AddComponent<ArmorPiercing>();
                    armorPiercing.armorPierce = 0.5f;
                }

                armorPiercing.remainingDamage = (float)(int)m_Damage.GetValue(__instance);
            }
        }



        // Disable colliders if block destroyed
        [HarmonyPatch(typeof(Damageable), "TryToDamage")]
        public static class PatchDamageable
        {
            public static void Postfix(ref Damageable __instance, ref float __result) {
                if (__result != 0.0f)
                {
                    // block destroyed
                    TankBlock block = __instance.Block;
                    if (block)
                    {
                        Collider[] colliders = block.GetComponentsInChildren<Collider>();
                        foreach (Collider collider in colliders)
                        {
                            collider.enabled = false;
                        }
                    }
                }
                return;
            }
        }

        // disable healing of dying blocks
        [HarmonyPatch(typeof(Damageable), "Repair")]
        public static class PatchHealing
        {
            public static bool Prefix(ref Damageable __instance)
            {
                if (__instance.Health <= 0.0f)
                {
                    return false;
                }
                return true;
            }
        }

        public static void Main()
        {
            HarmonyInstance.Create("flsoz.ttmm.armorpierce.mod").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
