// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class ProceduralSpriteGenerator
    {
        private const string SpriteDir = "Assets/UI/Sprites";

        [MenuItem("Tools/Generate Procedural UI Sprites")]
        public static void GenerateAllSprites()
        {
            string fullPath = Path.Combine(Application.dataPath, "UI/Sprites");
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // 1. 基本アイコン (128x128)
            SaveTexture("Icon_Stamina", CreateStaminaIcon(128, 128));
            SaveTexture("Icon_Skill", CreateSkillIcon(128, 128));
            SaveTexture("Icon_Mental", CreateMentalIcon(128, 128));
            SaveTexture("Icon_Attack", CreateAttackIcon(128, 128));
            SaveTexture("Icon_Shield", CreateShieldIcon(128, 128));
            SaveTexture("Icon_Study", CreateStudyIcon(128, 128));
            SaveTexture("Icon_Train", CreateTrainIcon(128, 128));
            SaveTexture("Icon_Rest", CreateRestIcon(128, 128));
            SaveTexture("Icon_Relic", CreateRelicIcon(128, 128));

            // 2. ボス幾何学エンブレム (256x256)
            SaveTexture("Boss_Emblem_Act1", CreateBossEmblem(256, new Color(0.9f, 0.4f, 0.2f), 3)); // 三角形
            SaveTexture("Boss_Emblem_Act2", CreateBossEmblem(256, new Color(0.8f, 0.2f, 0.5f), 4)); // 四角形
            SaveTexture("Boss_Emblem_Act3", CreateBossEmblem(256, new Color(0.3f, 0.6f, 0.9f), 5)); // 五角形
            SaveTexture("Boss_Emblem_Act4", CreateBossEmblem(256, new Color(0.9f, 0.2f, 0.2f), 6)); // 六角形

            // 3. UI 枠・ゲージ (128x128 / 64x64)
            SaveTexture("Frame_Card", CreateCardFrame(256, 320, 18));
            SaveTexture("Bar_Fill", CreateBarFill(64, 64));

            AssetDatabase.Refresh();
            ConfigureImporterSettings();
            Debug.Log("[ProceduralSpriteGenerator] All procedural UI sprites generated and configured successfully.");
        }

        private static void SaveTexture(string name, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            string path = $"{SpriteDir}/{name}.png";
            string fullPath = Path.Combine(Application.dataPath, $"UI/Sprites/{name}.png");
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureImporterSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    // カード枠は 9 スライスにして、拡大時も角丸が潰れないようにする
                    importer.spriteBorder = path.EndsWith("Frame_Card.png")
                        ? new Vector4(28.0f, 28.0f, 28.0f, 28.0f)
                        : Vector4.zero;
                    importer.SaveAndReimport();
                }
            }
        }

        // --- 幾何学テクスチャ生成ヘルパー ---

        private static Texture2D CreateClearTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static Texture2D CreateStaminaIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.22f, 0.85f, 0.45f, 1.0f);
            // 雷（ライトニング）ボルト形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float ny = (float)y / h;
                    if ((ny > 0.45f && nx > 0.35f + (ny - 0.45f) * 0.4f && nx < 0.65f + (ny - 0.45f) * 0.4f) ||
                        (ny <= 0.55f && nx > 0.25f + ny * 0.4f && nx < 0.55f + ny * 0.4f))
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSkillIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.25f, 0.65f, 0.98f, 1.0f);
            // ダイヤモンド・本形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((x - w * 0.5f) / (w * 0.4f));
                    float dy = Mathf.Abs((y - h * 0.5f) / (h * 0.4f));
                    if (dx + dy <= 1.0f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMentalIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.92f, 0.35f, 0.65f, 1.0f);
            // ハート形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = (x - w * 0.5f) / (w * 0.4f);
                    float py = (y - h * 0.4f) / (h * 0.4f);
                    float a = px * px + py * py - 1.0f;
                    if (a * a * a - px * px * py * py * py <= 0.0f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateAttackIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.95f, 0.30f, 0.30f, 1.0f);
            // 剣（クロスブレード）形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d1 = Mathf.Abs(x - y);
                    float d2 = Mathf.Abs(x - (h - y));
                    if ((d1 < w * 0.08f && x > w * 0.15f && x < w * 0.85f) ||
                        (d2 < w * 0.08f && x > w * 0.15f && x < w * 0.85f))
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateShieldIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.30f, 0.75f, 0.95f, 1.0f);
            // 盾形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x - w * 0.5f) / (w * 0.4f));
                    float ny = (float)y / h;
                    if (ny > 0.4f && nx <= 0.85f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else if (ny <= 0.4f && nx <= 0.85f * (ny / 0.4f))
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStudyIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color body = new Color(0.62f, 0.55f, 0.95f, 1.0f);
            Color dark = new Color(0.30f, 0.26f, 0.55f, 1.0f);

            // ペン軸のローカル座標（v- 側がペン先）
            Vector2[] pen =
            {
                new Vector2(0.00f, -0.95f),
                new Vector2(0.17f, -0.58f),
                new Vector2(0.17f, 0.60f),
                new Vector2(0.00f, 0.80f),
                new Vector2(-0.17f, 0.60f),
                new Vector2(-0.17f, -0.58f)
            };

            Draw(tex, body, (u, v) => InPolygon(ToPenLocal(u, v), pen));
            // 持ち手のバンドとペン先のスリット
            Draw(tex, dark, (u, v) =>
            {
                Vector2 l = ToPenLocal(u, v);
                return InPolygon(l, pen) && l.y >= 0.12f && l.y <= 0.38f;
            });
            Draw(tex, dark, (u, v) =>
            {
                Vector2 l = ToPenLocal(u, v);
                return Mathf.Abs(l.x) <= 0.045f && l.y >= -0.90f && l.y <= -0.40f;
            });
            // アカデミックアイコンとしての罫線
            Draw(tex, body, (u, v) => u >= -0.60f && u <= 0.78f && v >= -0.95f && v <= -0.80f);

            tex.Apply();
            return tex;
        }

        private static Vector2 ToPenLocal(float u, float v)
        {
            return Rotate(new Vector2(u - 0.06f, v - 0.14f), 32.0f);
        }

        private static Texture2D CreateTrainIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.95f, 0.65f, 0.20f, 1.0f);
            // ダンベル形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float ny = Mathf.Abs((y - h * 0.5f) / (h * 0.4f));
                    if ((nx < 0.25f || nx > 0.75f) && ny < 0.8f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else if (nx >= 0.25f && nx <= 0.75f && ny < 0.25f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateRestIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.40f, 0.85f, 0.75f, 1.0f);
            // カップ形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x - w * 0.45f) / (w * 0.35f));
                    float ny = (float)y / h;
                    if (ny > 0.25f && ny < 0.75f && nx < 0.8f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateRelicIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.95f, 0.80f, 0.25f, 1.0f);
            // 八角形クリスタル
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w * 0.5f);
                    float dy = Mathf.Abs(y - h * 0.5f);
                    if (dx < w * 0.38f && dy < h * 0.38f && (dx + dy) < w * 0.55f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBossEmblem(int size, Color mainColor, int sides)
        {
            Texture2D tex = CreateClearTexture(size, size);
            Color core = mainColor * 0.85f;
            core.a = 1.0f;
            Color inner = mainColor * 0.45f + new Color(0.10f, 0.10f, 0.10f, 0.0f);
            inner.a = 1.0f;

            // 外周リング
            Draw(tex, mainColor, (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v);
                return r >= 0.74f && r <= 0.94f;
            });
            // Act ごとに辺数の異なる正多角形コア
            Draw(tex, core, (u, v) => InRegularPolygon(u, v, 0.60f, sides, 90.0f));
            // 半ステップ回転させた内側多角形で紋章らしい重なりを作る
            Draw(tex, inner, (u, v) => InRegularPolygon(u, v, 0.30f, sides, 90.0f + 180.0f / sides));

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCardFrame(int w, int h, int radius)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color bg = new Color(0.16f, 0.19f, 0.26f, 0.95f);
            Color border = new Color(0.35f, 0.42f, 0.55f, 1.0f);
            const float BorderWidth = 3.0f;

            Draw(tex, border, (u, v) =>
                InRoundedRect(ToPixel(u, w), ToPixel(v, h), 0.0f, 0.0f, w, h, radius));
            Draw(tex, bg, (u, v) =>
                InRoundedRect(ToPixel(u, w), ToPixel(v, h), BorderWidth, BorderWidth,
                    w - BorderWidth * 2.0f, h - BorderWidth * 2.0f, Mathf.Max(radius - BorderWidth, 1.0f)));

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBarFill(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                // 上端をわずかに明るくし、Image の着色で滑らかな艶が出るようにする
                float value = Mathf.Lerp(0.78f, 1.0f, (y + 0.5f) / h);
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = new Color(value, value, value, 1.0f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // --- 被覆率サンプリング描画と幾何プリミティブ ---

        // (u, v) は中心を原点とする -1..1 の正規化座標で、v の正方向が上。
        private delegate bool ShapeTest(float u, float v);

        // 1 ピクセルあたり SubSamples x SubSamples 個のサンプルで被覆率を求めアンチエイリアスする。
        private const int SubSamples = 3;

        private static void Draw(Texture2D tex, Color color, ShapeTest test)
        {
            int w = tex.width;
            int h = tex.height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float coverage = Coverage(x, y, w, h, test);
                    if (coverage <= 0.0f) continue;

                    Color src = color;
                    src.a *= coverage;
                    tex.SetPixel(x, y, Blend(src, tex.GetPixel(x, y)));
                }
            }
        }

        private static float Coverage(int x, int y, int w, int h, ShapeTest test)
        {
            int hit = 0;
            for (int sy = 0; sy < SubSamples; sy++)
            {
                for (int sx = 0; sx < SubSamples; sx++)
                {
                    float u = (x + (sx + 0.5f) / SubSamples) / w * 2.0f - 1.0f;
                    float v = (y + (sy + 0.5f) / SubSamples) / h * 2.0f - 1.0f;
                    if (test(u, v)) hit++;
                }
            }
            return (float)hit / (SubSamples * SubSamples);
        }

        private static Color Blend(Color src, Color dst)
        {
            float a = src.a + dst.a * (1.0f - src.a);
            if (a <= 0.0f) return Color.clear;

            float inv = dst.a * (1.0f - src.a);
            return new Color(
                (src.r * src.a + dst.r * inv) / a,
                (src.g * src.a + dst.g * inv) / a,
                (src.b * src.a + dst.b * inv) / a,
                a);
        }

        private static float ToPixel(float normalized, int size)
        {
            return (normalized + 1.0f) * 0.5f * size;
        }

        private static Vector2 Rotate(Vector2 p, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
        }

        // 頂点が rotationDegrees 方向を向く正多角形。内接半径との比較で内外を判定する。
        private static bool InRegularPolygon(float u, float v, float radius, int sides, float rotationDegrees)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            if (r <= Mathf.Epsilon) return true;

            float step = Mathf.PI * 2.0f / sides;
            float angle = Mathf.Atan2(v, u) - rotationDegrees * Mathf.Deg2Rad;
            float offset = Mathf.Repeat(angle, step) - step * 0.5f;
            return r * Mathf.Cos(offset) <= radius * Mathf.Cos(step * 0.5f);
        }

        private static bool InRoundedRect(float x, float y, float left, float bottom, float w, float h, float radius)
        {
            float dx = Mathf.Abs(x - (left + w * 0.5f)) - (w * 0.5f - radius);
            float dy = Mathf.Abs(y - (bottom + h * 0.5f)) - (h * 0.5f - radius);
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0.0f) * Mathf.Max(dx, 0.0f) + Mathf.Max(dy, 0.0f) * Mathf.Max(dy, 0.0f));
            return outside + Mathf.Min(Mathf.Max(dx, dy), 0.0f) <= radius;
        }

        private static bool InPolygon(Vector2 p, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                    p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
#endif
