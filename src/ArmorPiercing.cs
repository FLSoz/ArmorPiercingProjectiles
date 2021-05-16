using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArmorPiercingProjectiles.src
{
    [RequireComponent(typeof(Projectile))]
    public class ArmorPiercing : MonoBehaviour
    {
        public float remainingDamage;
        public float armorPierce;
    }
}
