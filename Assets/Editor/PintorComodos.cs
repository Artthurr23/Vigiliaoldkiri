using System;

// Motor de desenho simples usado para pintar os 6 cômodos das câmeras.
public struct Cor
{
    public float r, g, b;
    public Cor(float r, float g, float b) { this.r = r; this.g = g; this.b = b; }
}

public struct Ponto
{
    public float x, y;
    public Ponto(float x, float y) { this.x = x; this.y = y; }
}

public class Tela
{
    public int L, A;
    public Cor[] px;
    public Tela(int l, int a) { L = l; A = a; px = new Cor[l * a]; }

    public void Mesclar(int x, int y, Cor c, float a)
    {
        if (x < 0 || y < 0 || x >= L || y >= A) return;
        int i = y * L + x;
        if (a >= 1f) { px[i] = c; return; }
        Cor o = px[i];
        px[i] = new Cor(o.r + (c.r - o.r) * a, o.g + (c.g - o.g) * a, o.b + (c.b - o.b) * a);
    }

    public void Ret(float x0, float y0, float x1, float y1, Cor c, float a = 1f)
    {
        int xa = (int)Math.Round(x0), ya = (int)Math.Round(y0), xb = (int)Math.Round(x1), yb = (int)Math.Round(y1);
        for (int y = ya; y < yb; y++)
            for (int x = xa; x < xb; x++) Mesclar(x, y, c, a);
    }

    public void RetGrad(float x0, float y0, float x1, float y1, Cor topo, Cor baixo)
    {
        int xa = (int)Math.Round(x0), ya = (int)Math.Round(y0), xb = (int)Math.Round(x1), yb = (int)Math.Round(y1);
        for (int y = ya; y < yb; y++)
        {
            float k = (y - ya) / (float)Math.Max(1, yb - ya);
            Cor c = new Cor(topo.r + (baixo.r - topo.r) * k, topo.g + (baixo.g - topo.g) * k, topo.b + (baixo.b - topo.b) * k);
            for (int x = xa; x < xb; x++) Mesclar(x, y, c, 1f);
        }
    }

    public void Poli(Ponto[] p, Cor c, float a = 1f)
    {
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < p.Length; i++) { minY = Math.Min(minY, p[i].y); maxY = Math.Max(maxY, p[i].y); }
        int ya = Math.Max(0, (int)Math.Floor(minY)), yb = Math.Min(A - 1, (int)Math.Ceiling(maxY));
        float[] xs = new float[p.Length + 1];
        for (int y = ya; y <= yb; y++)
        {
            float yc = y + 0.5f; int n = 0;
            for (int i = 0; i < p.Length; i++)
            {
                Ponto p1 = p[i], p2 = p[(i + 1) % p.Length];
                if ((p1.y <= yc && p2.y > yc) || (p2.y <= yc && p1.y > yc))
                {
                    float k = (yc - p1.y) / (p2.y - p1.y);
                    xs[n++] = p1.x + k * (p2.x - p1.x);
                }
            }
            Array.Sort(xs, 0, n);
            for (int k = 0; k + 1 < n; k += 2)
            {
                int xa = (int)Math.Round(xs[k]), xb = (int)Math.Round(xs[k + 1]);
                for (int x = xa; x < xb; x++) Mesclar(x, y, c, a);
            }
        }
    }

    public void Elipse(float cx, float cy, float rx, float ry, Cor c, float a = 1f)
    {
        int xa = (int)Math.Floor(cx - rx), xb = (int)Math.Ceiling(cx + rx), ya = (int)Math.Floor(cy - ry), yb = (int)Math.Ceiling(cy + ry);
        for (int y = ya; y <= yb; y++)
            for (int x = xa; x <= xb; x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Mesclar(x, y, c, a);
            }
    }

    // brilho aditivo (luz de lâmpada, janela, lareira...)
    public void Luz(float cx, float cy, float rx, float ry, float forca)
    {
        int xa = Math.Max(0, (int)Math.Floor(cx - rx)), xb = Math.Min(L - 1, (int)Math.Ceiling(cx + rx));
        int ya = Math.Max(0, (int)Math.Floor(cy - ry)), yb = Math.Min(A - 1, (int)Math.Ceiling(cy + ry));
        for (int y = ya; y <= yb; y++)
            for (int x = xa; x <= xb; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d >= 1f) continue;
                float k = forca * (1f - d) * (1f - d);
                Cor o = px[y * L + x];
                px[y * L + x] = new Cor(o.r + k, o.g + k, o.b + k);
            }
    }
}

public static class Pintor
{
    public const int Largura = 960, Altura = 540;

    static Cor G(float v) { return new Cor(v, v, v); }
    static Ponto P(float x, float y) { return new Ponto(x, y); }
    static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
    static float Hash(float n) { double s = Math.Sin(n * 12.9898 + 4.1414) * 43758.5453; return (float)(s - Math.Floor(s)); }

    public static Tela Gerar(int indice)
    {
        Tela t = new Tela(Largura, Altura);
        switch (indice)
        {
            case 0: Hall(t); break;
            case 1: Sala(t); break;
            case 2: Cozinha(t); break;
            case 3: Corredor(t); break;
            case 4: Quarto(t); break;
            default: Jardim(t); break;
        }
        Acabamento(t, indice);
        return t;
    }

    // ------------------------------------------------------------ peças básicas

