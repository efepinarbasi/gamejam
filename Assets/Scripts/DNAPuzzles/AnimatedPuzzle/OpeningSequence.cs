using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    public partial class DNAAnimatedPuzzleController
    {
        private readonly Dictionary<int, EditableGeneStrand> openingSequences = new Dictionary<int, EditableGeneStrand>();
        private int openingGap = -1;
        private int openingCreature = -1;

        private EditableGeneStrand OpeningSequence()
        {
            if (!openingSequences.TryGetValue(currentJigsawCreature, out var strand))
            {
                // P genes have ten pairs in the design document. Keep the original
                // genotype intact: this stage restores bases, not planetary traits.
                var creature = LacunaGeneticsDatabase.GetCreatures()[currentJigsawCreature];
                var gene = LacunaGeneticsDatabase.GetGeneByName(creature.geneP);
                strand = new EditableGeneStrand(gene, new List<LacunaGap>
                {
                    new LacunaGap(LacunaSlotType.P, 0, true, false),
                    new LacunaGap(LacunaSlotType.P, 2, true, false),
                    new LacunaGap(LacunaSlotType.P, 4, false, true),
                    new LacunaGap(LacunaSlotType.P, 6, true, false),
                    new LacunaGap(LacunaSlotType.P, 9, true, false)
                });
                openingSequences.Add(currentJigsawCreature, strand);
            }
            if (openingCreature != currentJigsawCreature)
            {
                CancelTabletDrag();
                openingCreature = currentJigsawCreature;
                openingGap = -1;
                jigsawMessage = "";
            }
            return strand;
        }

        private Vector2 OpeningHelixPoint(float x, bool top)
        {
            float angle = x * (2f * Mathf.PI / 440f) + helixPhase;
            float side = top ? 1f : -1f;
            return new Vector2(x + side * 20f * Mathf.Sin(angle), 235f - side * 70f * Mathf.Cos(angle));
        }

        private float OpeningDepth(int index, bool top)
        {
            float angle = (91 + index * 82) * (2f * Mathf.PI / 440f) + helixPhase;
            return Mathf.Sin(angle) * (top ? -1f : 1f);
        }

        private Rect OpeningBaseRect(int index, bool top)
        {
            Vector2 center = OpeningHelixPoint(91 + index * 82, top);
            float scale = Mathf.Lerp(.86f, 1.04f, (OpeningDepth(index, top) + 1f) * .5f);
            Vector2 size = new Vector2(54, 64) * scale;
            return new Rect(center - size * .5f, size);
        }

        private void DrawOpeningBackbones()
        {
            if (Event.current.type != EventType.Repaint) return;
            for (int strand = 0; strand < 2; strand++)
            {
                bool top = strand == 0;
                Color color = top ? new Color(.2f, .9f, .8f) : new Color(.62f, .58f, 1f);
                for (float x = 57; x < 863; x += 6)
                {
                    Vector2 a = OpeningHelixPoint(x, top);
                    Vector2 b = OpeningHelixPoint(Mathf.Min(x + 6, 863), top);
                    Vector2 delta = b - a;
                    var matrix = GUI.matrix;
                    GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, a);
                    DrawSolidRect(new Rect(a.x, a.y - 4, delta.magnitude + 1, 8), new Color(color.r, color.g, color.b, .12f));
                    DrawSolidRect(new Rect(a.x, a.y - 1.8f, delta.magnitude + 1, 3.6f), color);
                    GUI.matrix = matrix;
                }
            }
        }

        private void DrawOpeningSequence(bool interactive)
        {
            var strand = OpeningSequence();
            int remaining = strand.GetMissingCount();
            var accent = new Color(.39f, .91f, .83f);
            var panel = new Color(.07f, .14f, .21f);
            GUI.Label(new Rect(30, 14, 530, 32), "DNA DİZİLEME  /  10 BAZ ÇİFTİ", EditorBold(24));
            GUI.Label(new Rect(32, 49, 690, 26), "Eşleşme kuralı:  A ↔ T    C ↔ G     •     Beş eksik parçayı tamamla", EditorLabel(16));
            if (interactive && TabletButton(new Rect(794, 18, 104, 38), "Odaya dön", panel, 14))
            {
                StartTabletExit();
                return;
            }
            DrawSolidRect(new Rect(32, 86, 856, 6), panel);
            DrawSolidRect(new Rect(32, 86, 856 * (5 - remaining) / 5f, 6), accent);
            GUI.Label(new Rect(32, 101, 600, 24), $"{strand.originalGeneName}  •  Üst ve alt zinciri eşleştir", EditorLabel(15));
            GUI.Label(new Rect(690, 99, 198, 27), $"{5 - remaining} / 5 tamamlandı", CenteredBold(17));

            if (openingGap < 0 || strand.top[openingGap] != LacunaShape.None && strand.bottom[openingGap] != LacunaShape.None)
            {
                openingGap = -1;
                for (int i = 0; i < 10; i++)
                    if (strand.top[i] == LacunaShape.None || strand.bottom[i] == LacunaShape.None) { openingGap = i; break; }
            }
            // Hovering a gap while dragging chooses that gap; a snap keeps its target.
            if (interactive && (pieceMotion == TabletPieceMotion.Idle || pieceMotion == TabletPieceMotion.Held))
            {
                for (int i = 0; i < 10; i++)
                {
                    bool missingTop = strand.top[i] == LacunaShape.None;
                    if (!missingTop && strand.bottom[i] != LacunaShape.None) continue;
                    if (OpeningBaseRect(i, missingTop).Contains(Event.current.mousePosition)) openingGap = i;
                }
            }
            DrawOpeningBackbones();
            for (int i = 0; i < 10; i++)
                DrawHelixRung(OpeningBaseRect(i, true).center, OpeningBaseRect(i, false).center);
            // Back bases first; front bases and question marks remain legible at crossings.
            for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < 10; i++)
            for (int side = 0; side < 2; side++)
            {
                bool top = side == 0;
                var shape = top ? strand.top[i] : strand.bottom[i];
                if (shape == LacunaShape.None) continue;
                float depth = OpeningDepth(i, top);
                if ((depth < 0 ? 0 : 1) != pass) continue;
                Color previous = GUI.color;
                float shade = Mathf.Lerp(.65f, 1f, (depth + 1f) * .5f);
                GUI.color = new Color(previous.r * shade, previous.g * shade, previous.b * shade, previous.a);
                DrawOpeningBase(OpeningBaseRect(i, top), shape, top, false, accent);
                GUI.color = previous;
            }
            for (int i = 0; i < 10; i++)
            {
                if (strand.top[i] == LacunaShape.None)
                    DrawOpeningBase(OpeningBaseRect(i, true), LacunaShape.None, true, openingGap == i, accent);
                if (strand.bottom[i] == LacunaShape.None)
                    DrawOpeningBase(OpeningBaseRect(i, false), LacunaShape.None, false, openingGap == i, accent);
            }
            if (remaining == 0)
            {
                GUI.Label(new Rect(32, 344, 856, 40), "TAMAMLANDI — 10 çiftin tamamı doğru eşleşiyor!", CenteredBold(23));
                if (interactive && TabletButton(new Rect(270, 410, 380, 60), "GEN ANALİZİNE DEVAM ET  ›", new Color(.08f, .40f, .33f), 20))
                {
                    CancelTabletDrag();
                    jigsawMessage = "";
                    tabletScreenMode = TabletScreenMode.Home;
                }
                return;
            }
            bool topGap = strand.top[openingGap] == LacunaShape.None;
            var socket = OpeningBaseRect(openingGap, topGap);
            var partner = topGap ? strand.bottom[openingGap] : strand.top[openingGap];
            DrawTabletTray(socket, partner, topGap);
            GUI.Label(new Rect(32, 510, 856, 22), "DNA döner; parça tuttuğunda durur. A, T, C ve G parçalarını tekrar kullanabilirsin.", CenteredLabel(13));
        }

        private void DrawOpeningBase(Rect rect, LacunaShape shape, bool top, bool selected, Color accent)
        {
            if (shape != LacunaShape.None)
            {
                DrawMovingBase(rect, shape, top, false, false, 28);
                return;
            }
            DrawSolidRect(rect, new Color(.055f, .11f, .18f));
            DrawBorder(rect, selected ? accent : new Color(.5f, .65f, .75f), selected ? 2.5f : 1.5f);
            GUI.Label(rect, "?", CenteredBold(32));
        }

        private void AnswerOpeningGap(LacunaShape answer)
        {
            var strand = OpeningSequence();
            if (openingGap < 0 || openingGap >= 10) return;
            bool top = strand.top[openingGap] == LacunaShape.None;
            if (!top && strand.bottom[openingGap] != LacunaShape.None) return;
            var partner = top ? strand.bottom[openingGap] : strand.top[openingGap];
            if (!LacunaGeneticsDatabase.ArePartners(answer, partner))
            {
                jigsawMessage = "Bu parça eşleşmedi. A–T ve C–G kuralını kontrol et.";
                audioManager?.PlayMismatch();
                return;
            }
            if (top) strand.top[openingGap] = answer;
            else strand.bottom[openingGap] = answer;
            audioManager?.PlaySnap();
            jigsawMessage = "Doğru eşleşme! Sıradaki boşluğu seçebilirsin.";
            if (!strand.HasAnyMissing()) audioManager?.PlayVictory();
        }
    }
}
