using UnityEngine;

namespace Oath
{
    public enum ResourceType { Wood, Gold }

    /// <summary>Cây (gỗ) hoặc mỏ vàng để Thợ khai thác.</summary>
    public class ResourceNode : MonoBehaviour
    {
        public ResourceType type;
        public int amount, maxAmount;
        public Vector2Int cell;
        public int gatherers;
        public int variant;
        SpriteRenderer sr;
        SpriteAnimator anim;
        float shake;
        Vector3 spriteBase;

        public Vector2 Pos => new Vector2(cell.x + 0.5f, cell.y + 0.5f);

        static readonly float[] TreeFeet = { 1.77f, 1.89f, 1.16f, 1.12f };
        static readonly float[] StumpFeet = { 1.75f, 1.83f, 1.12f, 1.06f };
        static readonly float[] GoldFeet = { 0.23f, 0.17f, 0.38f, 0.34f, 0.56f, 0.53f };

        public static ResourceNode CreateTree(Vector2Int cell, int variant)
        {
            var n = Make("Cây", cell, ResourceType.Wood, 90);
            n.variant = Mathf.Clamp(variant, 1, 4);
            n.transform.position = new Vector2(cell.x + 0.5f, cell.y + 0.25f);
            n.sr = Util.NewSprite("Sprite", n.transform, null, new Vector3(0, TreeFeet[n.variant - 1], 0), Util.SortOrder(cell.y + 0.25f));
            n.anim = n.sr.gameObject.AddComponent<SpriteAnimator>();
            n.anim.target = n.sr;
            n.anim.Play(GameArt.I.Set("Tiny Swords/Pawn and Resources/Wood/Trees/Tree" + n.variant), 8f, true, true, null, true);
            n.spriteBase = n.sr.transform.localPosition;
            return n;
        }

        public static ResourceNode CreateGold(Vector2Int cell, int variant)
        {
            var n = Make("Mỏ vàng", cell, ResourceType.Gold, 700);
            n.variant = Mathf.Clamp(variant, 1, 6);
            n.transform.position = new Vector2(cell.x + 0.5f, cell.y + 0.3f);
            n.sr = Util.NewSprite("Sprite", n.transform, GameArt.I.Spr("Tiny Swords/Pawn and Resources/Gold/Gold Stones/Gold Stone " + n.variant),
                new Vector3(0, GoldFeet[n.variant - 1], 0), Util.SortOrder(cell.y + 0.3f));
            n.spriteBase = n.sr.transform.localPosition;
            return n;
        }

        static ResourceNode Make(string name, Vector2Int cell, ResourceType type, int amount)
        {
            var go = new GameObject(name);
            if (World.Root != null) go.transform.SetParent(World.Root, false);
            var n = go.AddComponent<ResourceNode>();
            n.cell = cell; n.type = type; n.amount = amount; n.maxAmount = amount;
            World.ResourceNodes.Add(n);
            return n;
        }

        public int Take(int want)
        {
            int got = Mathf.Min(want, amount);
            amount -= got;
            Shake();
            if (amount <= 0) Deplete();
            return got;
        }

        public void Shake() { shake = 0.3f; }

        void Update()
        {
            if (shake <= 0f || sr == null) return;
            shake -= Time.deltaTime;
            float k = Mathf.Max(0f, shake) / 0.3f;
            sr.transform.localPosition = spriteBase + new Vector3(Mathf.Sin(Time.time * 60f) * 0.04f * k, 0, 0);
        }

        void Deplete()
        {
            World.ResourceNodes.Remove(this);
            FX.Play("dust", Pos, 1f);
            if (type == ResourceType.Wood)
            {
                anim.enabled = false;
                sr.sprite = GameArt.I.Spr("Tiny Swords/Pawn and Resources/Wood/Trees/Stump " + variant);
                sr.transform.localPosition = new Vector3(0, StumpFeet[variant - 1], 0);
                spriteBase = sr.transform.localPosition;
                enabled = false;
            }
            else
            {
                if (World.Nav != null) World.Nav.SetBlocked(new RectInt(cell.x, cell.y, 1, 1), false);
                Destroy(gameObject);
            }
        }
    }
}