    static void Caixa(Tela t, float x0, float y0, float x1, float y1, float parede, float teto, float chao, float lat)
    {
        float L = t.L, A = t.A;
        t.Poli(new[] { P(0, 0), P(L, 0), P(x1, y0), P(x0, y0) }, G(teto));
        t.Poli(new[] { P(0, A), P(L, A), P(x1, y1), P(x0, y1) }, G(chao));
        t.RetGrad(x0, y0, x1, y1, G(parede * 1.08f), G(parede * 0.86f));
        for (int i = 0; i < 24; i++)
        {
            float s0 = i / 24f, s1 = (i + 1) / 24f;
            float v = lat * Lerp(0.55f, 1.05f, s0);
            t.Poli(new[] { P(Lerp(0, x0, s0), Lerp(0, y0, s0)), P(Lerp(0, x0, s1) + 1, Lerp(0, y0, s1)), P(Lerp(0, x0, s1) + 1, Lerp(A, y1, s1)), P(Lerp(0, x0, s0), Lerp(A, y1, s0)) }, G(v));
            t.Poli(new[] { P(Lerp(L, x1, s0), Lerp(0, y0, s0)), P(Lerp(L, x1, s1) - 1, Lerp(0, y0, s1)), P(Lerp(L, x1, s1) - 1, Lerp(A, y1, s1)), P(Lerp(L, x1, s0), Lerp(A, y1, s0)) }, G(v * 0.92f));
        }
        t.Ret(x0, y0, x0 + 6, y1, G(0.04f), 0.35f);
        t.Ret(x1 - 6, y0, x1, y1, G(0.04f), 0.35f);
        t.Ret(x0, y1 - 5, x1, y1, G(0.04f), 0.35f);
    }

    // ponto sobre a parede lateral: s = 0 (perto da câmera) .. 1 (fundo); f = 0 (chão) .. 1 (teto)
    static Ponto PL(bool esq, float x0, float y0, float x1, float y1, float L, float A, float s, float f)
    {
        float xo = esq ? 0 : L, xi = esq ? x0 : x1;
        float top = Lerp(0, y0, s), bot = Lerp(A, y1, s);
        return P(Lerp(xo, xi, s), Lerp(bot, top, f));
    }

    static Ponto[] QuadLat(bool esq, float x0, float y0, float x1, float y1, float L, float A, float s1, float s2, float f1, float f2)
    {
        return new[] { PL(esq, x0, y0, x1, y1, L, A, s1, f2), PL(esq, x0, y0, x1, y1, L, A, s2, f2), PL(esq, x0, y0, x1, y1, L, A, s2, f1), PL(esq, x0, y0, x1, y1, L, A, s1, f1) };
    }

    static void Sombra(Tela t, float cx, float cy, float rx, float ry)
    {
        t.Elipse(cx, cy, rx, ry, G(0f), 0.40f);
    }

    static void Movel(Tela t, float cx, float baseY, float w, float h, float prof, float cf, float ct)
    {
        Sombra(t, cx, baseY, w * 0.62f, prof * 0.45f + 5);
        t.Ret(cx - w / 2, baseY - h, cx + w / 2, baseY, G(cf));
        t.Ret(cx - w / 2, baseY - h * 0.35f, cx + w / 2, baseY, G(0f), 0.22f);
        t.Poli(new[] { P(cx - w / 2, baseY - h), P(cx + w / 2, baseY - h), P(cx + w / 2 * 0.93f, baseY - h - prof), P(cx - w / 2 * 0.93f, baseY - h - prof) }, G(ct));
        t.Ret(cx - w / 2, baseY - h, cx + w / 2, baseY - h + 2, G(Math.Min(1f, ct * 1.25f)));
        t.Ret(cx - w / 2, baseY - h, cx - w / 2 + 2, baseY, G(Math.Min(1f, cf * 1.2f)), 0.5f);
    }

    static void ChaoPadrao(Tela t, float x0, float y0, float x1, int tipo, int nU, int nV, float c1, float c2)
    {
        float L = t.L, A = t.A, qf = (x1 - x0) / L;
        for (int y = (int)y0; y < t.A; y++)
        {
            float p = (y + 0.5f - y0) / (A - y0);
            if (p < 0) continue;
            float xl = Lerp(x0, 0, p), xr = Lerp(x1, L, p);
            float q = qf + p * (1 - qf);
            float v = (1f / q - 1f) / (1f / qf - 1f);
            for (int x = (int)Math.Max(0, xl); x < (int)Math.Min(L, xr); x++)
            {
                float u = (x - xl) / (xr - xl);
                float val;
                if (tipo == 0)
                {
                    int k = ((int)(u * nU) + (int)(v * nV)) & 1;
                    val = k == 0 ? c1 : c2;
                    float fu = u * nU - (float)Math.Floor(u * nU), fv = v * nV - (float)Math.Floor(v * nV);
                    if (fu < 0.03f || fv < 0.03f) val *= 0.75f;
                }
                else
                {
                    int pl = (int)(u * nU);
                    val = c1 + (Hash(pl) - 0.5f) * 0.05f;
                    float fu = u * nU - pl;
                    if (fu < 0.05f) val *= 0.62f;
                    float vv = v * nV + Hash(pl + 7) * 3f;
                    if (vv - Math.Floor(vv) < 0.035f) val *= 0.7f;
                }
                val *= 1f - 0.30f * v;
                t.px[y * t.L + x] = G(val);
            }
        }
    }

    static void Janela(Tela t, float x0, float y0, float x1, float y1, float luz)
    {
        float mx = (x0 + x1) / 2, my = (y0 + y1) / 2;
        t.Ret(x0 - 6, y0 - 6, x1 + 6, y1 + 6, G(0.30f));
        t.Ret(x0 - 3, y0 - 3, x1 + 3, y1 + 3, G(0.12f));
        t.RetGrad(x0, y0, x1, y1, G(luz), G(luz * 0.55f));
        t.Ret(mx - 2, y0, mx + 2, y1, G(0.12f));
        t.Ret(x0, my - 2, x1, my + 2, G(0.12f));
        t.Ret(x0 - 8, y1 + 3, x1 + 8, y1 + 9, G(0.34f));
        t.Luz(mx, my, (x1 - x0) * 1.4f, (y1 - y0) * 1.1f, luz * 0.30f);
    }

