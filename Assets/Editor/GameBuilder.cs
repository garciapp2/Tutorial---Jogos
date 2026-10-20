using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Monta as cenas de Cold Ember a partir da pixel art (Tools/art, gerada com o pxl) e do mapa
/// em Assets/Levels/arena.txt. Menu: Cold Ember > Construir tudo.
/// Toda a interface e montada em pixels logicos: o canvas tem 300x240 unidades (600x480 em 2x).
/// </summary>
public static class GameBuilder
{
    const string PixelDir = "Assets/Art/Pixel/";
    const string AnimDir = "Assets/Art/Pixel/Anims/";
    const string GenDir = "Assets/Art/Generated/";
    const string AudioDir = "Assets/Audio/";
    const string SceneDir = "Assets/Scenes/";
    const string MapPath = "Assets/Levels/arena.txt";
    const string WallLayerName = "Walls";
    const float CameraSize = 7.5f; // 240 px de altura / 2 / 16 px por unidade (janela de 600x480 em 2x)

    static Material unlit;
    static Material swirl;
    static VolumeProfile profile;
    static readonly Dictionary<string, Sprite[]> anims = new Dictionary<string, Sprite[]>();

    [MenuItem("Cold Ember/Construir tudo")]
    public static void BuildAll()
    {
        EnsureLayer(WallLayerName);
        ImportArt();
        ImportAudio();
        CreateAssets();

        BuildTutorialScene();
        BuildGameScene();
        BuildMenuScene();
        ConfigureBuild();

        EditorSceneManager.OpenScene(SceneDir + "MainMenu.unity");
        Debug.Log("[GameBuilder] Cold Ember construido.");
    }

