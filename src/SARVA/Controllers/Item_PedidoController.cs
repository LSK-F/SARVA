// Item_PedidoController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARVA.Data;
using SARVA.Models;
using System.Threading.Tasks;

namespace SARVA.Controllers
{
    [Authorize]
    public class Item_PedidoController : BaseController
    {
        private readonly SARVADbContext _db;

        public Item_PedidoController(SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _db = db;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarItem_Pedido(int codigoIP, int idCiclo, int idVenda, int id_Pedido)
        {
            var itemVenda = await _db.Item_Venda.FirstOrDefaultAsync(x => x.id_ciclo_produto == idCiclo && x.codigo_produto == codigoIP && x.id_venda == idVenda);
            if (itemVenda == null) return NotFound("Item da venda original não encontrado.");

            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.id == id_Pedido);
            if (pedido == null) return NotFound("Pedido não encontrado.");

            var itemPedido = new Item_Pedido
            {
                codigo_IV = codigoIP,
                id_ciclo_IV = idCiclo,
                id_venda_IV = idVenda,
                id_pedido = id_Pedido,
                valor = itemVenda.valor
            };

            _db.Item_Pedido.Add(itemPedido);
            await _db.SaveChangesAsync();

            return RedirectToAction("CatalogoItemVenda", "Item_Venda", new { idPedido = id_Pedido, idEmpresa = pedido.id_empresa });
        }
    }
}