    static void Cortina(Tela t, float x0, float y0, float x1, float y1, bool esq)
    {
        float w = (x1 - x0);
        t.Poli(new[] { P(x0 - 14, y0 - 16), P(x0 + w * 0.28f, y0 - 16), P(x0 + w * 0.22f, y1 + 4), P(x0 - 14, y1 + 4) }, G(0.11f));
        t.Poli(new[] { P(x1 - w * 0.28f, y0 - 16), P(x1 + 14, y0 - 16), P(x1 + 14, y1 + 4), P(x1 - w * 0.22f, y1 + 4) }, G(0.11f));
        for (int i = 0; i < 4; i++)
        {
            t.Ret(x0 - 10 + i * 6, y0 - 12, x0 - 8 + i * 6, y1, G(0.20f), 0.35f);
            t.Ret(x1 + 10 - i * 6, y0 - 12, x1 + 8 - i * 6, y1, G(0.20f), 0.35f);
        }
        t.Ret(x0 - 20, y0 - 22, x1 + 20, y0 - 16, G(0.30f));
    }

    static void Quadro(Tela t, float x0, float y0, float x1, float y1)
    {
        t.Ret(x0 - 2, y0 - 2, x1 + 2, y1 + 2, G(0.04f), 0.5f);
        t.Ret(x0, y0, x1, y1, G(0.34f));
        t.Ret(x0 + 4, y0 + 4, x1 - 4, y1 - 4, G(0.10f));
        float h = Lerp(y0 + 4, y1 - 4, 0.58f);
        t.RetGrad(x0 + 4, y0 + 4, x1 - 4, h, G(0.26f), G(0.17f));
        t.Poli(new[] { P(x0 + 4, h), P(Lerp(x0, x1, 0.3f), h - (y1 - y0) * 0.16f), P(Lerp(x0, x1, 0.55f), h - 2), P(Lerp(x0, x1, 0.8f), h - (y1 - y0) * 0.12f), P(x1 - 4, h), P(x1 - 4, y1 - 4), P(x0 + 4, y1 - 4) }, G(0.07f));
    }

    static void Abajur(Tela t, float x, float topoMesa, float e)
    {
        t.Ret(x - 2 * e, topoMesa - 24 * e, x + 2 * e, topoMesa, G(0.22f));
        t.Elipse(x, topoMesa - 1 * e, 8 * e, 2.5f * e, G(0.24f));
        t.Poli(new[] { P(x - 13 * e, topoMesa - 52 * e), P(x + 13 * e, topoMesa - 52 * e), P(x + 19 * e, topoMesa - 24 * e), P(x - 19 * e, topoMesa - 24 * e) }, G(0.88f));
        t.Luz(x, topoMesa - 36 * e, 150 * e, 115 * e, 0.62f);
    }

    static void Lustre(Tela t, float cx, float cy)
    {
        t.Ret(cx - 1, 0, cx + 2, cy - 10, G(0.20f));
        t.Elipse(cx, cy, 72, 12, G(0.16f));
        t.Elipse(cx, cy - 4, 56, 8, G(0.30f));
        t.Elipse(cx, cy + 16, 22, 24, G(0.14f));
        for (int i = -3; i <= 3; i++)
        {
            float x = cx + i * 22, y = cy - 4 - (3 - Math.Abs(i)) * 1.5f;
            t.Ret(x - 2, y - 16, x + 2, y, G(0.70f));
            t.Elipse(x, y - 21, 3, 5, G(1f));
            t.Luz(x, y - 21, 34, 34, 0.55f);
        }
        t.Luz(cx, cy, 240, 130, 0.30f);
    }

    static void Planta(Tela t, float x, float baseY, float e)
    {
        Sombra(t, x, baseY, 24 * e, 7 * e);
        t.Poli(new[] { P(x - 16 * e, baseY - 30 * e), P(x + 16 * e, baseY - 30 * e), P(x + 11 * e, baseY), P(x - 11 * e, baseY) }, G(0.20f));
        t.Ret(x - 17 * e, baseY - 32 * e, x + 17 * e, baseY - 28 * e, G(0.28f));
        for (int i = 0; i < 9; i++)
        {
            float ang = (i / 8f - 0.5f) * 2.6f;
            float lx = x + (float)Math.Sin(ang) * 30 * e, ly = baseY - 62 * e - (float)Math.Cos(ang) * 24 * e;
            t.Elipse(lx, ly, 13 * e, 24 * e, G(0.09f + Hash(i) * 0.05f));
            t.Elipse(lx - 3 * e, ly - 6 * e, 4 * e, 11 * e, G(0.22f), 0.35f);
        }
    }

    static void Vaso(Tela t, float x, float base_, float e)
    {
        t.Elipse(x, base_ - 12 * e, 11 * e, 14 * e, G(0.30f));
        t.Ret(x - 4 * e, base_ - 34 * e, x + 4 * e, base_ - 20 * e, G(0.28f));
        t.Elipse(x, base_ - 36 * e, 6 * e, 3 * e, G(0.26f));
        t.Elipse(x - 4 * e, base_ - 14 * e, 3 * e, 6 * e, G(0.55f), 0.45f);
    }

