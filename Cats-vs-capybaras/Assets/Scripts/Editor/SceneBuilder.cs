using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using CatsVsCapybaras;

namespace CatsVsCapybarasEditor
{
    public static class SceneBuilder
    {
        // ═══════════════════════════════════════════════════════════
        //  GROUND COLLIDER
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a dedicated BoxCollider2D ground object that is NOT managed by
        /// TerrainDestruction and will never auto-regenerate from sprite alpha.
        /// Ground top sits at world Y=3.0.
        /// </summary>
        [MenuItem("CatsVsCapybaras/Setup Ground Collider")]
        public static void SetupGroundCollider()
        {
            var old = GameObject.Find("GroundCollider");
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject("GroundCollider");
            go.layer = LayerMask.NameToLayer("Terrain") >= 0
                ? LayerMask.NameToLayer("Terrain") : 0;
            go.transform.position = new Vector3(14.07f, 1.0f, 0f);

            var box = go.AddComponent<BoxCollider2D>();
            box.size   = new Vector2(28.14f, 4f);
            box.offset = Vector2.zero;

            MarkDirty();
            Debug.Log("[SceneBuilder] GroundCollider created. Top at world Y=3.");
        }

        // ═══════════════════════════════════════════════════════════
        //  CHARACTERS
        // ═══════════════════════════════════════════════════════════

        [MenuItem("CatsVsCapybaras/Build Characters")]
        public static void BuildCharacters()
        {
            var catSpawns = new (string name, int team, float x)[]
            {
                ("Cat_Miso",  0, 2.3f),
                ("Cat_Nova",  0, 3.9f),
            };
            var capySpawns = new (string name, int team, float x)[]
            {
                ("Capy_Bubba", 1, 23.1f),
                ("Capy_Lulu",  1, 24.8f),
            };

            foreach (var s in catSpawns)  CreateCharacter(s.name, s.team, s.x);
            foreach (var s in capySpawns) CreateCharacter(s.name, s.team, s.x);

            MarkDirty();
            Debug.Log("[SceneBuilder] Created 4 characters.");
        }

        static void CreateCharacter(string goName, int teamIndex, float worldX)
        {
            var existing = GameObject.Find(goName);
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject(goName);
            go.layer = LayerMask.NameToLayer("Characters") >= 0
                ? LayerMask.NameToLayer("Characters") : 0;
            go.transform.position   = new Vector3(worldX, 8f, 0f);
            go.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            // SpriteRenderer — Idle frame from sliced sheet
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 1;
            var allSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/mochi-character-board.png")
                .OfType<Sprite>().ToArray();
            var idle = allSprites.FirstOrDefault(s => s.name == "mochi-character-board_Idle")
                    ?? allSprites.FirstOrDefault();
            if (idle != null) sr.sprite = idle;

            // Physics
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.5f, 0.8f);
            col.direction = CapsuleDirection2D.Vertical;

            // Character logic
            var cc = go.AddComponent<CharacterController2D>();
            var so = new SerializedObject(cc);
            so.FindProperty("teamIndex").intValue    = teamIndex;
            so.FindProperty("characterName").stringValue = goName;
            so.FindProperty("walkSpeed").floatValue  = 3f;
            so.FindProperty("jumpForce").floatValue  = 9f;
            so.FindProperty("maxHealth").floatValue  = 100f;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<SpriteAnimator>();

            Debug.Log($"[SceneBuilder] Created {goName} at X={worldX}, team={teamIndex}");
        }

        // ═══════════════════════════════════════════════════════════
        //  WEAPON ASSETS
        // ═══════════════════════════════════════════════════════════

