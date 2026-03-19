using UnityEngine;

namespace CatsVsCapybaras
{
    [CreateAssetMenu(fileName = "Weapon_New", menuName = "Cats vs Capybaras/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName = "Unnamed";

        [TextArea(1, 3)]
        public string description;

        public Sprite icon;
        public GameObject projectilePrefab;

        [Header("Combat")]
        public int damage = 30;
        public float explosionRadius = 2f;

        [Header("Power")]
        [Tooltip("Launch speed at minimum charge")]
        public float minPower = 5f;
        [Tooltip("Launch speed at full charge")]
        public float maxPower = 15f;

        [Header("Ammo")]
        [Tooltip("-1 for infinite ammo")]
        public int startingAmmo = -1;
    }
}