    static void Sofa(Tela t, float cx, float baseY, float w)
    {
        Sombra(t, cx, baseY, w * 0.58f, 12);
        t.Ret(cx - w / 2 + 6, baseY - 92, cx + w / 2 - 6, baseY - 34, G(0.15f));
        t.Ret(cx - w / 2 + 6, baseY - 92, cx + w / 2 - 6, baseY - 89, G(0.28f));
        t.Ret(cx - w / 2 - 6, baseY - 62, cx - w / 2 + 14, baseY, G(0.19f));
        t.Ret(cx + w / 2 - 14, baseY - 62, cx + w / 2 + 6, baseY, G(0.19f));
        t.Ret(cx - w / 2 - 6, baseY - 62, cx - w / 2 + 14, baseY - 59, G(0.30f));
        t.Ret(cx + w / 2 - 14, baseY - 62, cx + w / 2 + 6, baseY - 59, G(0.30f));
        t.Ret(cx - w / 2 + 14, baseY - 40, cx + w / 2 - 14, baseY - 2, G(0.18f));
        t.Ret(cx - w / 2 + 14, baseY - 40, cx + w / 2 - 14, baseY - 37, G(0.30f));
        int n = Math.Max(2, (int)(w / 60));
        for (int i = 1; i < n; i++)
        {
            float x = Lerp(cx - w / 2 + 14, cx + w / 2 - 14, i / (float)n);
            t.Ret(x - 1, baseY - 38, x + 1, baseY - 2, G(0.05f), 0.6f);
        }
        t.Elipse(cx - w / 2 + 34, baseY - 66, 16, 13, G(0.24f));
    }

    static void Rodape(Tela t, float x0, float x1, float y1)
    {
        t.Ret(x0, y1 - 10, x1, y1, G(0.36f));
        t.Ret(x0, y1 - 10, x1, y1 - 8, G(0.46f));
    }

    static void Tapete(Tela t, Ponto[] pts, float cor, float borda)
    {
        t.Poli(pts, G(borda));
        float cx = 0, cy = 0;
        for (int i = 0; i < pts.Length; i++) { cx += pts[i].x; cy += pts[i].y; }
        cx /= pts.Length; cy /= pts.Length;
        Ponto[] menor = new Ponto[pts.Length];
        for (int i = 0; i < pts.Length; i++) menor[i] = P(cx + (pts[i].x - cx) * 0.86f, cy + (pts[i].y - cy) * 0.80f);
        t.Poli(menor, G(cor));
    }

    // ------------------------------------------------------------ cômodos

    static void Hall(Tela t)
    {
        float x0 = 190, y0 = 95, x1 = 770, y1 = 375;
        Caixa(t, x0, y0, x1, y1, 0.31f, 0.15f, 0.22f, 0.26f);
        ChaoPadrao(t, x0, y1, x1, 0, 8, 7, 0.34f, 0.15f);
        t.Ret(x0, y0 + 150, x1, y1, G(0.21f));
        t.Ret(x0, y0 + 148, x1, y0 + 154, G(0.40f));
        for (int i = 0; i < 7; i++) t.Ret(x0 + 12 + i * 86, y0 + 162, x0 + 14 + i * 86, y1 - 16, G(0.30f), 0.35f);
        t.Ret(x0, y0, x1, y0 + 10, G(0.40f));
        Rodape(t, x0, x1, y1);
        t.Elipse(480, 190, 84, 52, G(0.38f));
        t.Ret(396, 190, 564, y1 - 10, G(0.38f));
        t.Elipse(480, 190, 64, 38, G(0.52f));
        t.Ret(405, 190, 555, y1 - 10, G(0.09f));
        t.Ret(478, 150, 482, 190, G(0.10f));
        t.Ret(417, 214, 474, 296, G(0.13f));
        t.Ret(486, 214, 543, 296, G(0.13f));
        t.Ret(417, 308, 474, 360, G(0.13f));
        t.Ret(486, 308, 543, 360, G(0.13f));
        t.Ret(478, 190, 482, y1 - 10, G(0.30f));
        t.Elipse(468, 300, 4, 4, G(0.60f)); t.Elipse(492, 300, 4, 4, G(0.60f));
        t.Luz(480, 200, 130, 100, 0.20f);
        Quadro(t, 240, 150, 330, 240);
        Quadro(t, 630, 150, 720, 240);
        t.Elipse(370, 190, 6, 10, G(1f)); t.Luz(370, 190, 90, 90, 0.40f);
        t.Elipse(590, 190, 6, 10, G(1f)); t.Luz(590, 190, 90, 90, 0.40f);
        Tapete(t, new[] { P(410, 384), P(550, 384), P(640, 540), P(320, 540) }, 0.17f, 0.30f);
        Planta(t, 262, 384, 1.0f);
        Planta(t, 700, 384, 1.0f);
        Sombra(t, 862, 500, 40, 10);
        t.Ret(832, 300, 892, 500, G(0.14f));
        t.Ret(826, 292, 898, 306, G(0.26f));
        t.Ret(840, 318, 884, 382, G(0.07f));
        t.Elipse(862, 345, 17, 17, G(0.72f)); t.Elipse(862, 345, 14, 14, G(0.88f));
        t.Ret(861, 334, 863, 346, G(0.10f)); t.Ret(862, 345, 872, 347, G(0.10f));
        t.Ret(845, 400, 879, 478, G(0.09f)); t.Elipse(862, 440, 6, 6, G(0.55f));
        Movel(t, 112, 506, 130, 66, 30, 0.17f, 0.28f);
        t.Ret(60, 446, 164, 452, G(0.04f), 0.4f);
        Vaso(t, 82, 440, 1.2f);
        Abajur(t, 140, 440, 1.2f);
        Lustre(t, 480, 132);
    }

