using System.Collections.Generic;

namespace SARVA.Models;
public class CatalogoPageViewModel
{
    public List<CatalogoItemVendaViewModel>? ItensDoCatalogo { get; set; }

    // Propriedades para os filtros, para manter os valores na tela
    public int IdPedido { get; set; }
    public int IdEmpresa { get; set; }
    public int? NumVenda { get; set; }
    public string? NomeCliente { get; set; }
}