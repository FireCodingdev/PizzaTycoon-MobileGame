namespace PizzaTycoon.Items
{
    // Define todos os tipos de itens manipuláveis no jogo
    // A ordem reflete o fluxo de produção: Trigo → Massa → PizzaCrua → PizzaPronta
    public enum ItemType
    {
        None = 0,
        Wheat = 1,          // Trigo coletado do campo
        Dough = 2,          // Massa feita do trigo
        RawPizza = 3,       // Pizza montada (ainda crua)
        CookedPizza = 4,    // Pizza assada pronta para entrega
        PackagedPizza = 5,  // Pizza embalada pela PackagingStation
        Tomato = 6,         // Ingrediente extra — Receita Margherita
        Cheese = 7,         // Ingrediente extra — Receita Quatro Queijos
    }
}