    static void Sala(Tela t)
    {
        float x0 = 190, y0 = 95, x1 = 770, y1 = 375;
        Caixa(t, x0, y0, x1, y1, 0.28f, 0.14f, 0.21f, 0.23f);
        ChaoPadrao(t, x0, y1, x1, 1, 11, 9, 0.26f, 0.2f);
        t.Ret(x0, y0, x1, y0 + 9, G(0.38f));
        t.Ret(x0, y0 + 190, x1, y1, G(0.22f));
        t.Ret(x0, y0 + 188, x1, y0 + 193, G(0.38f));
        Rodape(t, x0, x1, y1);
        Janela(t, 232, 150, 322, 300, 0.60f); Cortina(t, 232, 150, 322, 300, true);
        Janela(t, 638, 150, 728, 300, 0.60f); Cortina(t, 638, 150, 728, 300, false);
        t.Ret(392, 222, 568, 376, G(0.30f));
        t.Ret(384, 214, 576, 228, G(0.40f));
        t.Ret(424, 276, 536, 376, G(0.04f));
        t.Poli(new[] { P(424, 276), P(536, 276), P(528, 286), P(432, 286) }, G(0.16f));
        t.Elipse(480, 360, 40, 18, G(0.95f)); t.Elipse(470, 348, 16, 28, G(0.95f)); t.Elipse(492, 350, 12, 22, G(1f));
        t.Luz(480, 340, 190, 160, 0.80f);
        t.Luz(480, 440, 300, 110, 0.38f);
        Quadro(t, 424, 128, 536, 206);
        Tapete(t, new[] { P(340, 410), P(620, 410), P(720, 530), P(240, 530) }, 0.18f, 0.28f);
        Sofa(t, 130, 480, 190);
        Sofa(t, 830, 480, 190);
        Movel(t, 480, 466, 190, 34, 34, 0.15f, 0.26f);
        t.Ret(402, 466, 408, 486, G(0.10f)); t.Ret(552, 466, 558, 486, G(0.10f));
        t.Elipse(470, 430, 22, 6, G(0.45f)); t.Ret(464, 424, 476, 430, G(0.60f));
        Movel(t, 280, 410, 46, 52, 22, 0.16f, 0.27f); Abajur(t, 280, 354, 1.1f);
        Movel(t, 680, 410, 46, 52, 22, 0.16f, 0.27f); Abajur(t, 680, 354, 1.1f);
        Planta(t, 880, 396, 1.1f);
    }

    static void Cozinha(Tela t)
    {
        float x0 = 190, y0 = 95, x1 = 770, y1 = 375;
        Caixa(t, x0, y0, x1, y1, 0.36f, 0.17f, 0.26f, 0.29f);
        ChaoPadrao(t, x0, y1, x1, 0, 12, 10, 0.34f, 0.19f);
        for (int x = (int)x0; x < x1; x += 24) t.Ret(x, y0, x + 1, y1, G(0.10f), 0.30f);
        for (int y = (int)y0; y < y1; y += 24) t.Ret(x0, y, x1, y + 1, G(0.10f), 0.30f);
        t.Ret(204, 118, 396, 208, G(0.20f)); t.Ret(564, 118, 756, 208, G(0.20f));
        for (int i = 0; i < 4; i++)
        {
            float xa = 204 + i * 48, xb = 564 + i * 48;
            t.Ret(xa + 3, 122, xa + 45, 204, G(0.25f)); t.Ret(xb + 3, 122, xb + 45, 204, G(0.25f));
            t.Ret(xa + 36, 184, xa + 40, 198, G(0.55f)); t.Ret(xb + 6, 184, xb + 10, 198, G(0.55f));
        }
        t.Ret(204, 208, 396, 213, G(0.07f)); t.Ret(564, 208, 756, 213, G(0.07f));
        t.Poli(new[] { P(432, 112), P(528, 112), P(550, 190), P(410, 190) }, G(0.30f));
        t.Ret(410, 190, 550, 198, G(0.22f));
        t.Ret(468, 98, 492, 112, G(0.26f));
        t.Ret(196, 300, 764, 372, G(0.20f));
        t.Ret(196, 296, 764, 304, G(0.44f));
        for (int i = 0; i < 8; i++) { t.Ret(204 + i * 70, 310, 204 + i * 70 + 64, 368, G(0.24f)); t.Ret(204 + i * 70 + 40, 316, 204 + i * 70 + 44, 332, G(0.55f)); }
        t.Ret(414, 296, 546, 372, G(0.13f));
        t.Ret(424, 322, 536, 366, G(0.07f)); t.Elipse(480, 345, 40, 15, G(0.30f), 0.4f);
        for (int i = 0; i < 4; i++) t.Elipse(436 + i * 30, 292, 11, 3.5f, G(0.07f));
        for (int i = 0; i < 4; i++) t.Elipse(436 + i * 30, 301, 3, 3, G(0.5f));
        t.Ret(222, 289, 340, 300, G(0.10f)); t.Ret(222, 289, 340, 292, G(0.40f));
        t.Ret(282, 262, 286, 289, G(0.55f)); t.Ret(282, 262, 296, 266, G(0.55f));
        t.Luz(270, 300, 70, 30, 0.22f);
        t.Ret(590, 232, 740, 235, G(0.5f));
        for (int i = 0; i < 4; i++) { t.Ret(606 + i * 36, 235, 608 + i * 36, 246, G(0.5f)); t.Elipse(607 + i * 36, 262, 12, 15, G(0.12f + i * 0.02f)); }
        Sombra(t, 860, 504, 62, 12);
        t.Ret(808, 232, 912, 504, G(0.39f));
        t.Ret(808, 232, 812, 504, G(0.55f)); t.Ret(808, 346, 912, 351, G(0.10f));
        t.Ret(822, 262, 828, 330, G(0.20f)); t.Ret(822, 372, 828, 450, G(0.20f));
        t.Ret(808, 232, 912, 238, G(0.52f));
        Tapete(t, new[] { P(340, 416), P(620, 416), P(700, 526), P(260, 526) }, 0.20f, 0.30f);
        t.Ret(370, 458, 378, 508, G(0.12f)); t.Ret(582, 458, 590, 508, G(0.12f));
        Movel(t, 480, 462, 250, 14, 52, 0.20f, 0.36f);
        for (int i = 0; i < 2; i++)
        {
            float cx = 400 + i * 160;
            t.Ret(cx - 18, 440, cx + 18, 470, G(0.13f)); t.Ret(cx - 18, 440, cx + 18, 444, G(0.28f));
            t.Ret(cx - 15, 470, cx - 11, 500, G(0.09f)); t.Ret(cx + 11, 470, cx + 15, 500, G(0.09f));
        }
        t.Elipse(480, 436, 16, 4, G(0.60f)); t.Elipse(480, 432, 10, 5, G(0.35f));
        t.Ret(479, 0, 482, 214, G(0.20f));
        t.Poli(new[] { P(458, 214), P(504, 214), P(520, 238), P(442, 238) }, G(0.30f));
        t.Luz(480, 260, 190, 120, 0.55f);
    }

