namespace PizzaTycoon.Monetization
{
    // IDs dos produtos IAP — devem coincidir com os IDs cadastrados nas lojas
    public static class IAPProducts
    {
        // ── Consumíveis (podem ser comprados múltiplas vezes) ────────────────
        public const string COINS_SMALL  = "coins_500";    // $0.99 — 500 moedas
        public const string COINS_MEDIUM = "coins_2500";   // $4.99 — 2500 moedas
        public const string COINS_LARGE  = "coins_10000";  // $19.99 — 10000 moedas

        // ── Não-consumíveis (compra única, permanente) ───────────────────────
        public const string REMOVE_ADS   = "remove_ads";   // $2.99
        public const string STARTER_PACK = "starter_pack"; // $1.99 — 2000 moedas + sem ads 7 dias

        // ── Assinatura ───────────────────────────────────────────────────────
        public const string VIP_WEEKLY   = "vip_weekly";   // $1.99/semana
        // VIP: 2x ganhos permanente + sem banner ads + skin exclusiva

        // ── Array de todos os IDs (para inicialização do SDK) ───────────────
        public static readonly string[] All =
        {
            COINS_SMALL, COINS_MEDIUM, COINS_LARGE,
            REMOVE_ADS, STARTER_PACK, VIP_WEEKLY
        };

        // ── Valores em coins de cada produto consumível ──────────────────────
        public static int GetCoinsForProduct(string productId) => productId switch
        {
            COINS_SMALL  => 500,
            COINS_MEDIUM => 2500,
            COINS_LARGE  => 10000,
            STARTER_PACK => 2000,
            _            => 0
        };

        // ── Preço de exibição (apenas referência — preço real vem da loja) ───
        public static string GetDisplayPrice(string productId) => productId switch
        {
            COINS_SMALL  => "$0,99",
            COINS_MEDIUM => "$4,99",
            COINS_LARGE  => "$19,99",
            REMOVE_ADS   => "$2,99",
            STARTER_PACK => "$1,99",
            VIP_WEEKLY   => "$1,99/sem",
            _            => "—"
        };
    }
}
