public class WepDef
{
    public string name;
    public bool auto, melee, scope;
    public float rate, dmg, head = 1f, rel, spread, move, kick, range = 120f, heavy, heavyRate;
    public int mag;
}

public static class OgeWeapons
{
    // Tarayıcı sürümündeki değerlerle aynı.
    public static readonly WepDef[] W =
    {
        new WepDef { name = "Tüfek", auto = true, rate = 0.1f, dmg = 24, head = 2.5f, mag = 30, rel = 2.2f, spread = 0.012f, move = 0.05f, kick = 0.018f },
        new WepDef { name = "Tabanca", rate = 0.17f, dmg = 30, head = 2.5f, mag = 12, rel = 1.5f, spread = 0.008f, move = 0.03f, kick = 0.03f },
        new WepDef { name = "Keskin nişancı", rate = 1.2f, dmg = 85, head = 2.4f, mag = 5, rel = 2.8f, spread = 0.09f, move = 0.15f, kick = 0.06f, scope = true },
        new WepDef { name = "Karambit", melee = true, auto = true, rate = 0.42f, heavyRate = 1.0f, dmg = 40, heavy = 65, head = 1.5f, range = 2.1f },
    };
}