    static void Corredor(Tela t)
    {
        float x0 = 372, y0 = 168, x1 = 588, y1 = 356, L = t.L, A = t.A;
        Caixa(t, x0, y0, x1, y1, 0.27f, 0.14f, 0.19f, 0.25f);
        ChaoPadrao(t, x0, y1, x1, 1, 7, 12, 0.22f, 0.2f);
        t.Poli(new[] { P(432, y1), P(528, y1), P(690, A), P(270, A) }, G(0.10f));
        t.Poli(new[] { P(440, y1), P(520, y1), P(660, A), P(300, A) }, G(0.17f));
        t.Ret(424, 232, 536, y1 - 4, G(0.35f));
        t.Ret(430, 238, 530, y1 - 4, G(0.08f));
        t.Ret(436, 244, 478, 300, G(0.12f)); t.Ret(482, 244, 524, 300, G(0.12f));
        t.Elipse(520, 318, 3, 3, G(0.6f));
        t.Luz(480, y1 - 2, 90, 26, 0.45f);
        float[,] portas = { { 0.20f, 0.36f }, { 0.52f, 0.64f }, { 0.78f, 0.86f } };
        for (int lado = 0; lado < 2; lado++)
        {
            bool esq = lado == 0;
            for (int i = 0; i < 3; i++)
            {
                float s1 = portas[i, 0], s2 = portas[i, 1];
                t.Poli(QuadLat(esq, x0, y0, x1, y1, L, A, s1 - 0.012f, s2 + 0.012f, 0.0f, 0.74f), G(0.34f));
                t.Poli(QuadLat(esq, x0, y0, x1, y1, L, A, s1, s2, 0.0f, 0.71f), G(0.09f));
                t.Poli(QuadLat(esq, x0, y0, x1, y1, L, A, s1 + (s2 - s1) * 0.15f, s2 - (s2 - s1) * 0.15f, 0.40f, 0.64f), G(0.14f));
                t.Poli(QuadLat(esq, x0, y0, x1, y1, L, A, s1 + (s2 - s1) * 0.15f, s2 - (s2 - s1) * 0.15f, 0.08f, 0.32f), G(0.14f));
                Ponto kn = PL(esq, x0, y0, x1, y1, L, A, esq ? s2 - (s2 - s1) * 0.12f : s1 + (s2 - s1) * 0.12f, 0.34f);
                t.Elipse(kn.x, kn.y, 3.5f * (1 - s1 * 0.6f), 3.5f * (1 - s1 * 0.6f), G(0.65f));
            }
            float[,] qs = { { 0.38f, 0.48f }, { 0.66f, 0.74f } };
            for (int i = 0; i < 2; i++)
            {
                t.Poli(QuadLat(esq, x0, y0, x1, y1, L, A, qs[i, 0], qs[i, 1], 0.42f, 0.66f), G(0.34f));
                t.Poli(QuadLat(esq, x0, y0, x1, y1, L, A, qs[i, 0] + 0.008f, qs[i, 1] - 0.008f, 0.45f, 0.63f), G(0.14f));
            }
            Ponto a1 = PL(esq, x0, y0, x1, y1, L, A, 0.44f, 0.72f);
            Ponto a2 = PL(esq, x0, y0, x1, y1, L, A, 0.70f, 0.72f);
            t.Elipse(a1.x, a1.y, 4, 7, G(1f)); t.Luz(a1.x, a1.y, 100, 90, 0.45f);
            t.Elipse(a2.x, a2.y, 3, 5, G(1f)); t.Luz(a2.x, a2.y, 60, 50, 0.35f);
        }
        t.Poli(QuadLat(true, x0, y0, x1, y1, L, A, 0f, 1f, 0f, 0.05f), G(0.34f));
        t.Poli(QuadLat(false, x0, y0, x1, y1, L, A, 0f, 1f, 0f, 0.05f), G(0.30f));
        t.Luz(480, 40, 380, 140, 0.30f);
        t.Luz(480, 300, 220, 90, 0.10f);
        Movel(t, 560, 358, 44, 26, 12, 0.14f, 0.26f);
        Vaso(t, 560, 332, 0.7f);
    }

