namespace SARVA.Models;
public class FinalizarVendaViewModel
{
    public Venda? Venda { get; set; }
    public IEnumerable<Item_Venda>? ItensVenda { get; set; }
    public decimal ValorTotal { get; set; }
    public int ScoreCliente { get; set; }
}