        [MenuItem("CatsVsCapybaras/1 - Create Weapon Assets")]
        public static void CreateWeaponAssets()
        {
            EnsureFolder("Assets/Weapons");

            // minPower/maxPower are Unity physics impulse units.
            // 3.7–14.4: reaches 3/4 of the map at full power on 45° arc.
            MakeWeapon("Weapon_Carrot", "Carrot", 32, 1.5f, 3.7f, 14.4f, -1);

            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] Weapon assets created/updated in Assets/Weapons/");
        }

        /// <summary>
        /// Creates or updates a WeaponData asset. Always overwrites values so
        /// re-running the setup picks up any tuning changes.
        /// </summary>
        static void MakeWeapon(string filename, string display, int dmg,
                               float radius, float minP, float maxP, int ammo)
        {
            string path = $"Assets/Weapons/{filename}.asset";
            var w = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (w == null)
            {
                w = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(w, path);
            }
            w.weaponName      = display;
            w.damage          = dmg;
            w.explosionRadius = radius;
            w.minPower        = minP;
            w.maxPower        = maxP;
            w.startingAmmo    = ammo;
            EditorUtility.SetDirty(w);
        }

        // ═══════════════════════════════════════════════════════════
        //  PROJECTILE PREFABS
        // ═══════════════════════════════════════════════════════════

        [MenuItem("CatsVsCapybaras/2 - Create Projectile Prefabs")]
        public static void CreateProjectilePrefabs()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Sprites/Projectiles");

            // Projectile bounds that match the terrain
            var boundsMin = new Vector2(-2f,  -20f);
            var boundsMax = new Vector2(30f,   25f);

            MakeProjectilePrefab<CarrotProjectile>("Projectile_Carrot",
                new Color(1f, 0.45f, 0f), null, boundsMin, boundsMax, false);

            // Link prefab → weapon asset
            LinkPrefabToWeapon("Assets/Weapons/Weapon_Carrot.asset",
                               "Assets/Prefabs/Projectile_Carrot.prefab");

            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] Carrot projectile prefab created in Assets/Prefabs/");
        }

        static void MakeProjectilePrefab<T>(string name, Color color, PhysicsMaterial2D mat,
                                            Vector2 bMin, Vector2 bMax, bool canRotate)
            where T : Component
        {
            string prefabPath = $"Assets/Prefabs/{name}.prefab";

            // Always delete and recreate so sorting order and other tweaks take effect
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);

            var go = new GameObject(name);

            // Coloured circle sprite — render in front of terrain (order 0) and characters (order 1)
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = GetOrCreateCircleSprite(name, color);
            sr.color        = Color.white;   // colour baked into texture
            sr.sortingOrder = 5;             // above terrain (0) and characters (1)

            // Physics
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.mass         = 0.3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints  = canRotate
                ? RigidbodyConstraints2D.None
                : RigidbodyConstraints2D.FreezeRotation;
            if (mat != null) rb.sharedMaterial = mat;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;
            if (mat != null) col.sharedMaterial = mat;

            // Projectile script (sets worldBounds via serialized props)
            var proj = go.AddComponent<T>() as ProjectileBase;
            if (proj != null)
            {
                var so = new SerializedObject(proj);
                so.FindProperty("worldBoundsMin").vector2Value = bMin;
                so.FindProperty("worldBoundsMax").vector2Value = bMax;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        static void LinkPrefabToWeapon(string weaponPath, string prefabPath)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (weapon == null || prefab == null) return;
            if (weapon.projectilePrefab == prefab) return;

            weapon.projectilePrefab = prefab;
            EditorUtility.SetDirty(weapon);
        }

        /// <summary>Creates a 16×16 circle sprite PNG at Assets/Sprites/Projectiles/.</summary>
        static Sprite GetOrCreateCircleSprite(string baseName, Color color)
        {
            string assetPath = $"Assets/Sprites/Projectiles/{baseName}_spr.png";
            string fullPath  = Application.dataPath +
                               $"/Sprites/Projectiles/{baseName}_spr.png";

            if (!File.Exists(fullPath))
            {
                const int S = 16;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                float cx = S / 2f - 0.5f, cy = S / 2f - 0.5f, r = S / 2f - 1.5f;
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = x - cx, dy = y - cy;
                        tex.SetPixel(x, y, (dx * dx + dy * dy <= r * r) ? color : Color.clear);
                    }
                tex.Apply();
                File.WriteAllBytes(fullPath, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(assetPath);

                var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType      = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.filterMode       = FilterMode.Point;
                    imp.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        // ═══════════════════════════════════════════════════════════
        //  WIRE SCENE REFERENCES
        // ═══════════════════════════════════════════════════════════

        [MenuItem("CatsVsCapybaras/3 - Wire Scene")]
        public static void WireScene()
        {
            // ── TeamManager ───────────────────────────────────────
            var teamMgr = Object.FindAnyObjectByType<TeamManager>();
            if (teamMgr != null)
            {
                var cat_miso   = CharacterByName("Cat_Miso");
                var cat_nova   = CharacterByName("Cat_Nova");
                var capy_bubba = CharacterByName("Capy_Bubba");
                var capy_lulu  = CharacterByName("Capy_Lulu");

                var so    = new SerializedObject(teamMgr);
                var teams = so.FindProperty("teams");
                teams.arraySize = 2;

                SetTeam(teams, 0, "Cats",       new Color(1f, 0.6f, 0.2f),
                        new CharacterController2D[] { cat_miso, cat_nova });
                SetTeam(teams, 1, "Capybaras",  new Color(0.4f, 0.8f, 0.3f),
                        new CharacterController2D[] { capy_bubba, capy_lulu });

                so.ApplyModifiedProperties();
                Debug.Log("[SceneBuilder] TeamManager wired: 2 teams, 2 characters each.");
            }
            else Debug.LogWarning("[SceneBuilder] TeamManager not found in scene.");

            // ── GameManager weapons ───────────────────────────────
            var gameMgr = Object.FindAnyObjectByType<GameManager>();
            if (gameMgr != null)
            {
                var carrot = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Weapons/Weapon_Carrot.asset");

                var so      = new SerializedObject(gameMgr);
                var weapons = so.FindProperty("weapons");
                weapons.arraySize = 1;
                weapons.GetArrayElementAtIndex(0).objectReferenceValue = carrot;
                so.ApplyModifiedProperties();
                Debug.Log("[SceneBuilder] GameManager wired with 1 weapon (Carrot).");
            }
            else Debug.LogWarning("[SceneBuilder] GameManager not found in scene.");

            // ── AimLine ───────────────────────────────────────────
            if (GameObject.Find("AimLine") == null)
            {
                var al = new GameObject("AimLine");
                al.AddComponent<LineRenderer>();  // AimLine requires LineRenderer
                al.AddComponent<AimLine>();
                Debug.Log("[SceneBuilder] AimLine added.");
            }

            // ── DeathZone ─────────────────────────────────────────
            if (GameObject.Find("DeathZone") == null)
            {
                var dz = new GameObject("DeathZone");
                dz.transform.position = new Vector3(14.07f, -9f, 0f);
                var dzCol = dz.AddComponent<BoxCollider2D>();
                dzCol.size      = new Vector2(60f, 2f);
                dzCol.isTrigger = true;
                dz.AddComponent<DeathZone>();
                Debug.Log("[SceneBuilder] DeathZone added at Y=-9.");
            }

            // ── SoundManager ─────────────────────────────────────
            if (Object.FindAnyObjectByType<SoundManager>() == null)
            {
                var smGo = new GameObject("SoundManager");
                var sm   = smGo.AddComponent<SoundManager>();
                smGo.AddComponent<AudioSource>();

                // Auto-wire carrot clips from Assets/Audio/SFX/
                var flyClip    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Carrot_Fly.wav");
                var impactClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Carrot_Impact.wav");
                var so = new SerializedObject(sm);
                so.FindProperty("carrotFlyClip").objectReferenceValue    = flyClip;
                so.FindProperty("carrotImpactClip").objectReferenceValue = impactClip;
                so.FindProperty("sfxSource").objectReferenceValue        = smGo.GetComponent<AudioSource>();
                so.ApplyModifiedProperties();
                Debug.Log("[SceneBuilder] SoundManager created and wired with carrot SFX.");
            }
            else Debug.Log("[SceneBuilder] SoundManager already exists.");

            // ── Boundary Walls (prevent infinite falling off edges) ────────────
            // Terrain spans world X = 0 to 28.14.
            // Wall centres are flush with the terrain edges so there is no gap to slip through.
            CreateBoundaryWall("WallLeft",   0f,     1f, 40f);   // spans X = -0.5 to  0.5
            CreateBoundaryWall("WallRight",  28.14f, 1f, 40f);   // spans X = 27.64 to 28.64
            Debug.Log("[SceneBuilder] Boundary walls created.");

            MarkDirty();
            Debug.Log("[SceneBuilder] Scene wired successfully.");
        }

        static void SetTeam(SerializedProperty teamsArr, int idx, string name,
                            Color color, CharacterController2D[] chars)
        {
            var t = teamsArr.GetArrayElementAtIndex(idx);
            t.FindPropertyRelative("teamName").stringValue  = name;
            t.FindPropertyRelative("teamColor").colorValue  = color;
            var charsProp = t.FindPropertyRelative("characters");
            charsProp.arraySize = chars.Length;
            for (int i = 0; i < chars.Length; i++)
                charsProp.GetArrayElementAtIndex(i).objectReferenceValue = chars[i];
        }

        static CharacterController2D CharacterByName(string n)
        {
            var go = GameObject.Find(n);
            return go != null ? go.GetComponent<CharacterController2D>() : null;
        }

        /// <summary>
        /// Creates (or recreates) an invisible solid wall at the given world X position.
        /// Characters bounce off it instead of falling infinitely off the map edge.
        /// </summary>
        static void CreateBoundaryWall(string wallName, float worldX, float width, float height)
        {
            var old = GameObject.Find(wallName);
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject(wallName);
            go.transform.position = new Vector3(worldX, 0f, 0f);

            var box  = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, height);
        }

        // ═══════════════════════════════════════════════════════════
        //  PROCEDURAL TERRAIN
        // ═══════════════════════════════════════════════════════════

        [MenuItem("CatsVsCapybaras/4 - Setup Procedural Terrain")]
        public static void SetupProceduralTerrain()
        {
            var terrainGo = GameObject.Find("Terrain");
            if (terrainGo == null)
            {
                Debug.LogWarning("[SceneBuilder] 'Terrain' GameObject not found in scene.");
                return;
            }

            // Ensure SpriteRenderer is enabled so the terrain is visible
            var sr = terrainGo.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            // Set PolygonCollider2D to solid (not trigger) so characters walk on terrain
            var polyCol = terrainGo.GetComponent<PolygonCollider2D>();
            if (polyCol != null)
            {
                polyCol.isTrigger = false;
                EditorUtility.SetDirty(polyCol);
                Debug.Log("[SceneBuilder] PolygonCollider2D set to solid (isTrigger=false).");
            }

            // Add ProceduralTerrainGenerator if not already present
            if (terrainGo.GetComponent<ProceduralTerrainGenerator>() == null)
            {
                terrainGo.AddComponent<ProceduralTerrainGenerator>();
                Debug.Log("[SceneBuilder] ProceduralTerrainGenerator added to Terrain.");
            }
            else
            {
                Debug.Log("[SceneBuilder] ProceduralTerrainGenerator already on Terrain.");
            }

            // Set a deep-night sky colour on the main camera to match the prototype
            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor  = new Color(0.04f, 0.04f, 0.10f, 1f);  // dark navy
                cam.clearFlags       = CameraClearFlags.SolidColor;
                EditorUtility.SetDirty(cam);
                Debug.Log("[SceneBuilder] Camera background set to dark navy.");
            }

            MarkDirty();
            Debug.Log("[SceneBuilder] Procedural terrain setup complete.");
        }

        // ═══════════════════════════════════════════════════════════
        //  FULL SETUP (runs 1-2-3-4 in sequence)
        // ═══════════════════════════════════════════════════════════

        [MenuItem("CatsVsCapybaras/0 - Run Full Setup")]
        public static void RunFullSetup()
        {
            CreateWeaponAssets();
            AssetDatabase.Refresh();
            CreateProjectilePrefabs();
            AssetDatabase.Refresh();
            WireScene();
            SetupProceduralTerrain();
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] ★ Full Setup complete! Hit Play to test.");
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        static void EnsureFolder(string assetPath)
        {
            string full = Application.dataPath + assetPath.Substring(6); // remove "Assets"
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
                AssetDatabase.Refresh();
            }
        }

        static void MarkDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