    static void Quarto(Tela t)
    {
        float x0 = 190, y0 = 95, x1 = 770, y1 = 375, L = t.L, A = t.A;
        Caixa(t, x0, y0, x1, y1, 0.29f, 0.15f, 0.21f, 0.24f);
        ChaoPadrao(t, x0, y1, x1, 1, 11, 9, 0.24f, 0.2f);
        for (int x = (int)x0 + 14; x < x1; x += 28) t.Ret(x, y0 + 10, x + 7, y1 - 10, G(0.34f), 0.22f);
        t.Ret(x0, y0, x1, y0 + 9, G(0.40f));
        Rodape(t, x0, x1, y1);
        Ponto[] jan = QuadLat(false, x0, y0, x1, y1, L, A, 0.30f, 0.68f, 0.38f, 0.82f);
        t.Poli(new[] { P(jan[0].x - 5, jan[0].y - 5), P(jan[1].x + 4, jan[1].y - 3), P(jan[2].x + 4, jan[2].y + 4), P(jan[3].x - 5, jan[3].y + 4) }, G(0.34f));
        t.Poli(jan, G(0.62f));
        Ponto m1 = PL(false, x0, y0, x1, y1, L, A, 0.49f, 0.82f), m2 = PL(false, x0, y0, x1, y1, L, A, 0.49f, 0.38f);
        t.Ret(m1.x - 2, m1.y, m1.x + 2, m2.y, G(0.12f));
        t.Luz(jan[0].x + 40, jan[0].y + 50, 190, 160, 0.38f);
        t.Poli(new[] { P(jan[3].x, jan[3].y), P(jan[2].x, jan[2].y), P(640, 470), P(540, 440) }, G(1f), 0.07f);
        Sombra(t, 120, 520, 74, 14);
        t.Ret(62, 250, 178, 520, G(0.15f));
        t.Ret(62, 250, 178, 258, G(0.30f));
        t.Ret(118, 262, 122, 516, G(0.05f));
        t.Ret(72, 272, 112, 500, G(0.19f)); t.Ret(128, 272, 168, 500, G(0.19f));
        t.Ret(108, 380, 112, 400, G(0.55f)); t.Ret(124, 380, 128, 400, G(0.55f));
        Quadro(t, 366, 124, 444, 184);
        Quadro(t, 470, 124, 548, 184);
        t.Ret(318, 196, 642, 340, G(0.14f));
        t.Ret(318, 196, 642, 204, G(0.28f));
        for (int i = 0; i < 5; i++) t.Ret(332 + i * 62, 214, 332 + i * 62 + 52, 330, G(0.11f));
        Tapete(t, new[] { P(300, 416), P(660, 416), P(760, 534), P(200, 534) }, 0.15f, 0.25f);
        Sombra(t, 480, 424, 200, 20);
        t.Ret(324, 300, 636, 430, G(0.11f));
        t.RetGrad(328, 288, 632, 410, G(0.40f), G(0.26f));
        t.Poli(new[] { P(328, 288), P(632, 288), P(618, 272), P(342, 272) }, G(0.34f));
        t.Elipse(400, 288, 44, 18, G(0.62f)); t.Elipse(560, 288, 44, 18, G(0.62f));
        t.Elipse(398, 283, 34, 6, G(0.80f), 0.5f); t.Elipse(558, 283, 34, 6, G(0.80f), 0.5f);
        t.Poli(new[] { P(328, 340), P(632, 340), P(632, 372), P(328, 372) }, G(0.18f));
        t.Ret(328, 340, 632, 344, G(0.45f));
        t.Ret(328, 410, 632, 430, G(0.08f));
        Movel(t, 268, 380, 56, 66, 22, 0.15f, 0.27f); Abajur(t, 268, 312, 1.1f);
        t.Ret(246, 340, 290, 344, G(0.06f)); t.Ret(262, 352, 274, 356, G(0.55f));
        Movel(t, 692, 380, 56, 66, 22, 0.15f, 0.27f); Abajur(t, 692, 312, 1.1f);
        t.Ret(670, 340, 714, 344, G(0.06f)); t.Ret(686, 352, 698, 356, G(0.55f));
        t.Elipse(480, 470, 42, 12, G(0.20f)); t.Ret(438, 452, 522, 470, G(0.17f)); t.Elipse(480, 452, 42, 11, G(0.28f));
        t.Luz(480, 150, 300, 100, 0.12f);
    }