    // ================================================================ assets
    static void EnsureLayer(string name)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == name) return;
        for (int i = 8; i < layers.arraySize; i++)
        {
            var p = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(p.stringValue)) continue;
            p.stringValue = name;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
    }

    static void ImportArt()
    {
        AssetDatabase.Refresh();
        foreach (var path in Directory.GetFiles(PixelDir, "*.png"))
            PixelSprite(path.Replace('\\', '/'), 16, Vector4.zero);
        PixelSprite("Assets/Resources/ui_notch.png", 100, new Vector4(2, 2, 2, 2));
        PixelSprite("Assets/Resources/ui_pixel.png", 100, Vector4.zero);

        // fonte: textura comum, sem filtro
        var font = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/PixelFont.png");
        font.textureType = TextureImporterType.Default;
        font.filterMode = FilterMode.Point;
        font.mipmapEnabled = false;
        font.npotScale = TextureImporterNPOTScale.None;
        font.textureCompression = TextureImporterCompression.Uncompressed;
        font.alphaIsTransparency = true;
        font.wrapMode = TextureWrapMode.Clamp;
        font.SaveAndReimport();

        // anims do pxl: tiras horizontais, fatiadas pelo numero de quadros do JSON
        anims.Clear();
        foreach (var path in Directory.GetFiles(AnimDir, "*.png"))
        {
            string p = path.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(p);
            string json = File.ReadAllText(Path.ChangeExtension(p, ".json"));
            int frames = Mathf.Max(1, json.Split(new[] { "\"duration\"" }, System.StringSplitOptions.None).Length - 1);
            SliceStrip(p, name, frames);
            anims[name] = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        }
    }

    static void PixelSprite(string path, float ppu, Vector4 border)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        ti.spriteBorder = border;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.wrapMode = TextureWrapMode.Clamp;
        ti.SetTextureSettings(settings);
        ti.SaveAndReimport();
    }

    static void SliceStrip(string path, string name, int frames)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();

        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        int fw = w / frames;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[frames];
        for (int i = 0; i < frames; i++)
        {
            rects[i] = new SpriteRect
            {
                name = $"{name}_{i}",
                rect = new Rect(i * fw, 0, fw, h),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate(),
            };
        }
        dp.SetSpriteRects(rects);
        var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        ids?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        dp.Apply();
        ti.SaveAndReimport();
    }

    static void ImportAudio()
    {
        foreach (var path in Directory.GetFiles(AudioDir, "*.wav"))
        {
            string p = path.Replace('\\', '/');
            var ai = (AudioImporter)AssetImporter.GetAtPath(p);
            if (ai == null) continue;
            bool music = Path.GetFileName(p).StartsWith("music");
            var s = ai.defaultSampleSettings;
            s.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = music ? 0.55f : 0.75f;
            ai.defaultSampleSettings = s;
            ai.forceToMono = true;
            ai.loadInBackground = music;
            ai.SaveAndReimport();
        }
    }

    static Sprite Px(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PixelDir + name + ".png");
    static Sprite[] Anim(string name) => anims[name];
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + name + ".wav");

    static void CreateAssets()
    {
        Directory.CreateDirectory(GenDir);
        unlit = AssetDatabase.LoadAssetAtPath<Material>(GenDir + "PixelUnlit.mat");
        if (unlit == null)
        {
            unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(unlit, GenDir + "PixelUnlit.mat");
        }
        swirl = AssetDatabase.LoadAssetAtPath<Material>(GenDir + "Swirl.mat");
        if (swirl == null)
        {
            swirl = new Material(Shader.Find("ColdEmber/Swirl"));
            AssetDatabase.CreateAsset(swirl, GenDir + "Swirl.mat");
        }

        // pos-processamento: florada, vinheta, franja de cor e a curvatura da tela de fliperama
        string vp = GenDir + "ScreenProfile.asset";
        AssetDatabase.DeleteAsset(vp);
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, vp);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.75f);
        bloom.intensity.Override(0.7f);
        bloom.scatter.Override(0.6f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.32f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(Color.black);
        var chroma = profile.Add<ChromaticAberration>(true);
        chroma.intensity.Override(0.04f);
        var lens = profile.Add<LensDistortion>(true);
        lens.intensity.Override(-0.07f);
        lens.scale.Override(1f);
        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    static TileBase MakeTile(string sprite, bool solid)
    {
        string path = GenDir + "Tile_" + sprite + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = Px(sprite);
        tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // ================================================================ cena do tutorial
    /// <summary>Exatamente o resultado do tutorial "Introducao Rapida", para referencia.</summary>
    static void BuildTutorialScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Pal.Dusk; // fundo da camera alterado
        cam.transform.position = new Vector3(0, 0, -10);
        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;

        // Player: sprite circulo vermelho + Rigidbody2D (gravidade 0) + CircleCollider2D + PlayerMovement
        var player = new GameObject("Player");
        player.tag = "Player";
        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = Px("player_white");
        sr.color = Color.red;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        player.AddComponent<CircleCollider2D>();
        player.AddComponent<PlayerMovement>().speed = 5f;

        // Paredes: sprites quadrados com BoxCollider2D
        var walls = new GameObject("Walls");
        void Wall(string n, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(n);
            go.transform.SetParent(walls.transform);
            go.transform.position = pos;
            var w = go.AddComponent<SpriteRenderer>();
            w.sprite = Px("wall_0");
            w.drawMode = SpriteDrawMode.Tiled;
            w.size = size;
            go.AddComponent<BoxCollider2D>().size = size;
        }
        Wall("Wall Top", new Vector2(0, 4.5f), new Vector2(18, 1));
        Wall("Wall Bottom", new Vector2(0, -4.5f), new Vector2(18, 1));
        Wall("Wall Left", new Vector2(-8.5f, 0), new Vector2(1, 8));
        Wall("Wall Right", new Vector2(8.5f, 0), new Vector2(1, 8));

        EditorSceneManager.SaveScene(scene, SceneDir + "Tutorial.unity");
    }

    // ================================================================ partes comuns
    static Camera CreateCamera(out Backdrop backdrop)
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Pal.Ink;
        go.transform.position = new Vector3(0, 0, -10);
        go.AddComponent<AudioListener>();
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.None;

        var bd = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bd.name = "Redemoinho";
        Object.DestroyImmediate(bd.GetComponent<Collider>());
        var mr = bd.GetComponent<MeshRenderer>();
        mr.sharedMaterial = swirl;
        mr.sortingOrder = -100;
        backdrop = bd.AddComponent<Backdrop>();
        backdrop.cam = cam;

        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;

        var vol = new GameObject("Tela (pos-processamento)").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        return cam;
    }

    static Fx CreateFx(Volume volume, Image flash, bool calm)
    {
        var fx = new GameObject("Fx").AddComponent<Fx>();
        fx.volume = volume;
        fx.flashOverlay = flash;
        fx.particleMaterial = unlit;
        fx.calm = calm;
        return fx;
    }

    static AudioManager CreateAudio(bool game)
    {
        var am = new GameObject("AudioManager").AddComponent<AudioManager>();
        if (game)
        {
            am.musicBase = Clip("music_game_base");
            am.musicDrums = Clip("music_game_drums");
            am.musicLead = Clip("music_game_lead");
        }
        else am.musicBase = Clip("music_menu");
        string[] names = { "pickup", "fragment", "hurt", "dash", "bump", "unlock", "ignite", "win", "ko",
            "ui_move", "ui_confirm", "ui_back", "ui_deny", "warning", "alert", "frost" };
        am.clips = names.Select(n => Clip("sfx_" + n)).ToArray();
        return am;
    }

    static SpriteRenderer SR(GameObject go, Sprite sprite, int order)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.sharedMaterial = unlit;
        return sr;
    }

    static GameObject Child(Transform parent, string name, Vector3 local = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        return go;
    }

    static SpriteAnim Animate(GameObject go, string anim, float fps)
    {
        var a = go.AddComponent<SpriteAnim>();
        a.frames = Anim(anim);
        a.fps = fps;
        return a;
    }

    static void Hitbox(Transform parent, float radius)
    {
        var hit = Child(parent, "Hitbox");
        var c = hit.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = radius;
        hit.AddComponent<DamageOnTouch>();
    }

    static GameObject CreatePlayer(Vector2 pos, bool controllable)
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<CircleCollider2D>().radius = 0.4f;
        if (controllable)
        {
            go.AddComponent<PlayerMovement>().speed = 6f;
            go.AddComponent<PlayerHealth>();
        }

        // a brasa: bola de vidro (o circulo vermelho do tutorial) com o fogo preso dentro
        var body = Child(go.transform, "Corpo");
        var bodySr = SR(body, Px("orb_back"), 10);
        var flame = Child(body.transform, "Fogo");
        var flameSr = SR(flame, Anim("core")[0], 11);
        Animate(flame, "core", 12f);
        var glass = Child(body.transform, "Vidro");
        var glassSr = SR(glass, Px("orb_glass"), 12);
        var white = Child(body.transform, "Clarao");
        var whiteSr = SR(white, Px("player_white"), 13);
        var arrow = new GameObject("Seta da fogueira");
        SR(arrow, Px("arrow"), 12);
        arrow.SetActive(false);

        var visual = go.AddComponent<PlayerVisual>();
        visual.body = body.transform;
        visual.bodyRenderer = bodySr;
        visual.whiteFlash = whiteSr;
        visual.flame = flameSr;
        visual.glass = glassSr;
        visual.arrow = arrow.transform;
        visual.ghostMaterial = unlit;
        return go;
    }

    static void CreateGhost(Transform parent, Vector2 pos, Vector2 dir, LayerMask walls, float speed = 3.2f)
    {
        var go = Child(parent, "Espirito de Gelo (patrulha)");
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var patrol = go.AddComponent<PatrolEnemy>();
        patrol.direction = dir;
        patrol.speed = speed;
        patrol.wallMask = walls;
        var vis = Child(go.transform, "Visual");
        SR(vis, Anim("ghost")[0], 5);
        Animate(vis, "ghost", 5f);
        patrol.visual = vis.transform;
        Hitbox(go.transform, 0.36f);
    }

    static void CreateHunter(Transform parent, Vector2 pos, Vector2 dir, LayerMask walls)
    {
        var go = Child(parent, "Cacador Glacial (persegue)");
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.mass = 5f;
        go.AddComponent<CircleCollider2D>().radius = 0.42f;
        var hunter = go.AddComponent<HunterEnemy>();
        hunter.patrolDirection = dir;
        hunter.wallMask = walls;
        var vis = Child(go.transform, "Visual");
        hunter.body = SR(vis, Anim("hunter")[0], 5);
        Animate(vis, "hunter", 4f);
        var alert = Child(go.transform, "Alerta", new Vector3(0, 0.95f, 0));
        SR(alert, Px("alert"), 13);
        hunter.alertIcon = alert;
        Hitbox(go.transform, 0.4f);
    }

    static Transform CreatePickup(Transform parent, Vector2 pos, Pickup.Kind kind)
    {
        bool frag = kind == Pickup.Kind.Fragment;
        var go = Child(parent, frag ? "Fragmento de Chama" : "Carvao");
        go.transform.position = pos;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = frag ? 0.45f : 0.5f;
        var p = go.AddComponent<Pickup>();
        p.type = kind;
        var vis = Child(go.transform, "Visual");
        SR(vis, Anim(frag ? "fragment" : "coal")[0], 2);
        Animate(vis, frag ? "fragment" : "coal", frag ? 8f : 6f);
        p.visual = vis.transform;
        return go.transform;
    }

    static void CreateSpikes(Transform parent, Vector2 pos)
    {
        var go = Child(parent, "Cristais de Gelo");
        go.transform.position = pos;
        SR(go, Px("spikes"), 1);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.75f, 0.6f);
        box.offset = new Vector2(0f, -0.15f);
        go.AddComponent<DamageOnTouch>();
        go.AddComponent<IceCrystal>();
    }

    static ExitBonfire CreateBonfire(Vector2 pos)
    {
        var go = new GameObject("Fogueira (saida)");
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.2f, 0.9f);
        var vis = Child(go.transform, "Sprite");
        var sr = SR(vis, Px("bonfire_off"), 0);
        var anim = Animate(vis, "bonfire_lit", 10f);
        anim.enabled = false;
        var exit = go.AddComponent<ExitBonfire>();
        exit.sprite = sr;
        exit.frozenSprite = Px("bonfire_off");
        exit.litFrames = Anim("bonfire_lit");
        return exit;
    }

    // ================================================================ tilemap
    static int Hash(int x, int y) { unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return (h ^ (h >> 16)) & 0x7fffffff; } }

    /// <summary>Monta chao, gelo e paredes. at(x, y) = caractere; y cresce para cima.</summary>
    static Grid BuildTiles(int minX, int minY, int w, int h, System.Func<int, int, char> at, int wallLayer)
    {
        var grid = new GameObject("Cripta").AddComponent<Grid>();
        Tilemap Map(string name, int order, bool walls)
        {
            var go = Child(grid.transform, name);
            var tm = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = order;
            r.sharedMaterial = unlit;
            if (walls) go.layer = wallLayer;
            return tm;
        }
        var floor = Map("Chao", -20, false);
        var frost = Map("Gelo", -19, false);
        var walls = Map("Paredes", -10, true);
        // o chao deixa o redemoinho pulsar de leve por baixo
        floor.color = new Color(1f, 1f, 1f, 0.5f);

        var floorTiles = new[] { MakeTile("floor_a", false), MakeTile("floor_a", false), MakeTile("floor_a", false), MakeTile("floor_b", false), MakeTile("floor_c", false) };
        var frostTiles = new[] { MakeTile("frost_a", false), MakeTile("frost_a", false), MakeTile("frost_b", false) };
        var pitTile = MakeTile("pit", false); var pitTop = MakeTile("pit_top", false);
        var w0 = MakeTile("wall_0", true); var wn = MakeTile("wall_n", true);
        var ws = MakeTile("wall_s", true); var wns = MakeTile("wall_ns", true);

        for (int y = minY; y < minY + h; y++)
        for (int x = minX; x < minX + w; x++)
        {
            char c = at(x, y);
            var cell = new Vector3Int(x, y, 0);
            if (c == '#')
            {
                bool north = at(x, y + 1) != '#';
                bool south = at(x, y - 1) != '#';
                walls.SetTile(cell, north && south ? wns : north ? wn : south ? ws : w0);
            }
            else if (c == '_')
            {
                frost.SetTile(cell, at(x, y + 1) == '_' ? pitTile : pitTop);
            }
            else if (c != ' ')
            {
                floor.SetTile(cell, floorTiles[Hash(x, y) % floorTiles.Length]);
                if (c == '~' || IsFrost(at, x, y)) frost.SetTile(cell, frostTiles[Hash(y, x) % frostTiles.Length]);
            }
        }
        // colisao: retangulos mesclados (linhas e depois colunas), um BoxCollider2D cada.
        // (O TilemapCollider2D com CompositeCollider2D gerava zero formas e deixava atravessar.)
        var solid = Child(grid.transform, "Colisao das Paredes");
        solid.layer = wallLayer;
        var used = new HashSet<Vector2Int>();
        for (int y = minY; y < minY + h; y++)
        for (int x = minX; x < minX + w; x++)
        {
            if (used.Contains(new Vector2Int(x, y)) || at(x, y) != '#') continue;
            int rw = 1;
            while (x + rw < minX + w && at(x + rw, y) == '#' && !used.Contains(new Vector2Int(x + rw, y))) rw++;
            int rh = 1;
            while (y + rh < minY + h)
            {
                bool full = true;
                for (int i = 0; i < rw; i++)
                    if (at(x + i, y + rh) != '#' || used.Contains(new Vector2Int(x + i, y + rh))) { full = false; break; }
                if (!full) break;
                rh++;
            }
            for (int j = 0; j < rh; j++) for (int i = 0; i < rw; i++) used.Add(new Vector2Int(x + i, y + j));
            var box = solid.AddComponent<BoxCollider2D>();
            box.offset = new Vector2(x + rw / 2f, y + rh / 2f);
            box.size = new Vector2(rw, rh);
        }
        // abismos: gatilhos um pouco menores que as casas (cai quem para com o centro em cima)
        var pits = Child(grid.transform, "Abismos");
        pits.AddComponent<Pit>();
        var seenPit = new HashSet<Vector2Int>();
        for (int y = minY; y < minY + h; y++)
        for (int x = minX; x < minX + w; x++)
        {
            if (at(x, y) != '_' || seenPit.Contains(new Vector2Int(x, y))) continue;
            int rw = 1;
            while (at(x + rw, y) == '_' && !seenPit.Contains(new Vector2Int(x + rw, y))) rw++;
            int rh = 1;
            while (true)
            {
                bool full = true;
                for (int i = 0; i < rw; i++) if (at(x + i, y + rh) != '_') { full = false; break; }
                if (!full) break;
                rh++;
            }
            for (int j = 0; j < rh; j++) for (int i = 0; i < rw; i++) seenPit.Add(new Vector2Int(x + i, y + j));
            var box = pits.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.offset = new Vector2(x + rw / 2f, y + rh / 2f);
            box.size = new Vector2(rw - 0.8f, rh - 0.8f);
        }
        return grid;
    }

    /// <summary>Uma celula especial cercada de gelo tambem recebe gelo (o fragmento no meio da sala fria).</summary>
    static bool IsFrost(System.Func<int, int, char> at, int x, int y)
    {
        int n = 0;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dy != 0) && at(x + dx, y + dy) == '~') n++;
        return n >= 6;
    }

    // ================================================================ cena do jogo
    static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string[] rows = File.ReadAllLines(MapPath).Where(r => r.Length > 0).ToArray();
        int H = rows.Length, W = rows[0].Length;
        int wallLayer = LayerMask.NameToLayer(WallLayerName);
        LayerMask wallMask = 1 << wallLayer;
        char At(int x, int y)
        {
            int row = H - 1 - y;
            if (x < 0 || x >= W || row < 0 || row >= H) return ' ';
            return rows[row][x];
        }
        Vector2 Cell(int x, int row) => new Vector2(x + 0.5f, H - 1 - row + 0.5f);

        var cam = CreateCamera(out var backdrop);
        backdrop.warm = Pal.BgMenu;
        backdrop.cold = Pal.BgCold;
        CreateAudio(true);
        BuildTiles(0, 0, W, H, At, wallLayer);

        // gatilhos das zonas congeladas
        var frostRoot = new GameObject("Zonas Congeladas");
        foreach (var r in Components(rows, '~'))
        {
            var go = Child(frostRoot.transform, "Zona Congelada");
            go.transform.position = new Vector3(r.x + r.width / 2f, H - r.y - r.height / 2f, 0);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(r.width - 0.3f, r.height - 0.3f);
            go.AddComponent<FrostZone>();
        }

        var pickups = new GameObject("Coletaveis").transform;
        var hazards = new GameObject("Perigos").transform;
        var enemies = new GameObject("Inimigos").transform;
        GameObject player = null;
        ExitBonfire exit = null;
        for (int row = 0; row < H; row++)
        for (int x = 0; x < W; x++)
        {
            var pos = Cell(x, row);
            switch (rows[row][x])
            {
                case 'P': player = CreatePlayer(pos, true); break;
                case 'B': exit = CreateBonfire(pos); break;
                case 'c': CreatePickup(pickups, pos, Pickup.Kind.Coal); break;
                case 'F': CreatePickup(pickups, pos, Pickup.Kind.Fragment); break;
                case '^': CreateSpikes(hazards, pos); break;
                case 'h': CreateGhost(enemies, pos, Vector2.right, wallMask); break;
                case 'v': CreateGhost(enemies, pos, Vector2.up, wallMask); break;
                case 'H': CreateHunter(enemies, pos, Vector2.right, wallMask); break;
                case 'V': CreateHunter(enemies, pos, Vector2.up, wallMask); break;
            }
        }

        var rig = cam.gameObject.AddComponent<CameraRig>();
        rig.target = player.transform;
        rig.bounds = new Rect(0, 0, W, H);
        rig.baseSize = CameraSize;
        cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10);
        cam.gameObject.AddComponent<AmbientSnow>().cam = cam;

        var canvas = CreateCanvas(cam, out var flash);
        var hud = BuildHud(canvas.transform);
        CreateFx(Object.FindAnyObjectByType<Volume>(), flash, false);

        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.player = player.GetComponent<PlayerHealth>();
        gm.exit = exit;
        gm.hud = hud;
        gm.backdrop = backdrop;
        EditorSceneManager.SaveScene(scene, SceneDir + "Game.unity");
    }

    struct RectI { public int x, y, width, height; }

    /// <summary>Retangulo envolvente de cada grupo conectado de um caractere (8 vizinhos).</summary>
    static List<RectI> Components(string[] rows, char ch)
    {
        int H = rows.Length, W = rows[0].Length;
        var seen = new bool[W, H];
        var result = new List<RectI>();
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            if (seen[x, y] || rows[y][x] != ch) continue;
            int minX = x, maxX = x, minY = y, maxY = y;
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(x, y));
            seen[x, y] = true;
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = p.x + dx, ny = p.y + dy;
                    if (nx < 0 || ny < 0 || nx >= W || ny >= H || seen[nx, ny] || rows[ny][nx] != ch) continue;
                    seen[nx, ny] = true;
                    stack.Push(new Vector2Int(nx, ny));
                }
            }
            result.Add(new RectI { x = minX, y = minY, width = maxX - minX + 1, height = maxY - minY + 1 });
        }
        return result;
    }

    // ================================================================ UI em pixel logico
    static Canvas CreateCanvas(Camera cam, out Image flash)
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(300, 240);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        scaler.referencePixelsPerUnit = 100;
        go.AddComponent<GraphicRaycaster>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        ConfigureUIModule(es.AddComponent<InputSystemUIInputModule>());

        // clarao de tela: o ultimo filho, por cima de tudo
        var f = Stretch(go.transform, "Clarao");
        flash = f.gameObject.AddComponent<Image>();
        flash.color = Pal.Alpha(Pal.White, 0f);
        flash.raycastTarget = false;
        flash.enabled = false;
        return canvas;
    }

    static void ConfigureUIModule(InputSystemUIInputModule module)
    {
        const string path = "Assets/Settings/InputSystem_Actions.inputactions";
        var refs = AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>().ToList();
        InputActionReference R(string name) => refs.FirstOrDefault(r => r.action != null && r.action.actionMap.name == "UI" && r.action.name == name);
        module.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        module.point = R("Point");
        module.leftClick = R("Click");
        module.scrollWheel = R("ScrollWheel");
        // navegacao e confirmacao ficam com o PixelMenu
        module.move = null;
        module.submit = null;
        module.cancel = null;
    }

    static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(Transform parent, string name)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static readonly Vector2 TL = new Vector2(0, 1), TC = new Vector2(0.5f, 1), TR = new Vector2(1, 1);
    static readonly Vector2 MC = new Vector2(0.5f, 0.5f), BL = Vector2.zero, BC = new Vector2(0.5f, 0);

    static PixelText Text(Transform parent, string name, string text, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
        Color color, int scale = 1, PixelText.Align align = PixelText.Align.Left, bool shadow = true, bool outline = false, bool middle = false, bool wrap = false)
    {
        var rt = Box(parent, name, anchor, pivot, pos, size);
        var t = rt.gameObject.AddComponent<PixelText>();
        t.Text = text;
        t.color = color;
        t.scale = scale;
        t.align = align;
        t.shadow = shadow;
        t.outline = outline;
        t.middle = middle;
        t.wrap = wrap;
        t.raycastTarget = false;
        return t;
    }

    static Image Icon(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, float scale = 1f)
    {
        var rt = Box(parent, name, anchor, MC, pos, new Vector2(sprite.rect.width, sprite.rect.height) * scale);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    static RectTransform Card(Transform parent, string name, Vector2 size, Vector2 pos, Color border)
    {
        var rt = Box(parent, name, MC, MC, pos, size);
        PixelPanel.Create(rt, Pal.Night, border);
        return rt;
    }

    static PixelMenuItem Item(Transform parent, PixelMenu menu, string label, Color color, Vector2 pos, Vector2 size, UnityAction action, bool option = false)
    {
        var root = Box(parent, label, TC, TC, pos, size);
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0); // area de clique
        var item = root.gameObject.AddComponent<PixelMenuItem>();
        var visual = Stretch(root, "Visual");
        item.visual = visual;
        item.panel = PixelPanel.Create(visual, Pal.Dusk, Pal.Slate);
        item.color = color;
        item.label = Text(visual, "Rotulo", label, MC, MC, new Vector2(option ? 8 : 0, 0), size - new Vector2(option ? 16 : 0, 0), Pal.Bone, 1,
            option ? PixelText.Align.Left : PixelText.Align.Center, false, false, true);
        if (option)
            item.valueText = Text(visual, "Valor", "", MC, MC, new Vector2(-8, 0), size - new Vector2(16, 0), Pal.Fog, 1, PixelText.Align.Right, false, false, true);
        if (action != null) UnityEventTools.AddPersistentListener(item.onConfirm, action);
        menu.items.Add(item);
        return item;
    }

    static PixelMenu Menu(GameObject go)
    {
        var m = go.GetComponent<PixelMenu>();
        return m != null ? m : go.AddComponent<PixelMenu>();
    }

    static OptionsPanel BuildOptionsPanel(Transform parent)
    {
        var root = Stretch(parent, "Opcoes");
        var card = Card(root, "Card", new Vector2(250, 182), Vector2.zero, Pal.Purple);
        Text(card, "Titulo", "OPÇÕES", TC, TC, new Vector2(0, -12), new Vector2(230, 20), Pal.White, 2, PixelText.Align.Center);
        var opts = root.gameObject.AddComponent<OptionsPanel>();
        var menu = Menu(card.gameObject);
        opts.menu = menu;
        var size = new Vector2(214, 18);
        float y = -40;
        opts.music = Item(card, menu, "MÚSICA", Pal.Purple, new Vector2(0, y), size, null, true); y -= 22;
        opts.sfx = Item(card, menu, "EFEITOS", Pal.Purple, new Vector2(0, y), size, null, true); y -= 22;
        opts.shake = Item(card, menu, "TREMOR DE TELA", Pal.Purple, new Vector2(0, y), size, null, true); y -= 22;
        opts.flashes = Item(card, menu, "CLARÕES", Pal.Purple, new Vector2(0, y), size, null, true); y -= 22;
        opts.crt = Item(card, menu, "TELA DE FLIPERAMA", Pal.Purple, new Vector2(0, y), size, null, true); y -= 26;
        opts.back = Item(card, menu, "VOLTAR", Pal.Slate, new Vector2(0, y), new Vector2(120, 18), null);
        return opts;
    }

    static HUD BuildHud(Transform canvas)
    {
        var root = Stretch(canvas, "HUD");
        root.SetAsFirstSibling();
        var hud = root.gameObject.AddComponent<HUD>();
        hud.heartFull = Px("heart");
        hud.heartEmpty = Px("heart_empty");

        // --- vida e pontos
        var left = Box(root, "Vida e Pontos", TL, TL, new Vector2(8, -8), new Vector2(92, 34));
        PixelPanel.Create(left, Pal.Night, Pal.Slate);
        hud.hearts = new Image[3];
        for (int i = 0; i < 3; i++) hud.hearts[i] = Icon(left, "Coracao" + (i + 1), Px("heart"), TL, new Vector2(12 + i * 14, -11));
        Text(left, "PontosRotulo", "PONTOS", TL, TL, new Vector2(7, -22), new Vector2(40, 10), Pal.Fog, 1, PixelText.Align.Left, false);
        hud.scoreText = Text(left, "Pontos", "000000", TR, TR, new Vector2(-7, -22), new Vector2(50, 10), Pal.White, 1, PixelText.Align.Right);

        // --- calor (cronometro regressivo)
        var heat = Box(root, "Calor", BC, BC, new Vector2(0, 8), new Vector2(178, 22));
        PixelPanel.Create(heat, Pal.Night, Pal.Slate);
        hud.heatIcon = Icon(heat, "Icone", Px("icon_heat"), TL, new Vector2(11, -11)).rectTransform;
        var bar = Box(heat, "Barra", TL, TL, new Vector2(22, -7), new Vector2(122, 8));
        bar.gameObject.AddComponent<Image>().color = Pal.Ink;
        var inner = Box(bar, "Fundo", TL, TL, new Vector2(1, -1), new Vector2(120, 6));
        inner.gameObject.AddComponent<Image>().color = Pal.Dusk;
        hud.heatLag = Box(inner, "Atraso", TL, TL, Vector2.zero, new Vector2(120, 6));
        hud.heatLag.gameObject.AddComponent<Image>().color = Pal.White;
        hud.heatFill = Box(inner, "Calor", TL, TL, Vector2.zero, new Vector2(120, 6));
        hud.heatFillImage = hud.heatFill.gameObject.AddComponent<Image>();
        hud.heatShine = Box(inner, "Brilho", TL, TL, Vector2.zero, new Vector2(120, 1));
        hud.heatShine.gameObject.AddComponent<Image>().color = Pal.Alpha(Pal.White, 0.35f);
        hud.heatBarWidth = 120;
        hud.heatText = Text(heat, "Segundos", "30.0s", TR, TR, new Vector2(-6, -8), new Vector2(30, 10), Pal.White, 1, PixelText.Align.Right);
        hud.frostLabel = Text(root, "Congelando", "CONGELANDO!", BC, BC, new Vector2(0, 36), new Vector2(120, 10), Pal.BlueLight, 1, PixelText.Align.Center, true, false);

        // --- tempo e fragmentos
        var right = Box(root, "Tempo e Fragmentos", TR, TR, new Vector2(-8, -8), new Vector2(78, 34));
        PixelPanel.Create(right, Pal.Night, Pal.Slate);
        Icon(right, "Relogio", Px("icon_clock"), TL, new Vector2(12, -11));
        hud.timeText = Text(right, "Tempo", "00:00.0", TR, TR, new Vector2(-7, -8), new Vector2(54, 10), Pal.White, 1, PixelText.Align.Right);
        hud.fragmentIcon = Icon(right, "Fragmento", Px("icon_fragment"), TL, new Vector2(12, -24)).rectTransform;
        hud.fragmentText = Text(right, "Fragmentos", "0/5", TR, TR, new Vector2(-7, -21), new Vector2(54, 10), Pal.White, 1, PixelText.Align.Right);

        hud.hint = Text(root, "Dica", "", TC, TC, new Vector2(0, -46), new Vector2(290, 10), Pal.Fog, 1, PixelText.Align.Center, true);

        // --- mensagens
        hud.popupLayer = Stretch(root, "Popups");
        hud.popupTemplate = Text(hud.popupLayer, "PopupModelo", "+3s", MC, MC, Vector2.zero, new Vector2(200, 24), Pal.Orange, 1, PixelText.Align.Center, true, true, true);
        hud.banner = Text(root, "Faixa", "", MC, MC, new Vector2(0, 40), new Vector2(296, 40), Pal.Yellow, 3, PixelText.Align.Center, true, true, true);
        hud.banner.dropIn = true;

        // --- escurecer
        var dimRt = Stretch(root, "Escurecer");
        hud.dim = dimRt.gameObject.AddComponent<Image>();
        hud.dim.raycastTarget = false;

        // --- pausa
        var pause = Stretch(root, "Pausa");
        var pc = Card(pause, "Card", new Vector2(160, 136), Vector2.zero, Pal.Red);
        Text(pc, "Titulo", "PAUSA", TC, TC, new Vector2(0, -12), new Vector2(150, 20), Pal.White, 2, PixelText.Align.Center);
        var pm = Menu(pc.gameObject);
        var isz = new Vector2(128, 18);
        Item(pc, pm, "CONTINUAR", Pal.Red, new Vector2(0, -38), isz, hud.OnResume);
        Item(pc, pm, "REINICIAR", Pal.Blue, new Vector2(0, -60), isz, hud.OnRestart);
        Item(pc, pm, "OPÇÕES", Pal.Purple, new Vector2(0, -82), isz, hud.OnOptions);
        Item(pc, pm, "MENU INICIAL", Pal.Slate, new Vector2(0, -104), isz, hud.OnMenu);
        hud.pausePanel = pause.gameObject;
        hud.pauseMenu = pm;
        hud.options = BuildOptionsPanel(root);

        // --- resultado / derrota
        var res = Stretch(root, "Resultado");
        var rc = Card(res, "Card", new Vector2(288, 196), new Vector2(0, 0), Pal.Orange);
        hud.resultsCard = rc;
        hud.resultsTitle = Text(rc, "Titulo", "", TC, TC, new Vector2(0, -12), new Vector2(280, 20), Pal.Yellow, 2, PixelText.Align.Center);
        hud.resultsReason = Text(rc, "Motivo", "", TL, TL, new Vector2(16, -38), new Vector2(200, 20), Pal.Bone, 1, PixelText.Align.Left, false, false, false, true);
        hud.resultsLabels = Text(rc, "Rotulos", "", TL, TL, new Vector2(16, -64), new Vector2(110, 60), Pal.Fog, 1, PixelText.Align.Left, false);
        hud.resultsValues = Text(rc, "Valores", "", TL, TL, new Vector2(16, -64), new Vector2(196, 60), Pal.White, 1, PixelText.Align.Right, true);
        hud.resultsGrade = Text(rc, "Nota", "", TR, MC, new Vector2(-44, -78), new Vector2(60, 60), Pal.Yellow, 7, PixelText.Align.Center, true, true, true);
        hud.resultsGradeLabel = Text(rc, "NotaRotulo", "NOTA", TR, MC, new Vector2(-44, -44), new Vector2(60, 10), Pal.Fog, 1, PixelText.Align.Center, false).gameObject;
        hud.resultsIcon = Icon(rc, "BrasaApagada", Px("player_white"), TR, new Vector2(-44, -72), 3f);
        hud.resultsIcon.color = Pal.BlueLight;
        var rm = Menu(rc.gameObject);
        Item(rc, rm, "JOGAR DE NOVO", Pal.Red, new Vector2(0, -144), isz, hud.OnRestart);
        Item(rc, rm, "MENU INICIAL", Pal.Slate, new Vector2(0, -166), isz, hud.OnMenu);
        hud.resultsPanel = res.gameObject;
        hud.resultsMenu = rm;
        return hud;
    }

    // ================================================================ menu inicial
    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        int wallLayer = LayerMask.NameToLayer(WallLayerName);
        LayerMask wallMask = 1 << wallLayer;
        var cam = CreateCamera(out var backdrop);
        backdrop.warm = Pal.BgMenu;
        backdrop.cold = Pal.BgMenu;
        CreateAudio(false);

        cam.gameObject.AddComponent<AmbientSnow>().cam = cam;

        // --- UI
        var canvas = CreateCanvas(cam, out var flash);
        CreateFx(Object.FindAnyObjectByType<Volume>(), flash, true);
        var root = Stretch(canvas.transform, "Menu");
        root.SetAsFirstSibling();
        var menu = root.gameObject.AddComponent<MainMenu>();
        menu.emberSprite = Px("orb_logo");

        var main = Stretch(root, "Principal");
        menu.mainPanel = main.gameObject;
        menu.logoRoot = Box(main, "Logo", TC, TC, new Vector2(0, -16), new Vector2(92, 80));
        Text(main, "Frase", "MANTENHA A CHAMA ACESA", TC, TC, new Vector2(0, -92), new Vector2(220, 10), Pal.Bone, 1, PixelText.Align.Center);
        var mm = Menu(main.gameObject);
        menu.menu = mm;
        var col = Box(main, "Botoes", TC, TC, new Vector2(0, -108), new Vector2(120, 100));
        var isz = new Vector2(120, 18);
        Item(col, mm, "JOGAR", Pal.Red, new Vector2(0, 0), isz, menu.Play);
        Item(col, mm, "MODO INFINITO", Pal.Orange, new Vector2(0, -21), isz, menu.PlayEndless);
        Item(col, mm, "COMO JOGAR", Pal.Blue, new Vector2(0, -42), isz, menu.ShowHowTo);
        Item(col, mm, "OPÇÕES", Pal.Purple, new Vector2(0, -63), isz, menu.ShowOptions);
        menu.quitItem = Item(col, mm, "SAIR", Pal.Slate, new Vector2(0, -84), isz, menu.Quit);
        menu.record = Text(main, "Recorde", "", BC, BC, new Vector2(0, 26), new Vector2(296, 10), Pal.Fog, 1, PixelText.Align.Center, false);
        menu.hint = Text(main, "Dica", "", BC, BC, new Vector2(0, 12), new Vector2(296, 10), Pal.Fog, 1, PixelText.Align.Center, false);

        // como jogar
        var how = Stretch(root, "Como Jogar");
        var hc = Card(how, "Card", new Vector2(292, 232), Vector2.zero, Pal.Blue);
        Text(hc, "Titulo", "COMO JOGAR", TC, TC, new Vector2(0, -8), new Vector2(280, 20), Pal.White, 2, PixelText.Align.Center);
        var rowsData = new (string sprite, bool anim, string text)[]
        {
            ("coal", true, "<#f58a3a>CARVÃO</>  +3s e recarrega o dash"),
            ("fragment", true, "<#f8cf4e>FRAGMENTO</>  junte 5 e volte à fogueira"),
            ("frost_a", false, "<#a3cbff>GELO</>  o calor cai 2,5x mais rápido"),
            ("pit_top", false, "<#7a75aa>ABISMO</>  só se cruza com dash"),
            ("ghost", true, "<#aaf3e4>ESPÍRITO</>  patrulha. O dash atordoa"),
            ("hunter", true, "<#d9b5ff>CAÇADOR</>  persegue. O dash atordoa"),
            ("spikes", false, "<#a3cbff>CRISTAIS</>  ferem. O dash quebra"),
        };
        for (int i = 0; i < rowsData.Length; i++)
        {
            var (sprite, isAnim, text) = rowsData[i];
            float y = -36 - i * 16;
            var s = isAnim ? Anim(sprite)[0] : Px(sprite);
            float k = Mathf.Min(1f, 13f / Mathf.Max(s.rect.width, s.rect.height));
            var icon = Icon(hc, sprite, s, TL, new Vector2(18, y), k);
            if (isAnim)
            {
                var a = icon.gameObject.AddComponent<UIAnim>();
                a.frames = Anim(sprite);
                a.fps = 6f;
            }
            Text(hc, "Linha" + i, text, TL, TL, new Vector2(30, y + 4), new Vector2(256, 10), Pal.White, 1);
        }
        Text(hc, "Teclas",
            "<#7a75aa>MOVER</>  WASD / setas   <#a3cbff>analógico</>\n" +
            "<#7a75aa>DASH</>  espaço / shift   <#a3cbff>A / RT</>  (8 direções)\n" +
            "<#7a75aa>PAUSA</>  esc / P   <#a3cbff>start</>",
            TL, TL, new Vector2(12, -150), new Vector2(270, 40), Pal.White, 1, PixelText.Align.Left, false);
        var hm = Menu(hc.gameObject);
        Item(hc, hm, "VOLTAR", Pal.Blue, new Vector2(0, -206), new Vector2(120, 18), menu.ShowMain);
        menu.howToPanel = how.gameObject;
        menu.howToMenu = hm;
        menu.options = BuildOptionsPanel(root);

        EditorSceneManager.SaveScene(scene, SceneDir + "MainMenu.unity");
    }

    // ================================================================ build
    static void ConfigureBuild()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SceneDir + "MainMenu.unity", true),
            new EditorBuildSettingsScene(SceneDir + "Game.unity", true),
        };
        PlayerSettings.productName = "Cold Ember";
        PlayerSettings.companyName = "garciapp2";
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 600;
        PlayerSettings.defaultWebScreenHeight = 480;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Deixa a pagina do WebGL com a cara do jogo (e o que aparece dentro do embed do itch.io):
    /// fundo da paleta, logo do Cold Ember no carregamento, barra de progresso nas cores do jogo
    /// e sem o rodape do Unity.
    /// </summary>
    [MenuItem("Cold Ember/Ajustar pagina WebGL")]
    public static void PatchWebTemplate() => PatchWebTemplate("Builds/WebGL");

    public static void PatchWebTemplate(string dir)
    {
        string css = Path.Combine(dir, "TemplateData", "style.css");
        if (!File.Exists(css)) return;
        File.AppendAllText(css, @"
/* Cold Ember */
html, body { background: #120e26; overflow: hidden; }
#unity-canvas { background: #120e26 !important; image-rendering: pixelated; }
#unity-loading-container { backdrop-filter: none; background: #120e26; }
#unity-logo { width: 240px; height: 132px; background: url('loading_logo.png') no-repeat center; image-rendering: pixelated; }
#unity-progress-bar-empty { width: 200px; height: 10px; margin: 14px auto 0; background: #231c40; border: 2px solid #0b0912; }
#unity-progress-bar-full { height: 10px; margin-top: 0; background: #f58a3a; box-shadow: inset 0 2px 0 #f8cf4e; }
#unity-footer { display: none; }
");
        string logo = "Tools/art/web/loading_logo.png";
        if (File.Exists(logo)) File.Copy(logo, Path.Combine(dir, "TemplateData", "loading_logo.png"), true);
        string html = Path.Combine(dir, "index.html");
        File.WriteAllText(html, File.ReadAllText(html).Replace("Unity Web Player | Cold Ember", "Cold Ember"));
        Debug.Log("[GameBuilder] Pagina WebGL ajustada.");
    }

    [MenuItem("Cold Ember/Gerar build WebGL")]
    public static void BuildWebGL()
    {
        ConfigureBuild();
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = UnityEditor.BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) PatchWebTemplate("Builds/WebGL");
        Debug.Log($"[GameBuilder] Build WebGL: {report.summary.result} ({report.summary.totalSize / (1024 * 1024f):0.0} MB) em {report.summary.totalTime}");
    }
}
