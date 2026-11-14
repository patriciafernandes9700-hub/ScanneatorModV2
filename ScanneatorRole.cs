using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace ScanneatorMod
{
    [BepInPlugin("com.usuario.scanneator", "Scanneator Mod", "1.0.0")]
    public class ScanneatorPlugin : BasePlugin
    {
        public static Sprite ScanearSprite;

        public override void Load()
        {
            ScanearSprite = LoadSprite("ScanneatorMod.ScanearButton.png");
            var harmony = new Harmony("com.usuario.scanneator");
            harmony.PatchAll();
        }

        public static void ExecutarScan(PlayerControl jogadorAlvo)
        {
            if (jogadorAlvo == null || jogadorAlvo.Data == null) return;

            string nomeAlvo = jogadorAlvo.Data.PlayerName;
            string nomeFuncao = jogadorAlvo.Data.Role.NiceName;
            string equipe = "Tripulante";

            if (jogadorAlvo.Data.Role.IsImpostor)
            {
                equipe = "IMPOSTOR";
            }
            else if (nomeFuncao.Contains("Jester") || nomeFuncao.Contains("Arsonist") || nomeFuncao.Contains("Executioner"))
            {
                equipe = "NEUTRO";
            }

            string mensagem = $"<color=#00FF00>[SCANNER]</color> {nomeAlvo} é <b>{nomeFuncao}</b> ({equipe})!";
            HudManager.Instance.Notifier.AddItem(mensagem);
        }

        private static Sprite LoadSprite(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            ImageConversion.LoadImage(texture, buffer, false);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudUpdatePatch
    {
        private static ActionButton scanButton;
        private static float cooldown = 0f;

        public static void Postfix(HudManager __instance)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

            if (scanButton == null)
            {
                scanButton = Object.Instantiate(__instance.UseButton, __instance.UseButton.transform.parent);
                scanButton.graphic.sprite = ScanneatorPlugin.ScanearSprite;
                scanButton.OverrideText("SCANEAR");
                scanButton.transform.localPosition += new Vector3(-1.5f, 0f, 0f);
            }

            bool estaVivo = !PlayerControl.LocalPlayer.Data.IsDead;
            scanButton.gameObject.SetActive(estaVivo);

            if (!estaVivo) return;

            if (cooldown > 0f)
            {
                cooldown -= Time.deltaTime;
                scanButton.SetCoolDown(cooldown, 25f);
                return;
            }

            PlayerControl alvoProximo = EncontrarAlvoProximo();
            if (alvoProximo != null)
            {
                scanButton.SetEnabled();
            }
            else
            {
                scanButton.SetDisabled();
            }

            if (alvoProximo != null && Input.GetMouseButtonDown(0))
            {
                Vector2 toquePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (scanButton.GetComponent<Collider2D>().OverlapPoint(toquePos))
                {
                    ScanneatorPlugin.ExecutarScan(alvoProximo);
                    cooldown = 25f;
                }
            }
        }

        private static PlayerControl EncontrarAlvoProximo()
        {
            PlayerControl maisProximo = null;
            float menorDistancia = 1.8f;

            foreach (var jogador in PlayerControl.AllPlayerControls)
            {
                if (jogador == PlayerControl.LocalPlayer || jogador.Data.IsDead) continue;

                float distancia = Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), jogador.GetTruePosition());
                if (distancia < menorDistancia)
                {
                    menorDistancia = distancia;
                    maisProximo = jogador;
                }
            }
            return maisProximo;
        }
    }
}