    static void Jardim(Tela t)
    {
        float L = t.L, A = t.A;
        Random r = new Random(7);
        t.RetGrad(0, 0, L, 330, G(0.10f), G(0.34f));
        for (int i = 0; i < 90; i++) { float sx = (float)r.NextDouble() * L, sy = (float)r.NextDouble() * 220; t.Elipse(sx, sy, 1.2f, 1.2f, G(0.55f + (float)r.NextDouble() * 0.4f), 0.8f); }
        t.Luz(720, 96, 260, 220, 0.55f);
        t.Elipse(720, 96, 40, 40, G(0.98f));
        t.Elipse(708, 88, 12, 10, G(0.80f), 0.5f); t.Elipse(734, 106, 8, 7, G(0.82f), 0.5f);
        t.Elipse(250, 130, 160, 11, G(0.30f), 0.35f); t.Elipse(420, 170, 210, 9, G(0.28f), 0.30f);
        t.Poli(new[] { P(0, 330), P(0, 250), P(120, 232), P(260, 258), P(420, 236), P(600, 262), P(780, 234), P(960, 255), P(960, 330) }, G(0.11f));
        t.Ret(30, 186, 250, 336, G(0.13f));
        t.Poli(new[] { P(18, 188), P(140, 120), P(262, 188) }, G(0.10f));
        t.Ret(72, 230, 108, 276, G(0.80f)); t.Ret(72, 252, 108, 254, G(0.15f)); t.Ret(88, 230, 90, 276, G(0.15f));
        t.Luz(90, 252, 90, 80, 0.30f);
        t.Ret(168, 230, 204, 276, G(0.14f));
        t.RetGrad(0, 330, L, A, G(0.15f), G(0.27f));
        for (int i = 0; i < 1400; i++)
        {
            float gx = (float)r.NextDouble() * L, gy = 332 + (float)r.NextDouble() * (A - 332);
            float k = (gy - 330) / (A - 330);
            t.Ret(gx, gy - 3 - 6 * k, gx + 1.5f, gy, G(0.07f + (float)r.NextDouble() * 0.09f), 0.8f);
        }
        t.Poli(new[] { P(462, 334), P(500, 334), P(660, A), P(300, A) }, G(0.31f));
        for (int i = 0; i < 9; i++)
        {
            float p0 = i / 9f, p1 = (i + 1) / 9f - 0.02f;
            float ya = Lerp(334, A, p0 * p0 * 0.9f + p0 * 0.1f), yb = Lerp(334, A, p1 * p1 * 0.9f + p1 * 0.1f);
            float xa1 = Lerp(462, 300, (ya - 334) / (A - 334)), xa2 = Lerp(500, 660, (ya - 334) / (A - 334));
            float xb1 = Lerp(462, 300, (yb - 334) / (A - 334)), xb2 = Lerp(500, 660, (yb - 334) / (A - 334));
            t.Poli(new[] { P(xa1 + 3, ya), P(xa2 - 3, ya), P(xb2 - 3, yb), P(xb1 + 3, yb) }, G(0.24f + Hash(i) * 0.10f));
        }
        t.Ret(0, 292, L, 298, G(0.14f)); t.Ret(0, 322, L, 328, G(0.14f));
        for (int x = 0; x < L; x += 22)
        {
            t.Poli(new[] { P(x, 256), P(x + 7, 246), P(x + 14, 256), P(x + 14, 336), P(x, 336) }, G(0.19f));
            t.Ret(x, 256, x + 2, 336, G(0.34f), 0.6f);
        }
        t.Ret(0, 334, L, 342, G(0.04f), 0.5f);
        t.Ret(440, 236, 520, 340, G(0.10f));
        t.Ret(436, 232, 444, 340, G(0.30f)); t.Ret(516, 232, 524, 340, G(0.30f));
        for (int x = 446; x < 516; x += 10) t.Ret(x, 244, x + 3, 338, G(0.28f));
        for (int i = 0; i < 12; i++)
        {
            float bx = 30 + i * 80 + Hash(i) * 30;
            if (bx > 400 && bx < 560) continue;
            t.Elipse(bx, 342, 42, 28, G(0.07f + Hash(i + 3) * 0.04f));
            t.Elipse(bx - 10, 330, 16, 8, G(0.20f), 0.35f);
        }
        float[] ax = { 838, 78 };
        for (int k = 0; k < 2; k++)
        {
            float cx = ax[k], bw = k == 0 ? 1f : 0.85f;
            t.Poli(new[] { P(cx - 22 * bw, A), P(cx - 10 * bw, 250), P(cx + 10 * bw, 250), P(cx + 24 * bw, A) }, G(0.05f));
            t.Ret(cx - 6 * bw, 270, cx - 2 * bw, A, G(0.17f), 0.5f);
            for (int i = 0; i < 12; i++)
            {
                float ang = i * 0.55f;
                float ex = cx + (float)Math.Cos(ang) * (60 + Hash(i + k) * 36) * bw, ey = 150 + (float)Math.Sin(ang) * (50 + Hash(i * 2 + k) * 28);
                t.Elipse(ex, ey, 56 * bw, 42 * bw, G(0.045f + Hash(i + 11) * 0.03f));
            }
            t.Elipse(cx - 30, 118, 70 * bw, 22 * bw, G(0.30f), 0.18f);
        }
        t.Ret(618, 270, 622, 420, G(0.05f)); t.Elipse(620, 262, 11, 14, G(1f)); t.Luz(620, 262, 200, 170, 0.70f);
        t.Luz(620, 420, 150, 40, 0.18f);
        for (int fy = 290; fy < 360; fy++)
        {
            float fa = 0.22f * (float)Math.Sin((fy - 290) / 70f * Math.PI);
            t.Ret(0, fy, L, fy + 1, G(0.42f), fa);
        }
    }

    // ------------------------------------------------------------ acabamento

    static void Desfocar(Tela t, int passos)
    {
        int L = t.L, A = t.A;
        Cor[] tmp = new Cor[t.px.Length];
        for (int p = 0; p < passos; p++)
        {
            for (int y = 0; y < A; y++)
                for (int x = 0; x < L; x++)
                {
                    Cor a = t.px[y * L + Math.Max(0, x - 1)], b = t.px[y * L + x], c = t.px[y * L + Math.Min(L - 1, x + 1)];
                    tmp[y * L + x] = new Cor((a.r + b.r + c.r) / 3f, (a.g + b.g + c.g) / 3f, (a.b + b.b + c.b) / 3f);
                }
            for (int y = 0; y < A; y++)
                for (int x = 0; x < L; x++)
                {
                    Cor a = tmp[Math.Max(0, y - 1) * L + x], b = tmp[y * L + x], c = tmp[Math.Min(A - 1, y + 1) * L + x];
                    t.px[y * L + x] = new Cor((a.r + b.r + c.r) / 3f, (a.g + b.g + c.g) / 3f, (a.b + b.b + c.b) / 3f);
                }
        }
    }

    // Suaviza, escurece as bordas e aplica o tom verde de câmera noturna. O grão é fixo (não anima).
    static void Acabamento(Tela t, int semente)
    {
        int L = t.L, A = t.A;
        Desfocar(t, 1);
        Random rnd = new Random(100 + semente);
        for (int y = 0; y < A; y++)
            for (int x = 0; x < L; x++)
            {
                Cor c = t.px[y * L + x];
                float dx = (x - L / 2f) / (L / 2f), dy = (y - A / 2f) / (A / 2f);
                float vig = 1f - 0.50f * (dx * dx * 0.7f + dy * dy * 0.9f);
                if (vig < 0.30f) vig = 0.30f;
                float lum = (c.r + c.g + c.b) / 3f;
                lum = (float)Math.Pow(Math.Max(0f, Math.Min(1f, lum)), 0.92f) * vig * 1.12f;
                lum += ((float)rnd.NextDouble() - 0.5f) * 0.025f;
                if ((y % 3) == 0) lum *= 0.93f;
                lum = Math.Max(0f, Math.Min(1f, lum));
                t.px[y * L + x] = new Cor(lum * 0.80f, lum * 1.00f, lum * 0.86f);
            }
    }
}
