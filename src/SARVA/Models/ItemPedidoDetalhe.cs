namespace SARVA.Models
{
    public class ItemPedidoDetalhe
    {
        public int VendaId { get; set; }
        public string? NomeCliente { get; set; }
        public string? NomeProduto { get; set; }
        public string? NomeCiclo { get; set; }
        public decimal ValorCalculado { get; set; }
        public int Quantidade { get; set; }
    }
}
